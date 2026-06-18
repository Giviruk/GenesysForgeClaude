namespace GenesysForge.Domain;

/// <summary>Ориентировочная сложность сцены. В Genesys баланс нарративный, поэтому значение справочное.</summary>
public enum ThreatLevel
{
    Trivial = 0,
    Easy = 1,
    Standard = 2,
    Hard = 3,
    Deadly = 4,
}
