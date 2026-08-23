using GenesysForge.Domain.Entities;

namespace GenesysForge.Domain.Rules;

/// <summary>
/// Длительные, однозначно применимые к проверкам эффекты критических травм (U-23).
/// Краткие эффекты «следующей проверки» намеренно не попадают сюда: без отдельной отметки
/// о том, что эффект уже израсходован, приложение не должно применять их к каждой проверке.
/// </summary>
public static class CriticalInjuryRules
{
    public sealed record CheckModifier(
        string SourceName,
        string SourceNameRu,
        CharacteristicType? Characteristic = null,
        string SkillName = "",
        int Setback = 0,
        int Difficulty = 0,
        int DifficultyUpgrades = 0,
        bool RemoveBoosts = false);

    /// <summary>
    /// Возвращает только те эффекты, область которых можно определить без вопроса к игроку или
    /// ведущему. Неизвестные ручные травмы и травмы с выбором конечности/характеристики остаются
    /// доступными для просмотра, но не превращаются в догадку о пуле.
    /// </summary>
    public static IReadOnlyList<CheckModifier> CheckModifiers(
        IEnumerable<CharacterCriticalInjury>? injuries)
    {
        var result = new List<CheckModifier>();
        foreach (var injury in injuries ?? [])
        {
            var code = injury.RuleCode;
            if (string.IsNullOrWhiteSpace(code)) continue;

            void Add(
                CharacteristicType? characteristic = null,
                string skillName = "",
                int setback = 0,
                int difficulty = 0,
                int upgrades = 0,
                bool removeBoosts = false) => result.Add(new CheckModifier(
                    code, injury.NameRu, characteristic, skillName, setback, difficulty, upgrades,
                    removeBoosts));

            switch (code)
            {
                // Дезориентация: одна помеха на каждую проверку, пока травма не вылечена.
                case "crit-ci_061_065":
                    Add(setback: 1);
                    break;

                // Потеря бонусных костей из-за состояния «в растерянных чувствах».
                case "crit-ci_066_070":
                    Add(removeBoosts: true);
                    break;

                // Длительная травма, повышающая сложность проверок двух характеристик.
                case "crit-ci_046_050":
                    Add(CharacteristicType.Intellect, difficulty: 1);
                    Add(CharacteristicType.Cunning, difficulty: 1);
                    break;
                case "crit-ci_051_055":
                    Add(CharacteristicType.Presence, difficulty: 1);
                    Add(CharacteristicType.Willpower, difficulty: 1);
                    break;
                case "crit-ci_056_060":
                    Add(CharacteristicType.Brawn, difficulty: 1);
                    Add(CharacteristicType.Agility, difficulty: 1);
                    break;
                case "crit-ci_086_090":
                    Add(difficulty: 1);
                    break;

                // Слепота усиливает сложность каждой проверки дважды; Perception и Vigilance —
                // ещё один раз (итого три), что соответствует формулировке таблицы.
                case "crit-ci_116_120":
                    Add(upgrades: 2);
                    Add(skillName: "Perception", upgrades: 1);
                    Add(skillName: "Vigilance", upgrades: 1);
                    break;

                // Уродство оставляет одну помеху на прочие проверки. Проверки, требующие
                // утраченной конечности, требуют отдельного решения ведущего и здесь не
                // угадываются.
                case "crit-ci_101_105":
                    Add(setback: 1);
                    break;
            }
        }

        return result;
    }
}
