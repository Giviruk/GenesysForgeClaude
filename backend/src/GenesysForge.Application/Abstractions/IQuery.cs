namespace GenesysForge.Application.Abstractions;

/// <summary>Запрос CQRS — читает состояние без изменений.</summary>
public interface IQuery<TResult>;
