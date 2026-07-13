using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Dtos;

/// <summary>Справочные таблицы правил (плоский список; группировка по Kind — на клиенте).</summary>
public record RulesResponse(List<RuleTableEntryDto> Entries);

public record RuleTableEntryDto(
    Guid Id, RuleTableKind Kind, string Code, string NameRu, string NameEn, string GroupRu,
    int SortOrder, string RollRange, string SymbolCost, string Body, string Notes,
    string Source, string SourcePage,
    string GroupEn = "", string BodyEn = "", string NotesEn = "");
