import { useState } from 'react'
import type { RollOutcome, RollPool } from '../utils/diceRoller'
import { combatTotal, expandDamage, type CombatQuality } from '../utils/combat'
import { DiceRoller, RollSymbolsView, type RollLogRequest } from './DiceRoller'
import { t } from '../i18n'

interface Props {
  /** Имя атаки/оружия. */
  title: string
  /** Подпись навыка броска (для шапки), null если навык не определён. */
  skillLabel: string | null
  /** Базовый пул из навыка (ability/proficiency); GM добавляет сложность/бонусы вручную. */
  basePool: Partial<RollPool>
  /** Урон оружия как хранится («+N» или абсолют). */
  damage: string
  /** Мощь бойца — для раскрытия «+N» урона. */
  brawn: number
  crit: string
  rangeBand: string
  qualities: CombatQuality[]
  onClose: () => void
  /** Логирование в стол (если есть кампания); без него — локальный расчёт. */
  onLog?: (req: RollLogRequest) => void
  canSecret?: boolean
}

/**
 * Боевой roller (U-17): собирает пул от атаки, бросает (через нарративный DiceRoller),
 * показывает базовый урон + нетто-успехи = итог и качества с ценой активации. Решения — за мастером.
 */
export function CombatRoller({
  title, skillLabel, basePool, damage, brawn, crit, rangeBand, qualities, onClose, onLog, canSecret,
}: Props) {
  const [outcome, setOutcome] = useState<RollOutcome | null>(null)
  const dmg = expandDamage(damage, brawn)
  const netSuccess = outcome?.net.success ?? 0
  const total = combatTotal(dmg.base, netSuccess)

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal" onClick={e => e.stopPropagation()}>
        <h3>{t(`🎲 Атака — ${title}`, `🎲 Attack — ${title}`)}</h3>
        <div className="combat-roller-head">
          {skillLabel && <span className="muted">{t('Навык:', 'Skill:')} {skillLabel}</span>}
          <div className="npc-weapon-stats">
            <span className="weapon-stat">{t('Урон', 'Damage')} <strong>{dmg.text}</strong></span>
            {crit && <span className="weapon-stat">{t('Крит', 'Crit')} <strong>{crit}</strong></span>}
            {rangeBand && <span className="weapon-stat">{rangeBand}</span>}
          </div>
          {qualities.length > 0 && (
            <ul className="combat-qualities">
              {qualities.map((q, i) => (
                <li key={i}>
                  <strong>{q.label}</strong>
                  {q.activationCost && <span className="muted small-text"> — {q.activationCost}</span>}
                </li>
              ))}
            </ul>
          )}
        </div>

        <p className="hint">{t('Базовый пул собран по навыку. Добавьте сложность/бонусы/помехи и бросьте — урон не решает за вас.', 'The base pool is built from the skill. Add difficulty/boosts/setbacks and roll — damage is not applied automatically.')}</p>
        <DiceRoller initialPool={basePool} label={title} onResult={setOutcome} onLog={onLog} canSecret={canSecret} />

        {outcome && (
          <div className="combat-damage">
            <RollSymbolsView symbols={outcome.net} />
            {total != null
              ? <div className="combat-damage-calc">
                  {t('Урон:', 'Damage:')} <strong>{dmg.base}</strong> {t('+ успехов', '+ successes')} <strong>{netSuccess}</strong> = <strong>{total}</strong>
                  {netSuccess === 0 && outcome.net.failure > 0 && <span className="muted small-text"> {t('(промах — нет успехов)', '(miss — no successes)')}</span>}
                </div>
              : <div className="muted small-text">{t('Урон оружия задан текстом — посчитайте вручную.', 'Weapon damage is text-only — calculate it manually.')}</div>}
          </div>
        )}

        <div className="modal-actions">
          <button type="button" onClick={onClose}>{t('Закрыть', 'Close')}</button>
        </div>
      </div>
    </div>
  )
}
