namespace GenesysForge.Application.Abstractions;

/// <summary>
/// Источник случайности для серверных бросков (стартовые деньги и т. п.).
/// Инъецируется, чтобы бросок был воспроизводим в тестах, а результат — записываем в audit.
/// </summary>
public interface IDiceRoller
{
    /// <summary>Один бросок кости с гранями <paramref name="sides"/>; результат в диапазоне 1..sides.</summary>
    int Roll(int sides);
}
