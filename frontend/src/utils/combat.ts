// Боевой расчёт (U-17): раскрытие урона оружия и итог с учётом нетто-успехов.
import type { Quality } from '../api/types'
import { t } from '../i18n'
import { parseAdvantageCost, type AdvantageSpendOption } from './advantageSpends'

/** Качество боя для отображения: подпись + цена активации (если есть). */
export interface CombatQuality {
  label: string
  activationCost: string
}

/** Активации конкретных качеств оружия, которые можно предложить рядом с общими боевыми тратами. */
export function qualityAdvantageSpends(qualities: CombatQuality[]): AdvantageSpendOption[] {
  return qualities.flatMap((quality, index) => {
    const cost = parseAdvantageCost(quality.activationCost)
    if (cost == null) return []
    const name = normQuality(quality.label)
    const worksOnMiss = ['sunder', 'повреждение', 'guided', 'наведение'].includes(name)
    const normal: AdvantageSpendOption = {
      id: `quality-${index}-${quality.label}`,
      cost,
      labelRu: `Активировать «${quality.label}»`,
      labelEn: `Activate “${quality.label}”`,
      detailRu: quality.activationCost,
      detailEn: quality.activationCost,
      requiresSuccess: !worksOnMiss,
    }
    if (!['blast', 'взрыв'].includes(name)) return [normal]
    return [
      normal,
      {
        ...normal,
        id: `${normal.id}-miss`,
        cost: 3,
        labelRu: `Активировать «${quality.label}» при промахе`,
        labelEn: `Activate “${quality.label}” on a miss`,
        requiresSuccess: false,
        requiresFailure: true,
      },
    ]
  })
}

/** Критическая травма имеет известную цену только при числовом критическом значении. */
export function criticalAdvantageSpend(critical: string): AdvantageSpendOption[] {
  const cost = Number(critical)
  if (!Number.isFinite(cost) || cost <= 0) return []
  return [{
    id: 'attack-critical',
    cost,
    labelRu: 'Нанести критическую травму',
    labelEn: 'Inflict a critical injury',
    detailRu: 'Только после попадания, которое нанесло урон после поглощения.',
    detailEn: 'Only after a hit that dealt damage past soak.',
    requiresSuccess: true,
  }]
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
    if (Number.isFinite(bonus)) return { base: brawn + bonus, text: `${brawn + bonus} ${t(`(Мощь ${dmg})`, `(Brawn ${dmg})`)}` }
    return { base: null, text: dmg }
  }
  const abs = Number(dmg)
  return Number.isFinite(abs) ? { base: abs, text: `${abs}` } : { base: null, text: dmg }
}

/**
 * Итоговый урон попадания: базовый + нетто-успехи (каждый успех = +1 урон).
 * Промах (успехов не осталось) обычного урона не наносит, поэтому возвращается `null`, а не
 * базовый урон оружия — иначе интерфейс показывал бы урон там, где атака не попала (ROT-CMB-01).
 * Итог остаётся подсказкой: авторитетное разрешение атаки делает сервер.
 */
export function combatTotal(base: number | null, netSuccess: number): number | null {
  if (base == null || netSuccess <= 0) return null
  return base + netSuccess
}

/** Попала ли атака: только положительные нетто-успехи. Оставшийся триумф промах не спасает. */
export function isHit(netSuccess: number): boolean {
  return netSuccess > 0
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
  // Строки свойств могли быть сохранены на любом языке — регистрируем оба имени.
  const byName = new Map(qs.flatMap(q =>
    [q.nameRu, q.nameEn].filter(Boolean).map(n => [normQuality(n), q] as const)))
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
