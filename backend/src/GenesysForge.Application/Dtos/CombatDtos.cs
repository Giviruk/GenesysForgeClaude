namespace GenesysForge.Application.Dtos;

/// <summary>Дополнительный удар той же атаки: у каждого своё поглощение (ROT-CMB-01).</summary>
public record AttackHitRequest(int TargetSoak, string Label = "");

/// <summary>Трата символа, которую клиент хочет применить.</summary>
public record AttackSpendRequest(
    string Code,
    bool MayActivateOnMiss = false,
    bool RequiresDamageThroughSoak = false);

/// <summary>
/// Качество атакующего профиля в запросе: только код справочника и рейтинг. Механику по коду
/// подставляет сервер (GEN-EQP-QUAL-01) — клиент не может объявить своё Проникающее.
/// </summary>
public record AttackQualityRequest(string Code, int Rating = 0);

/// <summary>Запрос разрешения атаки. Нетто-символы уже сокращены клиентом при разборе броска.</summary>
public record ResolveAttackRequest(
    int NetSuccesses,
    int BaseDamage,
    int TargetSoak,
    int NetAdvantages = 0,
    int Triumphs = 0,
    int Despairs = 0,
    List<AttackHitRequest>? AdditionalHits = null,
    List<AttackSpendRequest>? Spends = null,
    /// <summary>Качества оружия: из них считаются игнорируемое поглощение и прибавка к криту.</summary>
    List<AttackQualityRequest>? Qualities = null,
    /// <summary>У цели укреплённая броня: Проникающее и Бронебойное её поглощение не снимают.</summary>
    bool TargetReinforced = false);

/// <summary>
/// Урон одного удара после поглощения.
/// </summary>
/// <param name="TargetSoak">Поглощение цели уже после Проникающего и Бронебойного.</param>
/// <param name="IgnoredSoak">Сколько поглощения сняли качества оружия.</param>
public record AttackHitDto(string Label, int RawDamage, int TargetSoak, int Applied, int IgnoredSoak = 0);

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
    List<string> Log,
    /// <summary>Прибавка к броску критического ранения от Высококритичного.</summary>
    int CriticalRollBonus = 0);

/// <summary>Выбор активной брони (ROT-CMB-02). <c>null</c> снимает выбор.</summary>
public record SetActiveArmorRequest(Guid? CharacterItemId);

/// <summary>
/// Метнуть оружие или подобрать его обратно (ROT-WPN-01). Экземпляр не исчезает: он недоступен,
/// пока лежит у цели.
/// </summary>
public record SetItemThrownRequest(bool IsThrown);
