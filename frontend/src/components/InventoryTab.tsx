import { useMemo, useState } from 'react'
import { api } from '../api/client'
import type { CharacterSheet, ItemDef, ItemKind, ItemState, Reference, SheetItem } from '../api/types'
import {
  CURRENCY_LABEL, ITEM_KIND_LABELS, ITEM_STATE_LABELS, localizedDescription, localizedName, resolveWeaponSkillName,
  secondaryName,
} from '../utils/labels'
import { itemTags } from '../data/itemQualities'
import { DicePoolView } from './DicePoolView'
import { qualitiesFromProperties } from '../utils/combat'
import { PropertyTags } from './PropertyTags'
import { PrintPreview } from './print/PrintPreview'
import { ItemCard } from './print/cards'
import { useDiceRoller } from '../dice-roller-store'
import { lang, t } from '../i18n'

interface Props {
  sheet: CharacterSheet
  reference: Reference
  onError: (message: string) => void
  refresh: () => Promise<void>
}

const STATES: ItemState[] = ['equipped', 'carried', 'backpack']
const KIND_FILTERS: (ItemKind | 'all')[] = ['all', 'weapon', 'armor', 'gear']
const MULTIPLIERS = [50, 75, 100, 125, 150, 175, 200]

// Поиск: регистронезависимо и устойчиво к лишним пробелам.
const normalize = (s: string) => s.toLowerCase().replace(/\s+/g, ' ').trim()

