import { useMemo, useState, type ReactNode } from 'react'
import { api } from '../api/client'
import type {
  AttachmentDef, CharacterAttachment, CharacterSheet, ItemDamageState, Reference, SheetItem,
  WeaponFormTrait,
} from '../api/types'
import {
  ITEM_DAMAGE_STATE_HINTS, ITEM_DAMAGE_STATE_LABELS, ITEM_KIND_LABELS, isAttachmentCompatible,
  localizedName, localizedDescription, parseWeaponTraits,
} from '../utils/labels'
import { DamageStateControls } from './ItemDamageControls'
import { PropertyText } from './PropertyText'
import { t } from '../i18n'
import { Icon } from './Icon'
import { FilterChip, SectionCard } from './content/ContentUi'
import { BookReference } from './BookReference'

interface Props {
  sheet: CharacterSheet
  reference: Reference
  onError: (message: string) => void
  refresh: () => Promise<void>
}

/**
 * Правило книги показывается подсказкой, а броска нет (решение владельца): установка выполняется
 * кнопкой, а чем кончилась работа — решает стол.
 */
const INSTALL_HINT = t(
  'По книге установка занимает около часа и требует проверки Механики средней сложности: '
  + 'провал ничего не ставит, отчаяние портит улучшение, а успех с отчаянием даёт нестабильную работу. '
  + 'Приложение бросок не делает — нажатие ставит улучшение, исход решает стол.',
  'By the book, installing takes about an hour and an Average Mechanics check: a failure installs '
  + 'nothing, Despair ruins the attachment, and a success with Despair leaves it unstable. '
  + 'The app rolls nothing — the button installs it and the table decides the outcome.',
)

const ENCHANTMENT_HINT = t(
  'Чары ставит только тот, у кого есть хотя бы один ранг магического навыка. Одного карьерного '
  + 'статуса без рангов недостаточно.',
  'Enchantments require at least one rank in a magic skill. A career skill with no ranks is not enough.',
)

/** Предмет подходит улучшению: общее с именным оружием правило (ROT-EQP-ATT-02, ROT-HA-02). */
const isCompatible = (item: SheetItem, def: AttachmentDef, traits: WeaponFormTrait[]): boolean =>
  isAttachmentCompatible(item.kind, traits, def)

/**
 * Фильтры улучшений. Руны вынесены отдельной корзиной: их ставит только тот, у кого есть ранг
 * магического навыка, они бесценны и покупаются иначе, чем обычные накладки и клинья. Поэтому
 * «Оружие» и «Броня» показывают обычные улучшения без рун, а руны — свой список для обоих видов.
 */
type AttachmentFilter = 'all' | 'weapon' | 'armor' | 'enchantment'
const KIND_FILTERS: AttachmentFilter[] = ['all', 'weapon', 'armor', 'enchantment']

const FILTER_LABELS: Record<AttachmentFilter, string> = {
  all: t('Все', 'All'),
  weapon: ITEM_KIND_LABELS.weapon,
  armor: ITEM_KIND_LABELS.armor,
  enchantment: t('Руны', 'Runes'),
}

/** Улучшение попадает в выбранную корзину. */
const matchesAttachmentFilter = (def: AttachmentDef | undefined, filter: AttachmentFilter): boolean => {
  if (filter === 'all') return true
  if (!def) return false
  if (filter === 'enchantment') return def.isEnchantment
  return def.hostKind === filter && !def.isEnchantment
}

