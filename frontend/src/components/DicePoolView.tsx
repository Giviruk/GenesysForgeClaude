import type { DicePool } from '../api/types'
import { t } from '../i18n'

/** Жёлтые (Proficiency) и зелёные (Ability) кубы пула. */
export function DicePoolView({ pool }: { pool: DicePool }) {
  return (
    <span className="dice-pool" title={t(`${pool.proficiency} мастерства + ${pool.ability} способности`, `${pool.proficiency} proficiency + ${pool.ability} ability`)}>
      {Array.from({ length: pool.proficiency }).map((_, i) => (
        <span key={`p${i}`} className="die proficiency">⬣</span>
      ))}
      {Array.from({ length: pool.ability }).map((_, i) => (
        <span key={`a${i}`} className="die ability">◆</span>
      ))}
      {pool.proficiency === 0 && pool.ability === 0 && <span className="muted">—</span>}
    </span>
  )
}
