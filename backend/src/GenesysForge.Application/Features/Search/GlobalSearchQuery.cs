using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Application.Features.Search;

/// <summary>Глобальный поиск: справочник правил + контент системы + NPC/персонажи пользователя.</summary>
public record GlobalSearchQuery(Guid UserId, GameSystem System, string Query) : IQuery<SearchResponse>;
