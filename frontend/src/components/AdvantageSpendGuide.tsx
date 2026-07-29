import { useEffect, useState } from 'react'
import { api } from '../api/client'
import type { RuleTableEntry } from '../api/types'
import {
  affordableExtraSpends, affordableRuleSpends,
  type AdvantageSpendContext, type AdvantageSpendOption,
} from '../utils/advantageSpends'
import { t } from '../i18n'

export function AdvantageSpendGuide({
  advantages,
  successful = true,
  context = 'general',
  extra = [],
}: {
  advantages: number
  successful?: boolean
  context?: AdvantageSpendContext
  extra?: AdvantageSpendOption[]
}) {
  const [rules, setRules] = useState<RuleTableEntry[]>([])

  useEffect(() => {
    let cancelled = false
    // Некоторые изолированные тесты компонентов подменяют api частично. Ошибка справочника не
    // должна ломать сам бросок: конкретные качества всё равно остаются доступными.
    Promise.resolve().then(() => api.rules())
      .then(result => { if (!cancelled) setRules(result.entries) })
      .catch(() => { /* подсказка необязательна для выполнения броска */ })
    return () => { cancelled = true }
  }, [])

  if (advantages <= 0) return null
  const options = [
    ...affordableExtraSpends(extra, advantages, successful),
    ...affordableRuleSpends(rules, advantages, context, successful),
  ]
  const unique = options.filter((option, index, all) =>
    all.findIndex(candidate => candidate.id === option.id) === index)

  return (
    <div className="advantage-spends">
      <strong>{t(
        `Можно потратить ${advantages} преимуществ:`,
        `You can spend ${advantages} advantage:`,
      )}</strong>
      {unique.length > 0
        ? (
          <ul>
            {unique.map(option => (
              <li key={option.id}>
                <span className="advantage-cost">
                  ▲ {option.cost}{(option.costLabelRu || option.costLabelEn)
                    ? ` ${t(option.costLabelRu || option.costLabelEn || '', option.costLabelEn || option.costLabelRu || '')}`
                    : ''}
                </span>{' '}
                {t(option.labelRu, option.labelEn)}
                {(option.detailRu || option.detailEn) && (
                  <span className="muted small-text">
                    {' '}— {t(option.detailRu || option.detailEn || '', option.detailEn || option.detailRu || '')}
                  </span>
                )}
              </li>
            ))}
          </ul>
        )
        : <div className="muted small-text">{t(
          'Для этого числа и контекста нет автоматической подсказки; ведущий может разрешить повествовательную трату.',
          'No automatic suggestion matches this amount and context; the GM may allow a narrative spend.',
        )}</div>}
      <div className="muted small-text">
        {t('Выберите варианты на общую сумму не больше результата; одна и та же ▲ не оплачивает две траты.',
          'Choose options whose total cost does not exceed the result; the same ▲ cannot pay for two spends.')}
      </div>
    </div>
  )
}
