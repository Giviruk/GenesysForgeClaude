import type { DicePool } from '../api/types'
import { t } from '../i18n'

/**
 * Жёлтые (Proficiency) и зелёные (Ability) кубы пула, а также чёрные кости помех, которые
 * персонаж тащит на себе постоянно (ROT-ARM-01, ROT-EQP-01): броня и перегруз. Помехи —
 * часть пула, поэтому они видны рядом с ним, а не только в блоке веса.
 */
export function DicePoolView({ pool, setback = 0, setbackTitle }: {
  pool: DicePool
  setback?: number
  /** Расшифровка источников помех для подсказки. */
  setbackTitle?: string
}) {
  const poolTitle = t(`${pool.proficiency} мастерства + ${pool.ability} способности`,
    `${pool.proficiency} proficiency + ${pool.ability} ability`)
  return (
    <span className="dice-pool" title={setbackTitle ? `${poolTitle}\n${setbackTitle}` : poolTitle}>
      {Array.from({ length: pool.proficiency }).map((_, i) => (
        <span key={`p${i}`} className="die proficiency">⬣</span>
      ))}
      {Array.from({ length: pool.ability }).map((_, i) => (
        <span key={`a${i}`} className="die ability">◆</span>
      ))}
      {Array.from({ length: Math.max(0, setback) }).map((_, i) => (
        <span key={`s${i}`} className="die setback">■</span>
      ))}
      {pool.proficiency === 0 && pool.ability === 0 && setback === 0 && <span className="muted">—</span>}
    </span>
  )
}
