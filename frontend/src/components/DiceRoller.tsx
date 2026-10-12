import { useState } from 'react'
import {
  DIE_KINDS, SYMBOL_ORDER, applyDifficultyUpgrades, emptyPool, poolSize, rollPool, summarize,
  type DieKind, type RollPool, type RollSymbols, type RollOutcome,
} from '../utils/diceRoller'
import { t } from '../i18n'
import { GENESYS_SYMBOL_LABELS } from '../data/genesysSymbols'
import type { AdvantageSpendContext, AdvantageSpendOption } from '../utils/advantageSpends'
import { OutcomeSpendGuide } from './OutcomeSpendGuide'
import { DicePoolView } from './DicePoolView'
import { GenesysSymbol } from './GenesysSymbol'

/** Заявка на запись броска в лог стола. */
export interface RollLogRequest {
  poolJson: string
  resultJson: string
  summary: string
  label: string
  isSecret: boolean
}

interface Props {
  initialPool?: Partial<RollPool>
  /**
   * Усиления сложности (например, от критических травм). Применяются к собранному пулу
   * в момент броска: базовую сложность проверки выставляет игрок или мастер.
   */
  difficultyUpgrades?: number
  /** Что бросаем (навык/описание) — попадает в лог. */
  label?: string
  /** Если задан — после броска результат пишется в лог стола. Без него — локальный бросок. */
  onLog?: (req: RollLogRequest) => void
  /** Можно ли делать секретный бросок (только GM). */
  canSecret?: boolean
  /** Вызывается с исходом каждого броска (для боевого расчёта урона поверх roller). */
  onResult?: (outcome: RollOutcome) => void
  /** Контекст таблиц результатов: общий, боевой, социальный, магический или транспортный. */
  spendContext?: AdvantageSpendContext
  /** Активации конкретного оружия, магического эффекта и другие расходы этого броска. */
  advantageSpends?: AdvantageSpendOption[]
}

const DIE_DETAIL: Record<DieKind, string> = t({
  ability: 'зел. d8', proficiency: 'жёлт. d12', difficulty: 'фиол. d8',
  challenge: 'красн. d12', boost: 'син. d6', setback: 'чёрн. d6',
}, {
  ability: 'green d8', proficiency: 'yellow d12', difficulty: 'purple d8',
  challenge: 'red d12', boost: 'blue d6', setback: 'black d6',
})

export function DiceRoller({
  initialPool, difficultyUpgrades = 0, label, onLog, canSecret, onResult,
  spendContext = 'general', advantageSpends = [],
}: Props) {
  const [pool, setPool] = useState<RollPool>({ ...emptyPool(), ...initialPool })
  const [upgrades, setUpgrades] = useState(Math.max(0, difficultyUpgrades))
  const [outcome, setOutcome] = useState<RollOutcome | null>(null)
  const [secret, setSecret] = useState(false)

  // Бросается и пишется в лог пул уже после усилений, а счётчики показывают собранную базу.
  const rolled = applyDifficultyUpgrades(pool, upgrades)
  const total = poolSize(rolled)
  const bump = (kind: DieKind, delta: number) =>
    setPool(p => ({ ...p, [kind]: Math.max(0, Math.min(20, p[kind] + delta)) }))
  const bumpUpgrades = (delta: number) => setUpgrades(u => Math.max(0, Math.min(20, u + delta)))

  function reset() {
    setPool({ ...emptyPool(), ...initialPool })
    setUpgrades(Math.max(0, difficultyUpgrades))
  }

  function roll() {
    if (total === 0) return
    const result = rollPool(rolled)
    setOutcome(result)
    onResult?.(result)
    if (onLog) {
      onLog({
        poolJson: JSON.stringify(rolled),
        resultJson: JSON.stringify(result.net),
        summary: summarize(result.net),
        label: label ?? '',
        isSecret: canSecret ? secret : false,
      })
    }
  }

  return (
    <div className="dice-roller">
      <div className="dr-pool">
        {DIE_KINDS.map(kind => (
          <div key={kind} className={`dr-die dr-${kind}`} title={`${GENESYS_SYMBOL_LABELS[kind]} (${DIE_DETAIL[kind]})`}>
            <GenesysSymbol title={false} kind={kind} className="dr-die-glyph" />
            <span className="dr-die-count">{pool[kind]}</span>
            <span className="dr-die-btns">
              <button type="button" className="tiny" onClick={() => bump(kind, 1)} aria-label={`+${kind}`}>+</button>
              <button type="button" className="tiny" onClick={() => bump(kind, -1)} aria-label={`-${kind}`}>−</button>
            </span>
          </div>
        ))}
      </div>

      <div className="dr-upgrades">
        <span className="dr-upgrades-label" title={t(
          'Каждое усиление превращает кость сложности в кость вызова; если костей сложности нет — добавляет кость сложности.',
          'Each upgrade turns a difficulty die into a challenge die; with no difficulty dice left it adds a difficulty die.',
        )}>{t('Усиление сложности', 'Difficulty upgrades')}</span>
        <span className="dr-die-count">{upgrades}</span>
        <span className="dr-die-btns">
          <button type="button" className="tiny" onClick={() => bumpUpgrades(1)}
            aria-label={t('+усиление сложности', '+difficulty upgrade')}>+</button>
          <button type="button" className="tiny" onClick={() => bumpUpgrades(-1)}
            aria-label={t('−усиление сложности', '−difficulty upgrade')}>−</button>
        </span>
        {upgrades > 0 && (
          <span className="dr-rolled-pool">
            {t('Бросается:', 'Rolled:')}{' '}
            <DicePoolView pool={{ ability: rolled.ability, proficiency: rolled.proficiency }}
              boost={rolled.boost} setback={rolled.setback}
              difficulty={rolled.difficulty} challenge={rolled.challenge} />
          </span>
        )}
      </div>

      <div className="dr-actions">
        <button type="button" className="primary" onClick={roll} disabled={total === 0}>
          {t('🎲 Бросить', '🎲 Roll')}{total > 0 ? ` (${total})` : ''}
        </button>
        <button type="button" className="small" onClick={reset}>{t('Сброс', 'Reset')}</button>
        {canSecret && (
          <label className="checkbox dr-secret" title={t('Виден только мастеру', 'Visible only to the GM')}>
            <input type="checkbox" checked={secret} onChange={e => setSecret(e.target.checked)} /> {t('Секретно', 'Secret')}
          </label>
        )}
      </div>

      {outcome && (
        <div className="dr-result">
          <div className="dr-result-summary">
            <RollSymbolsView symbols={outcome.net} />
            <span className="dr-summary">{summarize(outcome.net)}</span>
          </div>
          <OutcomeSpendGuide result={outcome.net}
            context={spendContext} extra={advantageSpends} />
        </div>
      )}
    </div>
  )
}

/** Показ нетто-символов броска как набор бейджей. */
export function RollSymbolsView({ symbols }: { symbols: RollSymbols }) {
  const shown = SYMBOL_ORDER.filter(s => symbols[s] > 0)
  if (shown.length === 0) return <span className="muted">{t('ничья', 'wash')}</span>
  return (
    <span className="dr-symbols">
      {shown.map(s => (
        <span key={s} className={`roll-sym roll-${s}`} title={GENESYS_SYMBOL_LABELS[s]}>
          <GenesysSymbol kind={s} /> {symbols[s]}
        </span>
      ))}
    </span>
  )
}
