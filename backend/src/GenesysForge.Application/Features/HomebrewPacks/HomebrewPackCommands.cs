using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.HomebrewPacks;

public record GetHomebrewPacksQuery(Guid UserId) : IQuery<List<HomebrewPackListItemDto>>;
public record ExportHomebrewPackQuery(Guid UserId, Guid PackId) : IQuery<HomebrewPackExportDto>;
public record ImportHomebrewPackCommand(Guid UserId, HomebrewPackExportDto Document) : ICommand<HomebrewPackImportResult>;
public record ShareHomebrewPackCommand(Guid UserId, Guid PackId) : ICommand<HomebrewPackShareDto>;
public record ImportSharedHomebrewPackCommand(Guid UserId, string Token) : ICommand<HomebrewPackImportResult>;
public record SetHomebrewPackDefaultCommand(Guid UserId, Guid PackId, bool IsEnabled) : ICommand<Unit>;
public record SetCharacterHomebrewPackCommand(Guid UserId, Guid CharacterId, Guid PackId, bool IsEnabled) : ICommand<Unit>;
public record SetCampaignHomebrewPackCommand(Guid UserId, Guid CampaignId, Guid PackId, bool IsEnabled) : ICommand<Unit>;
