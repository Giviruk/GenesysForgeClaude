import { useEffect, useState } from 'react'
import { api } from '../api/client'
import type { CampaignHomebrewPack, CustomContentChange } from '../api/types'
import { lang, t } from '../i18n'

const FIELD_LABELS: Record<string, string> = {
  name: t('Название', 'Name'), nameRu: t('Русское название', 'Russian name'),
  description: t('Описание', 'Description'), safeDescription: t('Краткое описание', 'Short description'),
  source: t('Источник', 'Source'), characteristic: t('Характеристика', 'Characteristic'),
  kind: t('Тип', 'Type'), tier: t('Тир', 'Tier'), isRanked: t('Ранговый', 'Ranked'),
  category: t('Категория', 'Category'), activation: t('Активация', 'Activation'),
  woundBonus: t('Бонус ран', 'Wound bonus'), strainBonus: t('Бонус усталости', 'Strain bonus'),
  soakBonus: t('Поглощение', 'Soak'), meleeDefenseBonus: t('Ближняя защита', 'Melee defense'),
  rangedDefenseBonus: t('Дальняя защита', 'Ranged defense'),
  brawn: t('Мощь', 'Brawn'), agility: t('Ловкость', 'Agility'), intellect: t('Интеллект', 'Intellect'),
  cunning: t('Хитрость', 'Cunning'), willpower: t('Воля', 'Willpower'), presence: t('Харизма', 'Presence'),
  woundBase: t('Базовый порог ран', 'Base wounds'), strainBase: t('Базовый порог усталости', 'Base strain'),
  startingXp: t('Стартовый XP', 'Starting XP'), abilities: t('Способности', 'Abilities'),
  startingSkills: t('Стартовые навыки', 'Starting skills'), careerSkillNames: t('Карьерные навыки', 'Career skills'),
  startingMoneyFixed: t('Стартовые деньги', 'Starting money'), startingMoneyDice: t('Бросок стартовых денег', 'Starting money roll'),
  startingGear: t('Стартовое снаряжение', 'Starting gear'), rules: t('Правила', 'Rules'),
  encumbrance: t('Вес', 'Encumbrance'), meleeDefense: t('Ближняя защита', 'Melee defense'),
  rangedDefense: t('Дальняя защита', 'Ranged defense'), encumbranceThresholdBonus: t('Бонус переносимого веса', 'Encumbrance threshold bonus'),
  price: t('Цена', 'Price'), rarity: t('Редкость', 'Rarity'), skillName: t('Навык', 'Skill'),
  damage: t('Урон', 'Damage'), crit: t('Критическое значение', 'Critical rating'), rangeBand: t('Дистанция', 'Range'),
  properties: t('Свойства', 'Properties'), upgrades: t('Улучшения', 'Upgrades'), effects: t('Эффекты', 'Effects'),
  hardPoints: t('Свободные ячейки', 'Hard points'), attackProfiles: t('Профили атаки', 'Attack profiles'),
}

function readableValue(value: unknown): string {
  if (value === null || value === undefined || value === '') return '—'
  if (typeof value === 'boolean') return value ? t('да', 'yes') : t('нет', 'no')
  if (Array.isArray(value)) return value.length ? value.map(readableValue).join('\n') : '—'
  if (typeof value === 'object') return Object.entries(value).map(([key, item]) =>
    `${FIELD_LABELS[key] ?? key}: ${readableValue(item)}`).join('; ')
  return String(value)
}

function changeValue(raw: string | null): string {
  if (raw === null) return '—'
  try { return readableValue(JSON.parse(raw)) } catch { return raw }
}

export function CustomEditedDate({ at }: { at?: string | null }) {
  if (!at) return null
  return <span className="muted small-text">{
    t(`изменено ${new Date(at).toLocaleDateString('ru-RU')}`, `edited ${new Date(at).toLocaleDateString('en-US')}`)
  }</span>
}

export function PackChangeStatus({ pack }: { pack: CampaignHomebrewPack }) {
  return <span className="content-pack-dates">
    <CustomEditedDate at={pack.lastChangedAt} />
    {pack.changedAfterConnection && <span className="badge warn">{t('изменён после подключения', 'changed after connection')}</span>}
  </span>
}

