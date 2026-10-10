import { useState } from 'react'
import { api } from '../api/client'
import type { CharacterMount, CharacterSheet, MountDef, Reference, SheetItem } from '../api/types'
import {
  CHARACTERISTICS, CHARACTERISTIC_LABELS, MOVEMENT_MODE_LABELS, NPC_KIND_LABELS,
  TRANSPORT_KIND_LABELS, mountGearLabel,
} from '../utils/labels'
import { BuyControl, SellControl } from './PriceControls'
import { PropertyTags } from './PropertyTags'
import { lang, t } from '../i18n'
import { Icon } from './Icon'
import { FilterChip, Switch } from './content/ContentUi'
import { VitalCard } from './content/VitalCard'
import { BookReference } from './BookReference'

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

const itemName = (item: SheetItem): string =>
  lang === 'ru' ? item.nameRu || item.name : item.name

/**
 * Транспорт персонажа (ROT-MOUNT-ITEM-01, ROT-TRANSPORT-01). Скакун и повозка — не позиции
 * инвентаря: у них свой статблок, порог повреждений и вместимость, а их груз не входит в
 * переносимый вес владельца. Установленное снаряжение меняет сам транспорт, а не всадника.
 */
export function TransportTab({ sheet, reference, onError, refresh }: Props) {
  const [busy, setBusy] = useState(false)
  const [openBuy, setOpenBuy] = useState<string | null>(null)
  const [openSell, setOpenSell] = useState<string | null>(null)

  const [filter, setFilter] = useState<'all' | 'mount' | 'vehicle'>('all')
  const allCatalog = reference.mounts ?? []
  const catalog = allCatalog.filter(d => filter === 'all' || d.transportKind === filter)
  const funds = sheet.money

  async function run(action: () => Promise<unknown>) {
    if (busy) return
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
      <div className="mounts-intro">
        <p className="hint small-text"><Icon name="info-circle" className="button-icon" />
          {t(
            'Скакун и повозка — не предметы: у них свой статблок и своя вместимость, а их груз не входит '
            + 'в переносимый вес владельца. Попона и седельные сумки ставятся на конкретный транспорт: '
            + 'защита попоны достаётся ему, а не всаднику.',
            'Mounts and wagons are not items: they have their own statblock and capacity, and their cargo '
            + "is not part of the owner's encumbrance. Barding and saddlebags are installed on one specific "
            + 'transport: barding protects it rather than the rider.',
          )}
        </p>

      </div>
      <div className="transport-layout">
      <section className="transport-owned">
        {sheet.mounts.length === 0
          ? <p className="rd-empty"><Icon name="horse-toy" className="button-icon" />{t('Транспорта нет — купите или получите в конюшне', 'No transport — buy or receive one at the stable')}</p>
          : (
            <div className="mount-list">
              {sheet.mounts.map(mount => (
                <MountCard key={mount.id} mount={mount} sheet={sheet} busy={busy} run={run}
                  qualityLabel={qualityLabel}
                  qualityDefinitions={reference.qualities}
                  sellOpen={openSell === mount.id}
                  onToggleSell={() => setOpenSell(openSell === mount.id ? null : mount.id)} />
              ))}
            </div>
          )}
      </section>

      <aside className="panel sheet-sticky transport-stable">
        <h3 className="sheet-card-heading">{t('Конюшня', 'Stable')}<span className="sheet-wallet"><Icon name="coin" />{funds}</span></h3>
        <div className="sheet-chips">{(['all', 'mount', 'vehicle'] as const).map(k => <FilterChip key={k} active={filter === k} onClick={() => setFilter(k)}>{k === 'all' ? t('Все', 'All') : k === 'mount' ? t('Скакуны', 'Mounts') : t('Повозки', 'Wagons')} {allCatalog.filter(d => k === 'all' || d.transportKind === k).length}</FilterChip>)}</div>
        {catalog.length === 0
          ? <p className="muted">{t('В этой системе транспорта нет.', 'This system has no transport.')}</p>
          : <div className="mount-catalog-list">
            {catalog.map(def => (
              <div className="shop-row" key={def.id}>
                <div className="shop-row-head">
                  <div className="shop-row-info">
                    <strong>{mountName(def)}</strong>
                    {lang === 'ru' && def.name !== mountName(def) &&
                      <span className="muted small-text name-secondary"> · {def.name}</span>}
                    <div className="muted small-text">
                      {TRANSPORT_KIND_LABELS[def.transportKind]}
                      {def.transportKind === 'mount' && ` · ${NPC_KIND_LABELS[def.kind]}`}
                      {def.price == null
                        ? t(' · без обычной цены', ' · no ordinary price')
                        : ` · ${t('цена', 'price')} ${def.price}`}
                      {` · ${t('редкость', 'rarity')} ${def.rarity}`}
                      {` · ${t('вместимость', 'capacity')} ${def.capacity}`}
                    </div>
                    <div className="transport-catalog-tags"><span>{t('груз', 'cargo')} {def.capacity}</span><span>{t('погл.', 'soak')} {def.soak}</span><span>{def.transportKind === 'vehicle' ? t('прочн.', 'hull') : t('ран.', 'wounds')} {def.woundThreshold}</span>{def.movementMode === 'flight' && <span>{t('полёт', 'flight')}</span>}{def.requiresTraction && <span>{t('нужна тяга', 'needs traction')}</span>}{def.code === 'rot.mount.war-mount' && <span>{t('боевой', 'war mount')}</span>}</div>
                    <details className="transport-details"><summary>{t('Подробнее', 'Details')}</summary><MountStatline def={def} qualityLabel={qualityLabel} qualityDefinitions={reference.qualities} /></details>
                    {mountDescription(def) &&
                      <div className="muted small-text shop-desc">{mountDescription(def)}</div>}
                    <BookReference source={def.source} />
                  </div>
                  <div className="shop-row-actions">
                    {def.price != null && (
                      <button className="primary tiny" disabled={busy || def.price > funds} title={def.price > funds ? t('Недостаточно монет', 'Not enough coins') : undefined}
                        onClick={() => setOpenBuy(openBuy === def.id ? null : def.id)}>
                        {openBuy === def.id ? t('Отмена', 'Cancel') : t('Купить', 'Buy')}
                      </button>
                    )}
                    {/* Выдача без оплаты: находка, награда, транспорт от ведущего. */}
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
                      // Транспорт покупается по одному: это экземпляр, а не стопка вещей.
                      await api.buyMount(sheet.id, def.id, opts)
                      setOpenBuy(null)
                    })} />
                )}
              </div>
            ))}
          </div>}
      </aside>
      </div>
    </div>
  )
}

