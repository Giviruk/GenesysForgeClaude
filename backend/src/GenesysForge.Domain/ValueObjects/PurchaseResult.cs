namespace GenesysForge.Domain;

public readonly record struct PurchaseResult(bool Allowed, int Cost, string? Error)
{
    public static PurchaseResult Ok(int cost) => new(true, cost, null);
    public static PurchaseResult Fail(string error) => new(false, 0, error);
}