export function InventoryTab({ sheet, reference, onError, refresh }: Props) {
  const [search, setSearch] = useState('')
  const [kindFilter, setKindFilter] = useState<ItemKind | 'all'>('all')
  const [selectedTags, setSelectedTags] = useState<string[]>([])
  const [tagPickerOpen, setTagPickerOpen] = useState(false)
  // id предмета каталога, для которого открыт мини-магазин покупки
  const [buyOpen, setBuyOpen] = useState<string | null>(null)
  // id строки инвентаря, для которой открыта продажа
  const [sellOpen, setSellOpen] = useState<string | null>(null)

  // Все доступные теги (из свойств предметов) — для пикера; стабильны независимо от поиска.
  const allTags = useMemo(() => {
    const set = new Set<string>()
    for (const i of reference.items) for (const t of itemTags(i.properties)) set.add(t)
    return [...set].sort((a, b) => a.localeCompare(b, lang))
  }, [reference.items])

  function toggleTag(tag: string) {
    setSelectedTags(prev => prev.includes(tag) ? prev.filter(t => t !== tag) : [...prev, tag])
  }

  async function run(action: () => Promise<unknown>) {
    try {
      await action()
      await refresh()
    } catch (err) {
      onError(err instanceof Error ? err.message : t('Ошибка', 'Error'))
    }
  }

  const d = sheet.derived
  const skillNames = useMemo(() => sheet.skills.map(s => s.name), [sheet.skills])

  const catalogue = useMemo(() => {
    const q = normalize(search)
    const matchesText = (i: ItemDef) => {
      if (!q) return true
      // Совпадение по имени (рус/англ), безопасному/полному описанию, источнику и свойствам/тегам.
      const hay = normalize([i.nameRu, i.name, i.safeDescription, i.description, i.source, i.properties]
        .filter(Boolean).join(' '))
      return hay.includes(q)
    }
    const matchesTags = (i: ItemDef) => {
      if (selectedTags.length === 0) return true
      const tags = itemTags(i.properties)
      return selectedTags.every(t => tags.includes(t)) // фильтры компонуются (И)
    }
    return reference.items
      .filter(i => kindFilter === 'all' || i.kind === kindFilter)
      .filter(matchesText)
      .filter(matchesTags)
      .sort((a, b) => localizedName(a).localeCompare(localizedName(b), lang))
  }, [reference.items, search, kindFilter, selectedTags])

  return (
    <div className="inv-page">
      <MoneyPanel sheet={sheet} run={run} d={d} />

      <div className="inv-layout">
        {/* ── Инвентарь персонажа ── */}
        <section className="panel inv-current">
          <h3>{t('Инвентарь', 'Inventory')}</h3>
          {sheet.items.length === 0 && (
            <p className="muted">{t('Пусто. Купите или добавьте предметы из каталога справа.', 'Empty. Buy or add items from the catalogue on the right.')}</p>
          )}
          <div className="inv-items">
            {sheet.items.map(item => (
              <InventoryCard key={item.id} item={item} sheet={sheet} skillNames={skillNames} run={run}
                reference={reference}
                sellOpen={sellOpen === item.id} onToggleSell={() => setSellOpen(sellOpen === item.id ? null : item.id)} />
            ))}
          </div>
          {sheet.items.length > 0 && (
            <p className="hint">
              {t(
                '«Используется» — бонусы предмета активны, надетая броня весит на 3 меньше. ' +
                '«Не используется» и «В рюкзаке» — вес учитывается полностью, бонусы не действуют.',
                '“Equipped” — the item’s bonuses are active and worn armor weighs 3 less. ' +
                '“Carried” and “In backpack” — full weight counts and bonuses do not apply.',
              )}
            </p>
          )}
        </section>

        {/* ── Магазин / каталог ── */}
        <section className="panel inv-shop">
          <h3>{t('Магазин', 'Shop')}</h3>
          <div className="shop-search-row">
            <input className="shop-search" placeholder={t('Поиск: имя, описание, свойство…', 'Search: name, description, property…')} value={search}
              onChange={e => setSearch(e.target.value)} />
            <button type="button"
              className={`shop-tags-btn${selectedTags.length > 0 || tagPickerOpen ? ' active' : ''}`}
              disabled={allTags.length === 0}
              aria-expanded={tagPickerOpen}
              onClick={() => setTagPickerOpen(o => !o)}>
              {t('Теги', 'Tags')}{selectedTags.length > 0 && <span className="filter-count">{selectedTags.length}</span>}
            </button>
          </div>

          {selectedTags.length > 0 && (
            <div className="shop-selected-tags">
              {selectedTags.map(tag => (
                <button key={tag} type="button" className="chip active removable"
                  title={t('Убрать тег', 'Remove tag')} onClick={() => toggleTag(tag)}>
                  {tag}<span aria-hidden> ×</span>
                </button>
              ))}
              <button type="button" className="linklike" onClick={() => setSelectedTags([])}>{t('Сбросить', 'Clear')}</button>
            </div>
          )}

          {tagPickerOpen && (
            <div className="shop-tag-picker">
              <div className="shop-tag-picker-head">
                <strong>{t('Фильтр по тегам', 'Filter by tags')}</strong>
                <span className="shop-tag-picker-actions">
                  {selectedTags.length > 0 && (
                    <button type="button" className="linklike" onClick={() => setSelectedTags([])}>{t('Сбросить', 'Clear')}</button>
                  )}
                  <button type="button" className="linklike" onClick={() => setTagPickerOpen(false)}>{t('Закрыть', 'Close')}</button>
                </span>
              </div>
              <div className="chips">
                {allTags.map(t => (
                  <button key={t} type="button" className={selectedTags.includes(t) ? 'chip active' : 'chip'}
                    onClick={() => toggleTag(t)}>{t}</button>
                ))}
              </div>
            </div>
          )}

          <div className="shop-filters">
            {KIND_FILTERS.map(k => (
              <button key={k} className={kindFilter === k ? 'chip active' : 'chip'}
                onClick={() => setKindFilter(k)}>
                {k === 'all' ? t('Все', 'All') : ITEM_KIND_LABELS[k]}
              </button>
            ))}
          </div>
          <div className="shop-list">
            {catalogue.length === 0 && <p className="muted">{t('Ничего не найдено.', 'Nothing found.')}</p>}
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
          <button className="linklike money-value" title={t('Кликните, чтобы задать точный баланс', 'Click to set the exact balance')}
            onClick={() => setEdit(String(sheet.money))}>{sheet.money}</button>
        )}
        <span className="muted">{CURRENCY_LABEL}</span>
      </div>
      <div className="money-adjust">
        <input type="number" min={1} placeholder={t('сумма', 'amount')} value={delta}
          onChange={e => setDelta(e.target.value)} />
        <button className="small" onClick={() => adjust(1)} disabled={!delta}>{t('+ Прибавить', '+ Add')}</button>
        <button className="small" onClick={() => adjust(-1)} disabled={!delta}>{t('− Списать', '− Spend')}</button>
      </div>
      <div className="money-derived muted">
        {t('Переносимый вес', 'Encumbrance')} <strong className={d.encumbered ? 'error' : ''}>{d.encumbranceLoad}/{d.encumbranceThreshold}</strong>
        {d.encumbered && <span className="error"> · {t('перегружен!', 'encumbered!')}</span>}
        {' · '}{t('Поглощение', 'Soak')} {d.soak} · {t('Защита', 'Defense')} {d.meleeDefense}/{d.rangedDefense}
      </div>
    </section>
  )
}

