namespace GenesysForge.Application.Media;

/// <summary>
/// Определение типа изображения по сигнатуре содержимого. Заголовок <c>Content-Type</c> и расширение
/// из запроса не используются: клиент может прислать любые. Разрешены только JPEG, PNG и WebP —
/// форматы без активного содержимого (в отличие от SVG, который умеет исполнять скрипты).
/// </summary>
public static class ImageSignature
{
    /// <summary>Максимальный размер загружаемого изображения.</summary>
    public const long MaxBytes = 5 * 1024 * 1024;

    /// <summary>
    /// Распознаёт формат по первым байтам. Возвращает false, если формат не входит в белый список.
    /// </summary>
    public static bool TryDetect(ReadOnlySpan<byte> content, out string contentType, out string extension)
    {
        if (IsJpeg(content)) { contentType = "image/jpeg"; extension = "jpg"; return true; }
        if (IsPng(content)) { contentType = "image/png"; extension = "png"; return true; }
        if (IsWebp(content)) { contentType = "image/webp"; extension = "webp"; return true; }

        contentType = string.Empty;
        extension = string.Empty;
        return false;
    }

    private static bool IsJpeg(ReadOnlySpan<byte> c) =>
        c.Length >= 3 && c[0] == 0xFF && c[1] == 0xD8 && c[2] == 0xFF;

    private static bool IsPng(ReadOnlySpan<byte> c) =>
        c.Length >= 8 && c[0] == 0x89 && c[1] == 0x50 && c[2] == 0x4E && c[3] == 0x47
                      && c[4] == 0x0D && c[5] == 0x0A && c[6] == 0x1A && c[7] == 0x0A;

    // RIFF....WEBP — размер файла в байтах 4..7 не проверяем.
    private static bool IsWebp(ReadOnlySpan<byte> c) =>
        c.Length >= 12 && c[0] == 0x52 && c[1] == 0x49 && c[2] == 0x46 && c[3] == 0x46
                       && c[8] == 0x57 && c[9] == 0x45 && c[10] == 0x42 && c[11] == 0x50;
}
