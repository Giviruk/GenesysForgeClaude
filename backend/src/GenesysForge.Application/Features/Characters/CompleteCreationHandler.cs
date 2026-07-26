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
                "Для персонажа Realms of Terrinoth выберите героическую способность до завершения создания.");
        c.IsCreationPhase = false;

        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.CreationCompleted,
            "Создание персонажа завершено");

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
