import { useMemo, useState } from 'react'
import { api } from '../api/client'
import type { CharacterSheet, ItemDef, ItemKind, ItemState, Reference, SheetItem } from '../api/types'
import {
  CURRENCY_LABEL, ITEM_KIND_LABELS, ITEM_STATE_LABELS, resolveWeaponSkillName,
} from '../utils/labels'
import { DicePoolView } from './DicePoolView'
import { PropertyTags } from './PropertyTags'

interface Props {
  sheet: CharacterSheet
  reference: Reference
  onError: (message: string) => void
  refresh: () => Promise<void>
}

const STATES: ItemState[] = ['equipped', 'carried', 'backpack']
const KIND_FILTERS: (ItemKind | 'all')[] = ['all', 'weapon', 'armor', 'gear']
const MULTIPLIERS = [50, 75, 100, 125, 150, 175, 200]

export function InventoryTab({ sheet, reference, onError, refresh }: Props) {
  const [search, setSearch] = useState('')
  const [kindFilter, setKindFilter] = useState<ItemKind | 'all'>('all')
  // id предмета каталога, для которого открыт мини-магазин покупки
  const [buyOpen, setBuyOpen] = useState<string | null>(null)
  // id строки инвентаря, для которой открыта продажа
  const [sellOpen, setSellOpen] = useState<string | null>(null)

  async function run(action: () => Promise<unknown>) {
    try {
      await action()
      await refresh()
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Ошибка')
    }
  }

  const d = sheet.derived
  const skillNames = useMemo(() => sheet.skills.map(s => s.name), [sheet.skills])

  const catalogue = useMemo(() => {
    const q = search.trim().toLowerCase()
    return reference.items
      .filter(i => kindFilter === 'all' || i.kind === kindFilter)
      .filter(i => !q || i.nameRu.toLowerCase().includes(q) || i.name.toLowerCase().includes(q))
      .sort((a, b) => a.nameRu.localeCompare(b.nameRu, 'ru'))
  }, [reference.items, search, kindFilter])

  return (
    <div className="inv-page">
      <MoneyPanel sheet={sheet} run={run} d={d} />

      <div className="inv-layout">
        {/* ── Инвентарь персонажа ── */}
        <section className="panel inv-current">
          <h3>Инвентарь</h3>
          {sheet.items.length === 0 && (
            <p className="muted">Пусто. Купите или добавьте предметы из каталога справа.</p>
          )}
          <div className="inv-items">
            {sheet.items.map(item => (
              <InventoryCard key={item.id} item={item} sheet={sheet} skillNames={skillNames} run={run}
                sellOpen={sellOpen === item.id} onToggleSell={() => setSellOpen(sellOpen === item.id ? null : item.id)} />
            ))}
          </div>
          {sheet.items.length > 0 && (
            <p className="hint">
              «Используется» — бонусы предмета активны, надетая броня весит на 3 меньше.
              «Не используется» и «В рюкзаке» — вес учитывается полностью, бонусы не действуют.
            </p>
          )}
        </section>

        {/* ── Магазин / каталог ── */}
        <section className="panel inv-shop">
          <h3>Магазин</h3>
          <input className="shop-search" placeholder="Поиск предмета…" value={search}
            onChange={e => setSearch(e.target.value)} />
          <div className="shop-filters">
            {KIND_FILTERS.map(k => (
              <button key={k} className={kindFilter === k ? 'chip active' : 'chip'}
                onClick={() => setKindFilter(k)}>
                {k === 'all' ? 'Все' : ITEM_KIND_LABELS[k]}
              </button>
            ))}
          </div>
          <div className="shop-list">
            {catalogue.length === 0 && <p className="muted">Ничего не найдено.</p>}
            {catalogue.map(it => (
              <ShopRow key={it.id} item={it} money={sheet.money} run={run} sheetId={sheet.id}
                open={buyOpen === it.id} onToggle={() => setBuyOpen(buyOpen === it.id ? null : it.id)} />
            ))}
          </div>
        </section>
      </div>
    </div>
  )
}

type Run = (action: () => Promise<unknown>) => Promise<void>

