using GenesysForge.Domain.Entities;

namespace GenesysForge.Infrastructure.Persistence;

/// <summary>
/// Стандартные Secondary Effects из структуры правил RoT. Описания — собственные парафразы.
/// </summary>
public static class HeroicSecondaryEffectCatalog
{
    public static List<HeroicSecondaryEffectDef> Load() =>
    [
        Effect("devastating", "Devastating", "Сокрушительный",
            "Добавляет урон одной атаке каждый ход.", "While active, add 2 damage to one hit of each attack."),
        Effect("diminish", "Diminish", "Ослабление",
            "Мешает проверкам ближайших противников.", "While active, nearby enemies add one setback die to skill checks."),
        Effect("drain", "Drain", "Истощение",
            "Причиняет усталость ближайшим противникам.", "Nearby enemies suffer 2 strain on activation and at the start of each active turn."),
        Effect("empowered", "Empowered", "Усиление",
            "Усиливает проверки персонажа.", "While active, add one boost die to the character's skill checks."),
        Effect("empower-allies", "Empower Allies", "Усиление союзников",
            "Усиливает проверки ближайших союзников.", "While active, nearby allies add one boost die to skill checks."),
        Effect("rejuvenation", "Rejuvenation", "Омоложение",
            "Восстанавливает усталость персонажа.", "Heal 2 strain on activation and at the start of each active turn."),
        Effect("rejuvenate-allies", "Rejuvenate Allies", "Омоложение союзников",
            "Восстанавливает усталость ближайших союзников.", "Nearby allies heal 2 strain on activation and at the start of each active turn."),
        Effect("renewal", "Renewal", "Обновление",
            "Создаёт дополнительный слот инициативы группы.", "On activation, make a Cool or Vigilance check to add a PC initiative slot for the encounter."),
    ];

    private static HeroicSecondaryEffectDef Effect(
        string slug, string name, string nameRu, string safe, string descriptionEn) => new()
    {
        Id = Guid.NewGuid(),
        Code = $"rot.heroic.secondary.{slug}",
        Name = name,
        NameRu = nameRu,
        SafeDescription = safe,
        DescriptionEn = descriptionEn,
        Source = "Realms of Terrinoth, с. 79",
    };
}
