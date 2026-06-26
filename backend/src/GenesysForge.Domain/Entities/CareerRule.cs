namespace GenesysForge.Domain.Entities;

/// <summary>
/// Структурное правило/заметка карьеры (например подсказка по замене навыка). Отображается при выборе
/// карьеры; исполнение/автоматизация — вне рамок (см. <see cref="CareerRuleKind"/>).
/// </summary>
public class CareerRule
{
    public Guid Id { get; set; }
    public Guid CareerId { get; set; }
    public string Code { get; set; } = "";
    public CareerRuleKind Kind { get; set; } = CareerRuleKind.Advisory;
    public string Description { get; set; } = "";
}
