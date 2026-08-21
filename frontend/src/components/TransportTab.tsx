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

  const catalog = reference.mounts ?? []
  const funds = sheet.money

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
        <h3>{t('Транспорт', 'Transport')}</h3>
        <p className="hint small-text">
          {t(
            'Скакун и повозка — не предметы: у них свой статблок и своя вместимость, а их груз не входит '
            + 'в переносимый вес владельца. Попона и седельные сумки ставятся на конкретный транспорт: '
            + 'защита попоны достаётся ему, а не всаднику.',
            'Mounts and wagons are not items: they have their own statblock and capacity, and their cargo '
            + "is not part of the owner's encumbrance. Barding and saddlebags are installed on one specific "
            + 'transport: barding protects it rather than the rider.',
          )}
        </p>

        {sheet.mounts.length === 0
          ? <p className="muted">{t('Транспорта нет.', 'No transport yet.')}</p>
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

      <section className="panel">
        <h3>{t('Купить транспорт', 'Buy transport')}</h3>
        {catalog.length === 0
          ? <p className="muted">{t('В этой системе транспорта нет.', 'This system has no transport.')}</p>
          : catalog.map(def => (
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
                  <MountStatline def={def} qualityLabel={qualityLabel} qualityDefinitions={reference.qualities} />
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
      </section>
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

  return (
    <div className="panel mount-card">
      <div className="mount-card-head">
        <div>
          <input className="mount-name-input" value={name} maxLength={120}
            placeholder={mountName(def)}
            aria-label={t('Название', 'Name')}
            onChange={e => setName(e.target.value)}
            onBlur={() => name !== mount.name && run(() => api.updateMount(sheet.id, mount.id, { name }))} />
          <div className="muted small-text">
            {mountName(def)} · {TRANSPORT_KIND_LABELS[def.transportKind]}
            {!isVehicle && ` · ${NPC_KIND_LABELS[def.kind]}`}
            {mount.isIncapacitated &&
              <span className="error"> · {t('выведен из строя', 'incapacitated')}</span>}
            {mount.isOverloaded && <span className="error"> · {t('перегружен', 'overloaded')}</span>}
            {mount.needsTraction &&
              <span className="error"> · {t('без тяги', 'no traction')}</span>}
          </div>
        </div>
        <div className="mount-card-actions">
          <label className="small-text">
            <input type="checkbox" checked={mount.isActive} disabled={busy}
              onChange={e => run(() => api.updateMount(sheet.id, mount.id, { isActive: e.target.checked }))} />
            {' '}{isVehicle ? t('в ходу', 'in use') : t('под седлом', 'in use')}
          </label>
          {def.price != null && (
            <button className="tiny" disabled={busy} onClick={onToggleSell}>
              {sellOpen ? t('Отмена', 'Cancel') : t('Продать', 'Sell')}
            </button>
          )}
          <button className="tiny danger" disabled={busy}
            title={t('Удалить без выручки; груз вернётся владельцу',
              'Remove without proceeds; cargo returns to the owner')}
            onClick={() => run(() => api.removeMount(sheet.id, mount.id))}>
            {t('Удалить', 'Remove')}
          </button>
        </div>
      </div>

      <div className="mount-card-state small-text">
        <label>{isVehicle ? t('Повреждения', 'Damage') : t('Ранения', 'Wounds')}
          <input type="number" min={0} max={def.woundThreshold} value={mount.woundsCurrent}
            disabled={busy} style={{ width: '4rem' }}
            aria-label={isVehicle ? t('Повреждения', 'Damage') : t('Ранения', 'Wounds')}
            onChange={e => run(() => api.updateMount(sheet.id, mount.id,
              { woundsCurrent: Math.max(0, Math.trunc(Number(e.target.value)) || 0) }))} />
          <span className="muted"> / {def.woundThreshold}</span>
        </label>
        <span className="muted">
          {t('Поглощение', 'Soak')} {mount.soak}
          {' · '}{t('Защита', 'Defense')} {mount.meleeDefense}/{mount.rangedDefense}
        </span>
      </div>

      {def.requiresTraction && (
        <div className="mount-card-state small-text">
          <label>{t('Тяга', 'Drawn by')}
            <select value={mount.drawnByMountId ?? ''} disabled={busy}
              aria-label={t('Тяга', 'Drawn by')}
              onChange={e => run(() => api.updateMount(sheet.id, mount.id, e.target.value
                ? { drawnByMountId: e.target.value }
                : { clearDrawnBy: true }))}>
              <option value="">{t('— не запряжён —', '— unhitched —')}</option>
              {mount.drawnByMountId && (
                <option value={mount.drawnByMountId}>{mount.drawnByName}</option>
              )}
              {draftCandidates.map(m => (
                <option key={m.id} value={m.id}>{m.displayName}</option>
              ))}
            </select>
          </label>
          {mount.needsTraction && (
            <span className="muted">
              {t('Без тяги транспорт стоит, но груз остаётся на нём.',
                'Without traction the transport stays put, and its cargo stays with it.')}
            </span>
          )}
        </div>
      )}

      <MountStatline def={def} qualityLabel={qualityLabel} qualityDefinitions={qualityDefinitions} />

      <CargoSection sheet={sheet} mount={mount} busy={busy} run={run}
        cargo={cargo} installed={installed} loadable={loadable} />

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

  return (
    <div className="mount-cargo small-text">
      <strong>{t('Груз', 'Cargo')}</strong>{' '}
      <span className={mount.isOverloaded ? 'error' : 'muted'}>
        {mount.carriedLoad} / {mount.capacity}
      </span>

      {cargo.length === 0 && installed.length === 0
        ? <div className="muted">{t('Пусто.', 'Empty.')}</div>
        : (
          <ul className="cargo-list">
            {[...installed, ...cargo].map(item => (
              <li key={item.id}>
                {itemName(item)}
                {item.quantity > 1 && ` ×${item.quantity}`}
                <span className="muted">
                  {item.isInstalledOnMount
                    ? ` · ${t('установлено', 'installed')}`
                    : ` · ${t('вес', 'enc')} ${item.encumbrance * item.quantity}`}
                </span>
                {' '}
                <button className="tiny" disabled={busy}
                  title={t('Забрать владельцу', 'Take back to the owner')}
                  onClick={() => run(() => api.moveCargo(sheet.id, item.id, { mountId: null }))}>
                  {t('Снять', 'Unload')}
                </button>
              </li>
            ))}
          </ul>
        )}

      {loadable.length > 0 && (
        <div className="cargo-load">
          <select value={pick} disabled={busy} aria-label={t('Что погрузить', 'What to load')}
            onChange={e => { setPick(e.target.value); setQuantity(1) }}>
            <option value="">{t('— выберите позицию —', '— pick an item —')}</option>
            {loadable.map(item => (
              <option key={item.id} value={item.id}>
                {itemName(item)}{item.quantity > 1 ? ` ×${item.quantity}` : ''}
              </option>
            ))}
          </select>
          {picked && picked.quantity > 1 && (
            <input type="number" min={1} max={picked.quantity} value={quantity}
              disabled={busy} style={{ width: '4rem' }}
              aria-label={t('Сколько', 'How many')}
              onChange={e => setQuantity(Math.min(
                picked.quantity, Math.max(1, Math.trunc(Number(e.target.value)) || 1)))} />
          )}
          {needsGmReason && (
            <input type="text" value={overrideReason} maxLength={200} disabled={busy}
              aria-label={t('Причина ведущего', 'GM reason')}
              placeholder={t('Причина ведущего', 'GM reason')}
              onChange={e => setOverrideReason(e.target.value)} />
          )}
          <button className="tiny primary" disabled={busy || blocked}
            onClick={() => run(async () => {
              await api.moveCargo(sheet.id, pick, {
                mountId: mount.id,
                quantity,
                install,
                ...(needsGmReason ? { installOverrideReason: overrideReason.trim() } : {}),
              })
              setPick('')
              setQuantity(1)
              setOverrideReason('')
            })}>
            {install ? t('Установить', 'Install') : t('Погрузить', 'Load')}
          </button>
        </div>
      )}
      {needsGmReason && (
        <div className="muted">
          {t('Попона рассчитана на боевого скакуна — для другого нужна причина от ведущего.',
            'Barding is meant for a war mount — putting it on another one needs a GM reason.')}
        </div>
      )}
    </div>
  )
}
