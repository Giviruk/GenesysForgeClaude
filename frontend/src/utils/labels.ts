import type { Characteristic, GameSystem, ItemKind, ItemState, SkillKind } from '../api/types'

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

/** Стоимость таланта тира N — 5 × N XP. */
export const talentCost = (tier: number) => tier * 5

/** Эффективный тир следующего ранга рангового таланта (каждый ранг — на тир выше, максимум 5). */
export const nextRankTier = (baseTier: number, ranksOwned: number) =>
  Math.min(baseTier + ranksOwned, 5)
