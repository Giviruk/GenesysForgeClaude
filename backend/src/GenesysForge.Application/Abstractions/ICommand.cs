namespace GenesysForge.Application.Abstractions;

/// <summary>Команда CQRS — изменяет состояние и возвращает TResult.</summary>
public interface ICommand<TResult>;
