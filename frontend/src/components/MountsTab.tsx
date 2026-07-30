import { useState } from 'react'
import { api } from '../api/client'
import type { CharacterMount, CharacterSheet, MountDef, Reference } from '../api/types'
import {
  CHARACTERISTICS, CHARACTERISTIC_LABELS, NPC_KIND_LABELS, mountGearLabel,
} from '../utils/labels'
import { BuyControl, SellControl } from './PriceControls'
import { lang, t } from '../i18n'

interface Props {
  sheet: CharacterSheet
  reference: Reference
  onError: (message: string) => void
  refresh: () => Promise<void>
}

const mountName = (def: MountDef): string =>
  lang === 'ru' ? def.nameRu || def.name : def.name

const mountDescription = (def: MountDef): string =>
  lang === 'ru' ? def.description : def.descriptionEn || def.description

/**
 * Скакуны персонажа (ROT-MOUNT-ITEM-01). Скакун не позиция инвентаря: у него свой статблок, порог
 * ран и вместимость, и в переносимый вес владельца он не входит. Полный раздел «Транспорт» с
 * грузом по позициям, повозками и установкой попоны — отдельная задача (ROT-TRANSPORT-01).
 */
export function MountsTab({ sheet, reference, onError, refresh }: Props) {
  const [busy, setBusy] = useState(false)
  const [openBuy, setOpenBuy] = useState<string | null>(null)
  const [openSell, setOpenSell] = useState<string | null>(null)

  const catalog = reference.mounts ?? []
  const funds = sheet.money + (sheet.isCreationPhase ? sheet.startingPurchaseBudget : 0)

  async function run(action: () => Promise<unknown>) {
    setBusy(true)
    try {
      await action()
      await refresh()
    } catch (err) {
      onError(err instanceof Error ? err.message : t('Ошибка', 'Error'))
    } finally {
      setBusy(false)
    }
  }

  /** Русское название качества атаки из справочника; неизвестный код показывается как есть. */
  function qualityLabel(code: string): string {
    const quality = reference.qualities.find(q => q.code === code)
    if (!quality) return code
    return lang === 'ru' ? quality.nameRu || quality.nameEn : quality.nameEn
  }

  return (
    <div className="mounts-tab">
      <section className="panel">
        <h3>{t('Скакуны', 'Mounts')}</h3>
        <p className="hint small-text">
          {t(
            'Скакун — существо со своим статблоком, а не предмет: его вес не входит в переносимый груз '
            + 'владельца, а вместимость берётся из профиля книги. Груз, повозки, попона и седельные сумки '
            + 'появятся в разделе «Транспорт».',
            'A mount is a creature with its own statblock rather than an item: its weight is not part of the '
            + "owner's encumbrance, and its capacity comes from the book profile. Cargo, wagons, barding and "
            + 'saddlebags will arrive with the Transport section.',
          )}
        </p>

        {sheet.mounts.length === 0
          ? <p className="muted">{t('Скакунов нет.', 'No mounts yet.')}</p>
          : (
            <div className="mount-list">
              {sheet.mounts.map(mount => (
                <MountCard key={mount.id} mount={mount} sheet={sheet} busy={busy} run={run}
                  qualityLabel={qualityLabel}
                  sellOpen={openSell === mount.id}
                  onToggleSell={() => setOpenSell(openSell === mount.id ? null : mount.id)} />
              ))}
            </div>
          )}
      </section>

      <section className="panel">
        <h3>{t('Купить скакуна', 'Buy a mount')}</h3>
        {catalog.length === 0
          ? <p className="muted">{t('В этой системе скакунов нет.', 'This system has no mounts.')}</p>
          : catalog.map(def => (
            <div className="shop-row" key={def.id}>
              <div className="shop-row-head">
                <div className="shop-row-info">
                  <strong>{mountName(def)}</strong>
                  {lang === 'ru' && def.name !== mountName(def) &&
                    <span className="muted small-text name-secondary"> · {def.name}</span>}
                  <div className="muted small-text">
                    {NPC_KIND_LABELS[def.kind]}
                    {def.price == null
                      ? t(' · без обычной цены', ' · no ordinary price')
                      : ` · ${t('цена', 'price')} ${def.price}`}
                    {` · ${t('редкость', 'rarity')} ${def.rarity}`}
                    {` · ${t('вместимость', 'capacity')} ${def.capacity}`}
                  </div>
                  <MountStatline def={def} qualityLabel={qualityLabel} />
                  {mountDescription(def) &&
                    <div className="muted small-text shop-desc">{mountDescription(def)}</div>}
                  <div className="muted small-text">{def.source}</div>
                </div>
                <div className="shop-row-actions">
                  {def.price != null && (
                    <button className="primary tiny" disabled={busy}
                      onClick={() => setOpenBuy(openBuy === def.id ? null : def.id)}>
                      {openBuy === def.id ? t('Отмена', 'Cancel') : t('Купить', 'Buy')}
                    </button>
                  )}
                  {/* Выдача без оплаты: находка, награда, скакун от ведущего. */}
                  <button className="tiny" disabled={busy}
                    title={t('Выдать без оплаты', 'Grant without paying')}
                    onClick={() => run(() => api.buyMount(sheet.id, def.id, { free: true }))}>
                    {t('+ Выдать', '+ Grant')}
                  </button>
                </div>
              </div>
              {openBuy === def.id && def.price != null && (
                <BuyControl unitPrice={def.price} money={funds}
                  onConfirm={(_quantity, opts) => run(async () => {
                    // Скакун покупается по одному: это существо, а не стопка вещей.
                    await api.buyMount(sheet.id, def.id, opts)
                    setOpenBuy(null)
                  })} />
              )}
            </div>
          ))}
      </section>
    </div>
  )
}

