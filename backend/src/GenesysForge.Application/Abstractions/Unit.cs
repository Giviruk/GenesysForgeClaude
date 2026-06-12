namespace GenesysForge.Application.Abstractions;

/// <summary>Пустой результат команды.</summary>
public readonly record struct Unit
{
    public static readonly Unit Value = new();
}
