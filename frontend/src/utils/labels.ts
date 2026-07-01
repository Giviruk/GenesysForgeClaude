import type {
  AllowedState, Characteristic, ContentEntryType, CreatureTemplate, EncounterType, GameSystem, HouseRuleCategory,
  InitiativeSlotType, ItemKind, ItemState, NpcCombatStyle, NpcKind, NpcPowerLevel, NpcRole,
  NpcVisibility, ParticipantType, SkillKind, TalentCategory, ThreatLevel,
} from '../api/types'

export const SYSTEM_LABELS: Record<GameSystem, string> = {
  genesysCore: 'Genesys Core',
  realmsOfTerrinoth: 'Realms of Terrinoth',
}

export const CHARACTERISTIC_LABELS: Record<Characteristic, string> = {
  brawn: 'Мощь',
  agility: 'Ловкость',
  intellect: 'Интеллект',
  cunning: 'Хитрость',
  willpower: 'Воля',
  presence: 'Харизма',
}

export const CHARACTERISTICS: Characteristic[] = [
  'brawn', 'agility', 'intellect', 'cunning', 'willpower', 'presence',
]

/** Короткие подписи характеристик для узких таблиц. */
export const CHARACTERISTIC_SHORT_LABELS: Record<Characteristic, string> = {
  brawn: 'Мощ',
  agility: 'Лов',
  intellect: 'Инт',
  cunning: 'Хит',
  willpower: 'Вол',
  presence: 'Хар',
}

export const SKILL_KIND_LABELS: Record<SkillKind, string> = {
  general: 'Общие',
  combat: 'Боевые',
  social: 'Социальные',
  knowledge: 'Знания',
  magic: 'Магия',
}

export const TALENT_CATEGORY_LABELS: Record<TalentCategory, string> = {
  general: 'Общие',
  social: 'Социальные',
  combat: 'Боевые',
  magic: 'Магические',
}

export const TALENT_CATEGORIES: TalentCategory[] = ['general', 'social', 'combat', 'magic']

export const ITEM_KIND_LABELS: Record<ItemKind, string> = {
  weapon: 'Оружие',
  armor: 'Броня',
  gear: 'Снаряжение',
}

export const ITEM_STATE_LABELS: Record<ItemState, string> = {
  equipped: 'Используется',
  carried: 'Не используется',
  backpack: 'В рюкзаке',
}

/** Нейтральная подпись валюты («монеты»). */
export const CURRENCY_LABEL = 'монеты'
export const CURRENCY_SHORT = 'мон.'

export const localizedName = (value: { name: string; nameRu?: string | null }) =>
  value.nameRu?.trim() || value.name

/**
 * Подбирает навык листа для броска оружием. Оружие хранит англ. имя навыка
 * (например, «Melee (Light)»), но в Genesys Core навык называется просто «Melee» —
 * поэтому при отсутствии точного совпадения пробуем базовое имя без скобок.
 */
export function resolveWeaponSkillName(weaponSkill: string, skillNames: string[]): string | null {
  if (!weaponSkill) return null
  if (skillNames.includes(weaponSkill)) return weaponSkill
  const base = weaponSkill.replace(/\s*\(.*\)\s*/, '').trim()
  if (base && skillNames.includes(base)) return base
  return null
}

/** Подписи магических навыков (направлений магии). Ключ — стабильный код из seed. */
export const MAGIC_SKILL_LABELS: Record<string, string> = {
  Arcana: 'Тайная (Arcana)',
  Divine: 'Божественная (Divine)',
  Primal: 'Природная (Primal)',
  Runes: 'Руны (Runes)',
  Verse: 'Песнь (Verse)',
}

/** Подпись магического навыка с запасным вариантом для кастомных кодов. */
export const magicSkillLabel = (skill: string) => MAGIC_SKILL_LABELS[skill] ?? skill

/** Подписи уровней сложности проверки Genesys (число фиолетовых кубов). */
export const DIFFICULTY_LABELS: Record<number, string> = {
  0: 'Простая',
  1: 'Лёгкая',
  2: 'Средняя',
  3: 'Сложная',
  4: 'Трудная',
  5: 'Грозная',
}

/** Подпись уровня сложности с ограничением 0..5. */
export const difficultyLabel = (n: number) => DIFFICULTY_LABELS[Math.max(0, Math.min(5, n))] ?? `${n}`

/**
 * Извлекает числовое значение сложности из строки справочника магии:
 * базовый эффект — «2 (Average)» → 2; доп. эффект — «+1» → 1. Пусто/нечисло → 0.
 */
export const parseDifficulty = (raw: string): number => {
  const m = raw.match(/-?\d+/)
  return m ? parseInt(m[0], 10) : 0
}

export const NPC_KIND_LABELS: Record<NpcKind, string> = {
  minion: 'Миньон',
  rival: 'Ривал',
  nemesis: 'Немезида',
}