function MoneyPanel({ sheet, run, d }: { sheet: CharacterSheet; run: Run; d: CharacterSheet['derived'] }) {
  const [edit, setEdit] = useState<string | null>(null)
  const [delta, setDelta] = useState('')

  function saveExact() {
    if (edit === null) return
    const value = Number(edit)
    setEdit(null)
    if (!Number.isFinite(value) || value === sheet.money) return
    void run(() => api.updateCharacter(sheet.id, { money: Math.max(0, Math.trunc(value)) }))
  }

  function adjust(sign: 1 | -1) {
    const amount = Math.trunc(Number(delta))
    if (!Number.isFinite(amount) || amount <= 0) return
    setDelta('')
    void run(() => api.updateCharacter(sheet.id, { money: Math.max(0, sheet.money + sign * amount) }))
  }

  return (
    <section className="panel money-panel">
      <div className="money-balance">
        <span className="coin">🪙</span>
        {edit !== null ? (
          <input autoFocus className="money-input" value={edit}
            onChange={e => setEdit(e.target.value)}
            onBlur={saveExact}
            onKeyDown={e => e.key === 'Enter' && saveExact()} />
        ) : (
          <button className="linklike money-value" title="Кликните, чтобы задать точный баланс"
            onClick={() => setEdit(String(sheet.money))}>{sheet.money}</button>
        )}
        <span className="muted">{CURRENCY_LABEL}</span>
      </div>
      <div className="money-adjust">
        <input type="number" min={1} placeholder="сумма" value={delta}
          onChange={e => setDelta(e.target.value)} />
        <button className="small" onClick={() => adjust(1)} disabled={!delta}>+ Прибавить</button>
        <button className="small" onClick={() => adjust(-1)} disabled={!delta}>− Списать</button>
      </div>
      <div className="money-derived muted">
        Переносимый вес <strong className={d.encumbered ? 'error' : ''}>{d.encumbranceLoad}/{d.encumbranceThreshold}</strong>
        {d.encumbered && <span className="error"> · перегружен!</span>}
        {' · '}Поглощение {d.soak} · Защита {d.meleeDefense}/{d.rangedDefense}
      </div>
    </section>
  )
}

function ShopRow({ item, money, run, sheetId, open, onToggle }: {
  item: ItemDef; money: number; run: Run; sheetId: string; open: boolean; onToggle: () => void
}) {
  return (
    <div className="shop-row">
      <div className="shop-row-head">
        <div className="shop-row-info">
          <strong>{item.nameRu}</strong>
          {item.nameRu !== item.name && <span className="muted small-text"> · {item.name}</span>}
          <div className="muted small-text">
            {ITEM_KIND_LABELS[item.kind]} · цена {item.price} · редкость {item.rarity}
            {item.isCustom && ' · кастом'}
            {item.kind === 'weapon' && item.damage && ` · урон ${item.damage}, крит ${item.crit}`}
          </div>
          {(item.description || item.safeDescription) &&
            <div className="muted small-text shop-desc">{item.description || item.safeDescription}</div>}
          {item.properties && <PropertyTags properties={item.properties} className="shop-props small-text" />}
        </div>
        <div className="shop-row-actions">
          <button className="primary tiny" onClick={onToggle}>{open ? 'Отмена' : 'Купить'}</button>
          <button className="tiny" title="Добавить без оплаты"
            onClick={() => run(() => api.addItem(sheetId, item.id, 1, 'carried'))}>+ Добавить</button>
        </div>
      </div>
      {open && (
        <PriceControl basePrice={item.price} actionLabel="Купить" money={money}
          onConfirm={(total, qty) => run(async () => {
            await api.addItem(sheetId, item.id, qty, 'carried', total)
            onToggle()
          })} />
      )}
    </div>
  )
}

function InventoryCard({ item, sheet, skillNames, run, sellOpen, onToggleSell }: {
  item: SheetItem; sheet: CharacterSheet; skillNames: string[]; run: Run
  sellOpen: boolean; onToggleSell: () => void
}) {
  const hasBonus = item.soakBonus > 0 || item.meleeDefense > 0 || item.rangedDefense > 0 || item.encumbranceThresholdBonus > 0
  return (
    <div className={`inv-card${item.state === 'equipped' ? ' equipped' : ''}`}>
      <div className="inv-card-head">
        <div className="inv-card-title">
          <strong>{item.name}</strong>
          <span className="muted small-text"> · {ITEM_KIND_LABELS[item.kind]}{item.price > 0 && ` · ${item.price} 🪙`}</span>
        </div>
        <div className="inv-card-qty">
          <button className="tiny" onClick={() => item.quantity > 1 &&
            run(() => api.updateItem(sheet.id, item.id, { quantity: item.quantity - 1 }))}>−</button>
          {' '}×{item.quantity}{' '}
          <button className="tiny" onClick={() =>
            run(() => api.updateItem(sheet.id, item.id, { quantity: item.quantity + 1 }))}>+</button>
        </div>
      </div>

      {item.kind === 'weapon' && <WeaponLine item={item} sheet={sheet} skillNames={skillNames} />}

      {item.kind !== 'weapon' && item.properties && (
        <PropertyTags properties={item.properties} className="small-text" />
      )}

      {hasBonus && (
        <div className="muted small-text">
          {item.soakBonus > 0 && `Поглощение +${item.soakBonus} `}
          {item.meleeDefense > 0 && `Защ.ближ +${item.meleeDefense} `}
          {item.rangedDefense > 0 && `Защ.дальн +${item.rangedDefense} `}
          {item.encumbranceThresholdBonus > 0 && `Порог веса +${item.encumbranceThresholdBonus} `}
          {item.state !== 'equipped' && '(не действует — предмет не используется)'}
        </div>
      )}

      {item.description && <div className="inv-card-desc">{item.description}</div>}

      <div className="inv-card-foot">
        <div className="state-switch">
          {STATES.map(s => (
            <button key={s} className={item.state === s ? 'chip active' : 'chip'}
              onClick={() => item.state !== s && run(() => api.updateItem(sheet.id, item.id, { state: s }))}>
              {ITEM_STATE_LABELS[s]}
            </button>
          ))}
        </div>
        <div className="inv-card-end">
          <span className="muted small-text">вес {item.load}</span>
          <button className="small" onClick={onToggleSell}>{sellOpen ? 'Отмена' : 'Продать'}</button>
          <button className="danger small" title="Убрать без выручки"
            onClick={() => run(() => api.removeItem(sheet.id, item.id))}>✕</button>
        </div>
      </div>

      {sellOpen && (
        <PriceControl basePrice={item.price} actionLabel="Продать" maxQuantity={item.quantity}
          onConfirm={(total, qty) => run(async () => {
            await api.sellItem(sheet.id, item.id, qty, total)
            onToggleSell()
          })} />
      )}
    </div>
  )
}

