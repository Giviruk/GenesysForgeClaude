namespace GenesysForge.Application.Dtos;

/// <summary>Дополнительный удар той же атаки: у каждого своё поглощение (ROT-CMB-01).</summary>
public record AttackHitRequest(int TargetSoak, string Label = "");

/// <summary>Трата символа, которую клиент хочет применить.</summary>
public record AttackSpendRequest(
    string Code,
    bool MayActivateOnMiss = false,
    bool RequiresDamageThroughSoak = false);

/// <summary>Запрос разрешения атаки. Нетто-символы уже сокращены клиентом при разборе броска.</summary>
public record ResolveAttackRequest(
    int NetSuccesses,
    int BaseDamage,
    int TargetSoak,
    int NetAdvantages = 0,
    int Triumphs = 0,
    int Despairs = 0,
    List<AttackHitRequest>? AdditionalHits = null,
    List<AttackSpendRequest>? Spends = null);

/// <summary>Урон одного удара после поглощения.</summary>
public record AttackHitDto(string Label, int RawDamage, int TargetSoak, int Applied);

/// <summary>
/// Результат атаки. На промахе <paramref name="RawDamagePerHit"/> равен <c>null</c>, а список
/// ударов пуст — базовый урон в этом случае не показывается вовсе.
/// </summary>
public record ResolveAttackResponse(
    bool IsHit,
    int? RawDamagePerHit,
    List<AttackHitDto> Hits,
    int TotalApplied,
    List<string> AllowedSymbolSpends,
    List<string> RejectedSymbolSpends,
    List<string> Log);

/// <summary>Выбор активной брони (ROT-CMB-02). <c>null</c> снимает выбор.</summary>
public record SetActiveArmorRequest(Guid? CharacterItemId);
