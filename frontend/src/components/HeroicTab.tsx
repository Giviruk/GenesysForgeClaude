import { BookReference } from './BookReference'
import { useState } from 'react'
import { api } from '../api/client'
import type {
  ActivateCharacterAbilityResult, CharacterSheet, HeroicIdentity, HeroicOriginType, Reference,
  SignatureWeaponImprovement, SignatureWeaponProfile, WeaponCraftsmanship, WeaponFormTrait,
} from '../api/types'
import {
  CONFIRMABLE_WEAPON_TRAITS, formatWeaponTraits, HEROIC_ORIGIN_LABELS, HEROIC_ORIGIN_TYPES,
  HEROIC_UPGRADE_LABELS, heroicOriginFace, isAttachmentCompatible, localizedDescription, localizedName,
  parseWeaponTraits, signatureWeaponTraits,
  SIGNATURE_WEAPON_CRAFTSMANSHIPS, SIGNATURE_WEAPON_IMPROVEMENT_LABELS, SUPREME_ATTACHMENT_MAX_RARITY,
  SIGNATURE_WEAPON_PROFILE_LABELS, WEAPON_CRAFTSMANSHIP_LABELS,
  WEAPON_TRAIT_LABELS, SIGNATURE_WEAPON_CRAFTSMANSHIP_HINTS,
} from '../utils/labels'
import { t } from '../i18n'
import { PropertyText } from './PropertyText'
import { canonicalQualityName } from '../data/itemQualities'
import { Icon } from './Icon'
import { FilterChip } from './content/ContentUi'
import { readHeroicUses, writeHeroicUses } from '../utils/uiPreferences'

interface Props {
  sheet: CharacterSheet
  reference: Reference
  onError: (message: string) => void
  refresh: () => Promise<void>
}

/**
 * Вкладка героической способности (только Realms of Terrinoth): полное описание эффекта,
 * личность, параметр и покупка улучшений. На листе персонажа остаётся краткая сводка —
 * базовый эффект и уже купленные улучшения.
 */
export function HeroicTab({ sheet, reference, onError, refresh }: Props) {
  const [heroicPick, setHeroicPick] = useState('')

  async function run(action: () => Promise<unknown>) {
    try {
      await action()
      await refresh()
    } catch (err) {
      onError(err instanceof Error ? err.message : t('Ошибка', 'Error'))
    }
  }

  if (sheet.system !== 'realmsOfTerrinoth') {
    return <p className="hint">{t('Героические способности есть только в Realms of Terrinoth.',
      'Heroic abilities exist only in Realms of Terrinoth.')}</p>
  }

  return (
    <div className="heroic-tab">
      {sheet.heroicAbility ? (
        <HeroicAbilityCard key={sheet.id} sheet={sheet} reference={reference} run={run} />
      ) : (
        <section className="heroic-ability-card panel"><h3><Icon name="crown" className="button-icon" />{t('Героическая способность', 'Heroic ability')}</h3>
          <div className="inline-form">
            <select value={heroicPick} onChange={e => setHeroicPick(e.target.value)}>
              <option value="" disabled>{t('— выберите способность —', '— pick an ability —')}</option>
              {reference.heroicAbilities.map(h => (
                <option key={h.id} value={h.id}>{localizedName(h)}{h.isCustom ? t(' (кастом)', ' (custom)') : ''}</option>
              ))}
            </select>
            <button className="primary" disabled={!heroicPick}
              onClick={() => run(() => api.setHeroicAbility(sheet.id, heroicPick))}>
              {t('Выбрать', 'Choose')}
            </button>
          </div>
          {heroicPick && (() => {
            const h = reference.heroicAbilities.find(x => x.id === heroicPick)
            if (!h) return null
            const description = localizedDescription(h)
            return (
              <div className="heroic-ability-preview">
                {description && <div className="hint small-text"><b>{t('Что даёт способность:', 'What the ability gives:')}</b></div>}
                {description && <p className="hint">{description}</p>}
                <BookReference source={h.source} />
              </div>
            )
          })()}
        </section>
      )}
    </div>
  )
}

