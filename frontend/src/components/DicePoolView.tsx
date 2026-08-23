import type { DicePool } from '../api/types'
import { t } from '../i18n'

/**
 * Жёлтые (Proficiency) и зелёные (Ability) кубы пула, а также чёрные кости помех, которые
 * персонаж тащит на себе постоянно (ROT-ARM-01, ROT-EQP-01): броня и перегруз. Помехи —
 * часть пула, поэтому они видны рядом с ним, а не только в блоке веса.
 */
export function DicePoolView({ pool, setback = 0, setbackTitle, boost = 0, difficulty = 0, challenge = 0 }: {
  pool: DicePool
  setback?: number
  /** Расшифровка источников помех для подсказки. */
  setbackTitle?: string
  /** Бонусные кости от качеств оружия (Точное) — GEN-EQP-QUAL-01. */
  boost?: number
  /** Сложность, которую задаёт само оружие или нехватка характеристики (Громоздкое, Сноровка). */
  difficulty?: number
  /** Красные кости после усиления сложности (критические травмы). */
  challenge?: number
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
      {Array.from({ length: Math.max(0, boost) }).map((_, i) => (
        <span key={`b${i}`} className="die boost">□</span>
      ))}
      {Array.from({ length: Math.max(0, setback) }).map((_, i) => (
        <span key={`s${i}`} className="die setback">■</span>
      ))}
      {Array.from({ length: Math.max(0, difficulty) }).map((_, i) => (
        <span key={`d${i}`} className="die difficulty">◆</span>
      ))}
      {Array.from({ length: Math.max(0, challenge) }).map((_, i) => (
        <span key={`c${i}`} className="die challenge">⬣</span>
      ))}
      {pool.proficiency === 0 && pool.ability === 0 && setback === 0 && boost === 0 && difficulty === 0 && challenge === 0
        && <span className="muted">—</span>}
    </span>
  )
}
