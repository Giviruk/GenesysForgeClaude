using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain.Rules;
using Microsoft.EntityFrameworkCore;

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

        group.MapPost("/resolve-attack", async (
            ResolveAttackRequest req, IAppDbContext db, CancellationToken ct) =>
        {
            // Клиент присылает только коды качеств; что каждое из них делает, знает справочник
            // (GEN-EQP-QUAL-01). Неизвестный код молча игнорируется, а не считается как правило.
            var requested = req.Qualities ?? [];
            var codes = requested.Select(q => q.Code).ToList();
            var defs = codes.Count == 0
                ? []
                : await db.QualityDefs.AsNoTracking()
                    .Where(q => codes.Contains(q.Code))
                    .ToDictionaryAsync(q => q.Code, StringComparer.Ordinal, ct);

            var qualities = requested
                .Where(q => defs.ContainsKey(q.Code))
                .Select(q => new WeaponQualityInput(
                    defs[q.Code].NameEn, defs[q.Code].NameRu, defs[q.Code].EffectKind, q.Rating))
                .ToList();

            var result = CombatResolver.Resolve(new CombatAttackInput(
                req.NetSuccesses,
                req.BaseDamage,
                req.TargetSoak,
                req.NetAdvantages,
                req.Triumphs,
                req.Despairs,
                [.. (req.AdditionalHits ?? []).Select(h => new CombatHitInput(h.TargetSoak, h.Label))],
                [.. (req.Spends ?? []).Select(s =>
                    new CombatSymbolSpend(s.Code, s.MayActivateOnMiss, s.RequiresDamageThroughSoak))],
                qualities,
                req.TargetReinforced));

            return Results.Ok(new ResolveAttackResponse(
                result.IsHit,
                result.RawDamagePerHit,
                [.. result.Hits.Select(h => new AttackHitDto(
                    h.Label, h.RawDamage, h.TargetSoak, h.Applied, h.IgnoredSoak))],
                result.TotalApplied,
                [.. result.AllowedSymbolSpends],
                [.. result.RejectedSymbolSpends],
                [.. result.Log],
                result.CriticalRollBonus));
        });
    }
}