function HeroicAbilityCard({ sheet, reference, run }: {
  sheet: CharacterSheet
  reference: Reference
  run: (action: () => Promise<unknown>) => Promise<void>
}) {
  const h = sheet.heroicAbility!
  const upgrades = sheet.heroicUpgrades
  const rank = upgrades.powerRank
  const total = sheet.heroicUpgradePointsTotal
  const available = total - sheet.heroicUpgradePointsSpent
  const [used, setUsed] = useState(() => readHeroicUses(sheet.id))
  const [activating, setActivating] = useState(false)
  const usesTotal = 1 + upgrades.frequencyRanks
  const remaining = Math.max(0, usesTotal - used)
  const upgradesLocked = sheet.heroicIdentityIncomplete || sheet.heroicConfigurationIncomplete
  const weaponNeedsChoice = sheet.heroicConfiguration?.signatureWeapon?.improvement === 'none' && rank >= 1
  const [outcome, setOutcome] = useState<ActivateCharacterAbilityResult | null>(null)
  const selectedEffectIds = upgrades.secondaryEffects.map(x => x.id)

  function save(patch: Partial<typeof upgrades>) {
    const next = { ...upgrades, ...patch }
    return api.setHeroicUpgrades(sheet.id, {
      powerRank: next.powerRank,
      durationRanks: next.durationRanks,
      frequencyRanks: next.frequencyRanks,
      story: next.story,
      secondaryEffectIds: next.secondaryEffects.map(x => x.id),
    })
  }

  async function activate() {
    if (activating || !remaining) return
    setActivating(true)
    try { await run(async () => {
      const result = await api.activateCharacterAbility(sheet.id)
      setOutcome(result)
      setUsed(prev => { const next = prev + 1; writeHeroicUses(sheet.id, next); return next })
    }) } finally { setActivating(false) }
  }
  const meta = [
    { label: t('Активация', 'Activation'), value: upgrades.story ? t('1 очко сюжета', '1 Story Point') : h.activationCost, improved: upgrades.story ? t('улучшено: Сюжет', 'upgraded: Story') : '' },
    { label: t('Тип', 'Type'), value: h.activation, improved: '' },
    { label: t('Длительность', 'Duration'), value: h.duration, improved: upgrades.durationRanks ? t(`улучшено: +${upgrades.durationRanks} ход.`, `upgraded: +${upgrades.durationRanks} turns`) : '' },
    { label: t('Частота', 'Frequency'), value: upgrades.frequencyRanks ? t(`${usesTotal} раз за сессию`, `${usesTotal} times per session`) : h.frequency, improved: upgrades.frequencyRanks ? t(`улучшено: +${upgrades.frequencyRanks}`, `upgraded: +${upgrades.frequencyRanks}`) : '' },
  ]
  return (
    <div className="heroic sheet-two-column">
      <div className="heroic-main">
        <section className="heroic-ability-card">
          <h4><Icon name="crown" className="button-icon" />{t('Героическая способность · Terrinoth', 'Heroic ability · Terrinoth')}</h4>
          <h3>{sheet.heroicIdentity?.customName || localizedName(h)}</h3>
          <p className="muted small-text">{t('Эффект:', 'Effect:')} {localizedName(h)} · {h.source}</p>
          {localizedDescription(h) && <p>{localizedDescription(h)}</p>}
          <BookReference source={h.source} />
          <div className="heroic-meta-grid">{meta.map(m => <div key={m.label}><small>{m.label}</small><strong>{m.value}</strong>{m.improved && <span>{m.improved}</span>}</div>)}</div>
          {h.requirement && h.requirement !== '—' && <p className="hint small-text">{t('Требование:', 'Requirement:')} {h.requirement}</p>}
          {h.notes && <p className="hint small-text">{h.notes}</p>}
          <div className="heroic-activate">
            <button className="heroic-activate-button" disabled={activating || !remaining} onClick={() => void activate()}><Icon name="bolt" className="button-icon" />{t('Активировать', 'Activate')}</button>
            <div className="heroic-uses"><span className="heroic-dots" aria-hidden="true">{Array.from({ length: usesTotal }, (_, i) => <i className={i < remaining ? 'filled' : ''} key={i} />)}</span>
              <small role="status">{remaining ? t(`осталось ${remaining} из ${usesTotal}`, `${remaining} of ${usesTotal} remaining`) : t('применения исчерпаны', 'uses exhausted')}</small></div>
            <button className="heroic-session-reset" disabled={activating} onClick={() => { writeHeroicUses(sheet.id, 0); setUsed(0); setOutcome(null) }}>{t('Новая сессия', 'New session')}</button>
            {outcome && <div className="heroic-activate-result small-text">{outcome.applied.map((v, i) => <div key={`a${i}`} className="success-text"><Icon name="check" className="button-icon" />{v}</div>)}
              {outcome.manual.map((v, i) => <div key={`m${i}`} className="muted"><Icon name="hand-finger" className="button-icon" />{v}</div>)}</div>}
          </div>
          <HeroicIdentitySection sheet={sheet} run={run} />
          {sheet.isCreationPhase && <button className="small" onClick={() => run(() => api.setHeroicAbility(sheet.id, null))}>{t('Сбросить способность', 'Reset ability')}</button>}
        </section>
        <HeroicParameterSection sheet={sheet} reference={reference} run={run} section="configuration" />
      </div>
      <aside className="heroic-upgrades sheet-result-card">
          <div className="label-line">
            {t(`Улучшения · очков доступно: ${available} из ${total}`, `Upgrades · points available: ${available} of ${total}`)}
            <span className="heroic-dots" aria-hidden="true">{Array.from({ length: total }, (_, i) => <i key={i} className={i < available ? 'filled' : ''} />)}</span>
            <span className="hint"> {t('(по 1 за каждые 50 XP сверх стартового XP вида)', '(1 per 50 XP above species starting XP)')}</span>
          </div>
          {sheet.heroicIdentityIncomplete && (
            <p className="hint small-text">
              {t('Улучшения заблокированы, пока не заполнены личное название и происхождение.',
                'Upgrades are locked until the personal name and origin are filled in.')}
            </p>
          )}
          {sheet.heroicConfigurationIncomplete && (
            <p className="hint small-text">
              {t('Улучшения заблокированы, пока не выбран параметр способности.',
                'Upgrades are locked until the ability parameter is chosen.')}
            </p>
          )}
          <h4>{t('Сила', 'Power')}</h4>
          <div className="heroic-power-level bought"><span className="heroic-level-dot"><Icon name="check" /></span><div><strong>{t('Базовая', 'Basic')}</strong> <small className="muted">{t('есть всегда', 'always available')}</small>{localizedDescription(h) && <p className="small-text muted">{localizedDescription(h)}</p>}</div></div>
          {h.upgrades.map(u => {
            const purchased = rank >= u.level
            const isNext = u.level === rank + 1
            const canBuy = isNext && available >= u.cost && !upgradesLocked && !weaponNeedsChoice
            const isTop = purchased && u.level === rank
            return (
              <div key={u.level} className={`heroic-upgrade heroic-power-level${purchased ? ' bought' : isNext ? ' next' : ''}`}><span className="heroic-level-dot">{purchased ? <Icon name="check" /> : u.level + 1}</span><div>
                <div className="heroic-upgrade-head">
                  <strong>{HEROIC_UPGRADE_LABELS[u.level] ?? t(`Уровень ${u.level}`, `Level ${u.level}`)}</strong>
                  <span className="hint"> · {u.cost} {t('очк.', 'pts')}</span>
                  {purchased && <span className="badge"> {t('куплено', 'purchased')}</span>}
                  {!purchased && canBuy && (
                    <button className="small primary"
                      onClick={() => run(() => save({ powerRank: u.level }))}>
                      {t('Купить', 'Buy')}
                    </button>
                  )}
                  {!purchased && isNext && !canBuy && <span className="hint"> {upgradesLocked ? t('— сначала заполните способность', '— complete the ability first') : weaponNeedsChoice ? t('— сначала выбор выше', '— complete the choice above first') : t('— не хватает очков', '— not enough points')}</span>}
                  {!purchased && !isNext && <span className="hint"> {t('— сначала купите предыдущее', '— buy the previous one first')}</span>}
                  {isTop && sheet.isCreationPhase && (
                    <button className="small"
                      onClick={() => run(() => save({ powerRank: u.level - 1 }))}>
                      {t('Вернуть', 'Refund')}
                    </button>
                  )}
                </div>
                <BookReference source={u.source || h.source} />
                {localizedDescription(u) && <p>{localizedDescription(u)}</p>}
                {u.notes && <p className="hint small-text">{u.notes}</p>}
                {purchased && <HeroicParameterSection sheet={sheet} reference={reference} run={run} section={u.level === 1 ? 'improved' : 'supreme'} />}
              </div></div>
            )
          })}

          <h4>{t('Параметры', 'Parameters')}</h4>
          <div className="heroic-upgrade">
            <div className="heroic-upgrade-head">
              <strong>{t('Длительность', 'Duration')}</strong>
              <span className="hint"> · 1 {t('очк. за ранг', 'pt per rank')} · {t(`рангов: ${upgrades.durationRanks}`, `ranks: ${upgrades.durationRanks}`)}</span>
              <button className="heroic-plus small" aria-label={t('Купить Длительность', 'Buy Duration')} title={available < 1 ? t('Не хватает очков', 'Not enough points') : undefined} disabled={available < 1 || upgradesLocked} onClick={() => run(() => save({ durationRanks: upgrades.durationRanks + 1 }))}>+</button>
              {sheet.isCreationPhase && upgrades.durationRanks > 0 && <button className="small" onClick={() => run(() => save({ durationRanks: upgrades.durationRanks - 1 }))}>{t('Вернуть', 'Refund')}</button>}
            </div>
            <BookReference source="Realms of Terrinoth, с. 79" />
            <p className="hint small-text">{t('Каждый ранг продлевает эффект ещё на один ход.', 'Each rank extends the effect by one turn.')}</p>
          </div>

          <div className="heroic-upgrade">
            <div className="heroic-upgrade-head">
              <strong>{t('Частота', 'Frequency')}</strong>
              <span className="hint"> · 2 {t('очк. за ранг', 'pts per rank')} · {t(`рангов: ${upgrades.frequencyRanks}`, `ranks: ${upgrades.frequencyRanks}`)}</span>
              <button className="heroic-plus small" aria-label={t('Купить Частоту', 'Buy Frequency')} title={available < 2 ? t('Не хватает очков', 'Not enough points') : undefined} disabled={available < 2 || upgradesLocked} onClick={() => run(() => save({ frequencyRanks: upgrades.frequencyRanks + 1 }))}>+</button>
              {sheet.isCreationPhase && upgrades.frequencyRanks > 0 && <button className="small" onClick={() => run(() => save({ frequencyRanks: upgrades.frequencyRanks - 1 }))}>{t('Вернуть', 'Refund')}</button>}
            </div>
            <BookReference source="Realms of Terrinoth, с. 79" />
            <p className="hint small-text">{t('Каждый ранг даёт ещё одно применение за сессию.', 'Each rank grants one additional use per session.')}</p>
          </div>

          <div className={upgrades.story ? 'heroic-upgrade bought' : 'heroic-upgrade'}>
            <div className="heroic-upgrade-head">
              <strong>{t('Сюжет', 'Story')}</strong><span className="hint"> · 1 {t('очк.', 'pt')}</span>
              {upgrades.story && <span className="badge">{t('куплено', 'purchased')}</span>}
              {!upgrades.story && <button className="heroic-plus small" aria-label={t('Купить Сюжет', 'Buy Story')} title={available < 1 ? t('Не хватает очков', 'Not enough points') : undefined} disabled={available < 1 || upgradesLocked} onClick={() => run(() => save({ story: true }))}>+</button>}
              {sheet.isCreationPhase && upgrades.story && <button className="small" onClick={() => run(() => save({ story: false }))}>{t('Вернуть', 'Refund')}</button>}
            </div>
            <BookReference source="Realms of Terrinoth, с. 79" />
            <p className="hint small-text">{t('Снижает стоимость активации до одного очка сюжета.', 'Reduces activation cost to one Story Point.')}</p>
          </div>

          <div className="heroic-upgrade">
            <strong>{t(`Вторичные эффекты (${upgrades.secondaryEffects.length}/2)`, `Secondary effects (${upgrades.secondaryEffects.length}/2)`)}</strong>
            {reference.heroicSecondaryEffects.map(effect => {
              const selected = selectedEffectIds.includes(effect.id)
              const canBuy = !selected && upgrades.secondaryEffects.length < 2 && available >= 1 && !upgradesLocked
              return (
                <div key={effect.id} className={selected ? 'heroic-upgrade bought' : 'heroic-upgrade'}>
                  <div className="heroic-upgrade-head">
                    <strong>{localizedName(effect)}</strong><span className="hint"> · 1 {t('очк.', 'pt')}</span>
                    {selected && <span className="badge">{t('куплено', 'purchased')}</span>}
                    {!selected && <button className="small" disabled={!canBuy} onClick={() => run(() => save({ secondaryEffects: [...upgrades.secondaryEffects, effect] }))}>{t('1 очк.', '1 pt')}</button>}
                    {selected && sheet.isCreationPhase && <button className="small" onClick={() => run(() => save({ secondaryEffects: upgrades.secondaryEffects.filter(x => x.id !== effect.id) }))}>{t('Вернуть', 'Refund')}</button>}
                  </div>
                  <BookReference source={effect.source} />
                  {localizedDescription(effect) && <p>{localizedDescription(effect)}</p>}
                </div>
              )
            })}
          </div>
      </aside>
    </div>
  )
}

