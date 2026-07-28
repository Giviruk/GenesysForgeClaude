import { useMemo, useState } from 'react'
import { api } from '../api/client'
import type {
  AttachmentDef, CharacterAttachment, CharacterSheet, ItemDamageState, Reference, SheetItem,
  WeaponFormTrait,
} from '../api/types'
import {
  ITEM_DAMAGE_STATE_HINTS, ITEM_DAMAGE_STATE_LABELS, ITEM_KIND_LABELS, localizedName, parseWeaponTraits,
} from '../utils/labels'
import { DamageStateControls } from './ItemDamageControls'
import { t } from '../i18n'

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

/**
 * Предмет подходит улучшению по виду и признакам формы — то же правило, что и на сервере.
 * Здесь оно повторено только чтобы не предлагать заведомо невозможный выбор; решает сервер.
 */
function isCompatible(item: SheetItem, def: AttachmentDef, traits: WeaponFormTrait[]): boolean {
  if (item.kind !== def.hostKind) return false
  const has = (trait: WeaponFormTrait) => traits.includes(trait)
  const required = parseWeaponTraits(def.requiredTraits)
  const requiredAny = parseWeaponTraits(def.requiredAnyTraits)
  const forbidden = parseWeaponTraits(def.forbiddenTraits)
  if (!required.every(has)) return false
  if (requiredAny.length > 0 && !requiredAny.some(has)) return false
  return !forbidden.some(has)
}

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

/**
 * Сворачиваемый раздел вкладки. Каталог из двадцати одной записи выталкивал установленное далеко
 * вниз, поэтому разделы закрываются, а счётчик в заголовке показывает, сколько внутри — иначе
 * закрытый раздел выглядит пустым.
 */
function Section({ id, title, count, defaultOpen = true, children }: {
  /** Стабильный ключ раздела: по нему его находят стили и тесты. */
  id: string
  title: string
  count?: number
  defaultOpen?: boolean
  children: React.ReactNode
}) {
  const [open, setOpen] = useState(defaultOpen)
  return (
    <section className="attach-list" data-section={id}>
      <h4>
        <button type="button" className="section-toggle" aria-expanded={open}
          onClick={() => setOpen(o => !o)}>
          <span className="section-caret" aria-hidden>{open ? '▾' : '▸'}</span>
          {title}
          {count != null && <span className="muted section-count">{count}</span>}
        </button>
      </h4>
      {open && children}
    </section>
  )
}

