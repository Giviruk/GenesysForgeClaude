using GenesysForge.Application.Media;
using GenesysForge.Domain;

namespace GenesysForge.Api.Endpoints;

/// <summary>
/// Чтение загружаемого файла из сырого тела запроса. Файл шлётся как есть (не multipart):
/// это избавляет minimal API от antiforgery-требований form-биндинга, а формат всё равно
/// определяется по сигнатуре содержимого, не по заголовкам.
/// </summary>
public static class UploadBody
{
    /// <summary>Читает тело запроса, обрывая чтение при превышении лимита размера изображения.</summary>
    public static async Task<byte[]> ReadImageAsync(HttpRequest request, CancellationToken ct)
    {
        // Content-Length может отсутствовать или врать — лимит контролируем при чтении.
        using var buffer = new MemoryStream();
        var chunk = new byte[64 * 1024];
        int read;
        while ((read = await request.Body.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > ImageSignature.MaxBytes)
                throw new DomainRuleException($"Файл больше {ImageSignature.MaxBytes / (1024 * 1024)} МБ.");
            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
    }
}
