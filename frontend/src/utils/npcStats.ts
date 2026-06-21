import type {
  Characteristic, DicePool, ItemDef, NpcDetail, Reference, SkillDef,
} from '../api/types'
import { resolveWeaponSkillName } from './labels'

/** Отображаемое имя справочной записи: русское, с откатом на оригинальное. */
const refLabel = (o: { name: string; nameRu: string }) => o.nameRu || o.name

/**
 * Дайс-пул проверки навыка: большее из (характеристика, ранги) кубов,
 * из них min(хар, ранги) улучшаются до жёлтых Proficiency. Зеркалит
 * серверную GenesysRules.BuildDicePool, чтобы NPC считались как персонажи.
 */
export function buildPool(characteristic: number, ranks: number): DicePool {
  const c = Math.max(0, characteristic)
  const r = Math.max(0, ranks)
  const proficiency = Math.min(c, r)
  const ability = Math.max(c, r) - proficiency
  return { ability, proficiency }
}

/** Индекс «имя навыка (рус/англ) → справочная запись» для текущей системы. */
export function skillIndex(reference: Reference | null): Map<string, SkillDef> {
  const m = new Map<string, SkillDef>()
  if (!reference) return m
  for (const s of reference.skills) {
    m.set(refLabel(s), s)
    m.set(s.name, s)
  }
  return m
}

/** Характеристика навыка по справочнику; null для кастомного/неизвестного навыка. */
export function skillCharacteristic(name: string, index: Map<string, SkillDef>): Characteristic | null {
  return index.get(name)?.characteristic ?? null
}

/** Навык NPC с посчитанным пулом броска (пул null, если характеристика навыка неизвестна). */
export interface NpcSkillView {
  name: string
  ranks: number
  characteristic: Characteristic | null
  pool: DicePool | null
}

export function npcSkillViews(npc: NpcDetail, index: Map<string, SkillDef>): NpcSkillView[] {
  return npc.skills.map(s => {
    const characteristic = skillCharacteristic(s.name, index)
    const pool = characteristic ? buildPool(npc[characteristic], s.ranks) : null
    return { name: s.name, ranks: s.ranks, characteristic, pool }
  })
}

/** Оружие NPC: справочная запись + пул атаки (характеристика навыка оружия + ранги NPC в нём). */
export interface NpcWeaponView {
  /** Имя из снаряжения NPC (как сохранено). */
  name: string
  item: ItemDef
  /** Урон с раскрытием «+N» в ближнем бою как Мощь+N. */
  damageText: string
  crit: string
  rangeBand: string
  properties: string
  /** Подпись навыка броска (рус), null если навык оружия не найден в справочнике. */
  skillLabel: string | null
  /** Пул атаки; null, если навык оружия не сопоставлен. */
  pool: DicePool | null
}

/**
 * Сопоставляет строку снаряжения со справочным оружием и считает пул атаки.
 * Возвращает null, если запись — не оружие или не найдена в справочнике системы.
 */
export function npcWeaponView(equipmentName: string, npc: NpcDetail, reference: Reference | null): NpcWeaponView | null {
  if (!reference) return null
  const item = reference.items.find(i => refLabel(i) === equipmentName || i.name === equipmentName)
  if (!item || item.kind !== 'weapon') return null

  // Навык оружия хранится по-английски («Melee», «Ranged (Light)»); ищем его в справочнике.
  const skillName = resolveWeaponSkillName(item.skillName, reference.skills.map(s => s.name))
  const skillDef = skillName ? reference.skills.find(s => s.name === skillName) ?? null : null

  let pool: DicePool | null = null
  let skillLabel: string | null = null
  if (skillDef) {
    skillLabel = refLabel(skillDef)
    // Ранги NPC в навыке оружия (0, если навык не прокачан) — атака всё равно катится по характеристике.
    const ranks = npc.skills.find(s => s.name === skillLabel || s.name === skillDef.name)?.ranks ?? 0
    pool = buildPool(npc[skillDef.characteristic], ranks)
  }

  // Урон «+N» в ближнем бою — прибавка к Мощи; абсолютное число — итоговый урон.
  const dmg = item.damage.trim()
  let damageText = dmg
  if (dmg.startsWith('+')) {
    const bonus = Number(dmg.slice(1))
    if (Number.isFinite(bonus)) damageText = `${npc.brawn + bonus} (Мощь ${dmg})`
  }

  return {
    name: equipmentName,
    item,
    damageText: damageText || '—',
    crit: item.crit,
    rangeBand: item.rangeBand,
    properties: item.properties,
    skillLabel,
    pool,
  }
}

/** Прочее снаряжение NPC (броня/предметы) с привязкой к каталогу для описания и бонусов. */
export interface NpcGearView {
  /** Имя из снаряжения NPC (как сохранено). */
  name: string
  /** Справочная запись, если имя нашлось в каталоге (для описания и бонусов брони); иначе null. */
  item: ItemDef | null
}

/** Делит снаряжение NPC на оружие (с пулами) и прочие предметы (с привязкой к каталогу). */
export function splitEquipment(npc: NpcDetail, reference: Reference | null): {
  weapons: NpcWeaponView[]
  gear: NpcGearView[]
} {
  const weapons: NpcWeaponView[] = []
  const gear: NpcGearView[] = []
  for (const name of npc.equipment) {
    const w = npcWeaponView(name, npc, reference)
    if (w) { weapons.push(w); continue }
    const item = reference?.items.find(i => (i.nameRu || i.name) === name || i.name === name) ?? null
    gear.push({ name, item })
  }
  return { weapons, gear }
}
