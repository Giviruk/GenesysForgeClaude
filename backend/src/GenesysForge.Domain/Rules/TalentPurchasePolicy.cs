using GenesysForge.Domain.Entities;

namespace GenesysForge.Domain.Rules;

/// <summary>Отказ политики покупки/возврата таланта с машинным кодом причины.</summary>
public sealed record TalentPolicyError(string ReasonCode, string Message);

/// <summary>
/// Единая проверка покупки и возврата таланта (ROT-TAL-02). Выполняется целиком до первой
/// мутации: система и область видимости, retired, тир, пирамида, XP, prerequisite и
/// взаимоисключения. Любой отказ несёт стабильный <c>reasonCode</c>, а не только текст.
/// </summary>
public static class TalentPurchasePolicy
{
    public const string ReasonRetired = "talent.retired";
    public const string ReasonPrerequisiteMissing = "talent.prerequisite_missing";
    public const string ReasonExcluded = "talent.mutually_exclusive";
    public const string ReasonPyramidOrXp = "talent.purchase_not_allowed";
    public const string ReasonDependentExists = "talent.refund_blocked_by_dependent";

    /// <summary>Владение талантами персонажа, ключ — bare-slug код определения.</summary>
    /// <param name="OwnedCodes">Коды талантов, которыми персонаж уже владеет.</param>
    public readonly record struct OwnedTalents(IReadOnlySet<string> OwnedCodes)
    {
        public bool Has(string code) => code.Length > 0 && OwnedCodes.Contains(code);
    }

    /// <summary>
    /// Проверяет структурные ограничения покупки: retired, prerequisite и взаимоисключения.
    /// Пирамида, тир и XP проверяются <see cref="PurchaseValidator.BuyTalent"/> отдельно —
    /// вызывающий обязан выполнить обе проверки до мутации.
    /// </summary>
    /// <param name="definition">Покупаемый талант.</param>
    /// <param name="owned">Коды уже имеющихся талантов.</param>
    /// <param name="displayName">Функция «код → отображаемое имя» для текста ошибки.</param>
    public static TalentPolicyError? ValidatePurchase(
        TalentDef definition,
        OwnedTalents owned,
        Func<string, string> displayName)
    {
        // Retired-талант отклоняется и как новая покупка, и как повторный ранг: он исключён из
        // покупаемого набора системы (ROT-CLEAN-3.5). Уже купленные ранги продолжают работать,
        // XP за них не возвращается сам, а возврат остаётся разрешён отдельной командой.
        if (definition.Retired)
            return new TalentPolicyError(ReasonRetired,
                $"Талант «{definition.Name}» больше не входит в активный каталог этой системы.");

        if (definition.RequiresTalentCode.Length > 0 && !owned.Has(definition.RequiresTalentCode))
            return new TalentPolicyError(ReasonPrerequisiteMissing,
                $"Для «{definition.Name}» нужен талант «{displayName(definition.RequiresTalentCode)}».");

        foreach (var excluded in definition.ExcludesTalentCodes)
        {
            if (!owned.Has(excluded)) continue;
            return new TalentPolicyError(ReasonExcluded,
                $"«{definition.Name}» несовместим с уже имеющимся «{displayName(excluded)}».");
        }

        return null;
    }

    /// <summary>
    /// Проверяет, что возврат последнего ранга не оставит без основания уже купленный
    /// зависимый талант. Пирамида проверяется <see cref="PurchaseValidator.RefundTalent"/>.
    /// </summary>
    /// <param name="definition">Возвращаемый талант.</param>
    /// <param name="ranksAfterRefund">Сколько рангов останется; при &gt; 0 зависимости не рвутся.</param>
    /// <param name="ownedDefinitions">Определения всех талантов персонажа.</param>
    public static TalentPolicyError? ValidateRefund(
        TalentDef definition,
        int ranksAfterRefund,
        IEnumerable<TalentDef> ownedDefinitions)
    {
        if (ranksAfterRefund > 0) return null;

        var code = BareCode(definition.Code);
        if (code.Length == 0) return null;

        var dependent = ownedDefinitions.FirstOrDefault(d =>
            d.Id != definition.Id && d.RequiresTalentCode == code);

        return dependent is null
            ? null
            : new TalentPolicyError(ReasonDependentExists,
                $"Нельзя вернуть «{definition.Name}»: он нужен уже купленному «{dependent.Name}».");
    }

    /// <summary>
    /// Bare-slug из полного кода вида <c>rot.talent.parry</c>. Связи в каталоге хранятся
    /// bare-slug'ами, поэтому они одинаковы для обеих игровых систем.
    /// </summary>
    public static string BareCode(string fullCode)
    {
        var marker = fullCode.LastIndexOf(".talent.", StringComparison.Ordinal);
        return marker < 0 ? fullCode : fullCode[(marker + ".talent.".Length)..];
    }
}
