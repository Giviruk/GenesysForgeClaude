namespace GenesysForge.Domain.Rules;

/// <summary>
/// Памятка по ремонту одного экземпляра (GEN-EQP-DMG-01): всё, что игрок должен знать до нажатия
/// кнопки. Считается на сервере, потому что стоимость зависит от цены экземпляра с учётом качества
/// изготовления, а не от цены строки каталога.
/// </summary>
/// <param name="CanRepair">Обычный ремонт доступен: состояние между Незначительным и Серьёзным.</param>
/// <param name="Difficulty">Базовая сложность проверки по книге: 1/2/3. <c>null</c> — ремонта нет.</param>
/// <param name="HoursMin">Нижняя граница достаточного времени, часов.</param>
/// <param name="HoursMax">Верхняя граница достаточного времени, часов.</param>
/// <param name="MaterialPercent">Доля цены экземпляра, уходящая на материалы: 25/50/100.</param>
/// <param name="MaterialCost">Итоговая стоимость материалов после скидки за преимущества.</param>
/// <param name="BaseMaterialCost">Стоимость материалов без скидки — чтобы скидка была видна.</param>
public sealed record RepairEstimate(
    ItemDamageState State,
    bool CanRepair,
    int? Difficulty,
    int HoursMin,
    int HoursMax,
    int MaterialPercent,
    int MaterialCost,
    int BaseMaterialCost);

/// <summary>
/// Состояние повреждения экземпляра и ремонт (GEN-EQP-DMG-01).
///
/// Броска проверки Механики приложение не делает — решение владельца, то же, что и у установки
/// улучшений: правило книги показывается памяткой, а исход определяет стол. Поэтому здесь живут
/// только те части правила, которые считаются однозначно: штраф состояния, потеря свойств,
/// стоимость материалов, время и базовая сложность для памятки.
/// </summary>
public static class DamageStateRules
{
    /// <summary>Навык ремонта по умолчанию; иной — только явным решением ведущего.</summary>
    public const string DefaultSkillName = "Mechanics";

    /// <summary>Скидка самостоятельного ремонта за каждое чистое преимущество, процентов.</summary>
    public const int SelfRepairDiscountPercent = 10;

    /// <summary>Порядок состояний для UI и справочника — от целого к уничтоженному.</summary>
    public static IReadOnlyList<ItemDamageState> All { get; } =
        [ItemDamageState.Undamaged, ItemDamageState.Minor, ItemDamageState.Moderate,
         ItemDamageState.Major, ItemDamageState.Destroyed];

    /// <summary>Русское имя состояния: попадает в разбор поправок и в подсказку к пулу.</summary>
    public static string NameRu(ItemDamageState state) => state switch
    {
        ItemDamageState.Minor => "Незначительное повреждение",
        ItemDamageState.Moderate => "Умеренное повреждение",
        ItemDamageState.Major => "Серьёзное повреждение",
        ItemDamageState.Destroyed => "Уничтожено",
        _ => "Цел",
    };

    /// <summary>Английское имя состояния для тех же мест.</summary>
    public static string NameEn(ItemDamageState state) => state switch
    {
        ItemDamageState.Minor => "Minor damage",
        ItemDamageState.Moderate => "Moderate damage",
        ItemDamageState.Major => "Major damage",
        ItemDamageState.Destroyed => "Destroyed",
        _ => "Undamaged",
    };

    /// <summary>Неизвестное значение отклоняется машинным кодом, а не превращается молча в «цел».</summary>
    public static void EnsureKnown(ItemDamageState state)
    {
        if (!Enum.IsDefined(state))
            throw new DomainRuleException(
                "Неизвестное состояние предмета.", "item.damage_state.unknown");
    }

    /// <summary>
    /// Предметом можно пользоваться. Серьёзное и Уничтожено — нельзя: ни атак, ни поглощения,
    /// ни защиты, ни эффектов улучшений. Вес и содержимое при этом никуда не деваются.
    /// </summary>
    public static bool IsUsable(ItemDamageState state) => state < ItemDamageState.Major;

    /// <summary>Помехи ко всем проверкам, прямо использующим предмет: только Незначительное.</summary>
    public static int SetbackDice(ItemDamageState state) => state == ItemDamageState.Minor ? 1 : 0;

    /// <summary>
    /// Повышение сложности таких проверок: только Умеренное, ровно один раз. С Незначительным
    /// не складывается — предмет находится ровно в одном состоянии.
    /// </summary>
    public static int DifficultyIncrease(ItemDamageState state) =>
        state == ItemDamageState.Moderate ? 1 : 0;

    /// <summary>Состояние вообще влияет на проверки — есть что показать в UI.</summary>
    public static bool AffectsChecks(ItemDamageState state) =>
        SetbackDice(state) > 0 || DifficultyIncrease(state) > 0;

