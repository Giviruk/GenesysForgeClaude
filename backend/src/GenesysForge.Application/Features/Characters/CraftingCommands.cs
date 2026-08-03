using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Characters;

/// <summary>Предпросмотр проекта: сложность, время и стоимость до любой записи в базу.</summary>
public record PreviewCraftingQuery(Guid UserId, Guid CharacterId, CraftingProjectInput Request)
    : IQuery<CraftingPreviewDto>;

/// <summary>Начать проект изготовления, варки или зачарования.</summary>
public record StartCraftingCommand(Guid UserId, Guid CharacterId, CraftingProjectInput Request) : ICommand<Guid>;

/// <summary>Разрешить проект: символы броска и распределение трат.</summary>
public record ResolveCraftingCommand(
    Guid UserId, Guid CharacterId, Guid ProjectId, CraftingResolveInput Request) : ICommand<CraftingProjectDto>;

/// <summary>Отменить незавершённый проект.</summary>
public record CancelCraftingCommand(Guid UserId, Guid CharacterId, Guid ProjectId) : ICommand<Unit>;

/// <summary>Проекты персонажа со всеми тратами.</summary>
public record GetCraftingProjectsQuery(Guid UserId, Guid CharacterId) : IQuery<List<CraftingProjectDto>>;
