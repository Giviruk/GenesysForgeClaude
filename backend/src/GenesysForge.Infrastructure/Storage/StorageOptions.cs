namespace GenesysForge.Infrastructure.Storage;

/// <summary>
/// Настройки хранилища файлов (секция <c>Storage</c>). <see cref="Provider"/> выбирает реализацию
/// <see cref="GenesysForge.Application.Abstractions.IObjectStorage"/>: <c>S3</c> — S3-совместимое
/// хранилище, иначе — заглушка, при которой загрузка файлов отключена.
/// </summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Провайдер: <c>S3</c> или <c>None</c> (по умолчанию загрузка выключена).</summary>
    public string Provider { get; set; } = "None";

    public S3Options S3 { get; set; } = new();

    public bool UsesS3 => string.Equals(Provider, "S3", StringComparison.OrdinalIgnoreCase);

    public sealed class S3Options
    {
        /// <summary>Endpoint хранилища, например <c>https://s3.ru1.storage.beget.cloud</c>.</summary>
        public string ServiceUrl { get; set; } = "";

        public string Bucket { get; set; } = "";
        public string AccessKey { get; set; } = "";
        public string SecretKey { get; set; } = "";

        /// <summary>Регион для подписи SigV4. Для Beget — <c>ru1</c>.</summary>
        public string Region { get; set; } = "ru1";

        /// <summary>
        /// Префикс всех объектов этого стека, например <c>genesysforge/uploads/private</c>.
        /// Бакет может быть общим с другими проектами, поэтому пишем строго под своим префиксом
        /// и удаляем только то, что лежит под ним.
        /// </summary>
        public string Prefix { get; set; } = "genesysforge/uploads";

        /// <summary>
        /// База для публичных URL (без имени бакета). Пусто → берётся <see cref="ServiceUrl"/>.
        /// Задаётся, если перед хранилищем стоит CDN.
        /// </summary>
        public string? PublicBaseUrl { get; set; }
    }
}
