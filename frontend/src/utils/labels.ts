import type {
  AllowedState, Characteristic, ContentEntryType, CreatureTemplate, EncounterType, GameSystem, HouseRuleCategory,
  InitiativeSlotType, ItemKind, ItemState, NpcCombatStyle, NpcKind, NpcPowerLevel, NpcRole,
  NpcVisibility, ParticipantType, SkillKind, TalentCategory, ThreatLevel,
} from '../api/types'
import { t } from '../i18n'

export const SYSTEM_LABELS: Record<GameSystem, string> = {
  genesysCore: 'Genesys Core',
  realmsOfTerrinoth: 'Realms of Terrinoth',
}

export const CHARACTERISTIC_LABELS: Record<Characteristic, string> = t({
  brawn: 'Мощь',
  agility: 'Ловкость',
  intellect: 'Интеллект',
  cunning: 'Хитрость',
  willpower: 'Воля',
  presence: 'Харизма',
}, {
  brawn: 'Brawn',
  agility: 'Agility',
  intellect: 'Intellect',
  cunning: 'Cunning',
  willpower: 'Willpower',
  presence: 'Presence',
})

export const CHARACTERISTICS: Characteristic[] = [
  'brawn', 'agility', 'intellect', 'cunning', 'willpower', 'presence',
]

/** Короткие подписи характеристик для узких таблиц. */
export const CHARACTERISTIC_SHORT_LABELS: Record<Characteristic, string> = t({
  brawn: 'Мощ',
  agility: 'Лов',
  intellect: 'Инт',
  cunning: 'Хит',
  willpower: 'Вол',
  presence: 'Хар',
}, {
  brawn: 'Br',
  agility: 'Ag',
  intellect: 'Int',
  cunning: 'Cun',
  willpower: 'Will',
  presence: 'Pr',
})

export const SKILL_KIND_LABELS: Record<SkillKind, string> = t({
  general: 'Общие',
  combat: 'Боевые',
  social: 'Социальные',
  knowledge: 'Знания',
  magic: 'Магия',
}, {
  general: 'General',
  combat: 'Combat',
  social: 'Social',
  knowledge: 'Knowledge',
  magic: 'Magic',
})

export const TALENT_CATEGORY_LABELS: Record<TalentCategory, string> = t({
  general: 'Общие',
  social: 'Социальные',
  combat: 'Боевые',
  magic: 'Магические',
}, {
  general: 'General',
  social: 'Social',
  combat: 'Combat',
  magic: 'Magic',
})

export const TALENT_CATEGORIES: TalentCategory[] = ['general', 'social', 'combat', 'magic']

export const ITEM_KIND_LABELS: Record<ItemKind, string> = t({
  weapon: 'Оружие',
  armor: 'Броня',
  gear: 'Снаряжение',
}, {
  weapon: 'Weapon',
  armor: 'Armor',
  gear: 'Gear',
})

export const ITEM_STATE_LABELS: Record<ItemState, string> = t({
  equipped: 'Используется',
  carried: 'Не используется',
  backpack: 'В рюкзаке',
}, {
  equipped: 'Equipped',
  carried: 'Carried',
  backpack: 'In backpack',
})

/** Нейтральная подпись валюты («монеты»). */
export const CURRENCY_LABEL = t('монеты', 'coins')
export const CURRENCY_SHORT = t('мон.', 'coins')

/** Основное имя контента: русское в RU-интерфейсе, оригинальное (английское) — в EN. */
export const localizedName = (value: { name: string; nameRu?: string | null }) =>
  t(value.nameRu?.trim() || value.name, value.name.trim() || value.nameRu?.trim() || '')

/**
 * Описание контента на языке интерфейса: в EN-режиме — английский парафраз (descriptionEn),
 * с откатом на русский (полное описание или safe-парафраз), если перевода нет.
 */
export const localizedDescription = (value: {
  description?: string | null
  safeDescription?: string | null
  descriptionEn?: string | null
}): string => {
  const ru = value.description?.trim() || value.safeDescription?.trim() || ''
  return t(ru, value.descriptionEn?.trim() || ru)
}

/**
 * Вторичное (оригинальное/английское) название для RU/ENG отображения.
 * Пустая строка, если оно совпадает с основным (нечего дублировать) или отсутствует.
 */
export const secondaryName = (value: { name: string; nameRu?: string | null }): string => {
  const primary = localizedName(value).trim()
  const original = value.name.trim()
  return original && original.toLowerCase() !== primary.toLowerCase() ? original : ''
}

/**
 * Однострочный RU/ENG формат для option/плоского текста: «Ближний бой / Melee».
 * Для разметки со стилями используйте пару localizedName + secondaryName.
 */
