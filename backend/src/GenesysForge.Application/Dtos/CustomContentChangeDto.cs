using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Dtos;

public record CustomContentFieldChangeDto(string Field, string? From, string? To);
public record CustomContentChangeDto(Guid Id, Guid? HomebrewPackId, string DefinitionType,
    Guid DefinitionId, string DefinitionName, Guid UserId, string UserName,
    CustomContentChangeAction Action, List<CustomContentFieldChangeDto> Changes, DateTime CreatedAt);