/** Статблок профиля одной строкой: характеристики, пороги, защита, навыки, атака, способности. */
function MountStatline({ def, qualityLabel, qualityDefinitions }: {
  def: MountDef
  qualityLabel: (code: string) => string
  qualityDefinitions: Reference['qualities']
}) {
  const isVehicle = def.transportKind === 'vehicle'
  return (
    <div className="mount-statline small-text">
      {/* У повозки характеристик нет вовсе — показывать шесть нулей нечестно. */}
      {!isVehicle && (
        <div>
          {CHARACTERISTICS.map(key => (
            <span key={key} className="mount-char">
              {CHARACTERISTIC_LABELS[key]} <strong>{def.characteristics[key]}</strong>
            </span>
          ))}
        </div>
      )}
      <div className="muted">
        {t('Поглощение', 'Soak')} {def.soak}
        {/* У повозки порог ран профиля читается как прочность, а порог усталости — как порог систем. */}
        {' · '}{isVehicle ? t('Прочность', 'Hull') : t('Ранения', 'Wounds')} {def.woundThreshold}
        {def.strainThreshold != null &&
          ` · ${isVehicle ? t('Системы', 'Systems') : t('Усталость', 'Strain')} ${def.strainThreshold}`}
        {' · '}{t('Защита', 'Defense')} {def.meleeDefense}/{def.rangedDefense}
        {' · '}{t('силуэт', 'silhouette')} {def.silhouette}
        {' · '}{t('движение', 'movement')} {MOVEMENT_MODE_LABELS[def.movementMode]}
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
          {attack.qualityCodes.length > 0 && (
            <>
              {' · '}
              <PropertyTags properties={attack.qualityCodes.map(qualityLabel).join(', ')}
                qualityDefinitions={qualityDefinitions} />
            </>
          )}
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

/** Карточка транспорта: состояние, тяга, груз, установленное снаряжение, продажа и удаление. */
function MountCard({ mount, sheet, busy, run, qualityLabel, qualityDefinitions, sellOpen, onToggleSell }: {
  mount: CharacterMount
  sheet: CharacterSheet
  busy: boolean
  run: (action: () => Promise<unknown>) => Promise<void>
  qualityLabel: (code: string) => string
  qualityDefinitions: Reference['qualities']
  sellOpen: boolean
  onToggleSell: () => void
}) {
  const [name, setName] = useState(mount.name)
  const def = mount.definition
  const isVehicle = def.transportKind === 'vehicle'

  // Тянуть может только живой транспорт, которому тяга не нужна самому и который ещё не запряжён.
  const draftCandidates = sheet.mounts.filter(m =>
    m.id !== mount.id
    && m.definition.transportKind === 'mount'
    && !m.definition.requiresTraction
    && m.id !== mount.drawnByMountId
    && !sheet.mounts.some(other => other.id !== mount.id && other.drawnByMountId === m.id))
  const cargo = mount.cargo.filter(i => !i.isInstalledOnMount)
  const installed = mount.cargo.filter(i => i.isInstalledOnMount)
  // Погрузить можно то, что сейчас при персонаже.
  const loadable = sheet.items.filter(i => i.carriedByMountId == null)

  return <article className={`panel mount-card${mount.isActive ? ' active' : ''}`}>
    <div className="mount-card-head">
      <span className="mount-icon"><Icon name={isVehicle ? 'truck' : def.movementMode === 'flight' ? 'feather' : 'horse'} /></span>
      <div className="mount-card-name"><input className="mount-name-input" value={name} maxLength={120} placeholder={mountName(def)} aria-label={t('Название', 'Name')} disabled={busy}
        onChange={e => setName(e.target.value)} onBlur={() => name !== mount.name && run(() => api.updateMount(sheet.id, mount.id, { name }))} />
        <div className="muted small-text">{mountName(def)} · {TRANSPORT_KIND_LABELS[def.transportKind]}{!isVehicle && ` · ${NPC_KIND_LABELS[def.kind]}`}</div></div>
      <div className="mount-active-control"><Switch checked={mount.isActive} disabled={busy} label={isVehicle ? t('в ходу', 'in use') : t('под седлом', 'saddled')}
        onChange={isActive => void run(() => api.updateMount(sheet.id, mount.id, { isActive }))} /><small className="muted">{isVehicle ? t('в ходу', 'in use') : t('под седлом', 'saddled')}</small></div>
    </div>
    <div className="mount-flags">{[[mount.isIncapacitated, t('выведен из строя', 'incapacitated')], [mount.isOverloaded, t('перегружен', 'overloaded')], [mount.needsTraction, t('без тяги', 'no traction')]].filter(([show]) => show).map(([, text]) => <span key={String(text)}><Icon name="alert-triangle" className="button-icon" />{text}</span>)}</div>
    <div className="mount-card-body">
      <div><VitalCard kind="wounds" label={isVehicle ? t('Повреждения', 'Damage') : t('Ранения', 'Wounds')} current={mount.woundsCurrent} threshold={def.woundThreshold} disabled={busy}
        onChange={woundsCurrent => void run(() => api.updateMount(sheet.id, mount.id, { woundsCurrent }))} />
        <div className="mount-stat-tiles">{[[t('Погл.', 'Soak'), mount.soak], [t('Защита', 'Defense'), `${mount.meleeDefense}/${mount.rangedDefense}`], [t('Силуэт', 'Silhouette'), def.silhouette], [isVehicle ? t('Системы', 'Systems') : t('Движение', 'Movement'), isVehicle ? def.strainThreshold : MOVEMENT_MODE_LABELS[def.movementMode]]].map(([k,v]) => <div key={k}><small>{k}</small><b>{v}</b></div>)}</div>
        <MountStatline def={def} qualityLabel={qualityLabel} qualityDefinitions={qualityDefinitions} />
      </div>
      <div>{def.requiresTraction && <section className="mount-traction"><h4>{t('Тяга', 'Drawn by')}</h4><div className="sheet-chips">
        <FilterChip active={!mount.drawnByMountId} disabled={busy} onClick={() => void run(() => api.updateMount(sheet.id, mount.id, { clearDrawnBy: true }))}>{t('Не запряжён', 'Unhitched')}</FilterChip>
        {mount.drawnByMountId && <FilterChip active disabled={busy} onClick={() => {}}>{mount.drawnByName}</FilterChip>}
        {draftCandidates.map(m => <FilterChip key={m.id} active={false} disabled={busy} onClick={() => void run(() => api.updateMount(sheet.id, mount.id, { drawnByMountId: m.id }))}>{m.displayName}</FilterChip>)}</div>
        <p className="muted small-text">{t('Без тяги транспорт стоит, но груз остаётся на нём.', 'Without traction the transport stays put, and its cargo stays with it.')}</p></section>}
        <CargoSection sheet={sheet} mount={mount} busy={busy} run={run} cargo={cargo} installed={installed} loadable={loadable} />
      </div>
    </div>
    <footer className="sheet-card-heading mount-footer"><BookReference source={def.source} /><div className="form-actions">
      {def.price != null && <button className="small" disabled={busy} onClick={onToggleSell}>{sellOpen ? t('Отмена', 'Cancel') : t(`Продать · ${def.price}`, `Sell · ${def.price}`)}</button>}
      <button className="small danger" disabled={busy} title={t('Удалить без выручки; груз вернётся владельцу', 'Remove without proceeds; cargo returns to the owner')} aria-label={t('Удалить транспорт', 'Remove transport')}
        onClick={() => void run(() => api.removeMount(sheet.id, mount.id))}><Icon name="trash" className="button-icon" /></button></div></footer>
    {sellOpen && def.price != null && <SellControl unitPrice={def.price} maxQuantity={1} onConfirm={(_quantity, opts) => run(async () => { await api.sellMount(sheet.id, mount.id, opts); onToggleSell() })} />}
  </article>
}

/** Груз и установленное снаряжение транспорта: список, погрузка и снятие. */
function CargoSection({ sheet, mount, busy, run, cargo, installed, loadable }: {
  sheet: CharacterSheet
  mount: CharacterMount
  busy: boolean
  run: (action: () => Promise<unknown>) => Promise<void>
  cargo: SheetItem[]
  installed: SheetItem[]
  loadable: SheetItem[]
}) {
  const [pick, setPick] = useState('')
  const [quantity, setQuantity] = useState(1)
  const [overrideReason, setOverrideReason] = useState('')

  const picked = loadable.find(i => i.id === pick)
  // Попона и сумки ставятся, всё остальное кладётся грузом: выбирать это отдельно игроку незачем.
  const install = picked?.isMountGear ?? false
  // Попона рассчитана на боевого скакуна; на любого другого её ставит ведущий с причиной — то же
  // правило, что на сервере, поэтому кнопка заблокирована, пока причины нет (ROT-MOUNT-NPC-01).
  const needsGmReason = install && picked?.isBarding === true && mount.requiresGmApprovalForBarding
  const blocked = !picked || (needsGmReason && overrideReason.trim().length === 0)

  async function load(item: SheetItem, count = 1, gmReason = '') {
    await api.moveCargo(sheet.id, item.id, { mountId: mount.id, quantity: count, install: item.isMountGear, ...(gmReason ? { installOverrideReason: gmReason } : {}) })
    setPick(''); setQuantity(1); setOverrideReason('')
  }
  return <section className="mount-cargo small-text">
    <h4 className="sheet-card-heading">{t('Груз', 'Cargo')}<span className={mount.isOverloaded ? 'danger-text' : 'muted'}>{mount.carriedLoad} / {mount.capacity}</span></h4>
    <div className={`mount-cargo-meter${mount.isOverloaded ? ' overloaded' : ''}`} role="meter" aria-label={t('Груз', 'Cargo')} aria-valuemin={0} aria-valuemax={Math.max(1, mount.capacity)} aria-valuenow={Math.min(mount.carriedLoad, Math.max(1, mount.capacity))}><i style={{ width: `${Math.min(100, mount.capacity > 0 ? mount.carriedLoad / mount.capacity * 100 : 0)}%` }} /></div>
    {cargo.length === 0 && installed.length === 0 ? <p className="muted">{t('Пусто.', 'Empty.')}</p> : <ul className="cargo-list">{[...installed, ...cargo].map(item => <li key={item.id}>
      <Icon name={item.isInstalledOnMount ? 'link' : 'package'} className="button-icon" /><span>{itemName(item)}{item.quantity > 1 && ` ×${item.quantity}`}</span>
      <small className="muted">{item.isInstalledOnMount ? t('установлено', 'installed') : t(`вес ${item.encumbrance * item.quantity}`, `load ${item.encumbrance * item.quantity}`)}</small>
      <button className="tiny" disabled={busy} title={t('Забрать владельцу', 'Take back to the owner')} aria-label={t(`Забрать владельцу: ${itemName(item)}`, `Take back to the owner: ${itemName(item)}`)} onClick={() => void run(() => api.moveCargo(sheet.id, item.id, { mountId: null }))}><Icon name="arrow-back-up" className="button-icon" /></button>
    </li>)}</ul>}
    {loadable.length > 0 && <div className="cargo-load"><h4>{t('Погрузить с персонажа', 'Load from character')}</h4><div className="sheet-chips" role="group" aria-label={t('Что погрузить', 'What to load')}>
      {loadable.map(item => <FilterChip key={item.id} active={pick === item.id} disabled={busy} onClick={() => {
        const reasonNeeded = item.isMountGear && item.isBarding && mount.requiresGmApprovalForBarding
        if (item.quantity <= 1 && !reasonNeeded) void run(() => load(item))
        else { setPick(item.id); setQuantity(1); setOverrideReason('') }
      }}><Icon name={item.isMountGear ? 'link' : 'arrow-bar-to-down'} className="button-icon" />{itemName(item)}{item.quantity > 1 && ` ×${item.quantity}`}</FilterChip>)}
    </div>
    {picked && <div className="cargo-load-options">{picked.quantity > 1 && <label>{t('Сколько', 'How many')}<input type="number" min={1} max={picked.quantity} value={quantity} disabled={busy} onChange={e => setQuantity(Math.min(picked.quantity, Math.max(1, Math.trunc(Number(e.target.value)) || 1)))} /></label>}
      {needsGmReason && <div className="warn-text cargo-gm-reason"><p>{t('Попона рассчитана на боевого скакуна — для другого нужна причина от ведущего.', 'Barding is meant for a war mount — putting it on another one needs a GM reason.')}</p><label>{t('Причина ведущего', 'GM reason')}<input value={overrideReason} maxLength={200} disabled={busy} onChange={e => setOverrideReason(e.target.value)} /></label></div>}
      <button className="tiny primary" disabled={busy || blocked} onClick={() => void run(() => load(picked, quantity, needsGmReason ? overrideReason.trim() : ''))}>{install ? t('Установить', 'Install') : t('Погрузить', 'Load')}</button>
    </div>}</div>}
  </section>
}
