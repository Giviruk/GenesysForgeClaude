import { useState } from 'react'
import type { ItemDamageState, ItemRepair } from '../api/types'
import {
  CURRENCY_LABEL, DIFFICULTY_LABELS, ITEM_DAMAGE_STATE_HINTS, ITEM_DAMAGE_STATE_LABELS,
  ITEM_DAMAGE_STATES,
} from '../utils/labels'
import { InfoTip } from './InfoTip'
import { DISCOUNT_PERCENT_PER_ADVANTAGE, discountedCost } from '../utils/repair'
import { t } from '../i18n'

/**
 * Памятка по ремонту (GEN-EQP-DMG-01). Правило, сложность, время и материалы лежат отдельно от
 * кнопки: кнопка чинит, памятка объясняет, что за столом при этом происходит. Приложение броска
 * не делает — решение владельца, поэтому сложность и время здесь справочные.
 */
export function RepairMemo({ repair }: { repair: ItemRepair }) {
  const difficulty = repair.difficulty == null
    ? null
    : DIFFICULTY_LABELS[repair.difficulty] ?? String(repair.difficulty)
  return (
    <InfoTip label="?" title={t('Памятка: ремонт', 'Repair rules')}>
      <ul>
        <li>
          {t('Проверка: Механика, базовая сложность', 'Check: Mechanics, base difficulty')}
          {' '}<strong>{difficulty ?? t('— ремонт недоступен', '— no repair')}</strong>.
          {' '}{t('Иной навык — только решением ведущего.',
            'Any other skill takes the GM’s decision.')}
        </li>
        <li>
          {t('Приложение бросок не делает: кнопка чинит предмет, а исход за столом определяете вы.',
            'The app rolls nothing: the button repairs the item, the table decides the outcome.')}
        </li>
        <li>
          {t('Достаточное время: ', 'Adequate time: ')}
          <strong>{repair.hoursMin}–{repair.hoursMax} {t('ч', 'h')}</strong>
          {t(' (примерно 1–2 часа на каждую ступень сложности). Меньше — сложность +1; ' +
            'без подходящих инструментов — ещё +1, надбавки складываются.',
            ' (about 1–2 hours per step of base difficulty). Less than that — difficulty +1; ' +
            'without proper tools — one more, and they stack.')}
        </li>
        <li>
          {t('Материалы: ', 'Materials: ')}
          <strong>{repair.materialPercent}%</strong>
          {t(' цены экземпляра (Незначительное 25 %, Умеренное 50 %, Серьёзное 100 %).',
            ' of the instance price (Minor 25 %, Moderate 50 %, Major 100 %).')}
          {' '}{t('Качество изготовления в цене учтено, торговая наценка и цена улучшений — нет.',
            'Craftsmanship is included in that price; trade markup and attachment prices are not.')}
        </li>
        <li>
          {t('Самостоятельный ремонт дешевле на 10 % за каждое чистое преимущество, но не ниже нуля. ' +
            'Дробь округляется вверх до целой монеты.',
            'Self-repair costs 10 % less per net advantage, never below zero. ' +
            'Fractions round up to a whole coin.')}
        </li>
        <li>
          {t('Материалы списываются в момент починки. Уничтоженное обычным ремонтом не чинится.',
            'Materials are charged when the repair happens. Destroyed items are beyond ordinary repair.')}
        </li>
      </ul>
    </InfoTip>
  )
}

/**
 * Состояние предмета и ремонт одной строкой. Состояние меняется отдельными кнопками — и когда
 * в бою сработало Разрушающее, и когда вещь пострадала по сюжету: приложение не угадывает причину.
 */
