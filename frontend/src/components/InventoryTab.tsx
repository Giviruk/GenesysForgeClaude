import { useMemo, useState } from 'react'
import { api } from '../api/client'
import type {
  CharacterSheet, ImplementMaterial, ItemCheckModifier, ItemDef, ItemKind, ItemState, Reference,
  SheetItem, WeaponAttackProfile, WeaponCraftsmanship, WeaponRange,
} from '../api/types'
import {
  CHARACTERISTIC_LABELS, CURRENCY_LABEL, DIFFICULTY_LABELS, IMPLEMENT_MATERIAL_HINTS,
  IMPLEMENT_MATERIAL_LABELS, ITEM_DAMAGE_STATE_HINTS,
  ITEM_DAMAGE_STATE_LABELS, ITEM_KIND_LABELS, ITEM_STAT_FIELD_LABELS, ITEM_STATE_LABELS,
  localizedDescription, localizedName, parseWeaponTraits, resolveWeaponSkillName, secondaryName,
  WEAPON_CRAFTSMANSHIP_ADJECTIVES, WEAPON_CRAFTSMANSHIP_HINTS, WEAPON_CRAFTSMANSHIP_LABELS,
  WEAPON_CRAFTSMANSHIPS,
} from '../utils/labels'
import { DamageStateControls } from './ItemDamageControls'
import { craftsmanshipApplies, craftsmanshipPrice } from '../utils/craftsmanship'
import { IMPLEMENT_MATERIALS, implementPrice } from '../utils/implements'
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
  // Чем можно заплатить за материалы ремонта: в фазе создания сначала тратится бюджет покупок.
  const funds = sheet.money + (sheet.isCreationPhase ? sheet.startingPurchaseBudget : 0)

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
                reference={reference} funds={funds}
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
  // Качество изготовления выбирается один раз, при покупке, и дальше не меняется (ROT-WPN-02).
  const [craftsmanship, setCraftsmanship] = useState<WeaponCraftsmanship>('steel')
  // Материал магического инструмента выбирается там же и по тому же правилу: один раз, при
  // покупке, и он тоже меняет цену (ROT-MAG-MAT-01).
  const [material, setMaterial] = useState<ImplementMaterial>('oak')
  const canChoose = craftsmanshipApplies(item.kind)
  const isImplement = item.implement != null
  const unitPrice = isImplement
    ? implementPrice(item.price, material)
    : canChoose ? craftsmanshipPrice(item.price, craftsmanship) : item.price
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
          {/* Работа выбирается до любого из действий: и покупка, и бесплатная выдача берут её. */}
          {isImplement && (
            <select className="shop-craftsmanship-select" value={material}
              aria-label={t('Материал', 'Material')}
              title={IMPLEMENT_MATERIAL_HINTS[material]}
              onChange={e => setMaterial(e.target.value as ImplementMaterial)}>
              {IMPLEMENT_MATERIALS.map(m => (
                <option key={m} value={m}>{IMPLEMENT_MATERIAL_LABELS[m]}</option>
              ))}
            </select>
          )}
          {canChoose && (
            <select className="shop-craftsmanship-select" value={craftsmanship}
              aria-label={t('Качество изготовления', 'Craftsmanship')}
              title={WEAPON_CRAFTSMANSHIP_HINTS[craftsmanship]}
              onChange={e => setCraftsmanship(e.target.value as WeaponCraftsmanship)}>
              {WEAPON_CRAFTSMANSHIPS.map(c => (
                <option key={c} value={c}>{WEAPON_CRAFTSMANSHIP_LABELS[c]}</option>
              ))}
            </select>
          )}
          <button className="primary tiny" onClick={onToggle}>{open ? t('Отмена', 'Cancel') : t('Купить', 'Buy')}</button>
          <button className="tiny" title={t('Добавить без оплаты', 'Add without paying')}
            onClick={() => run(() => api.addItem(sheetId, item.id, 1, 'carried',
              { free: true, ...(canChoose ? { craftsmanship } : {}), ...(isImplement ? { material } : {}) }))}>{t('+ Добавить', '+ Add')}</button>
        </div>
      </div>
      {open && (
        <>
          {isImplement && (
            <div className="small-text shop-craftsmanship">
              <div className="muted">{IMPLEMENT_MATERIAL_HINTS[material]}</div>
              <div className="muted">
                {t('Выбирается один раз — потом не меняется.', 'Chosen once — it cannot be changed later.')}
              </div>
            </div>
          )}
          {canChoose && (
            <div className="small-text shop-craftsmanship">
              <div className="muted">{WEAPON_CRAFTSMANSHIP_HINTS[craftsmanship]}</div>
              <div className="muted">
                {t('Выбирается один раз — потом не меняется.', 'Chosen once — it cannot be changed later.')}
              </div>
            </div>
          )}
          <BuyControl unitPrice={unitPrice} money={money}
            onConfirm={(qty, opts) => run(async () => {
              await api.addItem(sheetId, item.id, qty, 'carried', {
                ...opts,
                ...(canChoose ? { craftsmanship } : {}),
                ...(isImplement ? { material } : {}),
              })
              onToggle()
            })} />
        </>
      )}
    </div>
  )
}

