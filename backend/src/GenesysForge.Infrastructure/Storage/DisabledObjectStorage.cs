using GenesysForge.Application.Abstractions;

namespace GenesysForge.Infrastructure.Storage;

/// <summary>
/// Заглушка на случай, когда хранилище не сконфигурировано (<c>Storage:Provider=None</c>):
/// локальная разработка и тесты. Загрузка отклоняется, удаление — no-op.
/// </summary>
public sealed class DisabledObjectStorage : IObjectStorage
{
    public bool IsEnabled => false;

    public Task<string> UploadPublicAsync(Stream content, string key, string contentType, CancellationToken ct) =>
        throw new InvalidOperationException("Хранилище файлов не настроено (Storage:Provider=None).");

    public Task DeleteByUrlAsync(string? url, CancellationToken ct) => Task.CompletedTask;
}
