import { useEffect, useState } from 'react'
import { api } from '../api/client'
import type { RuleTableEntry } from '../api/types'
import { t } from '../i18n'
import type { RollSymbols } from '../utils/diceRoller'
import {
  affordableExtraOutcomeSpends,
  affordableOutcomeRuleSpends,
  type AdvantageSpendContext,
  type AdvantageSpendOption,
  type OutcomeSpendKind,
  type OutcomeSpendOption,
  type SymbolPayment,
} from '../utils/advantageSpends'

const GLYPH: Record<SymbolPayment['symbol'], string> = {
  success: '✶',
  failure: '✸',
  advantage: '▲',
  threat: '▼',
  triumph: '★',
  despair: '☠',
}

function paymentLabel(payment: SymbolPayment): string {
  if (payment.mode === 'additional') return `+${GLYPH[payment.symbol]}`
  if (payment.mode === 'scaling') return `${GLYPH[payment.symbol]}×`
  return `${GLYPH[payment.symbol]} ${payment.cost}`
}

function costLabel(payments: SymbolPayment[]): string {
  return payments.map(paymentLabel).join(` ${t('или', 'or')} `)
}

function SpendSection({
  kind,
  titleRu,
  titleEn,
  options,
}: {
  kind: OutcomeSpendKind
  titleRu: string
  titleEn: string
  options: OutcomeSpendOption[]
}) {
  if (options.length === 0) return null
  return (
    <section className={`outcome-spend-section outcome-spend-${kind}`}>
      <strong>{t(titleRu, titleEn)}</strong>
      <ul>
        {options.map(option => (
          <li key={option.id}>
            <span className="outcome-symbol-cost">{costLabel(option.payments)}</span>{' '}
            {t(option.labelRu, option.labelEn)}
            {(option.detailRu || option.detailEn) && (
              <span className="muted small-text">
                {' '}— {t(option.detailRu || option.detailEn || '', option.detailEn || option.detailRu || '')}
              </span>
            )}
          </li>
        ))}
      </ul>
    </section>
  )
}

export function OutcomeSpendGuide({
  result,
  context = 'general',
  extra = [],
}: {
  result: RollSymbols
  context?: AdvantageSpendContext
  extra?: AdvantageSpendOption[]
}) {
  const [rules, setRules] = useState<RuleTableEntry[]>([])

  useEffect(() => {
    let cancelled = false
    Promise.resolve().then(() => api.rules())
      .then(response => { if (!cancelled) setRules(response.entries) })
      .catch(() => { /* Справочная подсказка не должна ломать сам бросок. */ })
    return () => { cancelled = true }
  }, [])

  const options = [
    ...affordableExtraOutcomeSpends(extra, result),
    ...affordableOutcomeRuleSpends(rules, result, context),
  ].filter((option, index, all) =>
    all.findIndex(candidate => candidate.id === option.id) === index)

  const positive = options.filter(option => option.kind === 'positive')
  const negative = options.filter(option => option.kind === 'negative')
  const checkResult = options.filter(option => option.kind === 'result')
  const hasPositiveSymbols = result.advantage > 0 || result.triumph > 0
  const hasNegativeSymbols = result.threat > 0 || result.despair > 0

  return (
    <div className="advantage-spends outcome-spends">
      <strong>{t('Варианты использования результата', 'Ways to use the result')}</strong>

      <SpendSection kind="positive"
        titleRu="Преимущества и триумфы можно потратить:"
        titleEn="Advantage and Triumph may be spent:"
        options={positive} />
      {hasPositiveSymbols && positive.length === 0 && (
        <p className="muted small-text">{t(
          'Для положительных символов нет автоматической подсказки в этом контексте; ведущий может разрешить подходящий повествовательный эффект.',
          'No automatic positive-symbol suggestion matches this context; the GM may allow a suitable narrative effect.',
        )}</p>
      )}

      <SpendSection kind="negative"
        titleRu="Возможные последствия угроз и крахов:"
        titleEn="Possible Threat and Despair consequences:"
        options={negative} />
      {hasNegativeSymbols && negative.length === 0 && (
        <p className="muted small-text">{t(
          'Для отрицательных символов нет отдельного автоматического последствия в этом контексте; результат разрешает ведущий.',
          'No specific automatic negative-symbol consequence matches this context; the GM resolves the result.',
        )}</p>
      )}

      <SpendSection kind="result"
        titleRu="Эффекты успехов или провалов этой проверки:"
        titleEn="Success or Failure effects for this check:"
        options={checkResult} />

      {result.success > 0 && checkResult.length === 0 && (
        <p className="muted small-text">{t(
          `Проверка успешна (${result.success}). Универсальной дополнительной траты нескольких успехов нет: их особый эффект применяется только когда его задаёт действие, предмет или способность.`,
          `The check succeeds (${result.success}). Multiple successes have no universal extra spend; apply an additional effect only when the action, item, or ability defines one.`,
        )}</p>
      )}
      {result.failure > 0 && checkResult.length === 0 && (
        <p className="muted small-text">{t(
          `Проверка провалена (${result.failure}). Универсального дополнительного эффекта за несколько провалов нет.`,
          `The check fails (${result.failure}). Multiple failures have no universal additional effect.`,
        )}</p>
      )}

      {(positive.length > 0 || negative.length > 0) && (
        <div className="muted small-text">
          {t(
            'Каждый символ можно использовать только один раз. Альтернативы через «или» — разные способы оплатить один эффект.',
            'Each symbol can be used only once. Alternatives joined by “or” are different ways to pay for one effect.',
          )}
        </div>
      )}
    </div>
  )
}
