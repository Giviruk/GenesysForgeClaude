using System.Reflection;
using System.Text.Json;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Infrastructure.Persistence;

/// <summary>
/// Каталог снаряжения, загружаемый из embedded JSON (<c>SeedContent/items.catalog.json</c>).
/// Источник — пользовательский CSV (структура + переработанные RU-описания, не текст книг),
/// собран скриптом <c>_books/_inventory/gen-items-catalog.mjs</c>.
/// Каждая запись разворачивается в <see cref="ItemDef"/> по игровым системам согласно сеттингу.
/// Fantasy активна только в Realms of Terrinoth. Any активна в Genesys Core, а в RoT — только
/// при явном <c>includeInRot</c>; так новый Core-предмет не попадает в фэнтезийный каталог сам.
/// </summary>
public static class ItemCatalog
{
    private sealed record Entry(
        string Code, string Name, string NameRu, string Kind, string Setting,
        int Enc, int Soak, int Def, int Rdef, int EncBonus, int? Price, int? Rarity,
        string Desc, string Source,
        string? SkillEn, string? Damage, string? Crit, string? RangeRu, string? Properties,
        string DescEn = "", bool Retired = false,
        /// <summary>Общий Core-предмет явно одобрен утверждённым RoT-манифестом.</summary>
        bool IncludeInRot = false,
        /// <summary>Разрешена обычная покупка через витрину.</summary>
        bool Purchasable = true,
        /// <summary>Разрешена обычная продажа экземпляра.</summary>
        bool Sellable = true,
        /// <summary>Слоты улучшений по таблице книги; null — книжного значения у записи нет.</summary>
        int? Hp = null,
        /// <summary>Влияние предмета на проверки навыков (ROT-ARM-01).</summary>
        ModifierEntry[]? Modifiers = null,
        /// <summary>
        /// Альтернативные профили атаки (ROT-WPN-01): метание, взятие в руку. Профиль по умолчанию
        /// не описывается — он строится из колонок таблицы.
        /// </summary>
        ProfileEntry[]? Profiles = null,
        /// <summary>Оружием нельзя атаковать вплотную (пика).</summary>
        bool CannotEngage = false,
        /// <summary>Сложность проверки, заданная самим оружием (пика — 2).</summary>
        int? Difficulty = null,
        /// <summary>Признаки формы для совместимости улучшений (ROT-EQP-ATT-01).</summary>
        string[]? Traits = null);

    /// <param name="DamageKind">«BrawnPlus» или «Fixed».</param>
    /// <param name="Range">Дистанция: Engaged, Short, Medium, Long, Extreme.</param>
    private sealed record ProfileEntry(
        string Code, string NameRu, string NameEn, string SkillEn, string DamageKind, int Damage,
        int Crit, string Range, ProfileQualityEntry[]? Qualities = null,
        bool CannotEngage = false, int? Difficulty = null);

    private sealed record ProfileQualityEntry(string Code, int Rating = 0);

    /// <param name="Kind">«AddSetback» или «RemoveSetback».</param>
    /// <param name="Skill">Английское имя навыка; пусто — отбор по характеристике.</param>
    /// <param name="Characteristic">Имя характеристики; пусто — отбор по навыку.</param>
    /// <param name="Worn">Действует только когда предмет надет (для брони — выбран активной).</param>
    private sealed record ModifierEntry(
        string Kind, string? Skill, string? Characteristic, int Value, bool Worn = true, string Condition = "");

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Разворачивает каталог в список встроенных предметов по системам.</summary>
    public static IEnumerable<ItemDef> Load(Assembly? assembly = null)
    {
        assembly ??= typeof(ItemCatalog).Assembly;
        var resource = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("items.catalog.json", StringComparison.OrdinalIgnoreCase));
        if (resource is null) yield break;

        using var stream = assembly.GetManifestResourceStream(resource)!;
        var entries = JsonSerializer.Deserialize<List<Entry>>(stream, JsonOptions) ?? [];

