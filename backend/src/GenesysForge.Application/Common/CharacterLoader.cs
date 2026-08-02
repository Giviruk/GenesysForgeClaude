using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Common;

/// <summary>
/// Загрузка графа персонажа. Связей у него больше десятка, и тянуть их все ради одного среза
/// незачем: у каждого чтения свой набор.
///
/// <para>
/// Общий <see cref="WithRelations"/> остаётся для правок (обработчик команды не знает заранее, что
/// ему понадобится) и для поверхностей, которым нужен весь лист сразу: печать, экспорт, публичная
/// ссылка, просмотр листа ведущим. Чтения вкладок берут свой узкий запрос.
/// </para>
/// </summary>
public static class CharacterLoader
{
    /// <summary>
    /// Карточки списка: пороги зависят от вида и талантов, но не от инвентаря. Отдельный query
    /// builder позволяет регрессионному тесту гарантировать, что тяжёлый Include предметов не вернётся.
    /// </summary>
    public static IQueryable<Character> CardListQuery(this IAppDbContext db, Guid userId) =>
        db.Characters.AsNoTracking()
            .Where(c => c.OwnerUserId == userId)
            .Include(c => c.Archetype)
            .Include(c => c.Career)
            .Include(c => c.Talents).ThenInclude(t => t.TalentDef)
            .OrderByDescending(c => c.CreatedAt);

    /// <summary>Загружает персонажа со всеми связями и проверяет владельца.</summary>
    public static async Task<Character> GetOwnedAsync(
        this IAppDbContext db, Guid userId, Guid characterId, bool tracking = true, CancellationToken ct = default)
    {
        var character = await db.LoadWithRelationsAsync(characterId, tracking, ct);
        if (character is null || character.OwnerUserId != userId)
            throw new DomainRuleException("Персонаж не найден.");
        return character;
    }

    /// <summary>
    /// Загружает персонажа со всеми связями <b>без</b> проверки владельца (или <c>null</c>, если не найден).
    /// Вызывающий обязан сам авторизовать доступ (напр. GM-доступ к листу участника кампании, U-20).
    /// </summary>
    public static async Task<Character?> LoadWithRelationsAsync(
        this IAppDbContext db, Guid characterId, bool tracking = true, CancellationToken ct = default)
    {
        var query = db.WithRelations();
        return await (tracking ? query : query.AsNoTracking())
            .FirstOrDefaultAsync(c => c.Id == characterId, ct);
    }

