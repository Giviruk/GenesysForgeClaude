import { useState } from 'react'
import {
  DIE_KINDS, SYMBOL_ORDER, emptyPool, poolSize, rollPool, summarize,
  type DieKind, type DieSymbol, type RollPool, type RollSymbols, type RollOutcome,
} from '../utils/diceRoller'

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
  /** Что бросаем (навык/описание) — попадает в лог. */
  label?: string
  /** Если задан — после броска результат пишется в лог стола. Без него — локальный бросок. */
  onLog?: (req: RollLogRequest) => void
  /** Можно ли делать секретный бросок (только GM). */
  canSecret?: boolean
  /** Вызывается с исходом каждого броска (для боевого расчёта урона поверх roller). */
  onResult?: (outcome: RollOutcome) => void
}

const DIE_META: Record<DieKind, { label: string; glyph: string }> = {
  ability: { label: 'Способности (зел. d8)', glyph: '◆' },
  proficiency: { label: 'Мастерство (жёлт. d12)', glyph: '⬣' },
  difficulty: { label: 'Сложность (фиол. d8)', glyph: '◆' },
  challenge: { label: 'Вызов (красн. d12)', glyph: '⬣' },
  boost: { label: 'Бонус (син. d6)', glyph: '◻' },
  setback: { label: 'Помеха (чёрн. d6)', glyph: '◻' },
}

const SYMBOL_META: Record<DieSymbol, { label: string; glyph: string }> = {
  success: { label: 'Успех', glyph: '✶' },
  failure: { label: 'Провал', glyph: '✸' },
  advantage: { label: 'Преимущество', glyph: '▲' },
  threat: { label: 'Угроза', glyph: '▼' },
  triumph: { label: 'Триумф', glyph: '★' },
  despair: { label: 'Отчаяние', glyph: '☠' },
}

export function DiceRoller({ initialPool, label, onLog, canSecret, onResult }: Props) {
  const [pool, setPool] = useState<RollPool>({ ...emptyPool(), ...initialPool })
  const [outcome, setOutcome] = useState<RollOutcome | null>(null)
  const [secret, setSecret] = useState(false)

  const total = poolSize(pool)
  const bump = (kind: DieKind, delta: number) =>
    setPool(p => ({ ...p, [kind]: Math.max(0, Math.min(20, p[kind] + delta)) }))

  function roll() {
    if (total === 0) return
    const result = rollPool(pool)
    setOutcome(result)
    onResult?.(result)
    if (onLog) {
      onLog({
        poolJson: JSON.stringify(pool),
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
          <div key={kind} className={`dr-die dr-${kind}`} title={DIE_META[kind].label}>
            <span className="dr-die-glyph">{DIE_META[kind].glyph}</span>
            <span className="dr-die-count">{pool[kind]}</span>
            <span className="dr-die-btns">
              <button type="button" className="tiny" onClick={() => bump(kind, 1)} aria-label={`+${kind}`}>+</button>
              <button type="button" className="tiny" onClick={() => bump(kind, -1)} aria-label={`-${kind}`}>−</button>
            </span>
          </div>
        ))}
      </div>

      <div className="dr-actions">
        <button type="button" className="primary" onClick={roll} disabled={total === 0}>
          🎲 Бросить{total > 0 ? ` (${total})` : ''}
        </button>
        <button type="button" className="small" onClick={() => setPool({ ...emptyPool(), ...initialPool })}>Сброс</button>
        {canSecret && (
          <label className="checkbox dr-secret" title="Виден только мастеру">
            <input type="checkbox" checked={secret} onChange={e => setSecret(e.target.checked)} /> Секретно
          </label>
        )}
      </div>

      {outcome && (
        <div className="dr-result">
          <RollSymbolsView symbols={outcome.net} />
          <span className="dr-summary">{summarize(outcome.net)}</span>
        </div>
      )}
    </div>
  )
}

/** Показ нетто-символов броска как набор бейджей. */
export function RollSymbolsView({ symbols }: { symbols: RollSymbols }) {
  const shown = SYMBOL_ORDER.filter(s => symbols[s] > 0)
  if (shown.length === 0) return <span className="muted">ничья</span>
  return (
    <span className="dr-symbols">
      {shown.map(s => (
        <span key={s} className={`roll-sym roll-${s}`} title={SYMBOL_META[s].label}>
          {SYMBOL_META[s].glyph} {symbols[s]}
        </span>
      ))}
    </span>
  )
}
