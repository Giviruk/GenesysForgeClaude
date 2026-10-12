// Russian Core Rulebook: dice/results pp. 9–13, characteristics pp. 13–15,
// difficulty p. 18, skill names p. 53, adversary types pp. 131–133.
export const CORE_RU = {
  characteristics: {
    brawn: 'Сила', agility: 'Ловкость', intellect: 'Интеллект',
    cunning: 'Смекалка', willpower: 'Воля', presence: 'Харизма',
  },
  dice: {
    ability: 'Кость способности', proficiency: 'Кость мастерства',
    difficulty: 'Кость сложности', challenge: 'Кость вызова',
    boost: 'Кость бонуса', setback: 'Кость штрафа',
  },
  results: {
    success: 'Успех', failure: 'Провал', advantage: 'Преимущество',
    threat: 'Осложнение', triumph: 'Триумф', despair: 'Крах',
  },
  difficulty: ['Элементарная', 'Лёгкая', 'Обычная', 'Сложная', 'Пугающая', 'Запредельная'],
  // Range bands, pp. 106–107.
  ranges: { engaged: 'Вплотную', short: 'Короткая', medium: 'Средняя', long: 'Длинная', extreme: 'Экстремальная' },
  npcKinds: { minion: 'Приспешник', rival: 'Соперник', nemesis: 'Главарь' },
} as const

// Previous names remain aliases for saved NPC skills, item profiles and imports.
// They do not override an exact match to a custom skill in the current reference.
export const LEGACY_SKILL_NAMES: Readonly<Record<string, readonly string[]>> = {
  Discipline: ['Дисциплина'], Operating: ['Операторство'],
  Perception: ['Внимательность', 'Восприятие'], Resilience: ['Выносливость'],
  Skulduggery: ['Плутовство'], Streetwise: ['Уличная смекалка'],
  Brawl: ['Рукопашный бой'], Gunnery: ['Тяжёлое орудие'], Coercion: ['Принуждение'],
  'Ranged (Heavy)': ['Дальний бой (тяжёлый)'], 'Ranged (Light)': ['Дальний бой (лёгкий)'],
  'Melee (Heavy)': ['Ближний бой (тяжёлый)'], 'Melee (Light)': ['Ближний бой (лёгкий)'],
  Arcana: ['Аркана', 'Тайная'], Divine: ['Божественное', 'Божественная'],
  Primal: ['Первозданное', 'Природная'],
}