export function HomebrewPackHistory({ packId, name, campaignId, onClose }: {
  packId: string; name: string; campaignId?: string; onClose: () => void
}) {
  const [rows, setRows] = useState<CustomContentChange[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  useEffect(() => {
    let cancelled = false
    api.homebrewPackChanges(packId, campaignId, 200)
      .then(data => { if (!cancelled) setRows(data) })
      .catch(err => { if (!cancelled) setError(err instanceof Error ? err.message : t('Ошибка', 'Error')) })
    return () => { cancelled = true }
  }, [packId, campaignId])
  const actionLabels = { created: t('Создано', 'Created'), updated: t('Изменено', 'Updated'), deleted: t('Удалено', 'Deleted') }
  return <div className="modal-backdrop" role="presentation" onClick={onClose}>
    <section className="modal wide content-history" role="dialog" aria-modal="true"
      aria-label={t('История набора', 'Pack history')} onClick={event => event.stopPropagation()}>
      <div className="label-line"><h3>{t('История', 'History')}: {name}</h3>
        <button onClick={onClose}>{t('Закрыть', 'Close')}</button></div>
      {error && <p className="error" role="alert">{error}</p>}
      {!rows && !error && <p>{t('Загрузка истории…', 'Loading history…')}</p>}
      {rows?.length === 0 && <p className="muted">{t('Изменений пока нет. Старые записи и импорт не имеют истории.', 'No changes yet. Earlier entries and imports have no history.')}</p>}
      {rows?.map(row => <article key={row.id} className="content-history-entry">
        <div className="label-line"><strong>{actionLabels[row.action]} · {row.definitionName}</strong>
          <time dateTime={row.createdAt}>{new Date(row.createdAt).toLocaleString(lang === 'ru' ? 'ru-RU' : 'en-US')}</time></div>
        <p className="muted small-text">{t('Автор', 'Author')}: {row.userName}</p>
        {row.changes.length > 0 && <table className="content-history-diff">
          <thead><tr><th>{t('Поле', 'Field')}</th><th>{t('Было', 'Before')}</th><th>{t('Стало', 'After')}</th></tr></thead>
          <tbody>{row.changes.map(change => <tr key={change.field}>
            <th>{FIELD_LABELS[change.field] ?? change.field}</th>
            <td>{changeValue(change.from)}</td><td>{changeValue(change.to)}</td>
          </tr>)}</tbody>
        </table>}
      </article>)}
    </section>
  </div>
}

export function CampaignPackHistoryPanel({ campaignId, refreshSignal = 0 }: { campaignId: string; refreshSignal?: number }) {
  const [packs, setPacks] = useState<CampaignHomebrewPack[]>([])
  const [selected, setSelected] = useState<CampaignHomebrewPack | null>(null)
  const [error, setError] = useState<string | null>(null)
  useEffect(() => {
    let cancelled = false
    api.campaignHomebrewPacks(campaignId).then(data => { if (!cancelled) setPacks(data) })
      .catch(err => { if (!cancelled) setError(err instanceof Error ? err.message : t('Ошибка', 'Error')) })
    return () => { cancelled = true }
  }, [campaignId, refreshSignal])
  return <section className="panel">
    <h3>{t('Наборы кампании', 'Campaign packs')}</h3>
    {error && <p className="error" role="alert">{error}</p>}
    {packs.map(pack => <div key={pack.id} className="custom-list-row">
      <div><strong>{pack.name}</strong><span> · {pack.ownerName}</span>
        {!pack.ownerIsMember && <span className="muted">{t(' · игрок покинул кампанию', ' · player left the campaign')}</span>}
        {!pack.isEnabled && <span className="badge">{t('отключён', 'disabled')}</span>}
        <PackChangeStatus pack={pack} /></div>
      <button type="button" className="small" onClick={() => setSelected(pack)}>{t('История', 'History')}</button>
    </div>)}
    {selected && <HomebrewPackHistory key={selected.id} packId={selected.id} name={selected.name} campaignId={campaignId} onClose={() => setSelected(null)} />}
  </section>
}
