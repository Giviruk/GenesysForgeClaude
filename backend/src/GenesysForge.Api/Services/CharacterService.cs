using GenesysForge.Api.Contracts;
using GenesysForge.Api.Data;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Api.Services;

public class DomainRuleException(string message) : Exception(message);

public class CharacterService(AppDbContext db)
{
    private const int MaxFreeCareerSkills = 4;

    public async Task<Character> CreateAsync(Guid userId, CreateCharacterRequest req)
    {
        var archetype = await db.ArchetypeDefs.FirstOrDefaultAsync(a => a.Id == req.ArchetypeId && a.System == req.System)
            ?? throw new DomainRuleException("Архетип не найден или принадлежит другой системе.");
        var career = await db.CareerDefs.FirstOrDefaultAsync(c => c.Id == req.CareerId && c.System == req.System)
            ?? throw new DomainRuleException("Карьера не найдена или принадлежит другой системе.");
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new DomainRuleException("Имя персонажа не может быть пустым.");

        var freeSkills = req.FreeCareerSkillNames ?? [];
        if (freeSkills.Count > MaxFreeCareerSkills)
            throw new DomainRuleException($"При создании можно выбрать не более {MaxFreeCareerSkills} карьерных навыков для бесплатного ранга.");
        var invalid = freeSkills.FirstOrDefault(n => !career.CareerSkillNames.Contains(n));
        if (invalid is not null)
            throw new DomainRuleException($"«{invalid}» не является карьерным навыком карьеры {career.Name}.");

        var character = new Character
        {
            Id = Guid.NewGuid(),
            OwnerUserId = userId,
            Name = req.Name.Trim(),
            System = req.System,
            ArchetypeId = archetype.Id,
            CareerId = career.Id,
            Brawn = archetype.Brawn,
            Agility = archetype.Agility,
            Intellect = archetype.Intellect,
            Cunning = archetype.Cunning,
            Willpower = archetype.Willpower,
            Presence = archetype.Presence,
            TotalXp = archetype.StartingXp,
        };

        // Строки навыков создаются для карьерных и бесплатных рангов; остальные подмешиваются динамически.
        var systemSkills = await db.SkillDefs
            .Where(s => s.System == req.System && (s.OwnerUserId == null || s.OwnerUserId == userId))
            .ToListAsync();
        foreach (var skill in systemSkills.Where(s => career.CareerSkillNames.Contains(s.Name)))
        {
            character.Skills.Add(new CharacterSkill
            {
                Id = Guid.NewGuid(),
                CharacterId = character.Id,
                SkillDefId = skill.Id,
                IsCareer = true,
                Ranks = freeSkills.Contains(skill.Name) ? 1 : 0,
            });
        }

        db.Characters.Add(character);
        await db.SaveChangesAsync();
        return character;
    }

    public async Task<Character> GetOwnedAsync(Guid userId, Guid characterId, bool tracking = true)
    {
        var query = db.Characters
            .Include(c => c.Archetype)
            .Include(c => c.Career)
            .Include(c => c.HeroicAbility)
            .Include(c => c.Skills).ThenInclude(s => s.SkillDef)
            .Include(c => c.Talents).ThenInclude(t => t.TalentDef)
            .Include(c => c.Items).ThenInclude(i => i.ItemDef);
        var character = await (tracking ? query : query.AsNoTracking())
            .FirstOrDefaultAsync(c => c.Id == characterId);
        if (character is null || character.OwnerUserId != userId)
            throw new DomainRuleException("Персонаж не найден.");
        return character;
    }

    public async Task BuyCharacteristicAsync(Guid userId, Guid characterId, CharacteristicType type)
    {
        var c = await GetOwnedAsync(userId, characterId);
        var current = c.Characteristics.Get(type);
        var result = PurchaseValidator.BuyCharacteristic(current, c.TotalXp - c.SpentXp, c.IsCreationPhase);
        if (!result.Allowed) throw new DomainRuleException(result.Error!);

        switch (type)
        {
            case CharacteristicType.Brawn: c.Brawn++; break;
            case CharacteristicType.Agility: c.Agility++; break;
            case CharacteristicType.Intellect: c.Intellect++; break;
            case CharacteristicType.Cunning: c.Cunning++; break;
            case CharacteristicType.Willpower: c.Willpower++; break;
            case CharacteristicType.Presence: c.Presence++; break;
        }
        c.SpentXp += result.Cost;
        await db.SaveChangesAsync();
    }