/**
 * Почему предмет нельзя взять в руки или надеть прямо сейчас: <c>null</c> — можно.
 * Правило то же, что на сервере: одна броня и две руки, двуручное занимает обе (ROT-EQP-01).
 */
function equipBlockReason(item: SheetItem, sheet: CharacterSheet): string | null {
  const equipped = sheet.items.filter(i => i.state === 'equipped' && !i.isThrown && i.id !== item.id)
  if (item.kind === 'armor') {
    return equipped.some(i => i.kind === 'armor')
      ? t('Уже надета другая броня', 'Another armor is already worn')
      : null
  }
  if (item.kind !== 'weapon') return null
  const cost = (i: SheetItem) => (parseWeaponTraits(i.formTraits).includes('twoHanded') ? 2 : 1)
  const used = equipped.filter(i => i.kind === 'weapon').reduce((sum, i) => sum + cost(i), 0)
  if (used + cost(item) <= 2) return null
  return cost(item) === 2
    ? t('Двуручное оружие занимает обе руки', 'A two-handed weapon needs both hands')
    : t('Обе руки заняты', 'Both hands are full')
}

/**
 * Человекочитаемый штраф снаряжения: «+1 помеха к Скрытности» (ROT-ARM-01). Имя навыка берётся
 * из листа, чтобы в русской локали не показывать английское «Stealth».
 */
function checkModifierText(m: ItemCheckModifier, sheet: CharacterSheet): string {
  const skill = sheet.skills.find(s => s.name === m.skillName)
  const target = skill
    ? localizedName(skill)
    : m.skillName || (m.characteristic
      ? CHARACTERISTIC_LABELS[m.characteristic]
      : t('всем проверкам', 'all checks'))
  const sign = m.kind === 'AddSetback' ? '+' : '−'
  const body = t(`${sign}${m.value} помех к «${target}»`, `${sign}${m.value} setback to ${target}`)
  return m.condition ? `${body} — ${m.condition}` : body
}

