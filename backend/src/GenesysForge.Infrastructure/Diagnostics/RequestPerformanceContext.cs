using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GenesysForge.Infrastructure.Diagnostics;

/// <summary>Собирает метрики обращений к БД в пределах одного HTTP-запроса.</summary>
public sealed class RequestPerformanceContext
{
    private long _databaseTicks;
    private int _databaseCommandCount;

    public int DatabaseCommandCount => Volatile.Read(ref _databaseCommandCount);
    public TimeSpan DatabaseDuration => TimeSpan.FromTicks(Interlocked.Read(ref _databaseTicks));

    internal void Record(TimeSpan duration)
    {
        Interlocked.Increment(ref _databaseCommandCount);
        Interlocked.Add(ref _databaseTicks, duration.Ticks);
    }
}

/// <summary>Учитывает только реально выполненные relational-команды EF Core.</summary>
public sealed class RequestDbCommandInterceptor(RequestPerformanceContext performance) : DbCommandInterceptor
{
    private T Record<T>(CommandExecutedEventData eventData, T result)
    {
        performance.Record(eventData.Duration);
        return result;
    }

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result) =>
        Record(eventData, result);

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Record(eventData, result));

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result) =>
        Record(eventData, result);

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, int result,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Record(eventData, result));

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result) =>
        Record(eventData, result);

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, object? result,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Record(eventData, result));
}
