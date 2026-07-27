namespace GenesysForge.Domain.Entities;

public class TalentDef : IContentDef
{
    public Guid Id { get; set; }
    public GameSystem System { get; set; }
    /// <summary>Стабильный код встроенного контента. У кастома пусто.</summary>
    public string Code { get; set; } = "";
    /// <summary>Оригинальное/английское название.</summary>
    public required string Name { get; set; }
    /// <summary>Русское название.</summary>
    public string NameRu { get; set; } = "";
    public int Tier { get; set; }
    public bool IsRanked { get; set; }
    /// <summary>UI-категория таланта для фильтрации: общий, социальный, боевой или магический.</summary>
    public TalentCategory Category { get; set; } = TalentCategory.General;
    /// <summary>Пригодность к сеттингам (флаги). Определяет, в каких системах талант доступен.</summary>
    public GenesysSetting Setting { get; set; } = GenesysSetting.Any;
    /// <summary>Полное (private) описание-парафраз. Отдаётся в режиме ContentMode.PrivateFull.</summary>
    public string Description { get; set; } = "";
    /// <summary>Copyright-safe краткое описание для публичной версии (ContentMode.PublicSafe).</summary>
    public string SafeDescription { get; set; } = "";
    /// <summary>Английское описание — собственный copyright-safe парафраз. Используется в обоих режимах контента.</summary>
    public string DescriptionEn { get; set; } = "";
    /// <summary>Ссылка на источник: книга/раздел/страница. Доступна в обоих режимах.</summary>
    public string Source { get; set; } = "";
    public string Activation { get; set; } = "Пассивный";
    /// <summary>
    /// Английская подпись тайминга активации (<c>Passive</c>, <c>Incidental</c>,
    /// <c>Out-of-turn Incidental</c> и т. п.). Стабильнее локализованной строки для сравнения.
    /// </summary>
    public string ActivationEn { get; set; } = "";
    /// <summary>
    /// Талант можно применять вне своего хода. <c>Out-of-turn Incidental</c> — отдельный тайминг,
    /// а не обычный Incidental (ROT-TAL-01).
    /// </summary>
    public bool CanUseOutOfTurn { get; set; }
    /// <summary>
    /// Талант увеличивает выбранную характеристику на 1 за каждый ранг (Dedication / «Повышение»).
    /// При покупке игрок выбирает характеристику; одну и ту же дважды этим талантом увеличить нельзя.
    /// </summary>
    public bool GrantsCharacteristic { get; set; }
    /// <summary>
    /// Навыки, которые талант делает карьерными, каноническими (английскими) именами.
    /// Учитываются резолвером карьерных навыков, пока талант принадлежит персонажу.
    /// </summary>
    public List<string> CareerSkillNames { get; set; } = [];

    /// <summary>
    /// Bare-slug код таланта, обязательного для покупки этого (ROT-TAL-02). Пусто — предусловий нет.
    /// Связь идёт по стабильному коду, а не по отображаемому имени.
    /// </summary>
    public string RequiresTalentCode { get; set; } = "";

    /// <summary>
    /// Bare-slug коды талантов, несовместимых с этим. Отношение симметрично: сид заполняет обе
    /// стороны пары, поэтому проверять достаточно одну.
    /// </summary>
    public List<string> ExcludesTalentCodes { get; set; } = [];

    /// <summary>
    /// Сколько раз талант применим в пределах <see cref="UseScope"/>. 0 — предела нет
    /// (ROT-TAL-05). Значение описательное: приложение показывает лимит, но не ведёт счётчик.
    /// </summary>
    public int UsesPerScope { get; set; }

    /// <summary>Область сброса лимита применений: сессия, encounter, раунд или ход.</summary>
    public AbilityUseScope UseScope { get; set; } = AbilityUseScope.None;

    /// <summary>Стоимость активации в Story Point игроков. 0 — активация бесплатна.</summary>
    public int StoryPointCost { get; set; }

    /// <summary>Стоимость активации в усталости (strain). 0 — талант её не требует.</summary>
    public int StrainCost { get; set; }

    /// <summary>
    /// Условие срабатывания для активных талантов: что именно должно произойти, чтобы талант
    /// стал доступен. Пусто — талант не привязан к триггеру.
    /// </summary>
    public string Trigger { get; set; } = "";
    // Пассивные бонусы, применяемые автоматически за каждый ранг.
    public int WoundBonus { get; set; }
    public int StrainBonus { get; set; }
    public int SoakBonus { get; set; }
    public int MeleeDefenseBonus { get; set; }
    public int RangedDefenseBonus { get; set; }
    public Guid? OwnerUserId { get; set; }
    public Guid? HomebrewPackId { get; set; }
    /// <summary>
    /// Запись исключена из новых выборов, но сохранена ради существующих ссылок
    /// (см. <see cref="IContentDef.Retired"/>).
    /// </summary>
    public bool Retired { get; set; }
}
