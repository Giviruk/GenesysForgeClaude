import { useState } from 'react'
import { t } from '../i18n'

/**
 * Общие контролы цены (ROT-ECO-01). Живут отдельным модулем, потому что покупать и продавать по
 * одним и тем же правилам приходится не только предметам: скакун покупается теми же способами
 * (торг долей, договорная цена, выдача без оплаты) и продаётся теми же тремя (ROT-MOUNT-ITEM-01).
 */

/** Доли цены при торге: от половины до двойной, четвертями (ROT-ECO-01). */
const PURCHASE_PERCENTS = [50, 75, 100, 125, 150, 175, 200]

/**
 * Покупка. Переключателя режимов нет: доля цены и договорная цена лежат рядом, а способ выбирает
 * само поле — пока цена за штуку пустая, покупка идёт по доле. Сумму в обоих случаях считает
 * сервер, здесь только предпросмотр.
 */
export function BuyControl({ unitPrice, money, onConfirm }: {
  unitPrice: number
  money: number
  onConfirm: (quantity: number, opts: {
    pricePercent?: number; priceOverride?: number; overrideReason?: string
  }) => void
}) {
  const [qty, setQty] = useState(1)
  const [percent, setPercent] = useState(100)
  const [ownPrice, setOwnPrice] = useState('')
  const [reason, setReason] = useState('')

  const hasOwnPrice = ownPrice.trim() !== ''
  const ownPriceNum = Math.max(0, Math.trunc(Number(ownPrice)) || 0)
  // Доля берётся от цены единицы и округляется вниз — тем же правилом, что и на сервере.
  const effectiveUnit = hasOwnPrice ? ownPriceNum : Math.floor(unitPrice * percent / 100)
  const total = effectiveUnit * qty
  const tooExpensive = total > money
  const needsReason = hasOwnPrice && reason.trim() === ''

  return (
    <div className="price-control">
      <div className="price-mults">
        {PURCHASE_PERCENTS.map(m => (
          <button key={m} className={!hasOwnPrice && percent === m ? 'chip active' : 'chip'}
            disabled={hasOwnPrice}
            title={hasOwnPrice ? t('Задана своя цена', 'An own price is set') : undefined}
            onClick={() => setPercent(m)}>{m}%</button>
        ))}
      </div>

      <div className="price-row">
        <label>{t('Своя цена/шт', 'Own price/unit')}
          <input type="number" min={0} value={ownPrice} placeholder={t('по доле', 'by fraction')}
            onChange={e => setOwnPrice(e.target.value)} style={{ width: '5rem' }} />
        </label>
        <label>{t('Причина', 'Reason')}
          <input value={reason} maxLength={200} disabled={!hasOwnPrice}
            placeholder={t('например, скидка от гильдии', 'e.g. a guild discount')}
            onChange={e => setReason(e.target.value)} />
        </label>
      </div>

      <div className="price-row">
        <label>{t('Кол-во', 'Qty')}
          <input type="number" min={1} value={qty}
            onChange={e => setQty(Math.max(1, Math.trunc(Number(e.target.value)) || 1))}
            style={{ width: '4rem' }} />
        </label>
        <span className="price-total">
          {!hasOwnPrice && percent !== 100 && `${percent}% · `}
          {t('Цена', 'Price')} {effectiveUnit} × {qty} = <strong className={tooExpensive ? 'error' : ''}>{total}</strong> 🪙
        </span>
        <button className="primary small" disabled={tooExpensive || needsReason}
          title={needsReason
            ? t('Для своей цены нужна причина', 'An own price needs a reason')
            : tooExpensive ? t('Недостаточно монет', 'Not enough coins') : undefined}
          onClick={() => onConfirm(qty, hasOwnPrice
            ? { priceOverride: ownPriceNum, overrideReason: reason.trim() }
            : { pricePercent: percent })}>{t('Купить', 'Buy')}</button>
      </div>
      <p className="hint small-text">
        {t('Доля берётся от цены экземпляра за штуку и округляется вниз; итог считает сервер. Своя цена отменяет долю и требует причины.',
          'The fraction applies to the instance unit price and rounds down; the server computes the total. An own price overrides the fraction and needs a reason.')}
      </p>
    </div>
  )
}