function InventoryCard({ item, sheet, skillNames, run, reference, funds, sellOpen, onToggleSell }: {
  item: SheetItem; sheet: CharacterSheet; skillNames: string[]; run: Run; reference: Reference
  funds: number; sellOpen: boolean; onToggleSell: () => void
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
    <div className={`inv-card${item.state === 'equipped' ? ' equipped' : ''}${item.isUsable ? '' : ' broken'}`}>
      <div className="inv-card-head">
        <div className="inv-card-title">
          <strong>{itemLabel}</strong>
          {/* Состояние видно сразу в заголовке: сломанная вещь не должна выглядеть как целая. */}
          {item.damageState !== 'undamaged' && (
            <span className={`chip damage-badge ${item.damageState}`}
              title={ITEM_DAMAGE_STATE_HINTS[item.damageState]}>
              {ITEM_DAMAGE_STATE_LABELS[item.damageState]}
            </span>
          )}
          <span className="muted small-text">
            {secondaryName(item) && ` · ${secondaryName(item)}`}
            {` · ${ITEM_KIND_LABELS[item.kind]}`}{item.price > 0 && ` · ${item.price} 🪙`}
            {/* Слоты улучшений берутся из таблицы книги; null — значения нет (ROT-ARM-01).
                Занятые показываются рядом: иначе непонятно, куда делись свободные (ROT-EQP-ATT-01). */}
            {item.hardPoints != null && ` · HP ${item.usedHardPoints}/${item.hardPoints}`}
            {/* Работа обычной стали ничего не меняет — её и не показываем (ROT-WPN-02). */}
            {item.craftsmanship !== 'steel' && ` · ${WEAPON_CRAFTSMANSHIP_LABELS[item.craftsmanship]}`}
            {item.reinforced && t(' · укреплённое', ' · reinforced')}
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

      {item.kind === 'weapon' && (
        <WeaponLine item={item} sheet={sheet} skill={skill} skillLabel={skillLabel}
          reference={reference} run={run} />
      )}

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
          {item.state === 'equipped' && item.kind === 'armor' && !item.isActiveArmor
            && t('(не действует — активна другая броня, эта даёт только вес)',
              '(inactive — another armor is active; this one only adds load)')}
        </div>
      )}

      {/* Сломанный предмет не даёт ничего, кроме веса, — и об этом надо сказать прямо, а не
          показывать нули без объяснения (GEN-EQP-DMG-01). */}
      {!item.isUsable && (
        <div className="damage-warn small-text">
          {t('Предмет не даёт ни поглощения, ни защиты, ни эффектов улучшений: вес и содержимое остаются.',
            'The item grants no soak, no defense and no attachment effects: weight and contents stay.')}
        </div>
      )}

      {/* Незначительное и Умеренное меняют пул: у оружия это видно в строке атаки, у прочего —
          только здесь, потому что «проверку этим предметом» приложение не строит. */}
      {item.isUsable && item.damageState !== 'undamaged' && item.kind !== 'weapon' && (
        <div className="damage-warn small-text">
          {item.damageState === 'minor'
            ? t('+1 помеха ко всем проверкам, прямо использующим предмет',
              '+1 setback to every check that directly uses the item')
            : t('+1 к сложности всех проверок, прямо использующих предмет',
              '+1 difficulty to every check that directly uses the item')}
        </div>
      )}

      {/* Штраф снаряжения к проверкам виден на карточке, а не только в пуле навыка (ROT-ARM-01). */}
      {item.checkModifiers?.length > 0 && (
        <div className="muted small-text">
          {item.checkModifiers.map((m, i) => (
            <span key={i}>
              {i > 0 && ' · '}
              {checkModifierText(m, sheet)}
              {m.requiresWorn && item.state !== 'equipped'
                && t(' (не действует — не надето)', ' (inactive — not worn)')}
              {m.requiresWorn && item.state === 'equipped' && item.kind === 'armor' && !item.isActiveArmor
                && t(' (не действует — активна другая броня)', ' (inactive — another armor is active)')}
            </span>
          ))}
        </div>
      )}

      {/* Установленные улучшения: числа выше уже с ними, но без списка непонятно, откуда они
          взялись и что снимать, если слоты кончились (ROT-EQP-ATT-01). */}
      {item.attachments?.length > 0 && (
        <div className="muted small-text">
          {t('Улучшения', 'Attachments')} · {item.attachments.map((a, i) => (
            <span key={a.id}>
              {i > 0 && ' · '}
              {a.nameRu || a.name}
              {a.hardPointCost > 0 && ` (${a.hardPointCost})`}
              {a.isEnchantment && t(' — чары', ' — enchantment')}
            </span>
          ))}
          {item.overCapacity && (
            <span className="error">
              {' · '}{t('слотов не хватает — снимите лишнее', 'not enough slots — remove one')}
            </span>
          )}
        </div>
      )}

      {/* Правила улучшений, которые приложение не исполняет: их нельзя терять молча. */}
      {item.attachmentNotes?.length > 0 && item.attachmentNotes.map((note, i) => (
        <div key={i} className="muted small-text">{note}</div>
      ))}

      {/* Разбор поправок: числа выше — уже эффективные, здесь видно, от чего они такие (ROT-WPN-02). */}
      {item.adjustments?.length > 0 && (
        <div className="muted small-text">
          {WEAPON_CRAFTSMANSHIP_ADJECTIVES[item.craftsmanship]} · {item.adjustments.map((a, i) => (
            <span key={`${a.stage}-${a.field}-${i}`}>
              {i > 0 && ' · '}
              {ITEM_STAT_FIELD_LABELS[a.field] ?? a.field} {a.base} → {a.effective}
              {/* Поправка от состояния подписывается: иначе непонятно, почему поглощение стало нулём. */}
              {a.stage === 'damageState'
                && ` (${ITEM_DAMAGE_STATE_LABELS[item.damageState]})`}
            </span>
          ))}
        </div>
      )}

      {localizedDescription(item) && <div className="inv-card-desc">{localizedDescription(item)}</div>}

      {/* Магический инструмент (ROT-MAG-IMP-01): материал, что он удешевляет и настроен ли он.
          Сам выбор эффектов делает ведущий на вкладке «Магия» — там уже загружен справочник. */}
      {item.implement && (
        <div className="muted small-text">
          {t('Инструмент', 'Implement')} · {IMPLEMENT_MATERIAL_LABELS[item.implement.material]}
          {item.implement.attackDamageBonus > 0
            && t(` · урон Атаки +${item.implement.attackDamageBonus}`,
              ` · Attack damage +${item.implement.attackDamageBonus}`)}
          {item.implement.boostDice > 0
            && t(` · +${item.implement.boostDice} бонусная кость`,
              ` · +${item.implement.boostDice} boost`)}
          {item.implement.requiredMagicSkill
            && t(` · только ${item.implement.requiredMagicSkill}`,
              ` · ${item.implement.requiredMagicSkill} only`)}
          {item.implement.discountEffects.length > 0
            && ` · ${item.implement.discountEffects.join(', ')}`}
          {item.implement.chosenEffects.length > 0
            && t(` · выбрано: ${item.implement.chosenEffects.join(', ')}`,
              ` · chosen: ${item.implement.chosenEffects.join(', ')}`)}
          {item.implement.pending && (
            <span className="damage-warn">
              {' · '}{t('не настроен — бесплатный эффект выбирает ведущий на вкладке «Магия»',
                'not configured — the GM picks the free effect on the Magic tab')}
            </span>
          )}
        </div>
      )}

      {/* Состояние и ремонт — отдельной строкой и отдельными кнопками (GEN-EQP-DMG-01):
          «используется/в рюкзаке» и «цел/сломан» — разные вещи, и путать их нельзя. */}
      <DamageStateControls state={item.damageState} repair={item.repair} funds={funds}
        reinforced={item.reinforced}
        onSetState={next => run(() => api.setItemDamageState(sheet.id, item.id, next))}
        onRepair={opts => run(() => api.repairItem(sheet.id, item.id, opts))} />

      <div className="inv-card-foot">
        <div className="state-switch">
          {STATES.map(s => {
            // Одна броня и две руки (ROT-EQP-01): сервер всё равно откажет, но лучше сказать заранее.
            const blocked = s === 'equipped' && item.state !== 'equipped' ? equipBlockReason(item, sheet) : null
            return (
              <button key={s} className={item.state === s ? 'chip active' : 'chip'}
                disabled={blocked !== null} title={blocked ?? undefined}
                onClick={() => item.state !== s && run(() => api.updateItem(sheet.id, item.id, { state: s }))}>
                {ITEM_STATE_LABELS[s]}
              </button>
            )
          })}
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
        <SellControl unitPrice={item.price} maxQuantity={item.quantity}
          onConfirm={(qty, opts) => run(async () => {
            await api.sellItem(sheet.id, item.id, qty, opts)
            onToggleSell()
          })} />
      )}
    </div>
  )
}

