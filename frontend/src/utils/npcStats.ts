import type {
  Characteristic, DicePool, ItemDef, NpcAttackEntry, NpcDetail, Reference, SkillDef,
} from '../api/types'
import { localizedName, resolveWeaponSkillName } from './labels'
import { t } from '../i18n'

/** Отображаемое имя справочной записи на языке интерфейса. */
const refLabel = (o: { name: string; nameRu: string }) => localizedName(o)

/**
 * Совпадает ли сохранённое имя (снаряжение/навык NPC) со справочной записью.
 * Сохранённые строки могли быть записаны и по-русски, и по-английски —
 * сверяем с обоими именами независимо от текущего языка интерфейса.
 */
const matchesRef = (o: { name: string; nameRu: string }, saved: string) =>
  saved === o.name || (!!o.nameRu && saved === o.nameRu)

/** Структурная атака из оружия каталога: навык/урон/крит/дистанция/качества переносятся из предмета. */
export function attackFromItem(item: ItemDef): NpcAttackEntry {
  return {
    name: refLabel(item),
    skillName: item.skillName,
    damage: item.damage,
    critical: item.crit,
    rangeBand: item.rangeBand,
    notes: '',
    qualities: item.qualities.map(q => ({ qualityCode: q.code, nameRu: q.nameRu || q.nameEn, rating: q.rating })),
    sourceWeapon: refLabel(item),
  }
}

/**
 * Синхронизирует атаки со снаряжением: для каждого оружия в инвентаре держит производную атаку
 * (ключ — подпись оружия), сохраняя ручные правки; кастомные атаки (sourceWeapon='') не трогает.
 */
export function syncAttacksWithEquipment(
  equipment: string[], attacks: NpcAttackEntry[], weaponsByLabel: Map<string, ItemDef>,
): NpcAttackEntry[] {
  const custom = attacks.filter(a => !a.sourceWeapon)
  const derivedByLabel = new Map(attacks.filter(a => a.sourceWeapon).map(a => [a.sourceWeapon, a]))
  const derived: NpcAttackEntry[] = []
  const seen = new Set<string>()
  for (const label of equipment) {
    const item = weaponsByLabel.get(label)
    if (!item || seen.has(label)) continue
    seen.add(label)
    derived.push(derivedByLabel.get(label) ?? attackFromItem(item)) // переиспользуем — сохраняем правки
  }
  return [...derived, ...custom]
}

/** Карта «подпись оружия → предмет каталога» из справочника (для синхронизации атак со снаряжением). */
export function weaponsByLabel(reference: Reference | null): Map<string, ItemDef> {
  const m = new Map<string, ItemDef>()
  for (const it of reference?.items ?? []) {
    if (it.kind !== 'weapon') continue
    // Регистрируем оба имени: сохранённое снаряжение могло быть записано на любом языке.
    m.set(it.name, it)
    if (it.nameRu) m.set(it.nameRu, it)
  }
  return m
}

/**
 * Пул кубов проверки навыка: большее из (характеристика, ранги) кубов,
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
    m.set(s.name, s)
    if (s.nameRu) m.set(s.nameRu, s)
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
  const item = reference.items.find(i => matchesRef(i, equipmentName))
  if (!item || item.kind !== 'weapon') return null

  // Навык оружия хранится по-английски («Melee», «Ranged (Light)»); ищем его в справочнике.
  const skillName = resolveWeaponSkillName(item.skillName, reference.skills.map(s => s.name))
  const skillDef = skillName ? reference.skills.find(s => s.name === skillName) ?? null : null

  let pool: DicePool | null = null
  let skillLabel: string | null = null
  if (skillDef) {
    skillLabel = refLabel(skillDef)
    // Ранги NPC в навыке оружия (0, если навык не прокачан) — атака всё равно катится по характеристике.
    const ranks = npc.skills.find(s => matchesRef(skillDef, s.name))?.ranks ?? 0
    pool = buildPool(npc[skillDef.characteristic], ranks)
  }

  // Урон «+N» в ближнем бою — прибавка к Мощи; абсолютное число — итоговый урон.
  const dmg = item.damage.trim()
  let damageText = dmg
  if (dmg.startsWith('+')) {
    const bonus = Number(dmg.slice(1))
    if (Number.isFinite(bonus)) damageText = `${npc.brawn + bonus} ${t(`(Мощь ${dmg})`, `(Brawn ${dmg})`)}`
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

/** Качество атаки для отображения: подпись (имя + рейтинг) и исходные поля. */
export interface NpcAttackQualityView {
  label: string
  nameRu: string
  rating: number | null
}

/** Структурная атака NPC с посчитанным пулом броска (зеркалит NpcWeaponView, но без привязки к предмету). */
export interface NpcAttackView {
  name: string
  damageText: string
  crit: string
  rangeBand: string
  notes: string
  qualities: NpcAttackQualityView[]
  /** Подпись навыка броска (рус), null если навык атаки не найден в справочнике. */
  skillLabel: string | null
  /** Пул атаки; null, если навык не сопоставлен. */
  pool: DicePool | null
}

/** Виды структурных атак NPC: считает пул по навыку атаки и раскрывает урон «+N» как Мощь+N. */
export function npcAttackViews(npc: NpcDetail, reference: Reference | null): NpcAttackView[] {
  return npc.attacks.map(a => {
    let pool: DicePool | null = null
    let skillLabel: string | null = null
    if (reference && a.skillName) {
      const skillName = resolveWeaponSkillName(a.skillName, reference.skills.map(s => s.name))
      const skillDef = skillName ? reference.skills.find(s => s.name === skillName) ?? null : null
      if (skillDef) {
        skillLabel = refLabel(skillDef)
        const ranks = npc.skills.find(s => matchesRef(skillDef, s.name))?.ranks ?? 0
        pool = buildPool(npc[skillDef.characteristic], ranks)
      }
    }

    const dmg = a.damage.trim()
    let damageText = dmg
    if (dmg.startsWith('+')) {
      const bonus = Number(dmg.slice(1))
      if (Number.isFinite(bonus)) damageText = `${npc.brawn + bonus} ${t(`(Мощь ${dmg})`, `(Brawn ${dmg})`)}`
    }

    const qualities = a.qualities.map(q => {
      const nameRu = q.nameRu || q.qualityCode
      return { nameRu, rating: q.rating, label: q.rating != null ? `${nameRu} ${q.rating}` : nameRu }
    })

    return { name: a.name, damageText: damageText || '—', crit: a.critical, rangeBand: a.rangeBand, notes: a.notes, qualities, skillLabel, pool }
  })
}

/** Снаряжение NPC (всё небоевое) с привязкой к каталогу для описания и бонусов. */
export function npcGearViews(npc: NpcDetail, reference: Reference | null): NpcGearView[] {
  return npc.equipment.map(name => ({
    name,
    item: reference?.items.find(i => matchesRef(i, name)) ?? null,
  }))
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
    const item = reference?.items.find(i => matchesRef(i, name)) ?? null
    gear.push({ name, item })
  }
  return { weapons, gear }
}
