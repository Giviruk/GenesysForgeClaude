using GenesysForge.Application.Dtos;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Api.Endpoints;

/// <summary>
/// Разрешение атаки на сервере (ROT-CMB-01). Клиентский расчёт остаётся только подсказкой:
/// попадание, урон и допустимость трат символов считает эта конечная точка.
/// </summary>
public static class CombatEndpoints
{
    public static void MapCombat(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/combat").RequireAuthorization();

        group.MapPost("/resolve-attack", (ResolveAttackRequest req) =>
        {
            var result = CombatResolver.Resolve(new CombatAttackInput(
                req.NetSuccesses,
                req.BaseDamage,
                req.TargetSoak,
                req.NetAdvantages,
                req.Triumphs,
                req.Despairs,
                [.. (req.AdditionalHits ?? []).Select(h => new CombatHitInput(h.TargetSoak, h.Label))],
                [.. (req.Spends ?? []).Select(s =>
                    new CombatSymbolSpend(s.Code, s.MayActivateOnMiss, s.RequiresDamageThroughSoak))]));

            return Results.Ok(new ResolveAttackResponse(
                result.IsHit,
                result.RawDamagePerHit,
                [.. result.Hits.Select(h => new AttackHitDto(h.Label, h.RawDamage, h.TargetSoak, h.Applied))],
                result.TotalApplied,
                [.. result.AllowedSymbolSpends],
                [.. result.RejectedSymbolSpends],
                [.. result.Log]));
        });
    }
}
