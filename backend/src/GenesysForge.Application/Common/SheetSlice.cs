namespace GenesysForge.Application.Common;

/// <summary>
/// Часть листа персонажа, которую можно запросить отдельно.
///
/// <para>
/// Лист играющего персонажа весит около 116 КБ, и 64 % из них — инвентарь, который главной вкладке
/// не нужен вовсе. Поэтому и чтение, и ответ на правку называют нужные части поимённо: вкладка
/// берёт свою и не платит за чужие.
/// </para>
///
/// <para>
/// Отсутствие среза в ответе значит «не загружен», а не «пусто»: в DTO это <c>null</c>, а не пустой
/// список. Путать эти два состояния нельзя — иначе вкладка нарисует «Пусто» вместо загрузки.
/// </para>
/// </summary>
[Flags]
public enum SheetSlice
{
    None = 0,
    /// <summary>Характеристики, пороги, навыки, героика, ранения, деньги, опыт.</summary>
    Base = 1,
    Items = 2,
    Talents = 4,
    Mounts = 8,
    Attachments = 16,
    /// <summary>Весь лист: печать, экспорт, публичная ссылка, просмотр ведущим.</summary>
    All = Base | Items | Talents | Mounts | Attachments,
}

public static class SheetSlices
{
    /// <summary>
    /// Разбирает список срезов из запроса («base,items»). Пусто или неразобранное — весь лист:
    /// так отвечали до разделения, и клиент, который о срезах не знает, ничего не теряет.
    /// </summary>
    public static SheetSlice Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return SheetSlice.All;
        var result = SheetSlice.None;
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            // Только имена: `Enum.TryParse` разобрал бы и «2» как набор флагов, а число в запросе —
            // это опечатка клиента, а не просьба про инвентарь.
            if (char.IsLetter(part[0])
                && Enum.TryParse<SheetSlice>(part, ignoreCase: true, out var slice)
                && slice != SheetSlice.None)
                result |= slice;
        return result == SheetSlice.None ? SheetSlice.All : result;
    }

    /// <summary>Запрошен хотя бы один из перечисленных срезов.</summary>
    public static bool HasAny(this SheetSlice slices, SheetSlice any) => (slices & any) != 0;
}