export function DamageStateControls({ state, repair, funds, reinforced, onSetState, onRepair }: {
  state: ItemDamageState
  repair: ItemRepair
  /** Чем персонаж может заплатить за материалы: кошелёк плюс бюджет создания. */
  funds: number
  /** Укреплённый экземпляр: Разрушающее его не берёт — но пожар и кислота берут. */
  reinforced?: boolean
  onSetState: (next: ItemDamageState) => void
  onRepair: (opts: { netAdvantages: number } | { costOverride: number; overrideReason: string }) => void
}) {
  const [advantages, setAdvantages] = useState(0)
  // Цена ведущего нужна там, где обычной цены нет вовсе: бесценная руна иначе осталась бы
  // сломанной навсегда, потому что чинить её не за что.
  const [gmPrice, setGmPrice] = useState('')
  const [gmReason, setGmReason] = useState('')

  const baseCost = repair.materialCost
  const needsGmQuote = baseCost == null
  const gmPriceNum = Math.max(0, Math.trunc(Number(gmPrice)) || 0)
  const cost = needsGmQuote
    ? (gmPrice.trim() === '' ? null : gmPriceNum)
    : discountedCost(baseCost!, advantages)
  const ready = cost != null && cost <= funds && (!needsGmQuote || gmReason.trim() !== '')

  return (
    <div className="damage-row small-text">
      <span className="muted">{t('Состояние', 'Condition')}</span>
      <span className="damage-switch">
        {ITEM_DAMAGE_STATES.map(s => (
          <button key={s} type="button" className={state === s ? 'chip active' : 'chip'}
            title={ITEM_DAMAGE_STATE_HINTS[s]}
            onClick={() => state !== s && onSetState(s)}>
            {ITEM_DAMAGE_STATE_LABELS[s]}
          </button>
        ))}
      </span>

      {repair.canRepair && !needsGmQuote && (
        <label className="damage-adv" title={t(
          'Чистые преимущества самостоятельного ремонта: каждое снимает 10 % стоимости материалов',
          'Net advantages of a self-repair: each takes 10 % off the material cost')}>
          {t('преим.', 'adv.')}
          <input type="number" min={0} value={advantages}
            onChange={e => setAdvantages(Math.max(0, Math.trunc(Number(e.target.value)) || 0))}
            style={{ width: '3.2rem' }} />
        </label>
      )}

      {repair.canRepair && needsGmQuote && (
        <>
          <label className="damage-adv">
            {t('материалы', 'materials')}
            <input type="number" min={0} value={gmPrice} placeholder={t('цена', 'price')}
              onChange={e => setGmPrice(e.target.value)} style={{ width: '4.5rem' }} />
          </label>
          <label className="damage-adv">
            {t('причина', 'reason')}
            <input value={gmReason} maxLength={200}
              placeholder={t('решение ведущего', 'the GM’s call')}
              onChange={e => setGmReason(e.target.value)} />
          </label>
        </>
      )}

      {repair.canRepair && (
        <button type="button" className="primary tiny" disabled={!ready}
          title={cost == null
            ? t('У этой записи нет обычной цены — стоимость материалов называет ведущий',
              'This entry has no ordinary price — the GM names the material cost')
            : needsGmQuote && gmReason.trim() === ''
              ? t('Для цены ведущего нужна причина', 'The GM’s price needs a reason')
              : cost > funds
                ? t('Недостаточно монет на материалы', 'Not enough coins for materials')
                : t('Починить: материалы спишутся, предмет станет целым',
                  'Repair: materials are charged and the item becomes undamaged')}
          onClick={() => onRepair(needsGmQuote
            ? { costOverride: gmPriceNum, overrideReason: gmReason.trim() }
            : { netAdvantages: advantages })}>
          🔧 {t('Починить', 'Repair')}
          {cost != null && <> — {cost} 🪙</>}
        </button>
      )}
      {state === 'destroyed' && (
        <span className="muted">
          {t('Обычный ремонт недоступен', 'Beyond ordinary repair')}
        </span>
      )}
      <RepairMemo repair={repair} />

      {state !== 'undamaged' && (
        <span className="damage-warn">{ITEM_DAMAGE_STATE_HINTS[state]}</span>
      )}
      {reinforced && (
        <span className="muted">
          {t('Укреплённое: Разрушающее его не берёт', 'Reinforced: immune to Sunder')}
        </span>
      )}
      {cost != null && baseCost != null && cost !== baseCost && advantages > 0 && (
        <span className="muted">
          {t(`материалы ${baseCost} − ${advantages * DISCOUNT_PERCENT_PER_ADVANTAGE} %`,
            `materials ${baseCost} − ${advantages * DISCOUNT_PERCENT_PER_ADVANTAGE} %`)}
          {' '}{CURRENCY_LABEL}
        </span>
      )}
    </div>
  )
}
