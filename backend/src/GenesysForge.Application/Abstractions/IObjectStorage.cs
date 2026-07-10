namespace GenesysForge.Application.Abstractions;

/// <summary>
/// Хранилище бинарных объектов (S3-совместимое). Вызывающий код задаёт относительный ключ
/// (например <c>avatars/{userId}/{guid}.jpg</c>); реализация добавляет префикс своего стека,
/// чтобы PrivateFull, PublicSafe и посторонние проекты в общем бакете не пересекались.
/// </summary>
public interface IObjectStorage
{
    /// <summary>Настроено ли хранилище. False → загрузка файлов недоступна (провайдер <c>None</c>).</summary>
    bool IsEnabled { get; }

    /// <summary>Кладёт объект с публичным доступом на чтение и возвращает его абсолютный URL.</summary>
    Task<string> UploadPublicAsync(Stream content, string key, string contentType, CancellationToken ct);

    /// <summary>
    /// Удаляет объект по ранее выданному URL. URL, не принадлежащий нашему бакету и префиксу,
    /// молча игнорируется — пользователь мог вписать ссылку на чужую картинку вручную.
    /// </summary>
    Task DeleteByUrlAsync(string? url, CancellationToken ct);
}