/** Статблок профиля одной строкой: характеристики, пороги, защита, навыки, атака, способности. */
function MountStatline({ def, qualityLabel }: {
  def: MountDef
  qualityLabel: (code: string) => string
}) {
  return (
    <div className="mount-statline small-text">
      <div>
        {CHARACTERISTICS.map(key => (
          <span key={key} className="mount-char">
            {CHARACTERISTIC_LABELS[key]} <strong>{def.characteristics[key]}</strong>
          </span>
        ))}
      </div>
      <div className="muted">
        {t('Поглощение', 'Soak')} {def.soak}
        {' · '}{t('Ранения', 'Wounds')} {def.woundThreshold}
        {def.strainThreshold != null && ` · ${t('Усталость', 'Strain')} ${def.strainThreshold}`}
        {' · '}{t('Защита', 'Defense')} {def.meleeDefense}/{def.rangedDefense}
        {' · '}{t('силуэт', 'silhouette')} {def.silhouette}
      </div>
      {def.skills.length > 0 && (
        <div className="muted">
          {t('Навыки', 'Skills')}: {def.skills.map(s => s.isGroupSkill
            ? `${s.name} (${t('групповой', 'group')})`
            : `${s.name} ${s.ranks}`).join(', ')}
        </div>
      )}
      {def.attacks.map(attack => (
        <div className="muted" key={attack.name}>
          {t('Атака', 'Attack')}: {lang === 'ru' ? attack.nameRu || attack.name : attack.name}
          {` · ${attack.skillName} · ${t('урон', 'damage')} ${attack.damage} · `}
          {t('крит', 'crit')} {attack.critical}
          {attack.qualityCodes.length > 0 && ` · ${attack.qualityCodes.map(qualityLabel).join(', ')}`}
        </div>
      ))}
      {def.abilities.map(ability => (
        <div className="muted" key={ability.name}>
          <strong>{lang === 'ru' ? ability.nameRu || ability.name : ability.name}</strong>
          {': '}{lang === 'ru' ? ability.description : ability.descriptionEn || ability.description}
        </div>
      ))}
      {def.includedGear.length > 0 && (
        <div className="muted">
          {t('В комплекте', 'Included')}: {def.includedGear.map(mountGearLabel).join(', ')}
        </div>
      )}
      {def.requiresRidingCheck && (
        <div className="muted">
          {t(
            'В бою и под стрессом наездник делает проверку Верховой езды; сложность назначает ведущий.',
            'In combat or under stress the rider makes a Riding check; the GM sets the difficulty.',
          )}
        </div>
      )}
    </div>
  )
}

