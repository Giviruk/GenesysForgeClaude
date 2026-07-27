namespace GenesysForge.Domain.Rules;

/// <summary>Как оплачена одна покупка: сколько снято с бюджета создания и сколько с кошелька.</summary>
public readonly record struct WalletCharge(int FromBudget, int FromMoney)
{
    public int Total => FromBudget + FromMoney;
}

/// <summary>
/// Правила стартовых денег (ROT-CRE-03). Бюджет создания и кошелёк — два разных счёта:
/// бюджет тратится только на стартовые покупки до завершения создания, кошелёк остаётся
/// реальной валютой персонажа.
/// </summary>
public static class StartingWallet
{
    /// <summary>Бюджет стартовых покупок в режиме <c>StandardMoney</c>.</summary>
    public const int StandardPurchaseBudget = 500;

    /// <summary>Формула стартовых карманных денег в режиме <c>StandardMoney</c>.</summary>
    public static MoneyFormula PocketMoneyFormula => new(0, 1, 100);

    /// <summary>
    /// Распределяет стоимость покупки: сначала бюджет создания, остаток — кошелёк.
    /// Вне фазы создания бюджет не участвует. <c>null</c> — денег не хватает; вызывающий обязан
    /// отклонить команду, а не списать частично.
    /// </summary>
    public static WalletCharge? Charge(int cost, int budget, int money, bool isCreationPhase)
    {
        if (cost <= 0) return new WalletCharge(0, 0);

        var fromBudget = isCreationPhase ? Math.Min(cost, Math.Max(0, budget)) : 0;
        var fromMoney = cost - fromBudget;
        return fromMoney > money ? null : new WalletCharge(fromBudget, fromMoney);
    }

    /// <summary>
    /// Распределяет выручку от продажи: во время создания она сначала восстанавливает бюджет
    /// до исходных 500 и только затем идёт в кошелёк. Иначе покупку и продажу можно было бы
    /// использовать как обмен бюджета на реальные деньги.
    /// </summary>
    public static WalletCharge Refund(int proceeds, int budget, StartingEquipmentMode mode, bool isCreationPhase)
    {
        if (proceeds <= 0) return new WalletCharge(0, 0);
        if (!isCreationPhase || mode != StartingEquipmentMode.StandardMoney)
            return new WalletCharge(0, proceeds);

        var restorable = Math.Max(0, StandardPurchaseBudget - budget);
        var toBudget = Math.Min(proceeds, restorable);
        return new WalletCharge(toBudget, proceeds - toBudget);
    }
}
