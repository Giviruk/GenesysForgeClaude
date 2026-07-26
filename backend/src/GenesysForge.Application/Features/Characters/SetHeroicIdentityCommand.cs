using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Characters;

public record SetHeroicIdentityCommand(Guid UserId, Guid CharacterId, SetHeroicIdentityRequest Request)
    : ICommand<Unit>;

/// <summary>Бросок по таблице происхождения выполняется сервером: клиент не присылает грани.</summary>
public record RollHeroicOriginCommand(Guid UserId, Guid CharacterId) : ICommand<HeroicOriginRollDto>;