/** Карточка скакуна персонажа: состояние, груз, продажа и удаление. */
function MountCard({ mount, sheet, busy, run, qualityLabel, sellOpen, onToggleSell }: {
  mount: CharacterMount
  sheet: CharacterSheet
  busy: boolean
  run: (action: () => Promise<unknown>) => Promise<void>
  qualityLabel: (code: string) => string
  sellOpen: boolean
  onToggleSell: () => void
}) {
  const [name, setName] = useState(mount.name)
  const def = mount.definition

  return (
    <div className="panel mount-card">
      <div className="mount-card-head">
        <div>
          <input className="mount-name-input" value={name} maxLength={120}
            placeholder={mountName(def)}
            aria-label={t('Кличка', 'Name')}
            onChange={e => setName(e.target.value)}
            onBlur={() => name !== mount.name && run(() => api.updateMount(sheet.id, mount.id, { name }))} />
          <div className="muted small-text">
            {mountName(def)} · {NPC_KIND_LABELS[def.kind]}
            {mount.isIncapacitated &&
              <span className="error"> · {t('выведен из строя', 'incapacitated')}</span>}
            {mount.isOverloaded && <span className="error"> · {t('перегружен', 'overloaded')}</span>}
          </div>
        </div>
        <div className="mount-card-actions">
          <label className="small-text">
            <input type="checkbox" checked={mount.isActive} disabled={busy}
              onChange={e => run(() => api.updateMount(sheet.id, mount.id, { isActive: e.target.checked }))} />
            {' '}{t('под седлом', 'in use')}
          </label>
          {def.price != null && (
            <button className="tiny" disabled={busy} onClick={onToggleSell}>
              {sellOpen ? t('Отмена', 'Cancel') : t('Продать', 'Sell')}
            </button>
          )}
          <button className="tiny danger" disabled={busy}
            title={t('Удалить без выручки', 'Remove without proceeds')}
            onClick={() => run(() => api.removeMount(sheet.id, mount.id))}>
            {t('Удалить', 'Remove')}
          </button>
        </div>
      </div>

      <div className="mount-card-state small-text">
        <label>{t('Ранения', 'Wounds')}
          <input type="number" min={0} max={def.woundThreshold} value={mount.woundsCurrent}
            disabled={busy} style={{ width: '4rem' }} aria-label={t('Ранения', 'Wounds')}
            onChange={e => run(() => api.updateMount(sheet.id, mount.id,
              { woundsCurrent: Math.max(0, Math.trunc(Number(e.target.value)) || 0) }))} />
          <span className="muted"> / {def.woundThreshold}</span>
        </label>
        <label>{t('Груз', 'Load')}
          <input type="number" min={0} value={mount.carriedLoad}
            disabled={busy} style={{ width: '4rem' }} aria-label={t('Груз', 'Load')}
            onChange={e => run(() => api.updateMount(sheet.id, mount.id,
              { carriedLoad: Math.max(0, Math.trunc(Number(e.target.value)) || 0) }))} />
          <span className={mount.isOverloaded ? 'error' : 'muted'}> / {mount.capacity}</span>
        </label>
      </div>

      <MountStatline def={def} qualityLabel={qualityLabel} />

      {sellOpen && def.price != null && (
        <SellControl unitPrice={def.price} maxQuantity={1}
          onConfirm={(_quantity, opts) => run(async () => {
            await api.sellMount(sheet.id, mount.id, opts)
            onToggleSell()
          })} />
      )}
    </div>
  )
}
