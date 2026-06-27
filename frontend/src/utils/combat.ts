// Боевой расчёт (U-17): раскрытие урона оружия и итог с учётом нетто-успехов.
import type { Quality } from '../api/types'

/** Качество боя для отображения: подпись + цена активации (если есть). */
export interface CombatQuality {
  label: string
  activationCost: string
}

/**
 * Раскрывает урон оружия: «+N» в ближнем бою = Мощь+N; абсолютное число — как есть.
 * `base` = числовой базовый урон (null, если не распарсилось); `text` — человекочитаемо.
 */
export function expandDamage(damage: string, brawn: number): { base: number | null; text: string } {
  const dmg = (damage ?? '').trim()
  if (dmg === '') return { base: null, text: '—' }
  if (dmg.startsWith('+')) {
    const bonus = Number(dmg.slice(1))
    if (Number.isFinite(bonus)) return { base: brawn + bonus, text: `${brawn + bonus} (Мощь ${dmg})` }
    return { base: null, text: dmg }
  }
  const abs = Number(dmg)
  return Number.isFinite(abs) ? { base: abs, text: `${abs}` } : { base: null, text: dmg }
}

/** Итоговый урон: базовый + нетто-успехи (каждый успех = +1 урон). null base → null. */
export function combatTotal(base: number | null, netSuccess: number): number | null {
  return base == null ? null : base + Math.max(0, netSuccess)
}

/** Нормализация имени качества для сопоставления по справочнику: нижний регистр, ё→е, без хвостового рейтинга. */
function normQuality(name: string): string {
  return name.toLowerCase().replace('ё', 'е').replace(/\s*\d+\s*$/, '').trim()
}

/**
 * Сопоставляет качества с ценой активации из справочника.
 * `byCode` — качества атаки NPC (есть код); `byName` — строки свойств оружия персонажа (есть только имя).
 */
export function resolveQualityCosts(
  source: { code?: string; label: string; rating: number | null }[],
  reference: { qualities: Quality[] } | null,
): CombatQuality[] {
  const qs = reference?.qualities ?? []
  const byCode = new Map(qs.map(q => [q.code, q]))
  const byName = new Map(qs.map(q => [normQuality(q.nameRu || q.nameEn), q]))
  return source.map(s => {
    const q = (s.code ? byCode.get(s.code) : undefined) ?? byName.get(normQuality(s.label))
    const ratingSuffix = s.rating != null ? ` ${s.rating}` : ''
    return { label: `${s.label}${ratingSuffix}`, activationCost: q?.activationCost ?? '' }
  })
}

/** Разбирает строку свойств предмета («Точное 1, Оборонительное 2») на качества для боевого расчёта. */
export function qualitiesFromProperties(
  properties: string, reference: { qualities: Quality[] } | null,
): CombatQuality[] {
  const parts = (properties ?? '').split(',').map(p => p.trim()).filter(Boolean)
  const source = parts.map(p => {
    const m = /\s*(\d+)\s*$/.exec(p)
    return { label: m ? p.slice(0, m.index).trim() : p, rating: m ? Number(m[1]) : null }
  })
  return resolveQualityCosts(source, reference)
}
