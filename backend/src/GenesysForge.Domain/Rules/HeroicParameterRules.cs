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
}