    public async Task BuySkillRankAsync(Guid userId, Guid characterId, Guid skillDefId)
    {
        var c = await GetOwnedAsync(userId, characterId);
        var skillDef = await db.SkillDefs.FirstOrDefaultAsync(s =>
                s.Id == skillDefId && s.System == c.System && (s.OwnerUserId == null || s.OwnerUserId == userId))
            ?? throw new DomainRuleException("Навык не найден.");

        var row = c.Skills.FirstOrDefault(s => s.SkillDefId == skillDefId);
        var isCareer = row?.IsCareer ?? c.Career!.CareerSkillNames.Contains(skillDef.Name);
        var currentRank = row?.Ranks ?? 0;

        var result = PurchaseValidator.BuySkillRank(currentRank, isCareer, c.TotalXp - c.SpentXp, c.IsCreationPhase);
        if (!result.Allowed) throw new DomainRuleException(result.Error!);

        if (row is null)
        {
            row = new CharacterSkill
            {
                Id = Guid.NewGuid(), CharacterId = c.Id, SkillDefId = skillDefId, IsCareer = isCareer,
            };
            db.CharacterSkills.Add(row); // явный Add: через навигацию EF счёл бы ключ существующим
            c.Skills.Add(row);
        }
        row.Ranks++;
        c.SpentXp += result.Cost;
        await db.SaveChangesAsync();
    }

    /// <summary>Каждый ранг таланта считается отдельным талантом своего эффективного тира.</summary>
    public static Dictionary<int, int> TierCounts(IEnumerable<CharacterTalent> talents)
    {
        var counts = new Dictionary<int, int>();
        foreach (var t in talents)
        {
            for (var rank = 0; rank < t.Ranks; rank++)
            {
                var tier = GenesysRules.RankedTalentEffectiveTier(t.TalentDef!.Tier, rank);
                counts[tier] = counts.GetValueOrDefault(tier) + 1;
            }
        }
        return counts;
    }

    public async Task BuyTalentAsync(Guid userId, Guid characterId, Guid talentDefId)
    {
        var c = await GetOwnedAsync(userId, characterId);
        var talentDef = await db.TalentDefs.FirstOrDefaultAsync(t =>
                t.Id == talentDefId && t.System == c.System && (t.OwnerUserId == null || t.OwnerUserId == userId))
            ?? throw new DomainRuleException("Талант не найден.");

        var row = c.Talents.FirstOrDefault(t => t.TalentDefId == talentDefId);
        var result = PurchaseValidator.BuyTalent(
            talentDef.Tier,
            row?.Ranks ?? 0,
            talentDef.IsRanked,
            TierCounts(c.Talents),
            c.TotalXp - c.SpentXp);
        if (!result.Allowed) throw new DomainRuleException(result.Error!);

        if (row is null)
        {
            row = new CharacterTalent
            {
                Id = Guid.NewGuid(), CharacterId = c.Id, TalentDefId = talentDefId, TalentDef = talentDef, Ranks = 0,
            };
            db.CharacterTalents.Add(row);
            c.Talents.Add(row);
        }
        row.Ranks++;
        c.SpentXp += result.Cost;
        await db.SaveChangesAsync();
    }

    public async Task SetHeroicAbilityAsync(Guid userId, Guid characterId, Guid? heroicAbilityId)
    {
        var c = await GetOwnedAsync(userId, characterId);
        if (c.System != GameSystem.RealmsOfTerrinoth)
            throw new DomainRuleException("Героические способности доступны только в Realms of Terrinoth.");
        if (heroicAbilityId is not null)
        {
            var exists = await db.HeroicAbilityDefs.AnyAsync(h =>
                h.Id == heroicAbilityId && (h.OwnerUserId == null || h.OwnerUserId == userId));
            if (!exists) throw new DomainRuleException("Героическая способность не найдена.");
        }
        c.HeroicAbilityId = heroicAbilityId;
        await db.SaveChangesAsync();
    }

