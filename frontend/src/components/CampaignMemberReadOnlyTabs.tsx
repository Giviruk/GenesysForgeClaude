import type { CharacterAttachment, CharacterSheet, SheetItem, SheetTalent } from '../api/types'
import { lang, t } from '../i18n'
import {
  ITEM_DAMAGE_STATE_LABELS, ITEM_KIND_LABELS, ITEM_STATE_LABELS, localizedDescription,
  localizedName, MOVEMENT_MODE_LABELS, secondaryName, TRANSPORT_KIND_LABELS,
} from '../utils/labels'
import { PropertyTags } from './PropertyTags'
import { RuleText } from './RuleText'

export function ReadOnlyTalentsTab({ sheet }: { sheet: CharacterSheet }) {
  const talents = sheet.talents.toSorted((a, b) => a.tier - b.tier || localizedName(a).localeCompare(localizedName(b), lang))
  return <section className="panel">
    <h3>{t('Таланты персонажа', 'Character talents')}</h3>
    {talents.length === 0 && <p className="muted">{t('Талантов пока нет.', 'No talents yet.')}</p>}
    <div className="talent-list">
      {talents.map(talent => <ReadOnlyTalent key={talent.talentDefId} talent={talent} />)}
    </div>
  </section>
}

function ReadOnlyTalent({ talent }: { talent: SheetTalent }) {
  return <div className="talent-row">
    <div className="talent-info">
      <strong>{localizedName(talent)}{secondaryName(talent) && <span className="muted small-text"> · {secondaryName(talent)}</span>}</strong>
      <div className="tag-row compact">
        <span className="badge tier">{t('Тир', 'Tier')} {talent.tier}</span>
        {talent.isRanked && <span className="badge">{t('Ранги', 'Ranks')}: {talent.ranks}</span>}
        {talent.activation && <span className="badge">{talent.activation}</span>}
        {talent.needsChoice && <span className="badge warn">{t('Нужен выбор', 'Choice required')}</span>}
      </div>
      {localizedDescription(talent) && <p className="muted"><RuleText text={localizedDescription(talent)} /></p>}
      {(talent.choices ?? []).length > 0 && <div className="small-text">
        {t('Выборы:', 'Choices:')} {talent.choices.map(choice => choice.displayName).join(' · ')}
      </div>}
    </div>
  </div>
}

export function ReadOnlyInventoryTab({ sheet }: { sheet: CharacterSheet }) {
  const groups = (['equipped', 'carried', 'backpack'] as const)
    .map(state => ({ state, items: sheet.items.filter(item => item.state === state) }))
    .filter(group => group.items.length > 0)
  return <div>
    <section className="panel readonly-character-summary">
      <span>{t('Деньги', 'Money')}: <b>{sheet.money}</b></span>
      <span>{t('Нагрузка', 'Encumbrance')}: <b>{sheet.derived.encumbranceLoad}/{sheet.derived.encumbranceThreshold}</b></span>
    </section>
    <section className="panel">
      <h3>{t('Инвентарь', 'Inventory')}</h3>
      {groups.length === 0 && <p className="muted">{t('Инвентарь пуст.', 'The inventory is empty.')}</p>}
      {groups.map(group => <div key={group.state} className="readonly-item-group">
        <h4>{ITEM_STATE_LABELS[group.state]}</h4>
        <div className="inv-items">{group.items.map(item => <ReadOnlyItem key={item.id} item={item} />)}</div>
      </div>)}
    </section>
  </div>
}

function ReadOnlyItem({ item }: { item: SheetItem }) {
  const profile = item.attackProfiles?.find(value => value.isDefault) ?? item.attackProfiles?.[0]
  return <div className={`inv-card${item.isUsable ? '' : ' broken'}`}>
    <div className="inv-card-head">
      <div className="inv-card-title">
        <strong>{localizedName(item)}</strong>
        {secondaryName(item) && <span className="muted small-text"> · {secondaryName(item)}</span>}
        <span className="muted small-text"> · {ITEM_KIND_LABELS[item.kind]} · ×{item.quantity}</span>
      </div>
      {item.damageState !== 'undamaged' && <span className={`chip damage-badge ${item.damageState}`}>
        {ITEM_DAMAGE_STATE_LABELS[item.damageState]}
      </span>}
    </div>
    {item.kind === 'weapon' && <div className="weapon-line">
      <span className="weapon-stat">{t('Урон', 'Damage')} <b>{profile?.baseDamage ?? (item.damage || '—')}</b></span>
      <span className="weapon-stat">{t('Крит', 'Crit')} <b>{profile?.crit ?? (item.crit || '—')}</b></span>
      <span className="weapon-stat">{profile?.range ?? item.rangeBand}</span>
    </div>}
    {item.kind === 'armor' && <div className="muted small-text">
      {t('Поглощение', 'Soak')} +{item.soakBonus} · {t('Защита', 'Defense')} {item.meleeDefense}/{item.rangedDefense}
    </div>}
    {(item.properties || item.reinforced) && <PropertyTags
      properties={[item.properties, item.reinforced ? 'Reinforced' : ''].filter(Boolean).join(', ')}
      className="small-text" />}
    {item.attachments.length > 0 && <div className="muted small-text">
      {t('Улучшения', 'Attachments')}: {item.attachments.map(value => localizedName(value)).join(' · ')}
    </div>}
    {localizedDescription(item) && <div className="inv-card-desc">{localizedDescription(item)}</div>}
    <div className="muted small-text">{t('Вес', 'Load')}: {item.load}</div>
  </div>
}

