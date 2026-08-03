import { useEffect, useMemo, useState } from 'react'
import { api } from '../api/client'
import type {
  CharacterListItem, CharacterSheet, GameSystem, ImplementMaterial, Reference,
  WeaponCraftsmanship,
} from '../api/types'
import {
  CURRENCY_LABEL, IMPLEMENT_MATERIAL_HINTS, IMPLEMENT_MATERIAL_LABELS, NPC_KIND_LABELS,
  WEAPON_CRAFTSMANSHIP_HINTS, WEAPON_CRAFTSMANSHIP_LABELS, WEAPON_CRAFTSMANSHIPS,
  localizedDescription, localizedName, secondaryName,
} from '../utils/labels'
import {
  craftsmanshipApplies, craftsmanshipPrice, craftsmanshipRarity,
} from '../utils/craftsmanship'
import {
  IMPLEMENT_MATERIALS, implementPrice, implementRarity,
} from '../utils/implements'
import {
  buildShopProducts, productPrice, productRarity, type ShopCategory, type ShopProduct,
} from '../utils/shopCatalog'
import { lang, t } from '../i18n'

const CATEGORY_ORDER: ShopCategory[] = [
  'all',
  'weaponLight',
  'weaponHeavy',
  'weaponRanged',
  'magicImplement',
  'magicItem',
  'weaponAttachment',
  'weaponEnchantment',
  'armor',
  'armorAttachment',
  'transport',
  'gear',
  'consumable',
  'service',
]

const CATEGORY_LABELS: Record<ShopCategory, string> = {
  all: t('Все товары', 'All products'),
  weaponLight: t('Оружие · лёгкое', 'Weapons · light'),
  weaponHeavy: t('Оружие · тяжёлое', 'Weapons · heavy'),
  weaponRanged: t('Оружие · дальнобойное', 'Weapons · ranged'),
  magicImplement: t('Магические инструменты', 'Magic implements'),
  magicItem: t('Магические предметы', 'Magic items'),
  weaponAttachment: t('Улучшения оружия', 'Weapon attachments'),
  weaponEnchantment: t('Зачарования оружия', 'Weapon enchantments'),
  armor: t('Броня', 'Armor'),
  armorAttachment: t('Улучшения брони', 'Armor attachments'),
  transport: t('Транспорт', 'Transport'),
  gear: t('Снаряжение', 'Gear'),
  consumable: t('Расходники', 'Consumables'),
  service: t('Услуги', 'Services'),
}

const normalize = (value: string) => value.toLowerCase().replace(/\s+/g, ' ').trim()

const productName = (product: ShopProduct): string =>
  product.type === 'item'
    ? localizedName(product.item)
    : product.type === 'mount'
      ? (lang === 'ru' ? product.mount.nameRu || product.mount.name : product.mount.name)
      : lang === 'ru'
        ? product.attachment.nameRu || product.attachment.name
        : product.attachment.name

const productSecondaryName = (product: ShopProduct): string =>
  product.type === 'item'
    ? secondaryName(product.item)
    : product.type === 'mount'
      ? (lang === 'ru' && product.mount.nameRu && product.mount.nameRu !== product.mount.name
          ? product.mount.name
          : '')
      : lang === 'ru' && product.attachment.nameRu && product.attachment.nameRu !== product.attachment.name
        ? product.attachment.name
        : ''

const productDescription = (product: ShopProduct): string =>
  product.type === 'item'
    ? localizedDescription(product.item)
    : product.type === 'mount'
      ? (lang === 'ru'
          ? product.mount.description
          : product.mount.descriptionEn || product.mount.description)
      : lang === 'ru'
        ? product.attachment.description
        : product.attachment.descriptionEn || product.attachment.description

const productSource = (product: ShopProduct): string =>
  product.type === 'item' ? product.item.source
    : product.type === 'mount' ? product.mount.source
      : product.attachment.source

