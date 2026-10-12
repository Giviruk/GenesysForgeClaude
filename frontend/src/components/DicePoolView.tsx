import type { DicePool } from '../api/types'
import { t } from '../i18n'
import { GenesysSymbol } from './GenesysSymbol'

/**
 * Жёлтые (Proficiency) и зелёные (Ability) кубы пула, а также чёрные кости помех, которые
 * персонаж тащит на себе постоянно (ROT-ARM-01, ROT-EQP-01): броня и перегруз. Помехи —
 * часть пула, поэтому они видны рядом с ним, а не только в блоке веса.
 */
export function DicePoolView({
  pool, setback = 0, setbackTitle, boost = 0, difficulty = 0, challenge = 0, difficultyUpgrades = 0, previousPool,
}: {
  pool: DicePool
  previousPool?: DicePool
  setback?: number
  /** Расшифровка источников помех для подсказки. */
  setbackTitle?: string
  /** Бонусные кости от качеств оружия (Точное) — GEN-EQP-QUAL-01. */
  boost?: number
  /** Сложность, которую задаёт само оружие или нехватка характеристики (Громоздкое, Сноровка). */
  difficulty?: number
  /** Красные кости, уже стоящие в пуле. */
  challenge?: number
  /**
   * Усиления сложности (критические травмы). Это не красные кости: при броске они превращают
   * фиолетовые кости проверки в красные, а базовой сложности здесь ещё нет — поэтому маркер.
   */
  difficultyUpgrades?: number
}) {
  const poolTitle = t(`${pool.proficiency} мастерства + ${pool.ability} способности`,
    `${pool.proficiency} proficiency + ${pool.ability} ability`)
  return (
    <span className="dice-pool" title={setbackTitle ? `${poolTitle}\n${setbackTitle}` : poolTitle}>
      {Array.from({ length: pool.proficiency }).map((_, i) => (
        <GenesysSymbol key={`p${i}`} kind="proficiency" className={`die proficiency${previousPool && i >= previousPool.proficiency ? ' rd-preview-die' : ''}`} />
      ))}
      {Array.from({ length: pool.ability }).map((_, i) => (
        <GenesysSymbol key={`a${i}`} kind="ability" className={`die ability${previousPool && i >= previousPool.ability ? ' rd-preview-die' : ''}`} />
      ))}
      {Array.from({ length: Math.max(0, boost) }).map((_, i) => (
        <GenesysSymbol key={`b${i}`} kind="boost" className="die boost" />
      ))}
      {Array.from({ length: Math.max(0, setback) }).map((_, i) => (
        <GenesysSymbol key={`s${i}`} kind="setback" className="die setback" />
      ))}
      {Array.from({ length: Math.max(0, difficulty) }).map((_, i) => (
        <GenesysSymbol key={`d${i}`} kind="difficulty" className="die difficulty" />
      ))}
      {Array.from({ length: Math.max(0, challenge) }).map((_, i) => (
        <GenesysSymbol key={`c${i}`} kind="challenge" className="die challenge" />
      ))}
      {difficultyUpgrades > 0 && (
        <span className="die-upgrade" title={t(
          `Усиление сложности ×${difficultyUpgrades}: при броске кость сложности превращается в кость вызова`,
          `Difficulty upgrade ×${difficultyUpgrades}: on the roll a difficulty die becomes a challenge die`,
        )}>↑{difficultyUpgrades > 1 ? difficultyUpgrades : ''}</span>
      )}
      {pool.proficiency === 0 && pool.ability === 0 && setback === 0 && boost === 0 && difficulty === 0 && challenge === 0
        && difficultyUpgrades <= 0
        && <span className="muted">—</span>}
    </span>
  )
}