export function ReadOnlyHeroicTab({ sheet }: { sheet: CharacterSheet }) {
  const ability = sheet.heroicAbility
  return <section className="panel">
    <h3>{t('Героическая способность', 'Heroic ability')}</h3>
    {!ability && <p className="muted">{t('Героическая способность не выбрана.', 'No heroic ability selected.')}</p>}
    {ability && <div className="sheet-entry">
      <strong>{sheet.heroicIdentity?.customName || localizedName(ability)}</strong>
      {sheet.heroicIdentity?.customName && <span className="muted small-text"> · {localizedName(ability)}</span>}
      <div className="tag-row compact">
        <span className="badge">{ability.activation}</span>
        <span className="badge">{ability.frequency}</span>
        <span className="badge">{ability.duration}</span>
      </div>
      {localizedDescription(ability) && <p>{localizedDescription(ability)}</p>}
      <div className="muted small-text">
        {t('Очки улучшений', 'Upgrade points')}: {sheet.heroicUpgradePointsSpent}/{sheet.heroicUpgradePointsTotal}
        {sheet.heroicUpgrades.powerRank > 0 && ` · ${t('Сила', 'Power')} ${sheet.heroicUpgrades.powerRank}`}
        {sheet.heroicUpgrades.durationRanks > 0 && ` · ${t('Длительность', 'Duration')} ${sheet.heroicUpgrades.durationRanks}`}
        {sheet.heroicUpgrades.frequencyRanks > 0 && ` · ${t('Частота', 'Frequency')} ${sheet.heroicUpgrades.frequencyRanks}`}
      </div>
      {sheet.heroicUpgrades.secondaryEffects.length > 0 && <div className="muted small-text">
        {t('Вторичные эффекты', 'Secondary effects')}: {sheet.heroicUpgrades.secondaryEffects.map(localizedName).join(' · ')}
      </div>}
    </div>}
  </section>
}

export function ReadOnlyAttachmentsTab({ sheet }: { sheet: CharacterSheet }) {
  const hostNames = new Map(sheet.items.map(item => [item.id, localizedName(item)]))
  return <section className="panel">
    <h3>{t('Улучшения', 'Attachments')}</h3>
    {sheet.attachments.length === 0 && <p className="muted">{t('Улучшений нет.', 'No attachments.')}</p>}
    <div className="notes-list">{sheet.attachments.map(value =>
      <ReadOnlyAttachment key={value.id} attachment={value} hostName={value.hostCharacterItemId ? hostNames.get(value.hostCharacterItemId) : undefined} />)}
    </div>
  </section>
}

function ReadOnlyAttachment({ attachment, hostName }: { attachment: CharacterAttachment; hostName?: string }) {
  return <div className="note-card">
    <strong>{localizedName(attachment)}</strong>
    <div className="muted small-text">
      {hostName ? t(`Установлено: ${hostName}`, `Installed on: ${hostName}`) : t('В запасе', 'Spare')}
      {attachment.hardPointCost > 0 && ` · HP ${attachment.hardPointCost}`}
      {attachment.damageState !== 'undamaged' && ` · ${ITEM_DAMAGE_STATE_LABELS[attachment.damageState]}`}
    </div>
    {attachment.note && <div>{attachment.note}</div>}
  </div>
}

export function ReadOnlyTransportTab({ sheet }: { sheet: CharacterSheet }) {
  return <section className="panel">
    <h3>{t('Транспорт', 'Transport')}</h3>
    {sheet.mounts.length === 0 && <p className="muted">{t('Транспорта нет.', 'No transport.')}</p>}
    <div className="notes-list">{sheet.mounts.map(mount => <div className="note-card" key={mount.id}>
      <strong>{mount.displayName}</strong>
      <div className="muted small-text">
        {TRANSPORT_KIND_LABELS[mount.definition.transportKind]} · {MOVEMENT_MODE_LABELS[mount.definition.movementMode]}
        {' · '}{t('Раны', 'Wounds')} {mount.woundsCurrent}/{mount.definition.woundThreshold}
        {' · '}{t('Поглощение', 'Soak')} {mount.soak} · {t('Защита', 'Defense')} {mount.meleeDefense}/{mount.rangedDefense}
        {' · '}{t('Груз', 'Cargo')} {mount.carriedLoad}/{mount.capacity}
      </div>
      {mount.notes && <div>{mount.notes}</div>}
    </div>)}</div>
  </section>
}

export function ReadOnlyBioTab({ sheet }: { sheet: CharacterSheet }) {
  const motivations = [
    [t('Стремление', 'Desire'), sheet.desire], [t('Страх', 'Fear'), sheet.fear],
    [t('Сильная сторона', 'Strength'), sheet.strength], [t('Слабость', 'Flaw'), sheet.flaw],
  ] as const
  return <div>
    <section className="panel">
      <h3>{t('Образ персонажа', 'Character bio')}</h3>
      {motivations.every(([, value]) => !value) && <p className="muted">{t('Мотивации не заполнены.', 'Motivations are empty.')}</p>}
      {motivations.filter(([, value]) => value).map(([label, value]) => <div className="sheet-entry" key={label}>
        <strong>{label}:</strong> {value}
      </div>)}
    </section>
    <section className="panel">
      <h3>{t('Предыстория', 'Background')}</h3>
      {sheet.background ? <div className="sheet-prewrap">{sheet.background}</div> : <p className="muted">—</p>}
    </section>
  </div>
}