export function AttachmentsTab({ sheet, reference, onError, refresh }: Props) {
  const [hostId, setHostId] = useState<string | null>(null)
  const [attachmentId, setAttachmentId] = useState<string | null>(null)
  const [reason, setReason] = useState('')
  const [kindFilter, setKindFilter] = useState<AttachmentFilter>('all')

  async function run(action: () => Promise<unknown>) {
    try {
      await action()
      await refresh()
    } catch (err) {
      onError(err instanceof Error ? err.message : t('Ошибка', 'Error'))
    }
  }

  // Чем можно заплатить за материалы ремонта: в фазе создания сначала тратится бюджет покупок.
  const funds = sheet.money + (sheet.isCreationPhase ? sheet.startingPurchaseBudget : 0)

  // Счётчик в заголовке закрытого раздела: сколько улучшений стоит на предметах.
  const installedCount = useMemo(
    () => sheet.items.reduce((sum, i) => sum + i.attachments.length, 0),
    [sheet.items])

  // Улучшать можно только оружие и броню: у снаряжения слотов не бывает.
  const hosts = useMemo(
    () => sheet.items.filter(i => i.kind === 'weapon' || i.kind === 'armor'),
    [sheet.items])
  const host = hosts.find(i => i.id === hostId) ?? null

  const spare = useMemo(
    () => sheet.attachments.filter(a => a.hostCharacterItemId === null),
    [sheet.attachments])

  const defsById = useMemo(() => {
    const map = new Map<string, AttachmentDef>()
    for (const d of reference.attachments ?? []) map.set(d.id, d)
    return map
  }, [reference.attachments])

  const matchesFilter = (def?: AttachmentDef) => matchesAttachmentFilter(def, kindFilter)
  // Фильтр по виду носителя касается только списков; выбор для установки и так ограничен
  // совместимостью выбранного предмета.
  const spareShown = useMemo(
    () => spare.filter(a => matchesFilter(defsById.get(a.attachmentDefId))),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [spare, defsById, kindFilter])
  const catalogue = useMemo(
    () => (reference.attachments ?? []).filter(d => matchesFilter(d)),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [reference.attachments, kindFilter])

  // Совместимость считается по признакам формы выбранного предмета; сервер проверит ещё раз.
  const hostTraits = useMemo(() => parseWeaponTraits(host?.formTraits), [host])
  const usable = useMemo(() => spare.filter(a => {
    const def = defsById.get(a.attachmentDefId)
    if (!def || !host) return false
    if (host.attachments.some(x => x.attachmentDefId === a.attachmentDefId)) return false
    return isCompatible(host, def, hostTraits)
  }), [spare, defsById, host, hostTraits])

  const chosen = usable.find(a => a.id === attachmentId) ?? null
  const free = host ? Math.max(0, (host.hardPoints ?? 0) - host.usedHardPoints) : 0
  const needsReason = chosen?.isEnchantment === true && !hasMagicRank(sheet)
  const canApply = host !== null && chosen !== null && free >= chosen.hardPointCost
    && (!needsReason || reason.trim() !== '')

  return (
    <div className="panel">
      <h3>{t('Улучшения', 'Attachments')}</h3>
      <p className="hint">{INSTALL_HINT}</p>

      <div className="attach-picker">
        <label>
          {t('Предмет', 'Item')}
          <select value={hostId ?? ''} onChange={e => { setHostId(e.target.value || null); setAttachmentId(null) }}>
            <option value="">{t('— выберите предмет —', '— choose an item —')}</option>
            {hosts.map(i => (
              <option key={i.id} value={i.id}>
                {localizedName(i)} · {ITEM_KIND_LABELS[i.kind]} · {t('слоты', 'slots')} {i.usedHardPoints}/{i.hardPoints ?? 0}
              </option>
            ))}
          </select>
        </label>

        <label>
          {t('Улучшение', 'Attachment')}
          <select value={attachmentId ?? ''} disabled={!host}
            onChange={e => setAttachmentId(e.target.value || null)}>
            <option value="">{t('— выберите улучшение —', '— choose an attachment —')}</option>
            {usable.map(a => (
              <option key={a.id} value={a.id}>
                {a.nameRu || a.name} · {t('слотов', 'slots')} {a.hardPointCost}
                {a.isEnchantment ? t(' · чары', ' · enchantment') : ''}
              </option>
            ))}
          </select>
        </label>

        <button className="primary" disabled={!canApply}
          title={!host ? t('Выберите предмет', 'Choose an item')
            : !chosen ? t('Выберите улучшение', 'Choose an attachment')
              : free < chosen.hardPointCost ? t('Не хватает слотов', 'Not enough slots')
                : needsReason && reason.trim() === ''
                  ? t('Нужна причина: у персонажа нет ранга магического навыка',
                    'A reason is required: the character has no magic skill rank')
                  : undefined}
          onClick={() => run(async () => {
            await api.installAttachment(sheet.id, chosen!.id, host!.id,
              needsReason ? reason.trim() : undefined)
            setAttachmentId(null)
            setReason('')
          })}>
          {t('Применить', 'Apply')}
        </button>
      </div>

      {host && (
        <p className="muted small-text">
          {t('Свободных слотов', 'Free slots')}: <strong>{free}</strong> {t('из', 'of')} {host.hardPoints ?? 0}
          {host.overCapacity && (
            <span className="error">
              {' · '}{t('улучшений больше, чем слотов — снимите лишнее',
                'more attachments than slots — remove one')}
            </span>
          )}
        </p>
      )}

      {needsReason && (
        <label className="small-text attach-reason">
          {t('Причина установки чар без магического навыка', 'Reason for enchanting without a magic skill')}
          <input value={reason} maxLength={200} onChange={e => setReason(e.target.value)}
            placeholder={t('например, помог городской чародей', 'e.g. the town wizard helped')} />
          <span className="muted"> {ENCHANTMENT_HINT}</span>
        </label>
      )}

      {spare.length === 0 && (
        <p className="muted">
          {t('В запасе нет улучшений — купите их в списке ниже.',
            'No attachments in reserve — buy one from the list below.')}
        </p>
      )}

      <div className="attach-filters">
        {KIND_FILTERS.map(k => (
          <button key={k} className={kindFilter === k ? 'chip active' : 'chip'}
            onClick={() => setKindFilter(k)}>
            {FILTER_LABELS[k]}
          </button>
        ))}
      </div>

      {/* Установленное — первым: это то, что у персонажа есть сейчас, и ради него сюда заходят.
          Запас и магазин лежат ниже и закрываются, чтобы каталог не выталкивал их за экран. */}
      <Section id="installed" title={t('Установленные', 'Installed')} count={installedCount}>
        {installedCount === 0 && (
          <p className="muted">{t('Пока ничего не установлено.', 'Nothing installed yet.')}</p>
        )}
        {hosts.filter(i => i.attachments.length > 0).map(i => (
          <div key={i.id} className="attach-host">
            <strong>{localizedName(i)}</strong>
            <span className="muted small-text">
              {' · '}{t('слоты', 'slots')} {i.usedHardPoints}/{i.hardPoints ?? 0}
            </span>
            {i.attachments.map(a => (
              <AttachmentRow key={a.id} attachment={a} def={defsById.get(a.attachmentDefId)}
                funds={funds}
                onDetach={outcome => run(() => api.detachAttachment(sheet.id, a.id, outcome))}
                onSetDamageState={state =>
                  run(() => api.setAttachmentDamageState(sheet.id, a.id, state))}
                onRepair={opts => run(() => api.repairAttachment(sheet.id, a.id, opts))} />
            ))}
            {i.attachmentNotes.length > 0 && (
              <ul className="muted small-text attach-notes">
                {i.attachmentNotes.map((n, idx) => <li key={idx}>{n}</li>)}
              </ul>
            )}
          </div>
        ))}
      </Section>

      {spare.length > 0 && (
        <Section id="reserve" title={t('В запасе', 'In reserve')} count={spareShown.length}>
          {spareShown.length === 0 && (
            <p className="muted small-text">{t('По фильтру ничего нет.', 'Nothing matches the filter.')}</p>
          )}
          {spareShown.map(a => (
            <AttachmentRow key={a.id} attachment={a} def={defsById.get(a.attachmentDefId)}
              onRemove={() => run(() => api.removeAttachment(sheet.id, a.id))} />
          ))}
        </Section>
      )}

      <Section id="shop" title={t('Купить улучшение', 'Buy an attachment')} count={catalogue.length}
        defaultOpen={false}>
        <p className="hint small-text">
          {t('Цену считает сервер. Бесценные улучшения обычной покупкой не берутся — их выдаёт ведущий.',
            'The server computes the price. Priceless attachments cannot be bought — the GM grants them.')}
        </p>
        {catalogue.map(d => (
          <div key={d.id} className="attach-row">
            <div>
              <strong>{d.nameRu || d.name}</strong>
              <span className="muted small-text">
                {' · '}{ITEM_KIND_LABELS[d.hostKind]} · {t('слотов', 'slots')} {d.hardPointCost}
                {' · '}{t('редкость', 'rarity')} {d.rarity}
                {d.price === null ? t(' · бесценно', ' · priceless') : ` · ${d.price} 🪙`}
                {d.isEnchantment && t(' · чары', ' · enchantment')}
              </span>
              {d.description && <div className="muted small-text">{d.description}</div>}
            </div>
            <div className="attach-row-actions">
              <button className="primary small"
                disabled={d.price === null || d.price > sheet.money}
                title={d.price === null
                  ? t('Цену назначает ведущий', 'The GM sets the price')
                  : d.price > sheet.money ? t('Недостаточно монет', 'Not enough coins') : undefined}
                onClick={() => run(() => api.buyAttachment(sheet.id, d.id))}>
                {t('Купить', 'Buy')}
              </button>
              <button className="small" title={t('Добавить без оплаты', 'Add without paying')}
                onClick={() => run(() => api.buyAttachment(sheet.id, d.id, { free: true }))}>
                {t('+ Добавить', '+ Add')}
              </button>
            </div>
          </div>
        ))}
      </Section>
    </div>
  )
}