/** Категории и грани сохранённого происхождения одной строкой. */
function originSummary(identity: HeroicIdentity): string {
  if (identity.originMode === 'custom') return identity.originNarrative ?? ''
  return [identity.originPrimary, identity.originSecondary]
    .filter((x): x is HeroicOriginType => !!x)
    .map(x => `${heroicOriginFace(x)} — ${HEROIC_ORIGIN_LABELS[x]}`)
    .join(' · ')
}

/**
 * Личное название и происхождение героической способности (ROT-HA-01). Заполняется при
 * создании и после него неизменяемо; исключение — однократное заполнение старого персонажа,
 * у которого этих данных ещё нет.
 */
export function HeroicIdentitySection({ sheet, run }: {
  sheet: CharacterSheet
  run: (action: () => Promise<unknown>) => Promise<void>
}) {
  const identity = sheet.heroicIdentity
  const editable = sheet.isCreationPhase || sheet.heroicIdentityIncomplete

  const [name, setName] = useState(identity?.customName ?? '')
  const [origin, setOrigin] = useState<HeroicOriginType | ''>(identity?.originPrimary ?? '')
  const canSave = name.trim().length > 0 && origin !== ''

  function save() {
    return api.setHeroicIdentity(sheet.id, {
      customName: name.trim(), originMode: 'standard', originPrimary: origin as HeroicOriginType,
    })
  }

  return (
    <div className="heroic-identity">
      <div className="label-line">{t('Название и происхождение', 'Name and origin')}</div>

      {identity?.complete && (
        <div className="hint small-text">
          <b>{t('Происхождение:', 'Origin:')}</b> {originSummary(identity)}
          {identity.originRolls.length > 0 && (
            <> · {t('броски d10:', 'd10 rolls:')} {identity.originRolls.join(', ')}
              {identity.originRolls.includes(0)
                && ` (${t('0 — бросить ещё дважды', '0 — roll twice more')})`}</>
          )}
        </div>
      )}

      {sheet.heroicIdentityIncomplete && !sheet.isCreationPhase && (
        <p className="hint small-text">
          {t('Данные не заполнены: укажите их один раз — после этого они станут неизменяемыми.',
            'These are missing: fill them in once — afterwards they become immutable.')}
        </p>
      )}

      {editable && (
        <div className="heroic-identity-form">
          <input value={name} maxLength={120} placeholder={t('Личное название', 'Personal name')}
            onChange={e => setName(e.target.value)} />

          <select aria-label={t('Происхождение', 'Origin')} value={origin}
            onChange={e => setOrigin(e.target.value as HeroicOriginType)}>
            <option value="" disabled>{t('— категория происхождения —', '— origin category —')}</option>
            {HEROIC_ORIGIN_TYPES.map(x => (
              <option key={x} value={x}>{heroicOriginFace(x)} — {HEROIC_ORIGIN_LABELS[x]}</option>
            ))}
          </select>

          <button className="small primary heroic-identity-save" disabled={!canSave} onClick={() => run(save)}>
            {t('Сохранить', 'Save')}
          </button>
        </div>
      )}
    </div>
  )
}