export function AttachmentsTab({ sheet, reference, onError, refresh }: Props) {
  const hosts = sheet.items.filter(i => (i.hardPoints ?? 0) > 0 || i.attachments.length > 0)
  const [hostId, setHostId] = useState<string | null>(() =>
    hosts.find(i => (i.hardPoints ?? 0) > i.usedHardPoints)?.id ?? hosts[0]?.id ?? null)
  const [reason, setReason] = useState('')
  const [mode, setMode] = useState<'reserve' | 'shop'>('reserve')
  const [kindFilter, setKindFilter] = useState<AttachmentFilter>('all')
  const [notice, setNotice] = useState('')
  const [busy, setBusy] = useState(false)
  async function run(action: () => Promise<unknown>) {
    if (busy) return
    setBusy(true)
    try { await action(); await refresh() }
    catch (err) { onError(err instanceof Error ? err.message : t('Ошибка', 'Error')) }
    finally { setBusy(false) }
  }
  const host = hosts.find(i => i.id === hostId) ?? hosts.find(i => (i.hardPoints ?? 0) > i.usedHardPoints) ?? hosts[0] ?? null
  const spare = sheet.attachments.filter(a => a.hostCharacterItemId === null)
  const defsById = useMemo(() => new Map((reference.attachments ?? []).map(d => [d.id, d])), [reference.attachments])
  const allDefs = (reference.attachments ?? []).filter(d => sheet.system === 'realmsOfTerrinoth' || !d.isEnchantment)
  const catalogue = allDefs.filter(d => matchesAttachmentFilter(d, kindFilter))
  const free = host ? Math.max(0, (host.hardPoints ?? 0) - host.usedHardPoints) : 0
  function blocked(a: CharacterAttachment): string {
    if (!host) return t('Сначала выберите предмет', 'Choose an item first')
    const def = defsById.get(a.attachmentDefId)
    if (!def) return t('Определение улучшения недоступно', 'Attachment definition unavailable')
    if (!isCompatible(host, def, parseWeaponTraits(host.formTraits))) {
      if (def.hostKind !== host.kind) return def.hostKind === 'weapon' ? t('Только для оружия', 'Weapons only') : t('Только для брони', 'Armor only')
      if (parseWeaponTraits(def.requiredTraits).includes('ranged')) return t('Только для дальнобойного', 'Ranged weapons only')
      return t('Требуется подходящий профиль оружия', 'Requires a compatible weapon profile')
    }
    if (host.attachments.some(x => x.attachmentDefId === a.attachmentDefId)) return t('Уже установлено', 'Already installed')
    if (free < a.hardPointCost) return free === 0 ? t('Нет свободных слотов', 'No free slots') : t(`Нужно ${a.hardPointCost} сл., свободно ${free}`, `Needs ${a.hardPointCost} slots, ${free} free`)
    if (a.isEnchantment && !hasMagicRank(sheet) && !reason.trim()) return ENCHANTMENT_HINT
    return ''
  }
  const reserve = spare.toSorted((a, b) => Number(!!blocked(a)) - Number(!!blocked(b)))
  async function buy(d: AttachmentDef, free = false) {
    await api.buyAttachment(sheet.id, d.id, free ? { free: true } : undefined)
    setNotice(t(`${localizedName(d)} — в запасе`, `${localizedName(d)} — in reserve`))
  }
  return <div className="attachments-layout sheet-two-column">
    <section>
      <h3 className="sheet-section-title">{t('Предметы со слотами', 'Items with slots')}<small title={INSTALL_HINT + (sheet.system === 'realmsOfTerrinoth' ? ' ' + ENCHANTMENT_HINT : '')}>
        <Icon name="info-circle" className="button-icon" />{t('Как проходит установка', 'How installation works')}</small></h3>
      {hosts.length === 0 && <p className="muted">{t('Нет предметов со слотами улучшений.', 'No items with attachment slots.')}</p>}
      {hosts.map(i => <article key={i.id} className={`attachment-host-card${host?.id === i.id ? ' selected' : ''}`}>
        <button className="attachment-host-select" aria-pressed={host?.id === i.id} onClick={() => { setHostId(i.id); setReason('') }}>
          <Icon name={i.kind === 'weapon' ? 'sword' : 'shield'} /><span><strong>{localizedName(i)}</strong><small>{ITEM_KIND_LABELS[i.kind]}</small></span>
          <span className="attachment-slots" aria-label={t(`Слоты ${i.usedHardPoints}/${i.hardPoints ?? 0}`, `Slots ${i.usedHardPoints}/${i.hardPoints ?? 0}`)}>
            {Array.from({ length: i.hardPoints ?? 0 }, (_, n) => <i key={n} className={n < i.usedHardPoints ? 'filled' : ''} />)}<small>{i.usedHardPoints}/{i.hardPoints ?? 0}</small></span>
        </button>
        {i.attachments.map(a => <AttachmentRow key={a.id} attachment={a} def={defsById.get(a.attachmentDefId)} qualityDefinitions={reference.qualities} funds={sheet.money}
          onDetach={outcome => void run(() => api.detachAttachment(sheet.id, a.id, outcome))}
          onSetDamageState={state => void run(() => api.setAttachmentDamageState(sheet.id, a.id, state))}
          onRepair={opts => void run(() => api.repairAttachment(sheet.id, a.id, opts))} />)}
        {i.attachmentNotes.length > 0 && <ul className="muted small-text attach-notes">{i.attachmentNotes.map((n, idx) => <li key={idx}><PropertyText text={n} qualities={reference.qualities} /></li>)}</ul>}
        {i.overCapacity && <p className="warn-text small-text">{t('Улучшений больше, чем слотов — снимите лишнее', 'More attachments than slots — remove one')}</p>}
        {(i.hardPoints ?? 0) > i.usedHardPoints && <div className="attachment-free-slots">{t(`Свободно слотов: ${(i.hardPoints ?? 0) - i.usedHardPoints}`, `Free slots: ${(i.hardPoints ?? 0) - i.usedHardPoints}`)}
          {host?.id === i.id && t(' — установите из запаса', ' — install from reserve')}</div>}
      </article>)}
    </section>
    <aside className="sheet-sticky"><SectionCard title={t('Улучшения', 'Attachments')} icon="adjustments">
      <div className="sheet-segment" role="group" aria-label={t('Запас и лавка', 'Reserve and shop')}>
        <button aria-pressed={mode === 'reserve'} onClick={() => setMode('reserve')}>{t('Запас', 'Reserve')} {spare.length}</button>
        <button aria-pressed={mode === 'shop'} onClick={() => setMode('shop')}>{t('Лавка', 'Shop')} {allDefs.length}</button>
      </div>
      {mode === 'reserve' ? <>
        <p className="muted small-text">{host ? <>{t('Установка на', 'Installing on')} <b>{localizedName(host)}</b> · {t(`свободно ${free} из ${host.hardPoints ?? 0}`, `${free} of ${host.hardPoints ?? 0} free`)}</> : t('Выберите предмет слева', 'Choose an item on the left')}</p>
        {spare.length === 0 && <div className="rd-empty"><p>{t('Запас пуст', 'Reserve is empty')}</p><button onClick={() => setMode('shop')}>{t('Открыть лавку', 'Open shop')}</button></div>}
        {spare.some(a => a.isEnchantment) && !hasMagicRank(sheet) && <label className="attach-reason small-text">{t('Причина установки чар без магического навыка', 'Reason for enchanting without a magic skill')}<input value={reason} maxLength={200} onChange={e => setReason(e.target.value)} />{ENCHANTMENT_HINT}</label>}
        {reserve.map(a => <AttachmentRow key={a.id} attachment={a} def={defsById.get(a.attachmentDefId)} qualityDefinitions={reference.qualities}
          unavailable={!!blocked(a)}
          onRemove={() => void run(() => api.removeAttachment(sheet.id, a.id))}>
          {blocked(a) ? <p className="muted small-text attachment-blocked"><Icon name="lock" className="button-icon" />{blocked(a)}</p>
            : <button className="primary small" disabled={busy} onClick={() => void run(async () => {
              await api.installAttachment(sheet.id, a.id, host!.id, a.isEnchantment && !hasMagicRank(sheet) ? reason.trim() : undefined); setReason('')
            })}><Icon name="arrow-left" className="button-icon" />{t('Установить', 'Install')}</button>}
        </AttachmentRow>)}
      </> : <>
        <div className="rd-toolbar"><div className="sheet-chips">{KIND_FILTERS.filter(k => sheet.system === 'realmsOfTerrinoth' || k !== 'enchantment').map(k => <FilterChip key={k} active={kindFilter === k} onClick={() => setKindFilter(k)}>{FILTER_LABELS[k]} {allDefs.filter(d => matchesAttachmentFilter(d, k)).length}</FilterChip>)}</div>
          <span className="sheet-wallet"><Icon name="coin" />{sheet.money}</span></div>
        {notice && <p role="status" className="success-text small-text">{notice}</p>}
        {catalogue.map(d => <article key={d.id} className="sheet-catalog-card"><div className="sheet-card-heading"><strong>{localizedName(d)}</strong><b>{d.price === null ? t('выдаёт ведущий', 'GM grants') : t(`${d.price} зол.`, `${d.price} coins`)}</b></div>
          <p className="muted small-text">{ITEM_KIND_LABELS[d.hostKind]} · {t(`${d.hardPointCost} сл. · редкость ${d.rarity}`, `${d.hardPointCost} slots · rarity ${d.rarity}`)}</p>
          <div className="muted small-text"><PropertyText text={localizedDescription(d)} qualities={reference.qualities} /></div><BookReference source={d.source} />
          <div className="form-actions"><button className="primary small" disabled={busy || d.price === null || d.price > sheet.money}
            title={d.price === null ? t('Цену назначает ведущий', 'The GM sets the price') : d.price > sheet.money ? t('Недостаточно монет', 'Not enough coins') : undefined}
            onClick={() => void run(() => buy(d))}>{t('Купить', 'Buy')}</button><button className="small" disabled={busy} onClick={() => void run(() => buy(d, true))}>{d.price === null ? t('+ Выдать', '+ Grant') : t('+ Без оплаты', '+ Free')}</button></div>
        </article>)}
      </>}
    </SectionCard></aside>
  </div>
}