/** Ранг магического навыка у персонажа: карьерный статус без рангов чары не разрешает. */
function hasMagicRank(sheet: CharacterSheet): boolean {
  return sheet.skills.some(s => s.kind === 'magic' && s.ranks > 0)
}

function AttachmentRow({ attachment, def, funds, onDetach, onRemove, onSetDamageState, onRepair }: {
  attachment: CharacterAttachment
  def?: AttachmentDef
  /** Чем персонаж может заплатить за материалы ремонта. */
  funds?: number
  onDetach?: (outcome: 'returned' | 'destroyed' | 'unusable') => void
  onRemove?: () => void
  onSetDamageState?: (state: ItemDamageState) => void
  onRepair?: (opts: { netAdvantages: number } | { costOverride: number; overrideReason: string }) => void
}) {
  return (
    <div className="attach-row">
      <div>
        <strong>{attachment.nameRu || attachment.name}</strong>
        {attachment.damageState !== 'undamaged' && (
          <span className={`chip damage-badge ${attachment.damageState}`}
            title={ITEM_DAMAGE_STATE_HINTS[attachment.damageState]}>
            {ITEM_DAMAGE_STATE_LABELS[attachment.damageState]}
          </span>
        )}
        <span className="muted small-text">
          {' · '}{t('слотов', 'slots')} {attachment.hardPointCost}
          {attachment.isEnchantment && t(' · чары', ' · enchantment')}
          {attachment.price === null
            ? t(' · бесценно', ' · priceless')
            : ` · ${attachment.price} 🪙`}
        </span>
        {def?.description && <div className="muted small-text">{def.description}</div>}
        {attachment.note && <div className="muted small-text">{attachment.note}</div>}
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
            funds={funds ?? 0}
            onSetState={onSetDamageState} onRepair={onRepair} />
        )}
      </div>
      <div className="attach-row-actions">
        {onDetach && (
          <>
            <button className="small" onClick={() => onDetach('returned')}>{t('Снять', 'Detach')}</button>
            <button className="small" title={t('Улучшение испорчено при снятии', 'Ruined while detaching')}
              onClick={() => onDetach('destroyed')}>{t('Сломать', 'Destroy')}</button>
          </>
        )}
        {onRemove && (
          <button className="danger small" title={t('Убрать из запаса', 'Remove from reserve')}
            onClick={onRemove}>✕</button>
        )}
      </div>
    </div>
  )
}
