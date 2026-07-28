import { useMemo, useState } from 'react'
import { api } from '../api/client'
import type {
  AttachmentDef, CharacterAttachment, CharacterSheet, Reference, SheetItem, WeaponFormTrait,
} from '../api/types'
import { ITEM_KIND_LABELS, localizedName, parseWeaponTraits } from '../utils/labels'
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

export function AttachmentsTab({ sheet, reference, onError, refresh }: Props) {
  const [hostId, setHostId] = useState<string | null>(null)
  const [attachmentId, setAttachmentId] = useState<string | null>(null)
  const [reason, setReason] = useState('')

  async function run(action: () => Promise<unknown>) {
    try {
      await action()
      await refresh()
    } catch (err) {
      onError(err instanceof Error ? err.message : t('Ошибка', 'Error'))
    }
  }

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

      {spare.length > 0 && (
        <section className="attach-list">
          <h4>{t('В запасе', 'In reserve')}</h4>
          {spare.map(a => (
            <AttachmentRow key={a.id} attachment={a} def={defsById.get(a.attachmentDefId)}
              onRemove={() => run(() => api.removeAttachment(sheet.id, a.id))} />
          ))}
        </section>
      )}

      <section className="attach-list">
        <h4>{t('Купить улучшение', 'Buy an attachment')}</h4>
        <p className="hint small-text">
          {t('Цену считает сервер. Бесценные улучшения обычной покупкой не берутся — их выдаёт ведущий.',
            'The server computes the price. Priceless attachments cannot be bought — the GM grants them.')}
        </p>
        {(reference.attachments ?? []).map(d => (
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
      </section>

      <section className="attach-list">
        <h4>{t('Установленные', 'Installed')}</h4>
        {hosts.every(i => i.attachments.length === 0) && (
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
                onDetach={outcome => run(() => api.detachAttachment(sheet.id, a.id, outcome))} />
            ))}
            {i.attachmentNotes.length > 0 && (
              <ul className="muted small-text attach-notes">
                {i.attachmentNotes.map((n, idx) => <li key={idx}>{n}</li>)}
              </ul>
            )}
          </div>
        ))}
      </section>
    </div>
  )
}

/** Ранг магического навыка у персонажа: карьерный статус без рангов чары не разрешает. */
function hasMagicRank(sheet: CharacterSheet): boolean {
  return sheet.skills.some(s => s.kind === 'magic' && s.ranks > 0)
}

function AttachmentRow({ attachment, def, onDetach, onRemove }: {
  attachment: CharacterAttachment
  def?: AttachmentDef
  onDetach?: (outcome: 'returned' | 'destroyed' | 'unusable') => void
  onRemove?: () => void
}) {
  return (
    <div className="attach-row">
      <div>
        <strong>{attachment.nameRu || attachment.name}</strong>
        <span className="muted small-text">
          {' · '}{t('слотов', 'slots')} {attachment.hardPointCost}
          {attachment.isEnchantment && t(' · чары', ' · enchantment')}
          {attachment.price === null
            ? t(' · бесценно', ' · priceless')
            : ` · ${attachment.price} 🪙`}
        </span>
        {def?.description && <div className="muted small-text">{def.description}</div>}
        {attachment.note && <div className="muted small-text">{attachment.note}</div>}
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
