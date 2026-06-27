using System.Text.RegularExpressions;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Npcs;

public partial class QuickDraftNpcHandler(IAppDbContext db) : ICommandHandler<QuickDraftNpcCommand, NpcDetailDto>
{
    public async Task<NpcDetailDto> Handle(QuickDraftNpcCommand command, CancellationToken ct = default)
    {
        var r = command.Request;
        var npc = NpcDraftGenerator.Generate(command.UserId,
            new NpcDraftRequest(r.System, r.Kind, r.Role, r.PowerLevel, r.PrimaryCharacteristic, r.CombatStyle, r.Name));

        // Снаряжение и навыки подбираем из каталога системы: оружие+броня по уровню силы,
        // вторичные навыки — чтобы у черновика сразу были пулы кубов, видимый доспех и
        // согласованное с ним поглощение/защита.
        var skills = await db.SkillDefs.AsNoTracking()
            .Where(s => s.System == r.System && (s.OwnerUserId == null || s.OwnerUserId == command.UserId))
            .ToListAsync(ct);
        var items = await db.ItemDefs.AsNoTracking()
            .Include(i => i.Qualities).ThenInclude(q => q.QualityDef)
            .Where(i => i.System == r.System && (i.Kind == ItemKind.Weapon || i.Kind == ItemKind.Armor)
                && (i.OwnerUserId == null || i.OwnerUserId == command.UserId))
            .ToListAsync(ct);
        ApplyCatalogLoadout(npc, r, skills, items);

        NpcValidator.Validate(npc);

        db.Npcs.Add(npc);
        await db.SaveChangesAsync(ct);
        return NpcMapper.ToDetail(npc, command.UserId);
    }

    /// <summary>
    /// Заменяет сгенерированные свободным текстом основной навык и оружие на записи каталога,
    /// добавляет броню и вторичные навыки по уровню силы и учитывает броню в Soak/защите.
    /// </summary>
    private static void ApplyCatalogLoadout(
        Npc npc, QuickDraftRequest r, IReadOnlyList<SkillDef> skills, IReadOnlyList<ItemDef> items)
    {
        if (npc.Skills.Count == 0) return;
        var level = (int)r.PowerLevel; // 0..3
        var weapons = items.Where(i => i.Kind == ItemKind.Weapon).ToList();
        var armors = items.Where(i => i.Kind == ItemKind.Armor).ToList();

        // ── Основной навык + оружие (навык оружия = навык NPC, чтобы пул совпадал с атакой) ──
        var weapon = r.CombatStyle switch
        {
            NpcCombatStyle.Melee => PickWeapon(weapons, "Melee", "Brawl"),
            NpcCombatStyle.Ranged => PickWeapon(weapons, "Ranged", "Gunnery"),
            _ => null,
        };
        var primary = weapon != null
            ? ResolveSkill(skills, weapon.SkillName)
            : r.CombatStyle switch
            {
                NpcCombatStyle.Magic => FirstByKind(skills, SkillKind.Magic),
                NpcCombatStyle.Social => FirstByKind(skills, SkillKind.Social),
                NpcCombatStyle.Melee => FirstCombat(skills, CharacteristicType.Brawn),
                NpcCombatStyle.Ranged => FirstCombat(skills, CharacteristicType.Agility),
                _ => null,
            };
        if (primary != null)
            npc.Skills[0].Name = Label(primary);

        // ── Броня по уровню силы (целевой бонус поглощения ≈ уровень) ──
        var armor = level == 0 ? null : PickArmor(armors, targetSoak: level);

        // Оружие → структурная атака (skill/damage/crit/range/качества из каталога); броня остаётся
        // небоевым снаряжением. При пустом каталоге оставляем сгенерированное (офлайн-фолбэк).
        if (weapon != null)
        {
            npc.Attacks.Clear();
            npc.Attacks.Add(AttackFromWeapon(weapon));
            npc.Equipment.Clear();
        }
        if (armor != null)
        {
            if (weapon == null) npc.Equipment.Clear();
            npc.Equipment.Add(Label(armor));
        }

        // Доспех увеличивает поглощение и защиту (у NPC это хранимые поля).
        if (armor != null)
        {
            npc.Soak += armor.SoakBonus;
            npc.MeleeDefense += armor.MeleeDefense;
            npc.RangedDefense += armor.RangedDefense;
        }

        // ── Вторичные навыки по уровню силы (weak 0 … elite 3), из каталога ──
        AddSecondarySkills(npc, r.Role, skills, count: level);
    }

