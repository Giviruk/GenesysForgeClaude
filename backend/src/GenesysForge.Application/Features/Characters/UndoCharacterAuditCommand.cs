using GenesysForge.Application.Abstractions;

namespace GenesysForge.Application.Features.Characters;

/// <summary>Отменяет конкретную покупку навыка или таланта, выбранную в истории персонажа.</summary>
public record UndoCharacterAuditCommand(Guid UserId, Guid CharacterId, Guid AuditEntryId) : ICommand<Unit>;