function ShopRow({ item, money, run, sheetId, open, onToggle }: {
  item: ItemDef; money: number; run: Run; sheetId: string; open: boolean; onToggle: () => void
}) {
  const itemLabel = localizedName(item)
  const itemOriginal = secondaryName(item)
  return (
    <div className="shop-row">
      <div className="shop-row-head">
        <div className="shop-row-info">
          <strong>{itemLabel}</strong>
          {itemOriginal && <span className="muted small-text name-secondary"> · {itemOriginal}</span>}
          <div className="muted small-text">
            {ITEM_KIND_LABELS[item.kind]} · {t('цена', 'price')} {item.price} · {t('редкость', 'rarity')} {item.rarity}
            {item.isCustom && t(' · кастом', ' · custom')}
            {item.kind === 'weapon' && item.damage && t(` · урон ${item.damage}, крит ${item.crit}`, ` · damage ${item.damage}, crit ${item.crit}`)}
          </div>
          {localizedDescription(item) &&
            <div className="muted small-text shop-desc">{localizedDescription(item)}</div>}
          {item.properties && <PropertyTags properties={item.properties} className="shop-props small-text" />}
        </div>
        <div className="shop-row-actions">
          <button className="primary tiny" onClick={onToggle}>{open ? t('Отмена', 'Cancel') : t('Купить', 'Buy')}</button>
          <button className="tiny" title={t('Добавить без оплаты', 'Add without paying')}
            onClick={() => run(() => api.addItem(sheetId, item.id, 1, 'carried'))}>{t('+ Добавить', '+ Add')}</button>
        </div>
      </div>
      {open && (
        <PriceControl basePrice={item.price} actionLabel={t('Купить', 'Buy')} money={money}
          onConfirm={(total, qty) => run(async () => {
            await api.addItem(sheetId, item.id, qty, 'carried', total)
            onToggle()
          })} />
      )}
    </div>
  )
}

function InventoryCard({ item, sheet, skillNames, run, reference, sellOpen, onToggleSell }: {
  item: SheetItem; sheet: CharacterSheet; skillNames: string[]; run: Run; reference: Reference
  sellOpen: boolean; onToggleSell: () => void
}) {
  const hasBonus = item.soakBonus > 0 || item.meleeDefense > 0 || item.rangedDefense > 0 || item.encumbranceThresholdBonus > 0
  const [printing, setPrinting] = useState(false)
  const itemLabel = localizedName(item)
  const skillName = resolveWeaponSkillName(item.skillName, skillNames)
  const skill = skillName ? sheet.skills.find(s => s.name === skillName) ?? null : null
  const skillLabel = skill ? localizedName(skill) : item.skillName

  if (printing) {
    return (
      <PrintPreview title={t(`Предмет — ${itemLabel}`, `Item — ${itemLabel}`)} onClose={() => setPrinting(false)}>
        {() => <ItemCard item={item} skillLabel={skillLabel} />}
      </PrintPreview>
    )
  }

  return (
    <div className={`inv-card${item.state === 'equipped' ? ' equipped' : ''}`}>
      <div className="inv-card-head">
        <div className="inv-card-title">
          <strong>{itemLabel}</strong>
          <span className="muted small-text">
            {secondaryName(item) && ` · ${secondaryName(item)}`}
            {` · ${ITEM_KIND_LABELS[item.kind]}`}{item.price > 0 && ` · ${item.price} 🪙`}
          </span>
        </div>
        <div className="inv-card-qty">
          <button className="tiny" onClick={() => item.quantity > 1 &&
            run(() => api.updateItem(sheet.id, item.id, { quantity: item.quantity - 1 }))}>−</button>
          {' '}×{item.quantity}{' '}
          <button className="tiny" onClick={() =>
            run(() => api.updateItem(sheet.id, item.id, { quantity: item.quantity + 1 }))}>+</button>
        </div>
      </div>

      {item.kind === 'weapon' && <WeaponLine item={item} sheet={sheet} skill={skill} skillLabel={skillLabel} reference={reference} />}

      {item.kind !== 'weapon' && item.properties && (
        <PropertyTags properties={item.properties} className="small-text" />
      )}

      {hasBonus && (
        <div className="muted small-text">
          {item.soakBonus > 0 && t(`Поглощение +${item.soakBonus} `, `Soak +${item.soakBonus} `)}
          {item.meleeDefense > 0 && t(`Защ.ближ +${item.meleeDefense} `, `Melee def. +${item.meleeDefense} `)}
          {item.rangedDefense > 0 && t(`Защ.дальн +${item.rangedDefense} `, `Ranged def. +${item.rangedDefense} `)}
          {item.encumbranceThresholdBonus > 0 && t(`Порог веса +${item.encumbranceThresholdBonus} `, `Encumbrance +${item.encumbranceThresholdBonus} `)}
          {item.state !== 'equipped' && t('(не действует — предмет не используется)', '(inactive — the item is not equipped)')}
        </div>
      )}

      {localizedDescription(item) && <div className="inv-card-desc">{localizedDescription(item)}</div>}

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
          <span className="muted small-text">{t('вес', 'load')} {item.load}</span>
          <button className="small" title={t('Печать карточки предмета', 'Print the item card')} onClick={() => setPrinting(true)}>🖨</button>
          <button className="small" onClick={onToggleSell}>{sellOpen ? t('Отмена', 'Cancel') : t('Продать', 'Sell')}</button>
          <button className="danger small" title={t('Убрать без выручки', 'Remove without proceeds')}
            onClick={() => run(() => api.removeItem(sheet.id, item.id))}>✕</button>
        </div>
      </div>

      {sellOpen && (
        <PriceControl basePrice={item.price} actionLabel={t('Продать', 'Sell')} maxQuantity={item.quantity}
          onConfirm={(total, qty) => run(async () => {
            await api.sellItem(sheet.id, item.id, qty, total)
            onToggleSell()
          })} />
      )}
    </div>
  )
}

