namespace GenesysForge.Domain;

/// <summary>Тип противника по правилам Genesys.</summary>
public enum NpcKind
{
    /// <summary>Миньон — слабый противник, обычно без порога усталости.</summary>
    Minion = 0,
    /// <summary>Ривал — средний NPC с навыками и снаряжением.</summary>
    Rival = 1,
    /// <summary>Немезида — главный NPC/босс, имеет порог усталости.</summary>
    Nemesis = 2,
}
