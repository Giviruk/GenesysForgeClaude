namespace GenesysForge.Domain;

/// <summary>
/// Структурная категория происхождения героической способности (ROT-HA-01, таблица d10).
/// Значение перечисления равно грани кости, поэтому таблица не дублируется отдельной картой.
/// Специальный результат «0» категорией не является: он означает «бросить ещё два раза»
/// и в этом виде не хранится (см. <see cref="Rules.HeroicOriginTable"/>).
/// </summary>
public enum HeroicOriginType
{
    /// <summary>1 — наследственная сила или особая кровь.</summary>
    Bloodline = 1,

    /// <summary>2 — избранность судьбой или пророчеством.</summary>
    Destiny = 2,

    /// <summary>3 — сила, связанная с артефактом.</summary>
    Artifact = 3,

    /// <summary>4 — покровительство невидимой сверхъестественной силы.</summary>
    Patron = 4,

    /// <summary>5 — исключительная внутренняя цель: долг, клятва или месть.</summary>
    Purpose = 5,

    /// <summary>6 — единственный преобразивший жизнь опыт.</summary>
    LifeChangingEvent = 6,

    /// <summary>7 — благословение либо проклятие.</summary>
    BlessingOrCurse = 7,

    /// <summary>8 — уникальная многолетняя подготовка.</summary>
    Training = 8,

    /// <summary>9 — воздействие неконтролируемой магии.</summary>
    WildMagic = 9,
}