function WeaponTraitsPicker({ selected, onChange }: {
  selected: WeaponFormTrait[]
  onChange: (traits: WeaponFormTrait[]) => void
}) {
  const [open, setOpen] = useState(false)
  const selectedLabels = CONFIRMABLE_WEAPON_TRAITS
    .filter(trait => selected.includes(trait))
    .map(trait => WEAPON_TRAIT_LABELS[trait])

  function toggle(trait: WeaponFormTrait) {
    onChange(selected.includes(trait)
      ? selected.filter(value => value !== trait)
      : [...selected, trait])
  }

  return (
    <div className="weapon-traits-picker">
      <button type="button" className="weapon-traits-trigger"
        aria-label={t('Признаки формы', 'Form traits')}
        aria-haspopup="listbox" aria-expanded={open}
        onClick={() => setOpen(value => !value)}>
        <span>
          {selectedLabels.length > 0
            ? selectedLabels.join(', ')
            : t('— выбрать признаки формы —', '— choose form traits —')}
        </span>
        <span className="weapon-traits-chevron" aria-hidden="true">▾</span>
      </button>
      {open && (
        <div className="weapon-traits-menu" role="listbox"
          aria-label={t('Признаки формы', 'Form traits')}>
          {CONFIRMABLE_WEAPON_TRAITS.map(trait => {
            const isSelected = selected.includes(trait)
            return (
              <button key={trait} type="button" role="option"
                aria-selected={isSelected} className={`weapon-trait-option${isSelected ? ' selected' : ''}`}
                onClick={() => toggle(trait)}>
                <span aria-hidden="true">{isSelected ? '✓' : ''}</span>
                {WEAPON_TRAIT_LABELS[trait]}
              </button>
            )
          })}
          <button type="button" className="small weapon-traits-done"
            onClick={() => setOpen(false)}>
            {t('Готово', 'Done')}
          </button>
        </div>
      )}
    </div>
  )
}