    /// <summary>Добавляет до <paramref name="count"/> вторичных навыков по характеристикам роли, без дублей.</summary>
    private static void AddSecondarySkills(Npc npc, NpcRole role, IReadOnlyList<SkillDef> skills, int count)
    {
        if (count <= 0 || skills.Count == 0) return;
        var taken = npc.Skills.Select(s => s.Name).ToHashSet();

        // Приоритет: навыки вторичной, затем основной характеристики роли (исключая боевые —
        // основной боевой уже есть). Детерминированно по имени.
        var chars = new[] { NpcDraftGenerator.SecondaryOf(role), NpcDraftGenerator.PrimaryOf(role) };
        var candidates = chars
            .SelectMany(ch => skills.Where(s => s.Characteristic == ch && s.Kind != SkillKind.Combat)
                .OrderBy(s => s.Name))
            .Concat(skills.Where(s => s.Kind != SkillKind.Combat).OrderBy(s => s.Name)) // добор любым небоевым
            .Select(Label)
            .Distinct();

        foreach (var name in candidates)
        {
            if (count <= 0) break;
            if (!taken.Add(name)) continue;
            npc.Skills.Add(new NpcSkill { NpcId = npc.Id, Name = name, Ranks = 1 });
            count--;
        }
    }

    /// <summary>Первое оружие, чей боевой навык (без скобочного уточнения) входит в перечень баз.</summary>
    private static ItemDef? PickWeapon(IReadOnlyList<ItemDef> weapons, params string[] skillBases) =>
        weapons.Where(w => skillBases.Contains(BaseName(w.SkillName), StringComparer.OrdinalIgnoreCase))
            .OrderBy(w => w.Name).FirstOrDefault();

    /// <summary>Броня с бонусом поглощения, ближайшим к целевому (но не меньше 1); тай-брейк — дешевле, затем имя.</summary>
    private static ItemDef? PickArmor(IReadOnlyList<ItemDef> armors, int targetSoak) =>
        armors.Where(a => a.SoakBonus >= 1)
            .OrderBy(a => Math.Abs(a.SoakBonus - targetSoak)).ThenBy(a => a.Price).ThenBy(a => a.Name)
            .FirstOrDefault();

    private static SkillDef? FirstByKind(IReadOnlyList<SkillDef> skills, SkillKind kind) =>
        skills.Where(s => s.Kind == kind).OrderBy(s => s.Name).FirstOrDefault();

    private static SkillDef? FirstCombat(IReadOnlyList<SkillDef> skills, CharacteristicType ch) =>
        skills.Where(s => s.Kind == SkillKind.Combat && s.Characteristic == ch).OrderBy(s => s.Name).FirstOrDefault();

    /// <summary>Навык каталога по английскому имени навыка оружия: точное, затем по базовому имени.</summary>
    private static SkillDef? ResolveSkill(IReadOnlyList<SkillDef> skills, string weaponSkill)
    {
        if (string.IsNullOrWhiteSpace(weaponSkill)) return null;
        return skills.FirstOrDefault(s => string.Equals(s.Name, weaponSkill, StringComparison.OrdinalIgnoreCase))
            ?? skills.Where(s => string.Equals(BaseName(s.Name), BaseName(weaponSkill), StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.Name).FirstOrDefault();
    }

    /// <summary>Структурная атака из оружейного предмета каталога (с переносом качеств).</summary>
    private static NpcAttack AttackFromWeapon(ItemDef w) => new()
    {
        Name = Label(w),
        SkillName = w.SkillName,
        Damage = w.Damage,
        Critical = w.Crit,
        RangeBand = w.RangeBand,
        Qualities = w.Qualities.Select(q => new NpcAttackQuality
        {
            QualityDefId = q.QualityDefId,
            QualityCode = q.QualityDef?.Code ?? "",
            NameRu = q.QualityDef?.NameRu ?? "",
            Rating = q.Rating,
        }).ToList(),
    };

    /// <summary>Отображаемое имя записи: русское, с откатом на оригинальное.</summary>
    private static string Label(SkillDef s) => string.IsNullOrWhiteSpace(s.NameRu) ? s.Name : s.NameRu;
    private static string Label(ItemDef i) => string.IsNullOrWhiteSpace(i.NameRu) ? i.Name : i.NameRu;

    /// <summary>Базовое имя навыка без скобочного уточнения: «Melee (Light)» → «Melee».</summary>
    private static string BaseName(string skill) => ParenSuffix().Replace(skill ?? "", "").Trim();

    [GeneratedRegex(@"\s*\(.*\)\s*")]
    private static partial Regex ParenSuffix();
}
