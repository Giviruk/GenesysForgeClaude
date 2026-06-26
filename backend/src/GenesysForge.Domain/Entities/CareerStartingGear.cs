namespace GenesysForge.Domain.Entities;

/// <summary>
/// Стартовое снаряжение карьеры. Фиксированные строки (<see cref="IsChoice"/> = false) выдаются в
/// инвентарь автоматически при создании; варианты выбора группируются по <see cref="ChoiceGroup"/>,
/// внутри группы один выбираемый набор — это все строки с одинаковым <see cref="ChoiceOption"/>.
/// </summary>
public class CareerStartingGear
{
    public Guid Id { get; set; }
    public Guid CareerId { get; set; }
    /// <summary>Bare-slug кода предмета (совпадает с суффиксом <c>ItemDef.Code</c> = <c>{sys}.item.{ItemCode}</c>).</summary>
    public string ItemCode { get; set; } = "";
    /// <summary>RU-имя предмета на случай, если код не резолвится (страховка для отображения).</summary>
    public string ItemNameFallback { get; set; } = "";
    public int Quantity { get; set; } = 1;
    /// <summary>Строка относится к слоту выбора, а не к фиксированной выдаче.</summary>
    public bool IsChoice { get; set; }
    /// <summary>Идентификатор слота выбора (например «slot-1»).</summary>
    public string ChoiceGroup { get; set; } = "";
    /// <summary>Индекс выбираемого варианта внутри слота; строки с одним индексом образуют один набор.</summary>
    public int ChoiceOption { get; set; }
}
