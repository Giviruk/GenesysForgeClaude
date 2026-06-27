namespace GenesysForge.Domain.Rules;

/// <summary>
/// Результат валидации статблока NPC: <see cref="Errors"/> блокируют сохранение,
/// <see cref="Warnings"/> показываются мастеру, но не блокируют (см. правила adversary, U-15).
/// </summary>
public sealed class NpcValidationResult
{
    public List<string> Errors { get; } = [];
    public List<string> Warnings { get; } = [];

    public bool IsValid => Errors.Count == 0;

    public void Error(string message) => Errors.Add(message);
    public void Warn(string message) => Warnings.Add(message);
}