/**
 * Продажа. По умолчанию — просто продать за долю каталожной цены, без всяких бросков.
 * Режим «по проверке» считает долю по правилу: 1 успех — 25 %, 2 — 50 %, 3 и больше — 75 %
 * (ROT-ECO-01). Сумму в обоих случаях считает сервер — здесь только предпросмотр.
 */
export function SellControl({ unitPrice, maxQuantity, onConfirm }: {
  unitPrice: number
  maxQuantity: number
  onConfirm: (quantity: number, opts: {
    percent?: number; netSuccesses?: number; priceOverride?: number; overrideReason?: string
  }) => void
}) {
  const [qty, setQty] = useState(1)
  const [mode, setMode] = useState<'direct' | 'check' | 'override'>('direct')
  const [percent, setPercent] = useState(100)
  const [successes, setSuccesses] = useState(1)
  const [ownPrice, setOwnPrice] = useState('')
  const [reason, setReason] = useState('')

  const ownPriceNum = Math.max(0, Math.trunc(Number(ownPrice)) || 0)
  const effectivePercent = mode === 'check'
    ? (successes <= 0 ? 0 : successes === 1 ? 25 : successes === 2 ? 50 : 75)
    : percent
  const preview = mode === 'override'
    ? ownPriceNum * qty
    : Math.floor(unitPrice * effectivePercent / 100) * qty
  // Договорная цена требует причины — она попадёт в историю персонажа.
  const canSell = mode === 'override'
    ? ownPrice.trim() !== '' && reason.trim() !== ''
    : effectivePercent > 0

  return (
    <div className="price-control">
      <div className="inline-form">
        {([
          ['direct', t('Просто продать', 'Just sell')],
          ['check', t('По проверке', 'With a check')],
          ['override', t('Своя цена', 'Own price')],
        ] as const).map(([value, label]) => (
          <label key={value}>
            <input type="radio" name="sell-mode" checked={mode === value}
              onChange={() => setMode(value)} /> {label}
          </label>
        ))}
      </div>

      {mode === 'direct' && (
        <div className="price-mults">
          {[25, 50, 75, 100].map(m => (
            <button key={m} className={percent === m ? 'chip active' : 'chip'}
              onClick={() => setPercent(m)}>{m}%</button>
          ))}
        </div>
      )}

      {mode === 'override' && (
        <div className="price-row">
          <label>{t('Цена/шт', 'Price/unit')}
            <input type="number" min={0} value={ownPrice}
              onChange={e => setOwnPrice(e.target.value)} style={{ width: '5rem' }} />
          </label>
          <label>{t('Причина', 'Reason')}
            <input value={reason} maxLength={200} placeholder={t('например, сделка с гильдией', 'e.g. a guild deal')}
              onChange={e => setReason(e.target.value)} />
          </label>
        </div>
      )}

      <div className="price-row">
        <label>{t('Кол-во', 'Qty')}
          <input type="number" min={1} max={maxQuantity} value={qty}
            onChange={e => setQty(Math.max(1, Math.min(maxQuantity, Math.trunc(Number(e.target.value)) || 1)))}
            style={{ width: '4rem' }} />
        </label>
        {mode === 'check' && (
          <label>{t('Нетто-успехов', 'Net successes')}
            <input type="number" min={0} value={successes}
              onChange={e => setSuccesses(Math.max(0, Math.trunc(Number(e.target.value)) || 0))}
              style={{ width: '4rem' }} />
          </label>
        )}
        <span className="price-total">
          {mode === 'override'
            ? <><strong>{preview}</strong> 🪙</>
            : effectivePercent > 0
              ? <>{effectivePercent}% · <strong>{preview}</strong> 🪙</>
              : <span className="error">{t('провал — сделки нет', 'failure — no sale')}</span>}
        </span>
        <button className="primary small" disabled={!canSell}
          title={mode === 'override' && !canSell ? t('Нужны цена и причина', 'Price and reason required') : undefined}
          onClick={() => onConfirm(qty,
            mode === 'check' ? { netSuccesses: successes }
              : mode === 'override' ? { priceOverride: ownPriceNum, overrideReason: reason.trim() }
                : { percent })}>
          {t('Продать', 'Sell')}
        </button>
      </div>
      <p className="hint small-text">
        {t('Доля берётся от цены каталога за штуку и округляется вниз; итог считает сервер.',
          'The fraction applies to the listed unit price and rounds down; the server computes the total.')}
      </p>
    </div>
  )
}
