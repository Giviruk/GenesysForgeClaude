using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Features.Characters;

public class CompleteCreationHandler(IAppDbContext db) : ICommandHandler<CompleteCreationCommand, Unit>
{
    public async Task<Unit> Handle(CompleteCreationCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        if (!c.IsCreationPhase) return Unit.Value; // идемпотентно: повторный вызов не плодит записи
        if (c.System == GameSystem.RealmsOfTerrinoth && c.HeroicAbilityId is null)
            throw new DomainRuleException(
                "Для персонажа Realms of Terrinoth выберите героическую способность до завершения создания.",
                "heroic.ability.required");
        // Личное название и происхождение — обязательная часть способности (ROT-HA-01): после
        // completion они неизменяемы, поэтому проверяются до фиксации порогов и смены фазы.
        HeroicIdentityGate.EnsureComplete(c);
        // Параметр primary effect (Paragon / Sixth Sense / Signature Weapon) обязателен там же.
        HeroicConfigurationGate.EnsureComplete(c);
        // Пороги фиксируются в той же транзакции до переключения фазы; повторный вызов сюда не
        // доходит (idempotent-выход выше), поэтому snapshot делается ровно один раз.
        var (wound, strain) = CharacterDerived.CreationSnapshot(c);
        c.CreationWoundThreshold = wound;
        c.CreationStrainThreshold = strain;
        c.ThresholdSnapshotProvenance = ThresholdSnapshotProvenance.CreationCompleted;
        c.IsCreationPhase = false;

        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.CreationCompleted,
            "Создание персонажа завершено",
            data: new { woundThreshold = wound, strainThreshold = strain, brawn = c.Brawn, willpower = c.Willpower });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