    /// <summary>Обычный ремонт доступен: уничтоженное чинят только особым правилом ведущего.</summary>
    public static bool CanRepair(ItemDamageState state) =>
        state is ItemDamageState.Minor or ItemDamageState.Moderate or ItemDamageState.Major;

    /// <summary>Базовая сложность проверки ремонта по книге: Лёгкая, Средняя, Тяжёлая.</summary>
    public static int? RepairDifficulty(ItemDamageState state) => state switch
    {
        ItemDamageState.Minor => 1,
        ItemDamageState.Moderate => 2,
        ItemDamageState.Major => 3,
        _ => null,
    };

    /// <summary>
    /// Достаточное время ремонта: примерно один-два часа на каждую ступень базовой сложности.
    /// Меньше — сложность растёт на ступень; без подходящих инструментов — ещё на одну, и эти
    /// надбавки складываются. Приложение их не подставляет: это часть исхода, а не расчёта.
    /// </summary>
    public static (int Min, int Max) RepairHours(ItemDamageState state) => state switch
    {
        ItemDamageState.Minor => (1, 2),
        ItemDamageState.Moderate => (2, 4),
        ItemDamageState.Major => (3, 6),
        _ => (0, 0),
    };

    /// <summary>Доля цены экземпляра на материалы: четверть, половина, полная.</summary>
    public static int MaterialPercent(ItemDamageState state) => state switch
    {
        ItemDamageState.Minor => 25,
        ItemDamageState.Moderate => 50,
        ItemDamageState.Major => 100,
        _ => 0,
    };

    /// <summary>
    /// Стоимость материалов. Считается от текущей базовой цены экземпляра: качество изготовления
    /// в ней учтено (ROT-WPN-02), а торговая наценка и цена установленных улучшений — нет.
    /// Скидка самостоятельного ремонта — 10 % за каждое чистое преимущество, не ниже нуля.
    ///
    /// Округление вверх до целой монеты — <c>ProductDecision</c>: дробных монет книга не знает.
    /// Округляется каждый шаг: сначала доля цены, потом скидка от уже округлённой суммы. Так
    /// игрок видит ровно ту арифметику, которую ему показали в памятке («материалы 26, −10 % → 24»),
    /// а не число, полученное из невидимой дроби.
    /// </summary>
    /// <param name="instancePrice">Цена экземпляра с учётом качества изготовления.</param>
    /// <param name="netAdvantages">Чистые преимущества самостоятельного ремонта; 0 — скидки нет.</param>
    public static int MaterialCost(int instancePrice, ItemDamageState state, int netAdvantages = 0)
    {
        var percent = MaterialPercent(state);
        if (percent == 0 || instancePrice <= 0) return 0;

        var baseCost = (int)Math.Ceiling((decimal)instancePrice * percent / 100m);
        var discountPercent = Math.Clamp(Math.Max(0, netAdvantages) * SelfRepairDiscountPercent, 0, 100);
        return (int)Math.Ceiling((decimal)baseCost * (100 - discountPercent) / 100m);
    }

    /// <summary>Полная памятка по ремонту экземпляра — то, что видит игрок до нажатия кнопки.</summary>
    public static RepairEstimate Estimate(
        int instancePrice, ItemDamageState state, int netAdvantages = 0)
    {
        var (min, max) = RepairHours(state);
        return new RepairEstimate(
            state, CanRepair(state), RepairDifficulty(state), min, max,
            MaterialPercent(state), MaterialCost(instancePrice, state, netAdvantages),
            MaterialCost(instancePrice, state));
    }

    /// <summary>
    /// Ступень вниз, как её даёт одна активация Sunder: до Уничтожено и не дальше. Сама активация
    /// в бою не автоматизируется — состояние меняет игрок или ведущий отдельной кнопкой.
    /// </summary>
    public static ItemDamageState Worsen(ItemDamageState state, int steps = 1)
    {
        if (steps <= 0) return state;
        var next = (int)state + steps;
        return (ItemDamageState)Math.Min(next, (int)ItemDamageState.Destroyed);
    }

    /// <summary>
    /// Состояние в пуле атаки (GEN-EQP-QUAL-01/GEN-EQP-DMG-01): помеха при Незначительном и рост
    /// сложности при Умеренном. Источник называется явно — игрок должен видеть, откуда куб.
    /// Серьёзное и Уничтожено в пул не попадают: таким оружием просто не атакуют.
    /// </summary>
    public static AttackPoolModifiers ApplyTo(AttackPoolModifiers pool, ItemDamageState state)
    {
        if (!AffectsChecks(state)) return pool;
        var contribution = new QualityContribution(
            NameEn(state), NameRu(state),
            Setback: SetbackDice(state), Difficulty: DifficultyIncrease(state));
        return new AttackPoolModifiers(
            pool.Boost,
            pool.Setback + contribution.Setback,
            pool.DifficultyIncrease + contribution.Difficulty,
            pool.AutomaticAdvantage,
            pool.AutomaticThreat,
            [.. pool.Sources, contribution]);
    }
}
