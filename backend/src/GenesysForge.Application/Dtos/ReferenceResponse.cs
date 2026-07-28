namespace GenesysForge.Application.Dtos;

public record ReferenceResponse(
    List<ArchetypeDto> Archetypes,
    List<CareerDto> Careers,
    List<SkillDefDto> Skills,
    List<TalentDefDto> Talents,
    List<ItemDefDto> Items,
    List<HeroicAbilityDto> HeroicAbilities,
    List<QualityDto> Qualities,
    List<HeroicSecondaryEffectDto> HeroicSecondaryEffects,
    /// <summary>Улучшения предметов (ROT-EQP-ATT-01): собственный тип контента, не снаряжение.</summary>
    List<AttachmentDefDto>? Attachments = null);

public record HeroicSecondaryEffectDto(
    Guid Id, string Code, string Name, string NameRu, string Description, string SafeDescription,
    string Source, string DescriptionEn = "");

public record QualityDto(
    Guid Id, string Code, string NameEn, string NameRu, GenesysForge.Domain.Entities.QualityKind Kind,
    bool IsActive, bool HasRating, string ActivationCost, string Category,
    string Description, string SafeDescription, string Source, string DescriptionEn = "",
    /// <summary>Механика качества; <c>Descriptive</c> — исполнения пока нет (GEN-EQP-QUAL-01).</summary>
    GenesysForge.Domain.QualityEffectKind EffectKind = GenesysForge.Domain.QualityEffectKind.Descriptive,
    /// <summary>Стоимость активации в преимуществах; у пассивного качества ноль.</summary>
    int AdvantageCost = 0,
    bool RequiresHit = false,
    bool CanActivateOnMiss = false,
    bool TriumphMayPay = false,
    GenesysForge.Domain.QualityRepeatability Repeatability = GenesysForge.Domain.QualityRepeatability.Once);
