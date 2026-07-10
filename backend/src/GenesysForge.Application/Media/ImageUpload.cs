using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;

namespace GenesysForge.Application.Media;

/// <summary>Общие проверки загружаемого изображения — одинаковы для аватара и портрета персонажа.</summary>
public static class ImageUpload
{
    public static void Validate(IObjectStorage storage, byte[] content, out string contentType, out string extension)
    {
        if (!storage.IsEnabled)
            throw new DomainRuleException("Загрузка изображений недоступна: хранилище не настроено.");

        if (content.Length == 0)
            throw new DomainRuleException("Файл пуст.");

        if (content.Length > ImageSignature.MaxBytes)
            throw new DomainRuleException($"Файл больше {ImageSignature.MaxBytes / (1024 * 1024)} МБ.");

        if (!ImageSignature.TryDetect(content, out contentType, out extension))
            throw new DomainRuleException("Поддерживаются только изображения JPEG, PNG и WebP.");
    }
}
