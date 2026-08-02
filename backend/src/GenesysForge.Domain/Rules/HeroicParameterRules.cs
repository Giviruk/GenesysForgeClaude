using GenesysForge.Domain.Entities;

namespace GenesysForge.Domain.Rules;

/// <summary>
/// Какой параметр требует primary effect и как его проверить (ROT-HA-02). Требование выводится
/// из стабильного кода способности: определять Paragon по отображаемому имени нельзя.
/// </summary>
public static class HeroicParameterRules
{
    public const int SixthSenseSubjectMaxLength = 300;
    public const int NarrativeFormMaxLength = 200;

    /// <summary>Коды способностей, требующих параметр. Кастомная способность (пустой код) — без параметра.</summary>
    public static HeroicParameterKind Required(string? abilityCode) => abilityCode switch
    {
        "rot.heroic.paragon" => HeroicParameterKind.ParagonSkill,
        "rot.heroic.sixth-sense" => HeroicParameterKind.SixthSenseSubject,
        "rot.heroic.signature-weapon" => HeroicParameterKind.SignatureWeapon,
        _ => HeroicParameterKind.None,
    };

    /// <summary>Категория Sixth Sense: обязательная, обрезается по краям, 1–300 символов.</summary>
    public static string ValidateSixthSenseSubject(string? subject)
    {
        var text = subject?.Trim() ?? "";
        if (text.Length == 0)
            throw new DomainRuleException(
                "Укажите категорию существ или явлений, которые воспринимает способность.",
                "heroic.parameter.subject_required");
        if (text.Length > SixthSenseSubjectMaxLength)
            throw new DomainRuleException(
                $"Категория Sixth Sense длиннее {SixthSenseSubjectMaxLength} символов.",
                "heroic.parameter.subject_too_long");
        return text;
    }

    /// <summary>Описание формы оружия: обязательное, 1–200 символов.</summary>
    public static string ValidateNarrativeForm(string? form)
    {
        var text = form?.Trim() ?? "";
        if (text.Length == 0)
            throw new DomainRuleException(
                "Опишите форму именного оружия.", "heroic.weapon.form_required");
        if (text.Length > NarrativeFormMaxLength)
            throw new DomainRuleException(
                $"Описание формы длиннее {NarrativeFormMaxLength} символов.", "heroic.weapon.form_too_long");
        return text;
    }

    /// <summary>
    /// Приводит подтверждённые GM признаки формы к профилю и отсекает физически несовместимые
    /// сочетания. Группу профиля всегда ставит сервер: прислать «дальнобойный меч» нельзя.
    /// </summary>
    public static WeaponFormTraits ValidateFormTraits(SignatureWeaponProfile profile, WeaponFormTraits confirmed)
    {
        var spec = SignatureWeaponProfiles.Get(profile);
        // Группа профиля не обсуждается: чужие группы из запроса выбрасываются, своя добавляется.
        var traits = (confirmed & ~SignatureWeaponProfiles.GroupTraits) | spec.GroupTrait;

        if (traits.HasFlag(WeaponFormTraits.Bladed) && traits.HasFlag(WeaponFormTraits.BluntOrCrushing))
            throw new DomainRuleException(
                "Оружие не может быть одновременно клинковым и дробящим.",
                "heroic.weapon.traits_conflict");
        if (traits.HasFlag(WeaponFormTraits.Sword) && !traits.HasFlag(WeaponFormTraits.OneHanded)
            && !traits.HasFlag(WeaponFormTraits.TwoHanded))
            throw new DomainRuleException(
                "Меч — ближняя одноручная или двуручная форма.", "heroic.weapon.traits_conflict");
        if (traits.HasFlag(WeaponFormTraits.BowOrCrossbow) && !traits.HasFlag(WeaponFormTraits.Ranged))
            throw new DomainRuleException(
                "Лук или арбалет — дальнобойная форма.", "heroic.weapon.traits_conflict");

        // Меч клинковый, а у клинка есть рабочая режущая кромка: эти следствия проставляются сами,
        // иначе Weighted Head (требует отсутствия кромки) прошёл бы на клинке.
        if (traits.HasFlag(WeaponFormTraits.Sword)) traits |= WeaponFormTraits.Bladed;
        if (traits.HasFlag(WeaponFormTraits.Bladed)) traits |= WeaponFormTraits.HasCuttingEdge;

        if (traits.HasFlag(WeaponFormTraits.WoodenWorkingEdge) && !traits.HasFlag(WeaponFormTraits.HasCuttingEdge))
            throw new DomainRuleException(
                "Деревянная рабочая кромка предполагает наличие самой кромки.",
                "heroic.weapon.traits_conflict");

        return traits;
    }

    /// <summary>
    /// Качество изготовления, доступное именному оружию при создании: способность даёт гномью или
    /// эльфийскую работу, обычная сталь — «без изменений». Железа книга не предлагает, а древняя
    /// работа — награда за Improved (ROT-HA-05), а не бесплатный выбор на старте.
    /// </summary>
    public static IReadOnlyList<WeaponCraftsmanship> SignatureCraftsmanshipChoices { get; } =
        [WeaponCraftsmanship.Steel, WeaponCraftsmanship.Dwarven, WeaponCraftsmanship.Elven];

