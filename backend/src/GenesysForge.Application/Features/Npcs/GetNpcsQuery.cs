using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Application.Features.Npcs;

/// <summary>Запрос библиотеки NPC с фильтрами. Пустые значения фильтра игнорируются.</summary>
public record GetNpcsQuery(
    Guid UserId,
    string? Search = null,
    GameSystem? System = null,
    NpcKind? Kind = null,
    NpcRole? Role = null,
    Guid? CampaignId = null,
    string? Tag = null,
    string? Sort = null) : IQuery<List<NpcListItemDto>>;