    public async Task<CharacterItem> AddItemAsync(Guid userId, Guid characterId, AddItemRequest req)
    {
        var c = await GetOwnedAsync(userId, characterId);
        var itemDef = await db.ItemDefs.FirstOrDefaultAsync(i =>
                i.Id == req.ItemDefId && i.System == c.System && (i.OwnerUserId == null || i.OwnerUserId == userId))
            ?? throw new DomainRuleException("Предмет не найден.");
        if (req.Quantity < 1) throw new DomainRuleException("Количество должно быть не меньше 1.");

        var item = new CharacterItem
        {
            Id = Guid.NewGuid(), CharacterId = c.Id, ItemDefId = itemDef.Id, ItemDef = itemDef,
            Quantity = req.Quantity, State = req.State,
        };
        db.CharacterItems.Add(item);
        c.Items.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    public async Task UpdateItemAsync(Guid userId, Guid characterId, Guid itemId, UpdateItemRequest req)
    {
        var c = await GetOwnedAsync(userId, characterId);
        var item = c.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainRuleException("Предмет не найден в инвентаре.");
        if (req.Quantity is < 1) throw new DomainRuleException("Количество должно быть не меньше 1.");
        if (req.State is not null) item.State = req.State.Value;
        if (req.Quantity is not null) item.Quantity = req.Quantity.Value;
        await db.SaveChangesAsync();
    }

    public async Task RemoveItemAsync(Guid userId, Guid characterId, Guid itemId)
    {
        var c = await GetOwnedAsync(userId, characterId);
        var item = c.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainRuleException("Предмет не найден в инвентаре.");
        c.Items.Remove(item);
        db.CharacterItems.Remove(item);
        await db.SaveChangesAsync();
    }

    public async Task<CharacterSheetDto> BuildSheetAsync(Guid userId, Character c)
    {
        var ch = c.Characteristics;

        var talentInputs = c.Talents.Select(t => new TalentInput(
            t.TalentDef!.Name, t.TalentDef.Tier, t.Ranks,
            t.TalentDef.WoundBonus, t.TalentDef.StrainBonus, t.TalentDef.SoakBonus,
            t.TalentDef.MeleeDefenseBonus, t.TalentDef.RangedDefenseBonus)).ToList();

        var itemInputs = c.Items.Select(i => new ItemInput(
            i.ItemDef!.Name, i.ItemDef.Kind, i.State, i.ItemDef.Encumbrance, i.Quantity,
            i.ItemDef.SoakBonus, i.ItemDef.MeleeDefense, i.ItemDef.RangedDefense,
            i.ItemDef.EncumbranceThresholdBonus)).ToList();

        var derived = SheetCalculator.ComputeDerived(
            ch, c.Archetype!.WoundBase, c.Archetype.StrainBase, talentInputs, itemInputs);

        // Все навыки системы (встроенные + кастомные владельца), объединённые со строками персонажа.
        var systemSkills = await db.SkillDefs.AsNoTracking()
            .Where(s => s.System == c.System && (s.OwnerUserId == null || s.OwnerUserId == userId))
            .OrderBy(s => s.Kind).ThenBy(s => s.Name)
            .ToListAsync();
        var rows = c.Skills.ToDictionary(s => s.SkillDefId);
        var skills = systemSkills.Select(def =>
        {
            rows.TryGetValue(def.Id, out var row);
            var ranks = row?.Ranks ?? 0;
            var isCareer = row?.IsCareer ?? c.Career!.CareerSkillNames.Contains(def.Name);
            var pool = GenesysRules.BuildDicePool(ch.Get(def.Characteristic), ranks);
            return new CharacterSkillDto(def.Id, def.Name, def.Kind, def.Characteristic, ranks, isCareer,
                new DicePoolDto(pool.Ability, pool.Proficiency),
                ranks < GenesysRules.MaxSkillRank ? GenesysRules.SkillRankCost(ranks + 1, isCareer) : 0);
        }).ToList();

        return new CharacterSheetDto(
            c.Id, c.Name, c.System,
            ToDto(c.Archetype),
            new CareerDto(c.Career!.Id, c.Career.Name, c.Career.Description, c.Career.CareerSkillNames),
            new Dictionary<string, int>
            {
                ["brawn"] = ch.Brawn, ["agility"] = ch.Agility, ["intellect"] = ch.Intellect,
                ["cunning"] = ch.Cunning, ["willpower"] = ch.Willpower, ["presence"] = ch.Presence,
            },
            c.TotalXp, c.SpentXp, c.TotalXp - c.SpentXp, c.IsCreationPhase,
            c.WoundsCurrent, c.StrainCurrent,
            new DerivedDto(derived.WoundThreshold, derived.StrainThreshold, derived.Soak, derived.MeleeDefense,
                derived.RangedDefense, derived.EncumbranceThreshold, derived.EncumbranceLoad, derived.Encumbered),
            skills,
            c.Talents
                .OrderBy(t => t.TalentDef!.Tier).ThenBy(t => t.TalentDef!.Name)
                .Select(t => new CharacterTalentDto(t.TalentDefId, t.TalentDef!.Name, t.TalentDef.Tier,
                    t.TalentDef.IsRanked, t.Ranks, t.TalentDef.Activation, t.TalentDef.Description))
                .ToList(),
            TierCounts(c.Talents),
            c.HeroicAbility is null
                ? null
                : new HeroicAbilityDto(c.HeroicAbility.Id, c.HeroicAbility.Name, c.HeroicAbility.Description,
                    c.HeroicAbility.OwnerUserId != null),
            c.Items
                .OrderBy(i => i.ItemDef!.Kind).ThenBy(i => i.ItemDef!.Name)
                .Select(i => new CharacterItemDto(i.Id, i.ItemDefId, i.ItemDef!.Name, i.ItemDef.Kind, i.State,
                    i.Quantity, i.ItemDef.Encumbrance, i.ItemDef.SoakBonus, i.ItemDef.MeleeDefense,
                    i.ItemDef.RangedDefense, i.ItemDef.EncumbranceThresholdBonus,
                    SheetCalculator.ItemLoad(new ItemInput(i.ItemDef.Name, i.ItemDef.Kind, i.State,
                        i.ItemDef.Encumbrance, i.Quantity)),
                    i.ItemDef.Description))
                .ToList());
    }

    public static ArchetypeDto ToDto(ArchetypeDef a) => new(a.Id, a.Name, a.Brawn, a.Agility, a.Intellect, a.Cunning,
        a.Willpower, a.Presence, a.WoundBase, a.StrainBase, a.StartingXp, a.Description);
}
