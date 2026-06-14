using System.Reflection;
using System.Text.Json;

namespace GenesysForge.Infrastructure.Persistence;

/// <summary>
/// Источник полных (private) описаний справочного контента. Тексты вынесены в файлы
/// <c>private-content/*.ru.json</c> (подключены как embedded resource), чтобы их можно было
/// удалить/вынести перед публичным открытием репозитория, не трогая код.
/// Формат файла: <c>{ "system": "...", "descriptions": { "&lt;code&gt;": "полное описание", ... } }</c>.
/// Если файлов нет (удалены перед публикацией) — store пуст и private-сид падает на safe-описания.
/// </summary>
public sealed class PrivateContentStore
{
    private readonly IReadOnlyDictionary<string, string> _byCode;

    private PrivateContentStore(IReadOnlyDictionary<string, string> byCode) => _byCode = byCode;

    /// <summary>Полное описание по стабильному коду или <c>null</c>, если его нет в private-наборе.</summary>
    public string? Get(string code) =>
        !string.IsNullOrEmpty(code) && _byCode.TryGetValue(code, out var desc) ? desc : null;

    /// <summary>Число загруженных приватных описаний (для диагностики/тестов).</summary>
    public int Count => _byCode.Count;

    /// <summary>Загружает private-описания из embedded resource'ов <c>*.ru.json</c> сборки Infrastructure.</summary>
    public static PrivateContentStore Load(Assembly? assembly = null)
    {
        assembly ??= typeof(PrivateContentStore).Assembly;
        var map = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var name in assembly.GetManifestResourceNames())
        {
            if (!name.EndsWith(".ru.json", StringComparison.OrdinalIgnoreCase)) continue;
            using var stream = assembly.GetManifestResourceStream(name);
            if (stream is null) continue;

            using var doc = JsonDocument.Parse(stream);
            if (!doc.RootElement.TryGetProperty("descriptions", out var descriptions)) continue;

            foreach (var entry in descriptions.EnumerateObject())
            {
                var text = entry.Value.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                    map[entry.Name] = text!; // последний файл побеждает при коллизии кода
            }
        }

        return new PrivateContentStore(map);
    }
}
