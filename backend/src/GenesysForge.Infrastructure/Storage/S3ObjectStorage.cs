using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using GenesysForge.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenesysForge.Infrastructure.Storage;

/// <summary>
/// S3-совместимое хранилище (Beget Cloud Storage — Ceph RGW). Все объекты пишутся под
/// <see cref="StorageOptions.S3Options.Prefix"/>, потому что бакет делится с другими проектами.
/// </summary>
public sealed class S3ObjectStorage : IObjectStorage, IDisposable
{
    private readonly StorageOptions.S3Options _options;
    private readonly ILogger<S3ObjectStorage> _logger;
    private readonly AmazonS3Client _client;
    private readonly string _prefix;
    private readonly string _publicBase;

    public S3ObjectStorage(IOptions<StorageOptions> options, ILogger<S3ObjectStorage> logger)
    {
        _options = options.Value.S3;
        _logger = logger;
        _prefix = _options.Prefix.Trim('/');

        var config = new AmazonS3Config
        {
            ServiceURL = _options.ServiceUrl,
            // Beget адресует бакеты путём, а не поддоменом.
            ForcePathStyle = true,
            AuthenticationRegion = _options.Region,
            // AWS SDK v4 по умолчанию добавляет CRC-checksum к каждому запросу; Ceph RGW его отвергает.
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };

        _client = new AmazonS3Client(new BasicAWSCredentials(_options.AccessKey, _options.SecretKey), config);
        _publicBase = (string.IsNullOrWhiteSpace(_options.PublicBaseUrl)
            ? _options.ServiceUrl
            : _options.PublicBaseUrl!).TrimEnd('/');
    }

    public bool IsEnabled => true;

    public async Task<string> UploadPublicAsync(Stream content, string key, string contentType, CancellationToken ct)
    {
        var fullKey = $"{_prefix}/{key.TrimStart('/')}";

        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.Bucket,
            Key = fullKey,
            InputStream = content,
            ContentType = contentType,
            // Бакет приватен по умолчанию; картинки должны читаться браузером напрямую.
            CannedACL = S3CannedACL.PublicRead,
        }, ct);

        return $"{_publicBase}/{_options.Bucket}/{fullKey}";
    }

    public async Task DeleteByUrlAsync(string? url, CancellationToken ct)
    {
        var key = TryGetOwnKey(url);
        if (key is null) return;

        try
        {
            await _client.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = _options.Bucket,
                Key = key,
            }, ct);
        }
        catch (AmazonS3Exception ex)
        {
            // Осиротевший объект не повод валить запрос пользователя.
            _logger.LogWarning(ex, "Не удалось удалить объект {Key} из S3", key);
        }
    }

    /// <summary>
    /// Возвращает ключ, только если URL указывает на наш бакет И лежит под нашим префиксом.
    /// Так пользователь не сможет чужой ссылкой заставить нас удалить посторонний объект.
    /// </summary>
    private string? TryGetOwnKey(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        var expectedStart = $"{_publicBase}/{_options.Bucket}/";
        if (!url.StartsWith(expectedStart, StringComparison.Ordinal)) return null;

        var key = url[expectedStart.Length..];
        return key.StartsWith($"{_prefix}/", StringComparison.Ordinal) ? key : null;
    }

    public void Dispose() => _client.Dispose();
}