export const NPC_ROLE_LABELS: Record<NpcRole, string> = {
  brute: 'Громила',
  skirmisher: 'Застрельщик',
  archer: 'Стрелок',
  caster: 'Маг',
  leader: 'Командир',
  social: 'Интриган',
  support: 'Поддержка',
  monster: 'Монстр',
  custom: 'Особая',
}

export const NPC_VISIBILITY_LABELS: Record<NpcVisibility, string> = {
  private: 'Приватный',
  campaignVisible: 'Виден в кампании',
  publicTemplate: 'Публичный шаблон',
}

export const NPC_POWER_LABELS: Record<NpcPowerLevel, string> = {
  weak: 'Слабый',
  standard: 'Обычный',
  strong: 'Сильный',
  elite: 'Элитный',
}

export const NPC_COMBAT_STYLE_LABELS: Record<NpcCombatStyle, string> = {
  melee: 'Ближний бой',
  ranged: 'Дальний бой',
  magic: 'Магия',
  social: 'Социальный',
}

export const CREATURE_TEMPLATE_LABELS: Record<CreatureTemplate, string> = {
  none: 'Без шаблона (гуманоид)',
  undead: 'Нежить',
  beast: 'Зверь',
  dragon: 'Дракон',
  demon: 'Демон',
  construct: 'Конструкт',
}
export const CREATURE_TEMPLATES: CreatureTemplate[] = ['none', 'undead', 'beast', 'dragon', 'demon', 'construct']

export const NPC_KINDS: NpcKind[] = ['minion', 'rival', 'nemesis']
export const NPC_ROLES: NpcRole[] = [
  'brute', 'skirmisher', 'archer', 'caster', 'leader', 'social', 'support', 'monster', 'custom',
]

export const PARTICIPANT_TYPE_LABELS: Record<ParticipantType, string> = {
  playerCharacter: 'Персонаж',
  npc: 'NPC',
  minionGroup: 'Группа миньонов',
  hazard: 'Осложнение',
}

export const SLOT_TYPE_LABELS: Record<InitiativeSlotType, string> = {
  player: 'Игроки',
  npc: 'NPC',
  neutral: 'Нейтрал',
}

export const ENCOUNTER_TYPE_LABELS: Record<EncounterType, string> = {
  combat: 'Бой',
  social: 'Социальный',
  exploration: 'Исследование',
  chase: 'Погоня',
  investigation: 'Расследование',
  travel: 'Путешествие',
  hazard: 'Опасность',
  mixed: 'Смешанный',
  custom: 'Особый',
}

export const ENCOUNTER_TYPES: EncounterType[] = [
  'combat', 'social', 'exploration', 'chase', 'investigation', 'travel', 'hazard', 'mixed', 'custom',
]

export const THREAT_LEVEL_LABELS: Record<ThreatLevel, string> = {
  trivial: 'Тривиальный',
  easy: 'Лёгкий',
  standard: 'Стандартный',
  hard: 'Тяжёлый',
  deadly: 'Смертельный',
}

export const THREAT_LEVELS: ThreatLevel[] = ['trivial', 'easy', 'standard', 'hard', 'deadly']

export const CONTENT_ENTRY_TYPE_LABELS: Record<ContentEntryType, string> = {
  archetype: 'Архетип',
  career: 'Карьера',
  skill: 'Навык',
  talent: 'Талант',
  item: 'Предмет',
  heroicAbility: 'Геройская способность',
  spell: 'Заклинание',
  magicAction: 'Магическое действие',
  alchemyRecipe: 'Алхимический рецепт',
  rune: 'Руна',
  houseRule: 'Домашнее правило',
  customNote: 'Заметка',
}

export const CONTENT_ENTRY_TYPES: ContentEntryType[] = [
  'talent', 'item', 'career', 'archetype', 'skill', 'heroicAbility',
  'spell', 'magicAction', 'alchemyRecipe', 'rune', 'houseRule', 'customNote',
]

export const ALLOWED_STATE_LABELS: Record<AllowedState, string> = {
  allowed: 'Разрешено',
  disallowed: 'Запрещено',
  askGm: 'С разрешения мастера',
}

export const ALLOWED_STATES: AllowedState[] = ['allowed', 'disallowed', 'askGm']

export const HOUSE_RULE_CATEGORY_LABELS: Record<HouseRuleCategory, string> = {
  none: '—',
  characterCreation: 'Создание персонажа',
  combat: 'Бой',
  magic: 'Магия',
  equipment: 'Снаряжение',
  xp: 'Опыт (XP)',
  campaignTone: 'Тон кампании',
  custom: 'Особая',
}

export const HOUSE_RULE_CATEGORIES: HouseRuleCategory[] = [
  'characterCreation', 'combat', 'magic', 'equipment', 'xp', 'campaignTone', 'custom',
]

/** Стоимость таланта тира N — 5 × N XP. */
export const talentCost = (tier: number) => tier * 5

/** Эффективный тир следующего ранга рангового таланта (каждый ранг — на тир выше, максимум 5). */
export const nextRankTier = (baseTier: number, ranksOwned: number) =>
  Math.min(baseTier + ranksOwned, 5)