function WeaponLine({ item, sheet, skill, skillLabel, reference }: {
  item: SheetItem; sheet: CharacterSheet; skill: CharacterSheet['skills'][number] | null; skillLabel: string; reference: Reference
}) {
  const { openRoller } = useDiceRoller()
  const itemLabel = localizedName(item)
  // Урон «+N» в ближнем бою — это прибавка к Мощи; абсолютное число — итоговый урон оружия.
  const dmg = item.damage.trim()
  let damageText = dmg
  if (dmg.startsWith('+')) {
    const bonus = Number(dmg.slice(1))
    if (Number.isFinite(bonus)) damageText = `${sheet.characteristics.brawn + bonus} ${t(`(Мощь ${dmg})`, `(Brawn ${dmg})`)}`
  }
  return (
    <div className="weapon-line">
      <span className="weapon-stat">{t('Урон', 'Damage')} <strong>{damageText || '—'}</strong></span>
      {item.crit && <span className="weapon-stat">{t('Крит', 'Crit')} <strong>{item.crit}</strong></span>}
      {item.rangeBand && <span className="weapon-stat">{item.rangeBand}</span>}
      {skill ? (
        <span className="weapon-pool" title={t(`Бросок: ${skillLabel}`, `Roll: ${skillLabel}`)}>
          <DicePoolView pool={skill.pool} /> <span className="muted small-text">{skillLabel}</span>
        </span>
      ) : item.skillName ? (
        <span className="muted small-text">{t(`навык ${skillLabel} не освоен`, `skill ${skillLabel} not trained`)}</span>
      ) : null}
      {item.properties && <PropertyTags properties={item.properties} className="weapon-props" />}
      <button type="button" className="small no-print" onClick={() => openRoller({
        kind: 'combat',
        title: itemLabel,
        skillLabel: skill ? skillLabel : null,
        basePool: skill ? { ability: skill.pool.ability, proficiency: skill.pool.proficiency } : {},
        damage: item.damage,
        brawn: sheet.characteristics.brawn,
        crit: item.crit,
        rangeBand: item.rangeBand,
        qualities: qualitiesFromProperties(item.properties, reference),
      })}>{t('🎲 Атаковать', '🎲 Attack')}</button>

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
        <label>{t('Своя цена/шт', 'Custom price/unit')}
          <input type="number" min={0} placeholder={String(Math.round(basePrice * mult / 100))}
            value={custom} onChange={e => setCustom(e.target.value)} style={{ width: '5rem' }} />
        </label>
        <label>{t('Кол-во', 'Qty')}
          <input type="number" min={1} max={maxQuantity} value={qty}
            onChange={e => setQty(Math.max(1, Math.min(maxQuantity ?? Infinity, Math.trunc(Number(e.target.value)) || 1)))}
            style={{ width: '4rem' }} />
        </label>
        <span className="price-total">
          {t('Итого', 'Total')} <strong className={tooExpensive ? 'error' : ''}>{total}</strong> 🪙
        </span>
        <button className="primary small" disabled={tooExpensive}
          title={tooExpensive ? t('Недостаточно монет', 'Not enough coins') : undefined}
          onClick={() => onConfirm(total, qty)}>{actionLabel}</button>
      </div>
    </div>
  )
}