    /// <summary>Работа выбрана из того, что даёт сама способность.</summary>
    public static void EnsureSignatureCraftsmanship(WeaponCraftsmanship craftsmanship)
    {
        if (!SignatureCraftsmanshipChoices.Contains(craftsmanship))
            throw new DomainRuleException(
                "Именное оружие бывает обычной, гномьей или эльфийской работы; древняя работа даётся "
                + "улучшением способности.",
                "heroic.weapon.craftsmanship_not_allowed");
    }

    /// <summary>
    /// Слоты улучшений именного оружия: слоты профиля с поправкой работы плюс два за Supreme.
    /// </summary>
    public static int HardPoints(int profileHardPoints, WeaponCraftsmanship craftsmanship, int upgradeRank)
    {
        var stats = CraftsmanshipRules.For(ItemKind.Weapon, craftsmanship, 0, 0, 0, 0, profileHardPoints, 0, 0);
        return (stats.HardPoints ?? profileHardPoints) + (upgradeRank >= 2 ? 2 : 0);
    }

    /// <summary>
    /// Бесплатное улучшение от Supreme (ROT-HA-05). В отличие от базового оно ставится по-настоящему:
    /// занимает слоты и ограничено редкостью 9. Совместимость — те же предикаты формы.
    /// </summary>
    /// <param name="availableHardPoints">Слоты оружия с учётом работы и прибавки Supreme.</param>
    /// <param name="baseAttachmentCode">Код базового улучшения: второй такой же не ставится.</param>
    public static void EnsureCanBeSupremeAttachment(
        WeaponFormTraits traits, int availableHardPoints, string? baseAttachmentCode, AttachmentDef def)
    {
        if (!AttachmentRules.IsCompatible(ItemKind.Weapon, traits, def))
            throw new DomainRuleException(
                "Это улучшение не подходит подтверждённой форме именного оружия.",
                "heroic.weapon.attachment_incompatible");

        if (def.Rarity > SupremeAttachmentMaxRarity)
            throw new DomainRuleException(
                $"Бесплатное улучшение Supreme ограничено редкостью {SupremeAttachmentMaxRarity}.",
                "heroic.weapon.attachment_too_rare");

        if (string.Equals(def.Code, baseAttachmentCode, StringComparison.Ordinal))
            throw new DomainRuleException(
                "Такое улучшение уже выбрано базовым.", "heroic.weapon.attachment_duplicate");

        if (def.HardPointCost > availableHardPoints)
            throw new DomainRuleException(
                "Улучшение не помещается в слоты именного оружия.", "attachment.no_hard_points");
    }

    /// <summary>Предел редкости бесплатного улучшения Supreme.</summary>
    public const int SupremeAttachmentMaxRarity = 9;

    /// <summary>
    /// Качества, которые улучшение выдаёт предмету. Прибавка к уже имеющемуся рейтингу — тоже
    /// выдача: правило запрещает брать базовым то, что у оружия профиля уже есть.
    /// </summary>
    public static IEnumerable<string> GrantedQualityCodes(AttachmentDef def) =>
        def.Effects
            .Where(e => e.Kind is AttachmentEffectKind.GrantOrIncreaseQuality
                or AttachmentEffectKind.SetQualityAtLeast
                or AttachmentEffectKind.GrantQualityOrCancelOpposite)
            .Select(e => e.QualityCode)
            .Where(code => !string.IsNullOrEmpty(code));

    /// <summary>
    /// Базовое улучшение именного оружия (ROT-HA-02). Совместимость считается теми же предикатами,
    /// что и обычная установка (ROT-EQP-ATT-02), — по подтверждённым признакам формы, а не по тексту
    /// названия. Редкость, цена, слоты, проверка установки чар и ранг магического навыка к этой
    /// временной героической копии не применяются: она не покупается и физически не ставится.
    /// </summary>
    /// <param name="traits">Подтверждённые признаки формы оружия.</param>
    /// <param name="effectiveQualityCodes">Качества, которые оружие профиля уже имеет.</param>
    public static void EnsureCanBeBaseAttachment(
        WeaponFormTraits traits, IReadOnlyCollection<string> effectiveQualityCodes, AttachmentDef def)
    {
        if (!AttachmentRules.IsCompatible(ItemKind.Weapon, traits, def))
            throw new DomainRuleException(
                "Это улучшение не подходит подтверждённой форме именного оружия.",
                "heroic.weapon.attachment_incompatible");

        // Улучшение, которое выдаёт уже имеющееся качество, ничего оружию не добавляет: книга
        // прямо не даёт брать базовым то, что и так есть в профиле.
        if (GrantedQualityCodes(def).Any(code => effectiveQualityCodes.Contains(code, StringComparer.Ordinal)))
            throw new DomainRuleException(
                "Это улучшение выдаёт качество, которое у именного оружия уже есть.",
                "heroic.weapon.attachment_redundant");
    }
}