    /// <summary>
    /// Читает только запрошенные срезы и проверяет владельца. Отслеживание не нужно: срезы
    /// показываются, а не правятся.
    /// </summary>
    public static async Task<Character> GetOwnedSlicesAsync(
        this IAppDbContext db, Guid userId, Guid characterId, SheetSlice slices,
        CancellationToken ct = default)
    {
        var character = await db.SliceQuery(slices).AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == characterId, ct);
        if (character is null || character.OwnerUserId != userId)
            throw new DomainRuleException("Персонаж не найден.");
        return character;
    }

    /// <summary>
    /// Весь граф. Вынесен отдельным запросом, без выполнения, чтобы тест мог посмотреть на
    /// сгенерированный SQL: коллекций здесь больше десятка, и в одной выборке они перемножаются
    /// (см. <c>UseQuerySplittingBehavior</c> в настройке контекста).
    /// </summary>
    public static IQueryable<Character> WithRelations(this IAppDbContext db)
        => db.Characters
            .Include(c => c.Archetype).ThenInclude(a => a!.StartingSkills)
            .Include(c => c.Archetype).ThenInclude(a => a!.Abilities)
            .Include(c => c.Career)
            .Include(c => c.HeroicAbility).ThenInclude(h => h!.Upgrades)
            .Include(c => c.HeroicSecondaryEffects).ThenInclude(x => x.HeroicSecondaryEffectDef)
            .Include(c => c.Skills).ThenInclude(s => s.SkillDef)
            .Include(c => c.Talents).ThenInclude(t => t.TalentDef)
            .Include(c => c.Talents).ThenInclude(t => t.Choices)
            .Include(c => c.Items).ThenInclude(i => i.ItemDef)
            // Штрафы снаряжения к проверкам нужны и листу, и Game Table (ROT-ARM-01).
            .Include(c => c.Items).ThenInclude(i => i.ItemDef!.CheckModifiers)
            // Качества предмета участвуют в защите (щит — оружие, ROT-WPN-01), профили — в атаках.
            .Include(c => c.Items).ThenInclude(i => i.ItemDef!.Qualities).ThenInclude(q => q.QualityDef)
            .Include(c => c.Items).ThenInclude(i => i.ItemDef!.AttackProfiles)
            // Улучшения меняют числа предмета и его качества (ROT-EQP-ATT-01).
            .Include(c => c.Attachments).ThenInclude(a => a.AttachmentDef!.Effects)
            // Транспорт показывается со статблоком профиля: без него нет ни порога ран, ни
            // вместимости (ROT-MOUNT-ITEM-01).
            .Include(c => c.Mounts).ThenInclude(m => m.MountDef!.Skills)
            .Include(c => c.Mounts).ThenInclude(m => m.MountDef!.Abilities)
            .Include(c => c.Mounts).ThenInclude(m => m.MountDef!.Attacks)
            // Груз транспорта отдельным Include не грузится: это те же строки `c.Items`, уже
            // загруженные со всеми справочниками выше. Раскладка по транспорту делается по
            // `CarriedByMountId`, а не через навигацию, чтобы одинаково работать и без отслеживания
            // (ROT-TRANSPORT-01).
            .Include(c => c.CriticalInjuries)
            .Include(c => c.HeroicConfiguration).ThenInclude(x => x!.ParagonSkillDef)
            .Include(c => c.SignatureWeapon).ThenInclude(w => w!.BaseAttachment).ThenInclude(a => a!.Effects)
            .Include(c => c.SignatureWeapon).ThenInclude(w => w!.SupremeAttachment).ThenInclude(a => a!.Effects);

    /// <summary>
    /// Запрос ровно под запрошенные срезы. Include'ы складываются объединением, потому что срез
    /// нередко считается из того, что сам не отдаёт:
    ///
    /// <list type="bullet">
    /// <item>базовый лист грузит предметы, улучшения и таланты, но не отдаёт их — поглощение,
    /// защита и порог веса считаются из них (<see cref="CharacterDerived"/>), как и помехи
    /// снаряжения в пулах навыков;</item>
    /// <item>инвентарю нужны улучшения: они меняют числа и качества носителя, иначе карточка
    /// показала бы каталожные значения вместо фактических (ROT-EQP-ATT-01);</item>
    /// <item>транспорту нужны предметы: его груз рисуется той же карточкой и считается теми же
    /// поправками, что и позиция за спиной (ROT-TRANSPORT-01).</item>
    /// </list>
    ///
    /// <para>
    /// А вот обратное неверно: на базовые числа транспорт не влияет вовсе (груз не входит в
    /// переносимый вес владельца), профили атак нужны только карточкам инвентаря, а выборы рангов —
    /// только вкладке талантов. Их и не грузим.
    /// </para>
    /// </summary>
    public static IQueryable<Character> SliceQuery(this IAppDbContext db, SheetSlice slices)
    {
        // Предметы участвуют в базовых числах и в грузе транспорта, улучшения — ещё и в них самих.
        var needsItems = slices.HasAny(SheetSlice.Base | SheetSlice.Items | SheetSlice.Mounts);
        var needsAttackProfiles = slices.HasAny(SheetSlice.Items | SheetSlice.Mounts);
        var needsAttachments = needsItems || slices.HasAny(SheetSlice.Attachments);
        var needsTalentDefs = slices.HasAny(SheetSlice.Base | SheetSlice.Talents);

        var query = db.Characters.AsQueryable();

        if (slices.HasAny(SheetSlice.Base))
            query = query
                .Include(c => c.Archetype).ThenInclude(a => a!.StartingSkills)
                .Include(c => c.Archetype).ThenInclude(a => a!.Abilities)
                .Include(c => c.Career)
                .Include(c => c.HeroicAbility).ThenInclude(h => h!.Upgrades)
                .Include(c => c.HeroicSecondaryEffects).ThenInclude(x => x.HeroicSecondaryEffectDef)
                .Include(c => c.Skills).ThenInclude(s => s.SkillDef)
                .Include(c => c.CriticalInjuries)
                .Include(c => c.HeroicConfiguration).ThenInclude(x => x!.ParagonSkillDef)
                .Include(c => c.SignatureWeapon).ThenInclude(w => w!.BaseAttachment).ThenInclude(a => a!.Effects)
            .Include(c => c.SignatureWeapon).ThenInclude(w => w!.SupremeAttachment).ThenInclude(a => a!.Effects);

        if (needsTalentDefs)
            query = query.Include(c => c.Talents).ThenInclude(t => t.TalentDef);
        // Выборы рангов рисует только вкладка талантов.
        if (slices.HasAny(SheetSlice.Talents))
            query = query.Include(c => c.Talents).ThenInclude(t => t.Choices);

        if (needsItems)
            query = query
                // Штрафы снаряжения к проверкам нужны и листу, и Game Table (ROT-ARM-01).
                .Include(c => c.Items).ThenInclude(i => i.ItemDef!.CheckModifiers)
                // Качества предмета участвуют в защите (щит — оружие, ROT-WPN-01).
                .Include(c => c.Items).ThenInclude(i => i.ItemDef!.Qualities).ThenInclude(q => q.QualityDef);
        if (needsAttackProfiles)
            query = query.Include(c => c.Items).ThenInclude(i => i.ItemDef!.AttackProfiles);

        if (needsAttachments)
            query = query.Include(c => c.Attachments).ThenInclude(a => a.AttachmentDef!.Effects);

        if (slices.HasAny(SheetSlice.Mounts))
            query = query
                // Транспорт показывается со статблоком профиля: без него нет ни порога ран, ни
                // вместимости (ROT-MOUNT-ITEM-01).
                .Include(c => c.Mounts).ThenInclude(m => m.MountDef!.Skills)
                .Include(c => c.Mounts).ThenInclude(m => m.MountDef!.Abilities)
                .Include(c => c.Mounts).ThenInclude(m => m.MountDef!.Attacks);

        return query;
    }
}
