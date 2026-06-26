namespace GenesysForge.Application.Dtos;

/// <summary>Результаты глобального поиска по справочнику правил, контенту и сущностям пользователя.</summary>
public record SearchResponse(List<SearchHitDto> Hits);

/// <summary>
/// Один результат поиска. <see cref="Type"/> — категория (rule/skill/item/npc/character/...),
/// <see cref="Route"/> — клиентский путь для перехода (или пусто, если перехода нет).
/// </summary>
public record SearchHitDto(
    string Type, string Group, string Title, string Subtitle, string Snippet, string Route);
