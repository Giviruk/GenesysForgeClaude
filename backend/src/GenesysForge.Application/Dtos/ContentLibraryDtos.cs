using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Dtos;

public record ContentEntryRef(CustomEntryType EntryType, Guid EntryId);
public record ContentEntriesRequest(IReadOnlyList<ContentEntryRef> Entries);
public record PackDetailsRequest(string Name, string? Description, GameSystem System);
public record UpdatePackDetailsRequest(string Name, string? Description);
public record BaseContentRef(BaseContentCategory Category, string Key);
public record PackExclusionsRequest(IReadOnlyList<BaseContentRef> Items);
public record BaseOverrideInput(BaseContentCategory Category, string Key, bool? Enabled);
public record BaseOverridesRequest(GameSystem System, IReadOnlyList<BaseOverrideInput> Items);
public record SaveRestrictionsRequest(GameSystem System, string? Name = null);
public record SystemOpenRequest(bool IsOpen);
public record CampaignPackEntryInput(CustomEntryType EntryType, Guid EntryId, bool Enabled);
public record CampaignPackEntriesRequest(IReadOnlyList<CampaignPackEntryInput> Entries);
public record ContentProposalRequest(Guid? PackId = null, CustomEntryType? EntryType = null, Guid? EntryId = null);

public record LibraryEntryDto(CustomEntryType EntryType, Guid Id, GameSystem System, string Name,
    string NameRu, string Meta, DateTime? LastEditedAt, IReadOnlyList<Guid> PackIds);
public record PackCampaignDto(Guid Id, string Name);
public record BaseCatalogEntryDto(BaseContentCategory Category, string Key, string Name, string NameRu,
    string Meta, bool IsSharedWithCore);
public record CampaignBaseEntryDto(BaseContentCategory Category, string Key, string Name, string NameRu,
    string Meta, bool IsSharedWithCore, bool Enabled, string? Source, string? SourcePackName, IReadOnlyList<string> UsedBy);
public record CampaignPackEntryDto(CustomEntryType EntryType, Guid EntryId, string Name, string NameRu, string State);
public record CampaignContentItemDto(Guid Id, CustomEntryType EntryType, Guid EntryId, GameSystem System,
    string Name, string NameRu, string Meta, bool IsEnabled, ContentConnectionStatus Status,
    string OwnerName, bool IsMine, Guid? ProposedBy);
public record CampaignContentCategoryDto(BaseContentCategory Category, int Total, int Enabled);
public record CampaignSystemContentDto(GameSystem System, bool IsOpen, IReadOnlyList<PackCampaignDto> Characters,
    IReadOnlyList<CampaignContentCategoryDto> Categories);
public record CampaignContentAlertDto(string Kind, Guid? TargetId, GameSystem? System = null, int Count = 0, IReadOnlyList<string>? UsedBy = null, IReadOnlyList<BaseCatalogEntryDto>? Entries = null);
public record CampaignContentDto(IReadOnlyList<CampaignSystemContentDto> Systems, IReadOnlyList<CampaignHomebrewPackDto> Packs,
    IReadOnlyList<CampaignContentItemDto> Items, int OverrideCount, IReadOnlyList<CampaignContentAlertDto> Alerts, int AvailableCount);
public record LibraryProposalDto(Guid CampaignId, string CampaignName, string Kind, Guid TargetId, ContentConnectionStatus Status);