        foreach (var e in entries)
        {
            // Any сохраняет legacy RoT-строку, но она retired без явного whitelist-флага.
            // Fantasy существует только в RoT и активна по умолчанию.
            var fantasy = string.Equals(e.Setting, "Fantasy", StringComparison.OrdinalIgnoreCase);
            var systems = fantasy
                ? new[] { GameSystem.RealmsOfTerrinoth }
                : [GameSystem.GenesysCore, GameSystem.RealmsOfTerrinoth];

            var kind = ParseKind(e.Kind);
            var shard = RuneboundShardRules.IsShard(e.Code);
            // Каталожная реликвия (ROT-MITEM-01) продаётся не больше, чем руна: цены книга ей не
            // задаёт, и ноль монет делал её бесплатным товаром витрины. Редкость, в отличие от рун,
            // в таблице есть и остаётся.
            var magicItem = MagicItemRules.IsMagicItem(e.Code);

            foreach (var sys in systems)
                yield return new ItemDef
                {
                    Id = Guid.NewGuid(),
                    System = sys,
                    Code = $"{(sys == GameSystem.GenesysCore ? "gc" : "rot")}.item.{e.Code}",
                    Name = e.Name,
                    NameRu = string.IsNullOrWhiteSpace(e.NameRu) ? e.Name : e.NameRu,
                    Kind = kind,
                    Encumbrance = e.Enc,
                    SoakBonus = e.Soak,
                    MeleeDefense = e.Def,
                    RangedDefense = e.Rdef,
                    EncumbranceThresholdBonus = e.EncBonus,
                    // У runebound shards в книге нет цены и редкости. Старые 0/1 были
                    // ошибочной подменой отсутствующего значения и делали реликвию бесплатной.
                    Price = shard || magicItem ? null : e.Price,
                    Rarity = shard ? null : e.Rarity,
                    Purchasable = !shard && !magicItem && e.Purchasable,
                    Sellable = !shard && !magicItem && e.Sellable,
                    SafeDescription = e.Desc,
                    DescriptionEn = e.DescEn,
                    Source = e.Source,
                    SkillName = e.SkillEn ?? "",
                    Damage = e.Damage ?? "",
                    Crit = e.Crit ?? "",
                    RangeBand = e.RangeRu ?? "",
                    Properties = e.Properties ?? "",
                    Retired = e.Retired
                        || (sys == GameSystem.RealmsOfTerrinoth && !fantasy && !e.IncludeInRot),
                    HardPoints = e.Hp,
                    FormTraits = ParseTraits(e.Traits),
                    CheckModifiers = [.. (e.Modifiers ?? []).Select(m => new ItemCheckModifier
                    {
                        Id = Guid.NewGuid(),
                        Kind = ParseModifierKind(m.Kind),
                        SkillName = m.Skill ?? "",
                        Characteristic = ParseCharacteristic(m.Characteristic),
                        Value = m.Value,
                        RequiresWorn = m.Worn,
                        Condition = m.Condition,
                    })],
                    AttackProfiles = AttackProfiles(e, kind),
                };
        }
    }

    /// <summary>Признаки формы из каталога. Неизвестное имя молча не проглатывается.</summary>
    internal static WeaponFormTraits ParseTraits(string[]? traits)
    {
        var result = WeaponFormTraits.None;
        foreach (var name in traits ?? [])
        {
            if (!Enum.TryParse<WeaponFormTraits>(name, ignoreCase: true, out var trait))
                throw new InvalidOperationException($"Неизвестный признак формы «{name}» в каталоге.");
            result |= trait;
        }
        return result;
    }

    private static ItemKind ParseKind(string kind) => kind.ToLowerInvariant() switch
    {
        "weapon" => ItemKind.Weapon,
        "armor" => ItemKind.Armor,
        _ => ItemKind.Gear,
    };

    /// <summary>
    /// Профили атаки предмета (ROT-WPN-01). Профиль по умолчанию строится из колонок таблицы —
    /// иначе одни и те же числа лежали бы в каталоге дважды и расходились. Альтернативные профили
    /// (метание, в руке) описаны в каталоге целиком, потому что отличаются от основного.
    /// </summary>
    private static List<WeaponAttackProfile> AttackProfiles(Entry e, ItemKind kind)
    {
        if (kind != ItemKind.Weapon) return [];

        var profiles = new List<WeaponAttackProfile>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Code = WeaponAttackProfile.DefaultCode,
                IsDefault = true,
                SkillName = e.SkillEn ?? "",
                DamageKind = ParseDamageKind(e.Damage),
                DamageValue = ParseDamageValue(e.Damage, e.Code),
                Crit = ParseCrit(e.Crit),
                Range = ParseRange(e.RangeRu, e.Code),
                CannotAttackEngaged = e.CannotEngage,
                FixedDifficulty = e.Difficulty,
            },
        };

        foreach (var p in e.Profiles ?? [])
            profiles.Add(new WeaponAttackProfile
            {
                Id = Guid.NewGuid(),
                Code = p.Code,
                NameRu = p.NameRu,
                NameEn = p.NameEn,
                IsDefault = false,
                SkillName = p.SkillEn,
                DamageKind = Enum.Parse<DamageKind>(p.DamageKind, ignoreCase: true),
                DamageValue = p.Damage,
                Crit = p.Crit,
                Range = Enum.Parse<WeaponRange>(p.Range, ignoreCase: true),
                CannotAttackEngaged = p.CannotEngage,
                FixedDifficulty = p.Difficulty,
                Qualities = [.. (p.Qualities ?? []).Select(q => new WeaponProfileQuality
                {
                    Code = q.Code,
                    Rating = q.Rating,
                })],
            });

        return profiles;
    }

    /// <summary>«+3» — прибавка к Мощи, «7» — итоговый урон оружия.</summary>
    private static DamageKind ParseDamageKind(string? damage) =>
        (damage ?? "").TrimStart().StartsWith('+') ? DamageKind.BrawnPlus : DamageKind.Fixed;

    private static int ParseDamageValue(string? damage, string code) =>
        int.TryParse((damage ?? "").Replace("+", "").Trim(), out var value)
            ? value
            : throw new InvalidOperationException($"Не разобран урон предмета «{code}»: «{damage}».");

    private static int ParseCrit(string? crit) => int.TryParse((crit ?? "").Trim(), out var value) ? value : 0;

    private static WeaponRange ParseRange(string? rangeRu, string code) => (rangeRu ?? "").Trim() switch
    {
        "Вплотную" => WeaponRange.Engaged,
        "Короткая" => WeaponRange.Short,
        "Средняя" => WeaponRange.Medium,
        "Длинная" => WeaponRange.Long,
        "Экстремальная" => WeaponRange.Extreme,
        var other => throw new InvalidOperationException($"Неизвестная дистанция предмета «{code}»: «{other}»."),
    };

    private static CheckModifierKind ParseModifierKind(string kind) =>
        Enum.TryParse<CheckModifierKind>(kind, ignoreCase: true, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"Неизвестный вид модификатора проверки: «{kind}».");

    private static CharacteristicType? ParseCharacteristic(string? characteristic) =>
        string.IsNullOrWhiteSpace(characteristic)
            ? null
            : Enum.TryParse<CharacteristicType>(characteristic, ignoreCase: true, out var parsed)
                ? parsed
                : throw new InvalidOperationException($"Неизвестная характеристика: «{characteristic}».");
}