/** Ранг магического навыка у персонажа: карьерный статус без рангов чары не разрешает. */
function hasMagicRank(sheet: CharacterSheet): boolean {
  return sheet.skills.some(s => s.kind === 'magic' && s.ranks > 0)
}

function AttachmentRow({ attachment, def, qualityDefinitions, funds, onDetach, onRemove, onSetDamageState, onRepair, children, unavailable = false }: {
  children?: ReactNode
  unavailable?: boolean
  attachment: CharacterAttachment
  def?: AttachmentDef
  qualityDefinitions?: Reference['qualities']
  /** Чем персонаж может заплатить за материалы ремонта. */
  funds?: number
  onDetach?: (outcome: 'returned' | 'destroyed' | 'unusable') => void
  onRemove?: () => void
  onSetDamageState?: (state: ItemDamageState) => void
  onRepair?: (opts: { netAdvantages: number } | { costOverride: number; overrideReason: string }) => void
}) {
  return (
    <div className={`attach-row${unavailable ? ' unavailable' : ''}`}>
      <div>
        <strong>{localizedName(attachment)}</strong>
        {attachment.damageState !== 'undamaged' && (
          <span className={`chip damage-badge ${attachment.damageState}`}
            title={ITEM_DAMAGE_STATE_HINTS[attachment.damageState]}>
            {ITEM_DAMAGE_STATE_LABELS[attachment.damageState]}
          </span>
        )}
        <span className="muted small-text">
          {def && <> · {ITEM_KIND_LABELS[def.hostKind]}</>}
          {' · '}{t('слотов', 'slots')} {attachment.hardPointCost}
          {attachment.isEnchantment && t(' · чары', ' · enchantment')}
          {attachment.price === null
            ? t(' · бесценно', ' · priceless')
            : <> · {attachment.price} <Icon name="coin" className="button-icon" /></>}
        </span>
        {def?.description && <div className="muted small-text">
          <PropertyText text={localizedDescription(def)} qualities={qualityDefinitions} />
        </div>}
        {def?.source && <BookReference source={def.source} />}
        {attachment.damageState !== 'undamaged' && <p className="warn-text small-text">
          <Icon name="alert-triangle" className="button-icon" />{ITEM_DAMAGE_STATE_HINTS[attachment.damageState]}
        </p>}
        {children}
        {attachment.note && <div className="muted small-text">
          <PropertyText text={attachment.note} qualities={qualityDefinitions} />
        </div>}
        {/* Сломанное улучшение молчит, но слот не отдаёт: об этом надо сказать прямо, иначе
            непонятно, почему свободных слотов не прибавилось (GEN-EQP-DMG-01). */}
        {!attachment.isUsable && (
          <div className="damage-warn small-text">
            {t('Эффекты не действуют; слот освободится только после снятия.',
              'Effects are off; the slot frees up only after detaching.')}
          </div>
        )}
        {onSetDamageState && onRepair && (
          <DamageStateControls state={attachment.damageState} repair={attachment.repair}
            showHint={false}
            funds={funds ?? 0}
            onSetState={onSetDamageState} onRepair={onRepair} />
        )}
      </div>
      <div className="attach-row-actions">
        {onDetach && (
          <>
            <button className="small" onClick={() => onDetach('returned')}>{t('Снять', 'Detach')}</button>
            <button className="small" title={t('Испорчено при снятии', 'Ruined while detaching')} aria-label={t('Испорчено при снятии', 'Ruined while detaching')}
              onClick={() => onDetach('destroyed')}><Icon name="trash" className="button-icon" /></button>
          </>
        )}
        {onRemove && (
          <button className="danger small" title={t('Убрать из запаса', 'Remove from reserve')}
            aria-label={t('Убрать из запаса', 'Remove from reserve')}
            onClick={onRemove}><Icon name="close" className="button-icon" /></button>
        )}
      </div>
    </div>
  )
}