/**
 * Параметр primary effect (ROT-HA-02): навык Paragon, категория Sixth Sense или именное оружие.
 * Выбирается вместе со способностью и после завершения создания не меняется; отдельная команда
 * замены остаётся доступной только для потерянного оружия.
 */
export function HeroicParameterSection({ sheet, reference, run, section = 'all' }: {
  section?: 'all' | 'configuration' | 'improved' | 'supreme'
  sheet: CharacterSheet
  reference: Reference
  run: (action: () => Promise<unknown>) => Promise<void>
}) {
  const config = sheet.heroicConfiguration
  const editable = sheet.isCreationPhase || sheet.heroicConfigurationIncomplete

  const [skillId, setSkillId] = useState(config?.paragonSkillDefId ?? '')
  const [subject, setSubject] = useState(config?.sixthSenseSubject ?? '')
  const weapon = config?.signatureWeapon ?? null
  const [profile, setProfile] = useState<SignatureWeaponProfile>(weapon?.profile ?? 'oneHanded')
  const [craftsmanship, setCraftsmanship] = useState<WeaponCraftsmanship>(weapon?.craftsmanship ?? 'steel')
  const [traits, setTraits] = useState<WeaponFormTrait[]>(parseWeaponTraits(weapon?.formTraits))
  const [baseAttachmentId, setBaseAttachmentId] = useState(weapon?.baseAttachment?.defId ?? '')
  const [improvement, setImprovement] = useState<SignatureWeaponImprovement>(
    weapon?.improvement ?? 'none')
  const [supremeAttachmentId, setSupremeAttachmentId] = useState(weapon?.supremeAttachment?.defId ?? '')

  // Список улучшений сужается признаками формы — теми же, что достроит сервер. Качество, которое
  // у профиля уже есть, отсеивает сервер: своей таблицы качеств профилей у клиента нет.
  const compatibleAttachments = (reference.attachments ?? []).filter(def =>
    isAttachmentCompatible('weapon', signatureWeaponTraits(profile, traits), def))
  const selectedBaseAttachment = (reference.attachments ?? [])
    .find(def => def.id === baseAttachmentId)

  // Supreme считает совместимость по уже подтверждённой форме оружия и знает про предел редкости;
  // вместимость по слотам и повтор базового проверяет сервер.
  const supremeChoices = (reference.attachments ?? []).filter(def =>
    weapon != null
    && isAttachmentCompatible('weapon', parseWeaponTraits(weapon.formTraits), def)
    && def.rarity <= SUPREME_ATTACHMENT_MAX_RARITY
    && def.code !== weapon.baseAttachment?.code)

  if (!config || config.kind === 'none') return null

  const title = config.kind === 'paragonSkill' ? t('Навык способности', 'Ability skill')
    : config.kind === 'sixthSenseSubject' ? t('Что воспринимает способность', 'What the ability senses')
      : t('Именное оружие', 'Signature weapon')

  return (
    <div className={`heroic-parameter${section === 'configuration' ? ' sheet-result-card heroic-weapon-card' : ''}`}>
      {(section === 'all' || section === 'configuration') && <>
      <h3>{config.kind === 'signatureWeapon' && <Icon name="sword" className="button-icon" />}{title}</h3>

      {config.kind === 'paragonSkill' && config.paragonSkillName && (
        <div className="hint small-text">
          {config.paragonSkillName}
          {config.paragonSkillMissing && ` · ${t('навык больше не доступен — требуется исправление',
            'the skill is no longer available — needs repair')}`}
        </div>
      )}
      {config.kind === 'sixthSenseSubject' && config.sixthSenseSubject && (
        <div className="hint small-text">{config.sixthSenseSubject}</div>
      )}
      {weapon && (
        <div className="hint small-text">
          <div className="sheet-card-heading"><strong>{weapon.narrativeForm}</strong><span className={weapon.isLost ? 'danger-text' : 'success-text'}>{weapon.isLost ? t('потеряно', 'lost') : t('в руках', 'in hand')}</span></div>
          <div className="heroic-weapon-stats">{[[t('Урон', 'Damage'), weapon.damage], [t('Крит', 'Crit'), weapon.crit], [t('Дистанция', 'Range'), weapon.rangeBand], [t('Вес', 'Load'), weapon.encumbrance], [t('Слоты', 'Slots'), weapon.hardPoints]].map(([k, v]) => <div key={k}><small>{k}</small><b>{v}</b></div>)}</div>
          {SIGNATURE_WEAPON_PROFILE_LABELS[weapon.profile]}
          {' · '}{WEAPON_CRAFTSMANSHIP_LABELS[weapon.craftsmanship]}
          {' · '}{weapon.skillName}
          {weapon.qualities.length > 0 && (
            <>
              {' · '}
              <PropertyText
                text={weapon.qualities.map(q => {
                  const name = canonicalQualityName(q.nameEn || q.nameRu)
                  return q.rating ? `${name} ${q.rating}` : name
                }).join(', ')}
                qualities={reference.qualities} />
            </>
          )}
          {weapon.baseAttachment && (
            <div>
              {t('Базовое улучшение:', 'Base attachment:')}{' '}
              {localizedName(weapon.baseAttachment)}
              {' · '}{t('временное, 0 слотов, действует только со способностью',
                'transient, 0 hard points, active only with the ability')}
            </div>
          )}
          {weapon.improvement !== 'none' && (
            <div>
              {t('Improved:', 'Improved:')} {SIGNATURE_WEAPON_IMPROVEMENT_LABELS[weapon.improvement]}
            </div>
          )}
          {weapon.supremeAttachment && (
            <div>
              {t('Улучшение Supreme:', 'Supreme attachment:')}{' '}
              {localizedName(weapon.supremeAttachment)}
              {' · '}{t('установлено постоянно и занимает слоты',
                'permanently installed and uses hard points')}
            </div>
          )}
          {weapon.craftsmanshipOutOfRules && (
            <div className="warning-text">
              {t('Качество изготовления выбрано вне нынешнего списка способности — решение за ведущим.',
                'The craftsmanship is outside what the ability now offers — the GM decides what to do.')}
            </div>
          )}
        </div>
      )}

      </>}
      {/* Improved и Supreme фиксируются при покупке: пока выбор не сделан, покупать дальше нельзя. */}
      {section !== 'configuration' && config.kind === 'signatureWeapon' && weapon && sheet.heroicUpgradeRank >= 1 && (
        <div className="heroic-weapon-upgrades">
          {section !== 'supreme' && weapon.improvement === 'none' && (
            <div className="inline-form">
              <div className="sheet-chips" role="group" aria-label={t('Улучшение Improved', 'Improved upgrade')}>{(['reinforced', 'ancient'] as SignatureWeaponImprovement[]).map(v => <FilterChip key={v} active={improvement === v} onClick={() => setImprovement(v)}>{SIGNATURE_WEAPON_IMPROVEMENT_LABELS[v]}</FilterChip>)}</div>
              <button className="small primary" disabled={improvement === 'none'}
                onClick={() => run(() => api.setSignatureWeaponUpgrades(sheet.id, { improvement }))}>
                {t('Выбрать навсегда', 'Choose permanently')}
              </button>
            </div>
          )}
          {section !== 'improved' && sheet.heroicUpgradeRank >= 2 && !weapon.supremeAttachment && (
            <div className="inline-form">
              <div className="sheet-chips" role="group" aria-label={t('Улучшение Supreme', 'Supreme attachment')}>
                {supremeChoices.map(a => <FilterChip key={a.id} active={supremeAttachmentId === a.id}
                  onClick={() => setSupremeAttachmentId(a.id)}>{localizedName(a)}</FilterChip>)}
              </div>
              <button className="small primary" disabled={!supremeAttachmentId}
                onClick={() => run(() => api.setSignatureWeaponUpgrades(sheet.id, {
                  supremeAttachmentDefId: supremeAttachmentId,
                }))}>
                {t('Выбрать навсегда', 'Choose permanently')}
              </button>
            </div>
          )}
          <p className="hint small-text">
            {section !== 'supreme' && t('Improved даёт ровно одно: Укреплённое либо древнюю работу, которая заменяет прежнюю и отнимает слот. Выбор навсегда.',
              'Improved grants exactly one: Reinforced or Ancient craftsmanship, which replaces the previous one and costs a hard point. This choice is permanent.')}
            {section === 'all' && ' '}
            {section !== 'improved' && t('Supreme добавляет два слота и одно бесплатное улучшение редкости не выше 9. Выбор навсегда.',
              'Supreme adds two hard points and one free attachment of rarity 9 or less. This choice is permanent.')}
          </p>
        </div>
      )}

      {(section === 'all' || section === 'configuration') && <>
      {sheet.heroicConfigurationIncomplete && (
        <p className="hint small-text">
          {t('Параметр обязателен: без него создание не завершается, а улучшения недоступны.',
            'The parameter is mandatory: creation cannot be finished and upgrades stay locked without it.')}
        </p>
      )}

      {editable && config.kind === 'paragonSkill' && (
        <div className="inline-form">
          <select value={skillId} onChange={e => setSkillId(e.target.value)}>
            <option value="" disabled>{t('— выберите навык —', '— pick a skill —')}</option>
            {reference.skills.map(s => (
              <option key={s.id} value={s.id}>{localizedName(s)}</option>
            ))}
          </select>
          <button className="small primary" disabled={!skillId}
            onClick={() => run(() => api.setHeroicConfiguration(sheet.id, { paragonSkillDefId: skillId }))}>
            {t('Сохранить', 'Save')}
          </button>
        </div>
      )}

      {editable && config.kind === 'sixthSenseSubject' && (
        <div className="inline-form">
          <input value={subject} maxLength={300} placeholder={t('например, духи', 'for example, spirits')}
            onChange={e => setSubject(e.target.value)} />
          <button className="small primary" disabled={!subject.trim()}
            onClick={() => run(() => api.setHeroicConfiguration(sheet.id, { sixthSenseSubject: subject.trim() }))}>
            {t('Сохранить', 'Save')}
          </button>
        </div>
      )}

      {config.kind === 'signatureWeapon' && (editable || weapon?.isLost) && (
        <div className="heroic-weapon-form">
          <div className="inline-form">
            <select aria-label={t('Профиль оружия', 'Weapon profile')} value={profile}
              onChange={e => setProfile(e.target.value as SignatureWeaponProfile)}>
              {Object.entries(SIGNATURE_WEAPON_PROFILE_LABELS).map(([value, label]) => (
                <option key={value} value={value}>{label}</option>
              ))}
            </select>
            <select aria-label={t('Качество изготовления', 'Craftsmanship')} value={craftsmanship}
              onChange={e => setCraftsmanship(e.target.value as WeaponCraftsmanship)}>
              {SIGNATURE_WEAPON_CRAFTSMANSHIPS.map(c => (
                <option key={c} value={c}>{WEAPON_CRAFTSMANSHIP_LABELS[c]}</option>
              ))}
            </select>
          </div>
          <p className="hint small-text craftsmanship-hint">
            <b>{t('Что даёт качество:', 'What the craftsmanship gives:')}</b>{' '}
            {SIGNATURE_WEAPON_CRAFTSMANSHIP_HINTS[craftsmanship]}
          </p>
          <WeaponTraitsPicker
            selected={traits.filter(trait => CONFIRMABLE_WEAPON_TRAITS.includes(trait))}
            onChange={setTraits}
          />
          <p className="hint small-text">
            {t('Признаки формы подтверждает ведущий: по ним, а не по названию, считается совместимость улучшений.',
              'The GM confirms the form traits: attachment compatibility follows them, not the name.')}
          </p>
          <div className="inline-form">
            <select value={baseAttachmentId} aria-label={t('Базовое улучшение', 'Base attachment')}
              onChange={e => setBaseAttachmentId(e.target.value)}>
              <option value="" disabled>{t('— базовое улучшение —', '— base attachment —')}</option>
              {compatibleAttachments.map(a => (
                <option key={a.id} value={a.id}>{localizedName(a)}</option>
              ))}
            </select>
          </div>
          {selectedBaseAttachment?.description && (
            <p className="hint small-text">
              <b>{t('Что даёт улучшение:', 'What the attachment gives:')}</b>{' '}
              <PropertyText text={localizedDescription(selectedBaseAttachment)} qualities={reference.qualities} />
            </p>
          )}
          <p className="hint small-text">
            {compatibleAttachments.length === 0
              ? t('Под выбранную форму улучшений нет — измените профиль или признаки формы.',
                'No attachment fits the chosen form — change the profile or the confirmed traits.')
              : t('Улучшение временное: действует только вместе со способностью, ничего не стоит и не занимает слотов. Того, что у оружия уже есть, взять нельзя.',
                'The attachment is transient: it works only together with the ability, costs nothing and uses no hard points. What the weapon already has cannot be taken.')}
          </p>
          <div className="inline-form">
            <button className="small primary" disabled={!baseAttachmentId}
              onClick={() => run(() => (editable
                ? api.setHeroicConfiguration(sheet.id, {
                  weaponProfile: profile,
                  craftsmanship,
                  narrativeForm: SIGNATURE_WEAPON_PROFILE_LABELS[profile],
                  formTraits: formatWeaponTraits(traits),
                  baseAttachmentDefId: baseAttachmentId,
                })
                : api.replaceSignatureWeapon(sheet.id, {
                  lost: false,
                  weaponProfile: profile,
                  craftsmanship,
                  narrativeForm: SIGNATURE_WEAPON_PROFILE_LABELS[profile],
                  formTraits: formatWeaponTraits(traits),
                  baseAttachmentDefId: baseAttachmentId,
                })))}>
              {editable ? t('Сохранить', 'Save') : t('Заменить оружие', 'Replace weapon')}
            </button>
            {weapon?.isLost && (
              <button className="small"
                onClick={() => run(() => api.replaceSignatureWeapon(sheet.id, { lost: false }))}>
                {t('Вернуть прежнее', 'Recover the old one')}
              </button>
            )}
          </div>
        </div>
      )}

      {weapon && !weapon.isLost && !sheet.isCreationPhase && (
        <button className="small"
          onClick={() => run(() => api.replaceSignatureWeapon(sheet.id, { lost: true }))}>
          {t('Отметить потерянным', 'Mark as lost')}
        </button>
      )}
      </>}
    </div>
  )
}
