using GenesysForge.Application.Abstractions;

namespace GenesysForge.Infrastructure;

/// <summary>Боевой источник случайности: криптостойкий RNG без смещения по модулю.</summary>
public sealed class SystemDiceRoller : IDiceRoller
{
    public int Roll(int sides)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sides, 2);
        return System.Security.Cryptography.RandomNumberGenerator.GetInt32(1, sides + 1);
    }
}