export const dualName = (value: { name: string; nameRu?: string | null }): string => {
  const original = secondaryName(value)
  return original ? `${localizedName(value)} / ${original}` : localizedName(value)
}

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
export const MAGIC_SKILL_LABELS: Record<string, string> = t({
  Arcana: 'Тайная (Arcana)',
  Divine: 'Божественная (Divine)',
  Primal: 'Природная (Primal)',
  Runes: 'Руны (Runes)',
  Verse: 'Песнь (Verse)',
}, {
  Arcana: 'Arcana',
  Divine: 'Divine',
  Primal: 'Primal',
  Runes: 'Runes',
  Verse: 'Verse',
})

/** Подпись магического навыка с запасным вариантом для кастомных кодов. */
export const magicSkillLabel = (skill: string) => MAGIC_SKILL_LABELS[skill] ?? skill

/** Подписи уровней сложности проверки Genesys (число фиолетовых кубов). */
export const DIFFICULTY_LABELS: Record<number, string> = t({
  0: 'Простая',
  1: 'Лёгкая',
  2: 'Средняя',
  3: 'Сложная',
  4: 'Трудная',
  5: 'Грозная',
}, {
  0: 'Simple',
  1: 'Easy',
  2: 'Average',
  3: 'Hard',
  4: 'Daunting',
  5: 'Formidable',
})

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

/** Потолок итоговой сложности магического действия по правилам Genesys. */
export const MAX_SPELL_DIFFICULTY = 5

/**
 * Итоговая сложность магического действия без потолка: базовый эффект + сумма
 * выбранных дополнительных эффектов. Строки «2 (Average)» и «+1» считаются parseDifficulty.
 */
export const spellDifficulty = (baseDifficulty: string, additional: string[]): number =>
  parseDifficulty(baseDifficulty) + additional.reduce((sum, d) => sum + parseDifficulty(d), 0)

/** true, если добавление эффекта с данной сложностью превысит потолок 5. */
export const wouldExceedSpellCap = (baseDifficulty: string, chosen: string[], candidate: string): boolean =>
  spellDifficulty(baseDifficulty, chosen) + parseDifficulty(candidate) > MAX_SPELL_DIFFICULTY

export const NPC_KIND_LABELS: Record<NpcKind, string> = t({
  minion: 'Миньон',
  rival: 'Ривал',
  nemesis: 'Немезида',
}, {
  minion: 'Minion',
  rival: 'Rival',
  nemesis: 'Nemesis',
})

export const NPC_ROLE_LABELS: Record<NpcRole, string> = t({
  brute: 'Громила',
  skirmisher: 'Застрельщик',
  archer: 'Стрелок',
  caster: 'Маг',
  leader: 'Командир',
  social: 'Интриган',
  support: 'Поддержка',
  monster: 'Монстр',
  custom: 'Особая',
}, {
  brute: 'Brute',
  skirmisher: 'Skirmisher',
  archer: 'Archer',
  caster: 'Caster',
  leader: 'Leader',
  social: 'Schemer',
  support: 'Support',
  monster: 'Monster',
  custom: 'Custom',
})

export const NPC_VISIBILITY_LABELS: Record<NpcVisibility, string> = t({
  private: 'Приватный',
  campaignVisible: 'Виден в кампании',
  publicTemplate: 'Публичный шаблон',
}, {
  private: 'Private',
  campaignVisible: 'Visible in campaign',
  publicTemplate: 'Public template',
})

export const NPC_POWER_LABELS: Record<NpcPowerLevel, string> = t({
  weak: 'Слабый',
  standard: 'Обычный',
  strong: 'Сильный',
  elite: 'Элитный',
}, {
  weak: 'Weak',
  standard: 'Standard',
  strong: 'Strong',
  elite: 'Elite',
})

export const NPC_COMBAT_STYLE_LABELS: Record<NpcCombatStyle, string> = t({
  melee: 'Ближний бой',
  ranged: 'Дальний бой',
  magic: 'Магия',
  social: 'Социальный',
}, {
  melee: 'Melee',
  ranged: 'Ranged',
  magic: 'Magic',
  social: 'Social',
})

export const CREATURE_TEMPLATE_LABELS: Record<CreatureTemplate, string> = t({
  none: 'Без шаблона (гуманоид)',
  undead: 'Нежить',
  beast: 'Зверь',
  dragon: 'Дракон',
  demon: 'Демон',
  construct: 'Конструкт',
}, {
  none: 'No template (humanoid)',
  undead: 'Undead',
  beast: 'Beast',
  dragon: 'Dragon',
  demon: 'Demon',
  construct: 'Construct',
})
export const CREATURE_TEMPLATES: CreatureTemplate[] = ['none', 'undead', 'beast', 'dragon', 'demon', 'construct']

