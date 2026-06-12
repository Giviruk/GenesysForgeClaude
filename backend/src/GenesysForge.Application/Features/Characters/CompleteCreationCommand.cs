using GenesysForge.Application.Abstractions;

namespace GenesysForge.Application.Features.Characters;

/// <summary>Завершает фазу создания: фиксирует характеристики и снимает лимит рангов навыков.</summary>
public record CompleteCreationCommand(Guid UserId, Guid CharacterId) : ICommand<Unit>;