const RANGE_LABELS: Record<WeaponRange, string> = t({
  engaged: 'Вплотную', short: 'Короткая', medium: 'Средняя', long: 'Длинная', extreme: 'Экстремальная',
}, {
  engaged: 'Engaged', short: 'Short', medium: 'Medium', long: 'Long', extreme: 'Extreme',
})

/** Подпись профиля: у основного её нет, у альтернативных — «в метании», «в руке». */
function profileLabel(p: WeaponAttackProfile): string {
  if (p.isDefault) return t('основной', 'main')
  return t(p.nameRu || p.nameEn, p.nameEn || p.nameRu)
}

/**
 * Строка оружия (ROT-WPN-01). Один экземпляр может иметь несколько профилей — кинжал бьют
 * и метают, — поэтому урон, навык, крит и дистанция берутся из выбранного профиля, а не из
 * общих полей предмета. Базовый урон считает сервер: клиент строку «+3» больше не разбирает.
 */
function WeaponLine({ item, sheet, skill, skillLabel, reference, run }: {
  item: SheetItem; sheet: CharacterSheet; skill: CharacterSheet['skills'][number] | null
  skillLabel: string; reference: Reference; run: Run
}) {
  const { openRoller } = useDiceRoller()
  const itemLabel = localizedName(item)
  const profiles = item.attackProfiles ?? []
  const [profileCode, setProfileCode] = useState(profiles.find(p => p.isDefault)?.code ?? '')
  const profile = profiles.find(p => p.code === profileCode) ?? profiles[0] ?? null

  // Навык броска берётся из профиля: метательный кинжал бросается Дальним боем, а не Ближним.
  const profileSkill = profile
    ? sheet.skills.find(s => s.name === profile.skillName) ?? null
    : skill
  const profileSkillLabel = profileSkill ? localizedName(profileSkill) : profile?.skillName ?? skillLabel

  const damageText = profile
    ? `${profile.baseDamage ?? profile.damageValue}${profile.damageKind === 'brawnPlus'
      ? ` ${t(`(Мощь +${profile.damageValue})`, `(Brawn +${profile.damageValue})`)}` : ''}`
    : item.damage
  const critText = profile ? String(profile.crit) : item.crit
  const rangeText = profile ? RANGE_LABELS[profile.range] : item.rangeBand
  // Качества берутся из профиля экземпляра, а не из строки каталога: работа и улучшения меняют
  // набор (Проникающее от Острого лезвия, Укреплённое у древней работы), а строка о них не знает.
  // item.properties остаётся запасным вариантом для записей без структурного профиля.
  const qualities = profile
    ? profile.qualities.map(q => `${t(q.nameRu, q.nameEn)}${q.rating ? ` ${q.rating}` : ''}`).join(', ')
    : item.properties

  // Что качества делают с пулом: Точное даёт бонусные кубы, Неточное — помехи, Громоздкое и
  // Сноровка поднимают сложность при нехватке характеристики (GEN-EQP-QUAL-01).
  const mods = profile?.poolModifiers ?? null
  // Источники поправок: качества оружия и состояние повреждения (GEN-EQP-DMG-01) — в одном
  // списке, потому что в пул они попадают одинаково и игрок должен видеть оба.
  const modsTitle = mods && mods.sources.length > 0
    ? [t('Поправки к пулу:', 'Pool modifiers:'), ...mods.sources.map(s => {
      const parts: string[] = []
      if (s.boost) parts.push(t(`+${s.boost} бонусных`, `+${s.boost} boost`))
      if (s.setback) parts.push(t(`+${s.setback} помех`, `+${s.setback} setback`))
      if (s.difficulty) parts.push(t(`+${s.difficulty} к сложности`, `+${s.difficulty} difficulty`))
      if (s.advantage) parts.push(t(`+${s.advantage} преимущество`, `+${s.advantage} advantage`))
      if (s.threat) parts.push(t(`+${s.threat} угроза`, `+${s.threat} threat`))
      return `${t(s.nameRu, s.nameEn)}: ${parts.join(', ')}`
    })].join('\n')
    : undefined

  return (
    <div className="weapon-line">
      {profiles.length > 1 && (
        <span className="weapon-profiles no-print">
          {profiles.map(p => (
            <button key={p.code} type="button"
              className={`tiny${p.code === profile?.code ? ' active' : ''}`}
              onClick={() => setProfileCode(p.code)}>
              {profileLabel(p)}
            </button>
          ))}
        </span>
      )}
      <span className="weapon-stat">{t('Урон', 'Damage')} <strong>{damageText || '—'}</strong></span>
      {critText && <span className="weapon-stat">{t('Крит', 'Crit')} <strong>{critText}</strong></span>}
      {rangeText && <span className="weapon-stat">{rangeText}</span>}
      {/* Оружие, которое не достаёт вплотную и задаёт свою сложность (пика). */}
      {profile?.cannotAttackEngaged && (
        <span className="weapon-stat warn" title={t('Пика бьёт только на короткой дистанции.',
          'A pike only reaches at short range.')}>
          {t('не бьёт вплотную', 'not engaged')}
        </span>
      )}
      {profile?.fixedDifficulty != null && (
        <span className="weapon-stat">
          {t('Сложность', 'Difficulty')} <strong>{DIFFICULTY_LABELS[profile.fixedDifficulty] ?? profile.fixedDifficulty}</strong>
        </span>
      )}
      {profileSkill ? (
        <span className="weapon-pool"
          title={[t(`Бросок: ${profileSkillLabel}`, `Roll: ${profileSkillLabel}`), modsTitle]
            .filter(Boolean).join('\n')}>
          <DicePoolView pool={profileSkill.pool}
            setback={profileSkill.setbackDice + (mods?.setback ?? 0)}
            boost={mods?.boost ?? 0}
            difficulty={mods?.difficultyIncrease ?? 0} />
          {' '}<span className="muted small-text">{profileSkillLabel}</span>
          {(mods?.automaticAdvantage ?? 0) > 0 && (
            <span className="muted small-text">
              {' '}{t(`+${mods!.automaticAdvantage} преим.`, `+${mods!.automaticAdvantage} adv.`)}
            </span>
          )}
        </span>
      ) : profileSkillLabel ? (
        <span className="muted small-text">
          {t(`навык ${profileSkillLabel} не освоен`, `skill ${profileSkillLabel} not trained`)}
        </span>
      ) : null}
      {qualities && <PropertyTags properties={qualities} className="weapon-props" />}

      {!item.isUsable ? (
        // Сломанным оружием не атакуют: кнопки броска нет вовсе, чтобы её нельзя было нажать
        // по привычке (GEN-EQP-DMG-01).
        <span className="weapon-stat warn"
          title={ITEM_DAMAGE_STATE_HINTS[item.damageState]}>
          {t('сломано — атаковать нельзя', 'broken — cannot attack')}
        </span>
      ) : item.isThrown ? (
        <>
          <span className="weapon-stat warn">{t('метнуто', 'thrown')}</span>
          <button type="button" className="small no-print"
            title={t('Оружие лежит у цели — подобрать его', 'The weapon lies by the target — pick it up')}
            onClick={() => run(() => api.setItemThrown(sheet.id, item.id, false))}>
            {t('Подобрать', 'Pick up')}
          </button>
        </>
      ) : (
        <>
          <button type="button" className="small no-print" onClick={() => openRoller({
            kind: 'combat',
            title: profile && !profile.isDefault ? `${itemLabel} — ${profileLabel(profile)}` : itemLabel,
            skillLabel: profileSkill ? profileSkillLabel : null,
            // Перегруз мешает и в бою (ROT-EQP-01), а сложность, заданную оружием, подставляем
            // сразу: пику иначе бросали бы без её Средней сложности.
            basePool: {
              ...(profileSkill
                ? {
                  ability: profileSkill.pool.ability,
                  proficiency: profileSkill.pool.proficiency,
                  setback: profileSkill.setbackDice + (mods?.setback ?? 0),
                }
                : { setback: mods?.setback ?? 0 }),
              ...(mods?.boost ? { boost: mods.boost } : {}),
              // Сложность оружия и надбавка за нехватку Мощи/Ловкости складываются.
              ...((profile?.fixedDifficulty ?? 0) + (mods?.difficultyIncrease ?? 0) > 0
                ? { difficulty: (profile?.fixedDifficulty ?? 0) + (mods?.difficultyIncrease ?? 0) }
                : {}),
            },
            // Урон уже посчитан сервером под Мощь персонажа — клиенту его разбирать не нужно.
            damage: profile ? String(profile.baseDamage ?? profile.damageValue) : item.damage,
            brawn: sheet.characteristics.brawn,
            crit: critText,
            rangeBand: rangeText,
            qualities: qualitiesFromProperties(qualities, reference),
          })}>{t('🎲 Атаковать', '🎲 Attack')}</button>
          {/* Метательный профиль расходует сам экземпляр: оружие остаётся у цели (ROT-WPN-01). */}
          {profiles.some(p => p.qualities.some(q => q.code === 'limited-ammo')) && (
            <button type="button" className="small no-print"
              title={t('Отметить, что оружие метнули и оно осталось у цели',
                'Mark the weapon as thrown and left by the target')}
              onClick={() => run(() => api.setItemThrown(sheet.id, item.id, true))}>
              {t('Метнуть', 'Throw')}
            </button>
          )}
        </>
      )}
    </div>
  )
}

/** Доли цены при торге: от половины до двойной, четвертями (ROT-ECO-01). */
const PURCHASE_PERCENTS = [50, 75, 100, 125, 150, 175, 200]

/**
 * Покупка. Переключателя режимов нет: доля цены и договорная цена лежат рядом, а способ выбирает
 * само поле — пока цена за штуку пустая, покупка идёт по доле. Сумму в обоих случаях считает
 * сервер, здесь только предпросмотр.
 */
function BuyControl({ unitPrice, money, onConfirm }: {
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
function SellControl({ unitPrice, maxQuantity, onConfirm }: {
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