export const NPC_KINDS: NpcKind[] = ['minion', 'rival', 'nemesis']
export const NPC_ROLES: NpcRole[] = [
  'brute', 'skirmisher', 'archer', 'caster', 'leader', 'social', 'support', 'monster', 'custom',
]

export const PARTICIPANT_TYPE_LABELS: Record<ParticipantType, string> = t({
  playerCharacter: 'Персонаж',
  npc: 'NPC',
  minionGroup: 'Группа миньонов',
  hazard: 'Осложнение',
}, {
  playerCharacter: 'Player character',
  npc: 'NPC',
  minionGroup: 'Minion group',
  hazard: 'Hazard',
})

export const SLOT_TYPE_LABELS: Record<InitiativeSlotType, string> = t({
  player: 'Игроки',
  npc: 'NPC',
  neutral: 'Нейтрал',
}, {
  player: 'Players',
  npc: 'NPC',
  neutral: 'Neutral',
})

export const ENCOUNTER_TYPE_LABELS: Record<EncounterType, string> = t({
  combat: 'Бой',
  social: 'Социальный',
  exploration: 'Исследование',
  chase: 'Погоня',
  investigation: 'Расследование',
  travel: 'Путешествие',
  hazard: 'Опасность',
  mixed: 'Смешанный',
  custom: 'Особый',
}, {
  combat: 'Combat',
  social: 'Social',
  exploration: 'Exploration',
  chase: 'Chase',
  investigation: 'Investigation',
  travel: 'Travel',
  hazard: 'Hazard',
  mixed: 'Mixed',
  custom: 'Custom',
})

export const ENCOUNTER_TYPES: EncounterType[] = [
  'combat', 'social', 'exploration', 'chase', 'investigation', 'travel', 'hazard', 'mixed', 'custom',
]

export const THREAT_LEVEL_LABELS: Record<ThreatLevel, string> = t({
  trivial: 'Тривиальный',
  easy: 'Лёгкий',
  standard: 'Стандартный',
  hard: 'Тяжёлый',
  deadly: 'Смертельный',
}, {
  trivial: 'Trivial',
  easy: 'Easy',
  standard: 'Standard',
  hard: 'Hard',
  deadly: 'Deadly',
})

export const THREAT_LEVELS: ThreatLevel[] = ['trivial', 'easy', 'standard', 'hard', 'deadly']

export const CONTENT_ENTRY_TYPE_LABELS: Record<ContentEntryType, string> = t({
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
}, {
  archetype: 'Archetype',
  career: 'Career',
  skill: 'Skill',
  talent: 'Talent',
  item: 'Item',
  heroicAbility: 'Heroic ability',
  spell: 'Spell',
  magicAction: 'Magic action',
  alchemyRecipe: 'Alchemy recipe',
  rune: 'Rune',
  houseRule: 'House rule',
  customNote: 'Note',
})

export const CONTENT_ENTRY_TYPES: ContentEntryType[] = [
  'talent', 'item', 'career', 'archetype', 'skill', 'heroicAbility',
  'spell', 'magicAction', 'alchemyRecipe', 'rune', 'houseRule', 'customNote',
]

export const ALLOWED_STATE_LABELS: Record<AllowedState, string> = t({
  allowed: 'Разрешено',
  disallowed: 'Запрещено',
  askGm: 'С разрешения мастера',
}, {
  allowed: 'Allowed',
  disallowed: 'Disallowed',
  askGm: 'Ask the GM',
})

export const ALLOWED_STATES: AllowedState[] = ['allowed', 'disallowed', 'askGm']

export const HOUSE_RULE_CATEGORY_LABELS: Record<HouseRuleCategory, string> = t({
  none: '—',
  characterCreation: 'Создание персонажа',
  combat: 'Бой',
  magic: 'Магия',
  equipment: 'Снаряжение',
  xp: 'Опыт (XP)',
  campaignTone: 'Тон кампании',
  custom: 'Особая',
}, {
  none: '—',
  characterCreation: 'Character creation',
  combat: 'Combat',
  magic: 'Magic',
  equipment: 'Equipment',
  xp: 'Experience (XP)',
  campaignTone: 'Campaign tone',
  custom: 'Custom',
})

export const HOUSE_RULE_CATEGORIES: HouseRuleCategory[] = [
  'characterCreation', 'combat', 'magic', 'equipment', 'xp', 'campaignTone', 'custom',
]

/** Стоимость таланта тира N — 5 × N XP. */
export const talentCost = (tier: number) => tier * 5

/** Эффективный тир следующего ранга рангового таланта (каждый ранг — на тир выше, максимум 5). */
export const nextRankTier = (baseTier: number, ranksOwned: number) =>
  Math.min(baseTier + ranksOwned, 5)
