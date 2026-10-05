using GenesysForge.Domain.Entities;

namespace GenesysForge.Infrastructure.Persistence;

/// <summary>Standard secondary effects; prose is an optional private resource.</summary>
public static class HeroicSecondaryEffectCatalog
{
    public static List<HeroicSecondaryEffectDef> Load() =>
    [
        Effect("devastating", "Devastating", "Сокрушительный"),
        Effect("diminish", "Diminish", "Ослабление"),
        Effect("drain", "Drain", "Истощение"),
        Effect("empowered", "Empowered", "Усиление"),
        Effect("empower-allies", "Empower Allies", "Усиление союзников"),
        Effect("rejuvenation", "Rejuvenation", "Восстановление"),
        Effect("rejuvenate-allies", "Rejuvenate Allies", "Восстановление союзников"),
        Effect("renewal", "Renewal", "Обновление")
    ];

    private static HeroicSecondaryEffectDef Effect(string slug, string name, string nameRu)
    {
        var text = PrivateRuleTextCatalog.Get($"secondary.{slug}");
        return new()
        {
            Id = Guid.NewGuid(), Code = $"rot.heroic.secondary.{slug}", Name = name, NameRu = nameRu,
            Description = text.Desc, SafeDescription = text.Safe, DescriptionEn = text.DescEn,
            Source = "Realms of Terrinoth, с. 79",
        };
    }
}
