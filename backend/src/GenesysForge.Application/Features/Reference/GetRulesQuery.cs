using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Reference;

/// <summary>Справочные таблицы правил (системо-независимы; опц. фильтр по подстроке).</summary>
public record GetRulesQuery(string? Query = null) : IQuery<RulesResponse>;