export function ShopPage() {
  const [characters, setCharacters] = useState<CharacterListItem[]>([])
  const [system, setSystem] = useState<GameSystem>('realmsOfTerrinoth')
  const [reference, setReference] = useState<Reference | null>(null)
  const [category, setCategory] = useState<ShopCategory>('all')
  const [search, setSearch] = useState('')
  const [selected, setSelected] = useState<ShopProduct | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    api.characters()
      .then(rows => {
        if (cancelled) return
        setCharacters(rows)
        if (rows.length > 0 && !rows.some(c => c.system === system)) {
          setLoading(true)
          setSystem(rows[0].system)
        }
      })
      .catch(err => !cancelled && setError(err instanceof Error ? err.message : t('Ошибка', 'Error')))
    return () => { cancelled = true }
  }, []) // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    let cancelled = false
    api.reference(system)
      .then(data => { if (!cancelled) setReference(data) })
      .catch(err => !cancelled && setError(err instanceof Error ? err.message : t('Ошибка', 'Error')))
      .finally(() => { if (!cancelled) setLoading(false) })
    return () => { cancelled = true }
  }, [system])

  const products = useMemo(
    () => reference ? buildShopProducts(reference) : [],
    [reference],
  )

  const filtered = useMemo(() => {
    const query = normalize(search)
    return products
      .filter(product => category === 'all' || product.category === category)
      .filter(product => {
        if (!query) return true
        return normalize([
          productName(product),
          productSecondaryName(product),
          productDescription(product),
          productSource(product),
        ].filter(Boolean).join(' ')).includes(query)
      })
      .sort((a, b) => productName(a).localeCompare(productName(b), lang))
  }, [products, category, search])

  const availableSystems = useMemo(() => {
    const set = new Set(characters.map(c => c.system))
    if (set.size === 0) set.add('realmsOfTerrinoth')
    return [...set]
  }, [characters])

  return (
    <div className="page shop-page">
      <div className="page-head">
        <div>
          <h2>{t('Магазин', 'Shop')}</h2>
          <p className="muted">
            {t(
              'Выберите категорию и откройте товар. Персонаж и материал выбираются перед покупкой.',
              'Choose a category and open a product. Select the character and material before buying.',
            )}
          </p>
        </div>
        <div className="system-switch" aria-label={t('Игровая система', 'Game system')}>
          {availableSystems.map(value => (
            <button key={value} type="button" className={system === value ? 'chip active' : 'chip'}
              onClick={() => {
                setLoading(true)
                setError(null)
                setSystem(value)
                setSelected(null)
              }}>
              {value === 'realmsOfTerrinoth' ? 'Realms of Terrinoth' : 'Genesys Core'}
            </button>
          ))}
        </div>
      </div>

      {error && <div className="error-box">{error}</div>}

      <section className="panel shop-catalogue">
        <div className="shop-toolbar">
          <input className="shop-search" value={search}
            placeholder={t('Поиск по названию и описанию…', 'Search names and descriptions…')}
            onChange={event => setSearch(event.target.value)} />
        </div>

        <div className="shop-category-grid">
          {CATEGORY_ORDER.map(value => {
            const count = value === 'all'
              ? products.length
              : products.filter(product => product.category === value).length
            return (
              <button key={value} type="button"
                className={category === value ? 'shop-category active' : 'shop-category'}
                onClick={() => setCategory(value)}>
                <span>{CATEGORY_LABELS[value]}</span>
                <span className="muted">{count}</span>
              </button>
            )
          })}
        </div>

        {loading ? (
          <p className="muted">{t('Загрузка каталога…', 'Loading catalogue…')}</p>
        ) : (
          <div className="shop-product-list">
            {filtered.length === 0 && (
              <p className="muted">{t('В этой категории ничего не найдено.', 'Nothing found in this category.')}</p>
            )}
            {filtered.map(product => (
              <button key={product.key} type="button" className="shop-product-row"
                onClick={() => setSelected(product)}>
                <span className="shop-product-name">
                  <strong>{productName(product)}</strong>
                  {productSecondaryName(product) && (
                    <span className="muted small-text"> · {productSecondaryName(product)}</span>
                  )}
                  <span className="muted small-text shop-product-category">
                    {CATEGORY_LABELS[product.category]}
                  </span>
                </span>
                <span className="shop-product-stats">
                  <span>
                    {t('Цена', 'Price')}{' '}
                    <strong>{productPrice(product) == null ? '—' : productPrice(product)}</strong>
                    {' '}{CURRENCY_LABEL}
                  </span>
                  <span>
                    {t('Редкость', 'Rarity')}{' '}
                    <strong>{productRarity(product) ?? '—'}</strong>
                  </span>
                </span>
              </button>
            ))}
          </div>
        )}
      </section>

      {selected && (
        <ProductModal product={selected}
          characters={characters.filter(character => character.system === system)}
          onClose={() => setSelected(null)} />
      )}
    </div>
  )
}

