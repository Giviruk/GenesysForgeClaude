namespace GenesysForge.Domain.Rules;

/// <summary>
/// Состояние переносимого веса (ROT-EQP-01): не только «перегружен или нет», но и точная цена
/// перегруза, которую игрок должен видеть до того, как соберёт рюкзак.
/// </summary>
/// <param name="Threshold">Порог: 5 + текущая Мощь + применимые бонусы.</param>
/// <param name="Load">Фактический вес, включая вклад предметов с нулевым Enc.</param>
/// <param name="Overload">Превышение порога; 0 — перегруза нет.</param>
/// <param name="SetbackDice">
/// Столько костей помех добавляется ко всем проверкам, связанным с Мощью или Ловкостью.
/// Они складываются с прочими помехами.
/// </param>
/// <param name="HasFreeManoeuvre">Остался ли бесплатный манёвр в свой ход.</param>
/// <param name="StrainPerManoeuvre">
/// Цена каждого манёвра в усталости, включая первый. Ноль — манёвры бесплатны как обычно.
/// </param>
/// <param name="ZeroEncumbranceLoad">Сколько веса добавили предметы с нулевым Enc.</param>
public sealed record EncumbranceState(
    int Threshold,
    int Load,
    int Overload,
    int SetbackDice,
    bool HasFreeManoeuvre,
    int StrainPerManoeuvre,
    int ZeroEncumbranceLoad)
{
    public bool Encumbered => Overload > 0;
}

/// <summary>Правила переносимого веса и перегруза.</summary>
public static class EncumbranceRules
{
    private const int LoosePerLoadPoint = 10;
    private const int EfficientPerLoadPoint = 20;

    /// <summary>Цена одного манёвра при тяжёлом перегрузе.</summary>
    private const int OverloadedManoeuvreStrain = 2;

    /// <summary>
    /// Вклад предметов с нулевым Enc. Нулевой вес не умножается на количество, но и не пропадает:
    /// счёт идёт по всем таким предметам вместе, иначе десять строк по одному предмету обошли бы
    /// правило. Остаток (1–9 россыпью, 1–19 уложенных) веса не даёт.
    /// </summary>
    public static int ZeroEncumbranceLoad(int looseCount, int efficientCount = 0) =>
        Math.Max(0, looseCount) / LoosePerLoadPoint + Math.Max(0, efficientCount) / EfficientPerLoadPoint;

    /// <summary>
    /// Сводит порог, вес и последствия перегруза. При перегрузе добавляется ровно столько костей
    /// помех, каково превышение; когда превышение достигает текущей Мощи, бесплатного манёвра нет,
    /// а каждый сделанный манёвр — включая первый — стоит 2 усталости.
    /// </summary>
    public static EncumbranceState Compute(int brawn, int load, int thresholdBonuses = 0, int zeroEncLoad = 0)
    {
        var threshold = GenesysRules.EncumbranceThreshold(brawn, thresholdBonuses);
        var totalLoad = Math.Max(0, load) + Math.Max(0, zeroEncLoad);
        var overload = Math.Max(0, totalLoad - threshold);
        // Равенство тоже считается тяжёлым перегрузом: правило говорит «не меньше Мощи».
        var heavy = overload > 0 && overload >= brawn;

        return new EncumbranceState(
            Threshold: threshold,
            Load: totalLoad,
            Overload: overload,
            SetbackDice: overload,
            HasFreeManoeuvre: !heavy,
            StrainPerManoeuvre: heavy ? OverloadedManoeuvreStrain : 0,
            ZeroEncumbranceLoad: Math.Max(0, zeroEncLoad));
    }
}
