namespace GenesysForge.Domain.Rules;

/// <summary>Общие правила кошелька персонажа.</summary>
public static class MoneyRules
{
    /// <summary>Стандартное количество денег при создании персонажа.</summary>
    public const int StandardStartingMoney = 500;

    /// <summary>Проверяет, хватает ли денег на покупку, и возвращает сумму списания.</summary>
    public static int? Charge(int cost, int money)
    {
        if (cost <= 0) return 0;
        return cost > money ? null : cost;
    }
}
