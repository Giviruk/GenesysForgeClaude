using GenesysForge.Application.Abstractions;

namespace GenesysForge.Application.Features.Characters;

public record SetHeroicUpgradeRankCommand(Guid UserId, Guid CharacterId, int Rank) : ICommand<Unit>;
