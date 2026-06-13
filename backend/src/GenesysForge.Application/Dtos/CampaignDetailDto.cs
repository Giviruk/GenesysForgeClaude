namespace GenesysForge.Application.Dtos;

public record CampaignDetailDto(
    Guid Id,
    string Name,
    string Description,
    bool IsGm,
    /// <summary>Код присоединения виден только GM.</summary>
    string? JoinCode,
    List<CampaignMemberDto> Members,
    List<CampaignNoteDto> Notes);
