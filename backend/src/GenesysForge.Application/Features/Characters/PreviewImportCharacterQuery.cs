using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Characters;

public record PreviewImportCharacterQuery(Guid UserId, CharacterExportDto Payload, Guid? CampaignId = null) : IQuery<ImportPreviewDto>;