function WeaponLine({ item, sheet, skillNames }: { item: SheetItem; sheet: CharacterSheet; skillNames: string[] }) {
  const skillName = resolveWeaponSkillName(item.skillName, skillNames)
  const skill = skillName ? sheet.skills.find(s => s.name === skillName) : null
  // Урон «+N» в ближнем бою — это прибавка к Мощи; абсолютное число — итоговый урон оружия.
  const dmg = item.damage.trim()
  let damageText = dmg
  if (dmg.startsWith('+')) {
    const bonus = Number(dmg.slice(1))
    if (Number.isFinite(bonus)) damageText = `${sheet.characteristics.brawn + bonus} (Мощь ${dmg})`
  }
  return (
    <div className="weapon-line">
      <span className="weapon-stat">Урон <strong>{damageText || '—'}</strong></span>
      {item.crit && <span className="weapon-stat">Крит <strong>{item.crit}</strong></span>}
      {item.rangeBand && <span className="weapon-stat">{item.rangeBand}</span>}
      {skill ? (
        <span className="weapon-pool" title={`Бросок: ${skill.name}`}>
          <DicePoolView pool={skill.pool} /> <span className="muted small-text">{skill.name}</span>
        </span>
      ) : item.skillName ? (
        <span className="muted small-text">навык {item.skillName} не освоен</span>
      ) : null}
      {item.properties && <PropertyTags properties={item.properties} className="weapon-props" />}
    </div>
  )
}

function PriceControl({ basePrice, actionLabel, onConfirm, money, maxQuantity }: {
  basePrice: number
  actionLabel: string
  onConfirm: (total: number, quantity: number) => void
  money?: number
  maxQuantity?: number
}) {
  const [mult, setMult] = useState(100)
  const [custom, setCustom] = useState('')
  const [qty, setQty] = useState(1)

  const customNum = custom.trim() === '' ? null : Math.max(0, Math.trunc(Number(custom)))
  const unitPrice = customNum != null && Number.isFinite(customNum)
    ? customNum
    : Math.round(basePrice * mult / 100)
  const total = unitPrice * qty
  const tooExpensive = money != null && total > money

  return (
    <div className="price-control">
      <div className="price-mults">
        {MULTIPLIERS.map(m => (
          <button key={m} className={customNum == null && mult === m ? 'chip active' : 'chip'}
            onClick={() => { setCustom(''); setMult(m) }}>{m}%</button>
        ))}
      </div>
      <div className="price-row">
        <label>Своя цена/шт
          <input type="number" min={0} placeholder={String(Math.round(basePrice * mult / 100))}
            value={custom} onChange={e => setCustom(e.target.value)} style={{ width: '5rem' }} />
        </label>
        <label>Кол-во
          <input type="number" min={1} max={maxQuantity} value={qty}
            onChange={e => setQty(Math.max(1, Math.min(maxQuantity ?? Infinity, Math.trunc(Number(e.target.value)) || 1)))}
            style={{ width: '4rem' }} />
        </label>
        <span className="price-total">
          Итого <strong className={tooExpensive ? 'error' : ''}>{total}</strong> 🪙
        </span>
        <button className="primary small" disabled={tooExpensive}
          title={tooExpensive ? 'Недостаточно монет' : undefined}
          onClick={() => onConfirm(total, qty)}>{actionLabel}</button>
      </div>
    </div>
  )
}