function ProductModal({ product, characters, onClose }: {
  product: ShopProduct
  characters: CharacterListItem[]
  onClose: () => void
}) {
  const [characterId, setCharacterId] = useState(characters[0]?.id ?? '')
  const [sheet, setSheet] = useState<CharacterSheet | null>(null)
  const [craftsmanship, setCraftsmanship] = useState<WeaponCraftsmanship>('steel')
  const [material, setMaterial] = useState<ImplementMaterial>('oak')
  const [quantity, setQuantity] = useState(1)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  useEffect(() => {
    if (!characterId) return
    let cancelled = false
    api.sheet(characterId)
      .then(value => { if (!cancelled) setSheet(value) })
      .catch(err => !cancelled && setError(err instanceof Error ? err.message : t('Ошибка', 'Error')))
    return () => { cancelled = true }
  }, [characterId])

  const item = product.type === 'item' ? product.item : null
  const attachment = product.type === 'attachment' ? product.attachment : null
  // Скакун покупается своей командой и всегда по одному: это существо, а не стопка вещей.
  const mount = product.type === 'mount' ? product.mount : null
  const isService = item?.shopCategory === 'service'
  const isImplement = item?.implement != null
  const hasCraftsmanship = item ? craftsmanshipApplies(item.kind) : false
  const basePrice = productPrice(product)
  const baseRarity = productRarity(product)
  const effectivePrice = basePrice == null ? null
    : isImplement ? implementPrice(basePrice, material)
      : hasCraftsmanship ? craftsmanshipPrice(basePrice, craftsmanship)
        : basePrice
  const effectiveRarity = baseRarity == null ? null
    : isImplement ? implementRarity(baseRarity, material)
      : hasCraftsmanship ? craftsmanshipRarity(baseRarity, craftsmanship)
        : baseRarity
  const units = attachment || mount ? 1 : quantity
  const total = effectivePrice == null ? null : effectivePrice * units
  const funds = sheet
    ? sheet.money + (sheet.isCreationPhase ? sheet.startingPurchaseBudget : 0)
    : 0
  const canBuy = !!characterId && !busy && effectivePrice != null
    && total != null && total <= funds && (item?.purchasable ?? true)
  const canAdd = !!characterId && !busy

  async function refreshSheet() {
    if (characterId) setSheet(await api.sheet(characterId))
  }

  async function act(free: boolean) {
    if (!characterId) return
    setBusy(true)
    setError(null)
    setSuccess(null)
    try {
      if (isService && item) {
        await api.buyService(characterId, item.id, quantity, free)
      } else if (item) {
        await api.addItem(characterId, item.id, quantity, 'carried', {
          free,
          ...(hasCraftsmanship ? { craftsmanship } : {}),
          ...(isImplement ? { material } : {}),
        })
      } else if (attachment) {
        await api.buyAttachment(characterId, attachment.id, { free })
      } else if (mount) {
        await api.buyMount(characterId, mount.id, { free })
      }
      await refreshSheet()
      setSuccess(isService
        ? free
          ? t(
              'Услуга оказана без оплаты. В инвентарь ничего не добавлено.',
              'Service granted without payment. Nothing was added to inventory.',
            )
          : t(
              'Услуга оплачена. В инвентарь ничего не добавлено.',
              'Service paid. Nothing was added to inventory.',
            )
        : mount
          ? free
            ? t(
                'Скакун выдан без оплаты — он появился во вкладке «Скакуны», а не в инвентаре.',
                'The mount was granted without payment — it is on the Mounts tab, not in inventory.',
              )
            : t(
                'Скакун куплен — он появился во вкладке «Скакуны», а не в инвентаре.',
                'The mount was bought — it is on the Mounts tab, not in inventory.',
              )
        : free
          ? t('Добавлено без оплаты.', 'Added without payment.')
          : t('Покупка добавлена выбранному персонажу.', 'Purchase added to the selected character.'))
    } catch (err) {
      setError(err instanceof Error ? err.message : t('Ошибка', 'Error'))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal wide shop-product-modal" role="dialog" aria-modal="true"
        aria-labelledby="shop-product-title" onClick={event => event.stopPropagation()}>
        <div className="modal-header">
          <h3 id="shop-product-title">{productName(product)}</h3>
          <button type="button" className="small" onClick={onClose}>{t('Закрыть', 'Close')}</button>
        </div>

        {productSecondaryName(product) && (
          <p className="muted">{productSecondaryName(product)}</p>
        )}
        <p>{productDescription(product) || t('Описание отсутствует.', 'No description available.')}</p>
        {productSource(product) && <p className="muted small-text">{productSource(product)}</p>}

        <div className="shop-modal-facts">
          <div><span>{t('Категория', 'Category')}</span><strong>{CATEGORY_LABELS[product.category]}</strong></div>
          <div><span>{t('Цена', 'Price')}</span><strong>{effectivePrice ?? '—'} {CURRENCY_LABEL}</strong></div>
          <div><span>{t('Редкость', 'Rarity')}</span><strong>{effectiveRarity ?? '—'}</strong></div>
          {attachment && (
            <div><span>{t('Слоты улучшений', 'Hard points')}</span><strong>{attachment.hardPointCost}</strong></div>
          )}
          {mount && (
            <>
              <div><span>{t('Тип', 'Type')}</span><strong>{NPC_KIND_LABELS[mount.kind]}</strong></div>
              <div><span>{t('Ранения', 'Wounds')}</span><strong>{mount.woundThreshold}</strong></div>
              <div><span>{t('Поглощение', 'Soak')}</span><strong>{mount.soak}</strong></div>
              <div>
                <span>{t('Защита', 'Defense')}</span>
                <strong>{mount.meleeDefense}/{mount.rangedDefense}</strong>
              </div>
              <div><span>{t('Вместимость', 'Capacity')}</span><strong>{mount.capacity}</strong></div>
            </>
          )}
        </div>

        <div className="shop-modal-options">
          <label>
            {t('Персонаж', 'Character')}
            <select value={characterId} onChange={event => {
              setError(null)
              setSuccess(null)
              setCharacterId(event.target.value)
            }}>
              {characters.length === 0 && (
                <option value="">{t('Нет персонажа этой системы', 'No character for this system')}</option>
              )}
              {characters.map(character => (
                <option key={character.id} value={character.id}>
                  {character.name} · {character.archetype} · {character.career}
                </option>
              ))}
            </select>
          </label>

          {isImplement && (
            <label>
              {t('Материал', 'Material')}
              <select value={material}
                title={IMPLEMENT_MATERIAL_HINTS[material]}
                onChange={event => setMaterial(event.target.value as ImplementMaterial)}>
                {IMPLEMENT_MATERIALS.map(value => (
                  <option key={value} value={value}>{IMPLEMENT_MATERIAL_LABELS[value]}</option>
                ))}
              </select>
              <span className="muted small-text">{IMPLEMENT_MATERIAL_HINTS[material]}</span>
            </label>
          )}

          {hasCraftsmanship && (
            <label>
              {t('Материал / качество изготовления', 'Material / craftsmanship')}
              <select value={craftsmanship}
                title={WEAPON_CRAFTSMANSHIP_HINTS[craftsmanship]}
                onChange={event => setCraftsmanship(event.target.value as WeaponCraftsmanship)}>
                {WEAPON_CRAFTSMANSHIPS.map(value => (
                  <option key={value} value={value}>{WEAPON_CRAFTSMANSHIP_LABELS[value]}</option>
                ))}
              </select>
              <span className="muted small-text">{WEAPON_CRAFTSMANSHIP_HINTS[craftsmanship]}</span>
            </label>
          )}

          {!attachment && !mount && (
            <label>
              {t('Количество', 'Quantity')}
              <input type="number" min={1} value={quantity}
                onChange={event =>
                  setQuantity(Math.max(1, Math.trunc(Number(event.target.value)) || 1))} />
            </label>
          )}
        </div>

        {sheet && (
          <p className="shop-funds">
            {t('Доступно персонажу', 'Available to character')}: <strong>{funds} {CURRENCY_LABEL}</strong>
            {total != null && <> · {t('Итого', 'Total')}: <strong>{total} {CURRENCY_LABEL}</strong></>}
          </p>
        )}
        {mount && (
          <p className="hint">
            {t(
              'Скакун — существо со своим статблоком: он появляется во вкладке «Скакуны», а не в '
              + 'инвентаре, и его вес не входит в переносимый груз персонажа.',
              'A mount is a creature with its own statblock: it appears on the Mounts tab rather than in '
              + "inventory, and its weight is not part of the character's encumbrance.",
            )}
          </p>
        )}
        {isService && (
          <p className="hint">
            {t(
              'Услуга записывается как расход и никогда не появляется в инвентаре.',
              'A service is recorded as an expense and never appears in inventory.',
            )}
          </p>
        )}
        {error && <div className="error-box">{error}</div>}
        {success && <div className="success-box">{success}</div>}

        <div className="modal-actions">
          <button type="button" disabled={!canAdd} onClick={() => void act(true)}>
            {busy
              ? t('Сохранение…', 'Saving…')
              : mount
                // «Добавить» здесь неверно: скакун не попадает в инвентарь, его выдают.
                ? t('+ Выдать без оплаты', '+ Grant without payment')
                : t('+ Добавить без оплаты', '+ Add without payment')}
          </button>
          <button type="button" className="primary" disabled={!canBuy}
            title={!characterId
              ? t('Выберите персонажа', 'Choose a character')
              : effectivePrice == null
                ? t('У товара нет обычной цены', 'The product has no ordinary price')
                : total != null && total > funds
                  ? t('Недостаточно средств', 'Insufficient funds')
                  : undefined}
            onClick={() => void act(false)}>
            {busy ? t('Покупка…', 'Buying…') : t('Купить', 'Buy')}
          </button>
        </div>
      </div>
    </div>
  )
}
