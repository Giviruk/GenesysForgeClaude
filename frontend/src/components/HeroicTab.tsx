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
  SIGNATURE_WEAPON_PROFILE_LABELS, SIGNATURE_WEAPON_PROFILES, WEAPON_CRAFTSMANSHIP_LABELS,
  WEAPON_TRAIT_LABELS, SIGNATURE_WEAPON_CRAFTSMANSHIP_HINTS,
} from '../utils/labels'
import { t } from '../i18n'
import { PropertyText } from './PropertyText'
import { canonicalQualityName } from '../data/itemQualities'

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
    <section className="panel">
      <h3>{t('Героическая способность', 'Heroic ability')}</h3>
      {sheet.heroicAbility ? (
        <HeroicAbilityCard sheet={sheet} reference={reference} run={run} />
      ) : (
        <>
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
          {heroicPick && (
            <div className="heroic-ability-preview">
              <div className="hint small-text"><b>{t('Что даёт способность:', 'What the ability gives:')}</b></div>
              <p className="hint">{(() => {
                const h = reference.heroicAbilities.find(x => x.id === heroicPick)
                return h ? localizedDescription(h) : ''
              })()}</p>
            </div>
          )}
        </>
      )}
    </section>
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
    await run(async () => { setOutcome(await api.activateCharacterAbility(sheet.id)) })
  }
  const meta: [string, string][] = [
    [t('Активация', 'Activation'), [
      upgrades.story ? t('1 очко сюжета', '1 Story Point') : h.activationCost,
      h.activation,
    ].filter(Boolean).join(' · ')],
    [t('Длительность', 'Duration'), upgrades.durationRanks > 0
      ? t(`${h.duration} · +${upgrades.durationRanks} ход.`, `${h.duration} · +${upgrades.durationRanks} turn(s)`)
      : h.duration],
    [t('Частота', 'Frequency'), upgrades.frequencyRanks > 0
      ? t(`${1 + upgrades.frequencyRanks} раз за сессию`, `${1 + upgrades.frequencyRanks} times per session`)
      : h.frequency],
    [t('Требование', 'Requirement'), h.requirement && h.requirement !== '—' ? h.requirement : ''],
  ]

  return (
    <div className="heroic">
      <strong>{sheet.heroicIdentity?.customName || localizedName(h)}</strong>
      {sheet.heroicIdentity?.customName && (
        <div className="hint small-text">{t('Эффект:', 'Effect:')} {localizedName(h)}</div>
      )}
      <div className="hint small-text"><b>{t('Что даёт способность:', 'What the ability gives:')}</b></div>
      <p>{localizedDescription(h)}</p>
      {meta.filter(([, v]) => v).map(([k, v]) => (
        <div key={k} className="hint small-text"><b>{k}:</b> {v}</div>
      ))}
      {h.notes && <p className="hint small-text">{h.notes}</p>}

      <HeroicIdentitySection sheet={sheet} run={run} />
      <HeroicParameterSection sheet={sheet} reference={reference} run={run} />

      <div className="heroic-upgrades">
          <div className="label-line">
            {t(`Улучшения · очков доступно: ${available} из ${total}`, `Upgrades · points available: ${available} of ${total}`)}
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
          {h.upgrades.map(u => {
            const purchased = rank >= u.level
            const isNext = u.level === rank + 1
            const canBuy = isNext && available >= u.cost
            const isTop = purchased && u.level === rank
            return (
              <div key={u.level} className={purchased ? 'heroic-upgrade bought' : 'heroic-upgrade'}>
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
                  {!purchased && isNext && !canBuy && <span className="hint"> {t('— не хватает очков', '— not enough points')}</span>}
                  {!purchased && !isNext && <span className="hint"> {t('— сначала купите предыдущее', '— buy the previous one first')}</span>}
                  {isTop && sheet.isCreationPhase && (
                    <button className="small"
                      onClick={() => run(() => save({ powerRank: u.level - 1 }))}>
                      {t('Вернуть', 'Refund')}
                    </button>
                  )}
                </div>
                <p>{localizedDescription(u)}</p>
                {u.notes && <p className="hint small-text">{u.notes}</p>}
              </div>
            )
          })}

          <div className="heroic-upgrade">
            <div className="heroic-upgrade-head">
              <strong>{t('Длительность', 'Duration')}</strong>
              <span className="hint"> · 1 {t('очк. за ранг', 'pt per rank')} · {t(`рангов: ${upgrades.durationRanks}`, `ranks: ${upgrades.durationRanks}`)}</span>
              {available >= 1 && <button className="small primary" onClick={() => run(() => save({ durationRanks: upgrades.durationRanks + 1 }))}>{t('Купить', 'Buy')}</button>}
              {sheet.isCreationPhase && upgrades.durationRanks > 0 && <button className="small" onClick={() => run(() => save({ durationRanks: upgrades.durationRanks - 1 }))}>{t('Вернуть', 'Refund')}</button>}
            </div>
            <p className="hint small-text">{t('Каждый ранг продлевает эффект ещё на один ход.', 'Each rank extends the effect by one turn.')}</p>
          </div>

          <div className="heroic-upgrade">
            <div className="heroic-upgrade-head">
              <strong>{t('Частота', 'Frequency')}</strong>
              <span className="hint"> · 2 {t('очк. за ранг', 'pts per rank')} · {t(`рангов: ${upgrades.frequencyRanks}`, `ranks: ${upgrades.frequencyRanks}`)}</span>
              {available >= 2 && <button className="small primary" onClick={() => run(() => save({ frequencyRanks: upgrades.frequencyRanks + 1 }))}>{t('Купить', 'Buy')}</button>}
              {sheet.isCreationPhase && upgrades.frequencyRanks > 0 && <button className="small" onClick={() => run(() => save({ frequencyRanks: upgrades.frequencyRanks - 1 }))}>{t('Вернуть', 'Refund')}</button>}
            </div>
            <p className="hint small-text">{t('Каждый ранг даёт ещё одно применение за сессию.', 'Each rank grants one additional use per session.')}</p>
          </div>

          <div className={upgrades.story ? 'heroic-upgrade bought' : 'heroic-upgrade'}>
            <div className="heroic-upgrade-head">
              <strong>{t('Сюжет', 'Story')}</strong><span className="hint"> · 1 {t('очк.', 'pt')}</span>
              {upgrades.story && <span className="badge">{t('куплено', 'purchased')}</span>}
              {!upgrades.story && available >= 1 && <button className="small primary" onClick={() => run(() => save({ story: true }))}>{t('Купить', 'Buy')}</button>}
              {sheet.isCreationPhase && upgrades.story && <button className="small" onClick={() => run(() => save({ story: false }))}>{t('Вернуть', 'Refund')}</button>}
            </div>
            <p className="hint small-text">{t('Снижает стоимость активации до одного очка сюжета.', 'Reduces activation cost to one Story Point.')}</p>
          </div>

          <div className="heroic-upgrade">
            <strong>{t(`Вторичные эффекты (${upgrades.secondaryEffects.length}/2)`, `Secondary effects (${upgrades.secondaryEffects.length}/2)`)}</strong>
            {reference.heroicSecondaryEffects.map(effect => {
              const selected = selectedEffectIds.includes(effect.id)
              const canBuy = !selected && upgrades.secondaryEffects.length < 2 && available >= 1
              return (
                <div key={effect.id} className={selected ? 'heroic-upgrade bought' : 'heroic-upgrade'}>
                  <div className="heroic-upgrade-head">
                    <strong>{localizedName(effect)}</strong><span className="hint"> · 1 {t('очк.', 'pt')}</span>
                    {selected && <span className="badge">{t('куплено', 'purchased')}</span>}
                    {canBuy && <button className="small primary" onClick={() => run(() => save({ secondaryEffects: [...upgrades.secondaryEffects, effect] }))}>{t('Купить', 'Buy')}</button>}
                    {selected && sheet.isCreationPhase && <button className="small" onClick={() => run(() => save({ secondaryEffects: upgrades.secondaryEffects.filter(x => x.id !== effect.id) }))}>{t('Вернуть', 'Refund')}</button>}
                  </div>
                  <p>{localizedDescription(effect)}</p>
                </div>
              )
            })}
          </div>
      </div>

      {h.effects.length > 0 && (
        <div className="heroic-activate">
          <button className="small primary" onClick={() => void activate()}>{t('🎯 Активировать', '🎯 Activate')}</button>
          {outcome && (
            <div className="heroic-activate-result small-text">
              {outcome.applied.map((a, i) => <div key={`a${i}`}>{a}</div>)}
              {outcome.manual.map((m, i) => <div key={`m${i}`} className="muted">{m}</div>)}
            </div>
          )}
        </div>
      )}

      {sheet.isCreationPhase && (
        <button className="small" onClick={() => run(() => api.setHeroicAbility(sheet.id, null))}>
          {t('Сбросить способность', 'Reset ability')}
        </button>
      )}
    </div>
  )
}

/** Как игрок задаёт происхождение: категория таблицы, собственный текст или бросок. */
type OriginSource = 'table' | 'custom' | 'rolled'

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
  const [source, setSource] = useState<OriginSource>(
    identity?.originRolls.length ? 'rolled' : identity?.originMode === 'custom' ? 'custom' : 'table')
  const [origin, setOrigin] = useState<HeroicOriginType | ''>(identity?.originPrimary ?? '')
  const [narrative, setNarrative] = useState(identity?.originNarrative ?? '')

  const hasRolledOrigin = (identity?.originRolls.length ?? 0) > 0
  const canSave = name.trim().length > 0 && (
    source === 'table' ? origin !== ''
      : source === 'custom' ? narrative.trim().length > 0
        : hasRolledOrigin)

  function save() {
    // Для брошенного происхождения режим не отправляется: сохранённые категории и грани
    // остаются серверными, клиент меняет только личное название.
    return api.setHeroicIdentity(sheet.id, source === 'rolled'
      ? { customName: name.trim() }
      : source === 'custom'
        ? { customName: name.trim(), originMode: 'custom', originNarrative: narrative.trim() }
        : { customName: name.trim(), originMode: 'standard', originPrimary: origin as HeroicOriginType })
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

      {sheet.heroicIdentityIncomplete && (
        <p className="hint small-text">
          {sheet.isCreationPhase
            ? t('Личное название и происхождение обязательны для завершения создания.',
              'The personal name and origin are required to finish character creation.')
            : t('Данные не заполнены: укажите их один раз — после этого они станут неизменяемыми.',
              'These are missing: fill them in once — afterwards they become immutable.')}
        </p>
      )}

      {editable && (
        <div className="heroic-identity-form">
          <input value={name} maxLength={120} placeholder={t('Личное название', 'Personal name')}
            onChange={e => setName(e.target.value)} />

          <div className="inline-form">
            {(['table', 'custom', 'rolled'] as OriginSource[]).map(kind => (
              <label key={kind}>
                <input type="radio" name={`origin-source-${sheet.id}`} checked={source === kind}
                  onChange={() => setSource(kind)} />
                {kind === 'table' ? t(' выбрать', ' choose')
                  : kind === 'custom' ? t(' описать', ' describe')
                    : t(' бросить', ' roll')}
              </label>
            ))}
          </div>

          {source === 'table' && (
            <select value={origin} onChange={e => setOrigin(e.target.value as HeroicOriginType)}>
              <option value="" disabled>{t('— категория происхождения —', '— origin category —')}</option>
              {HEROIC_ORIGIN_TYPES.map(x => (
                <option key={x} value={x}>{heroicOriginFace(x)} — {HEROIC_ORIGIN_LABELS[x]}</option>
              ))}
            </select>
          )}

          {source === 'custom' && (
            <textarea value={narrative} maxLength={2000} rows={3}
              placeholder={t('Откуда взялась сила', 'Where the power came from')}
              onChange={e => setNarrative(e.target.value)} />
          )}

          {source === 'rolled' && (
            <div className="inline-form">
              <button className="small" onClick={() => run(() => api.rollHeroicOrigin(sheet.id))}>
                {t('🎲 Бросить d10', '🎲 Roll d10')}
              </button>
              {!hasRolledOrigin && (
                <span className="hint small-text">
                  {t('Специальный результат «0» даёт два происхождения.',
                    'The special result “0” yields two origins.')}
                </span>
              )}
            </div>
          )}

          <button className="small primary" disabled={!canSave} onClick={() => run(save)}>
            {t('Сохранить', 'Save')}
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
export function HeroicParameterSection({ sheet, reference, run }: {
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
  const [form, setForm] = useState(weapon?.narrativeForm ?? '')
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

  function toggleTrait(trait: WeaponFormTrait) {
    setTraits(prev => prev.includes(trait) ? prev.filter(x => x !== trait) : [...prev, trait])
  }

  const title = config.kind === 'paragonSkill' ? t('Навык способности', 'Ability skill')
    : config.kind === 'sixthSenseSubject' ? t('Что воспринимает способность', 'What the ability senses')
      : t('Именное оружие', 'Signature weapon')

  return (
    <div className="heroic-parameter">
      <div className="label-line">{title}</div>

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
          {weapon.narrativeForm} · {SIGNATURE_WEAPON_PROFILE_LABELS[weapon.profile]}
          {' · '}{WEAPON_CRAFTSMANSHIP_LABELS[weapon.craftsmanship]}
          {' · '}{weapon.skillName} · {t('урон', 'damage')} {weapon.damage}
          {' · '}{t('крит', 'crit')} {weapon.crit} · {weapon.rangeBand}
          {' · '}{t('вес', 'enc')} {weapon.encumbrance} · HP {weapon.hardPoints}
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
          {weapon.isLost && ` · ${t('потеряно', 'lost')}`}
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

      {/* Improved и Supreme фиксируются при покупке: пока выбор не сделан, покупать дальше нельзя. */}
      {config.kind === 'signatureWeapon' && weapon && sheet.heroicUpgradeRank >= 1 && (
        <div className="heroic-weapon-upgrades">
          {weapon.improvement === 'none' && (
            <div className="inline-form">
              <select value={improvement} aria-label={t('Улучшение Improved', 'Improved upgrade')}
                onChange={e => setImprovement(e.target.value as SignatureWeaponImprovement)}>
                {(['none', 'reinforced', 'ancient'] as SignatureWeaponImprovement[]).map(v => (
                  <option key={v} value={v} disabled={v === 'none'}>
                    {SIGNATURE_WEAPON_IMPROVEMENT_LABELS[v]}
                  </option>
                ))}
              </select>
              <button className="small primary" disabled={improvement === 'none'}
                onClick={() => run(() => api.setSignatureWeaponUpgrades(sheet.id, { improvement }))}>
                {t('Выбрать навсегда', 'Choose permanently')}
              </button>
            </div>
          )}
          {sheet.heroicUpgradeRank >= 2 && !weapon.supremeAttachment && (
            <div className="inline-form">
              <select value={supremeAttachmentId}
                aria-label={t('Улучшение Supreme', 'Supreme attachment')}
                onChange={e => setSupremeAttachmentId(e.target.value)}>
                <option value="" disabled>{t('— бесплатное улучшение —', '— free attachment —')}</option>
                {supremeChoices.map(a => (
                  <option key={a.id} value={a.id}>{localizedName(a)}</option>
                ))}
              </select>
              <button className="small primary" disabled={!supremeAttachmentId}
                onClick={() => run(() => api.setSignatureWeaponUpgrades(sheet.id, {
                  supremeAttachmentDefId: supremeAttachmentId,
                }))}>
                {t('Установить', 'Install')}
              </button>
            </div>
          )}
          <p className="hint small-text">
            {t('Improved даёт ровно одно: Укреплённое либо древнюю работу, которая заменяет прежнюю и отнимает слот. Supreme добавляет два слота и одно бесплатное улучшение редкости не выше 9. Оба выбора навсегда.',
              'Improved grants exactly one: Reinforced or Ancient craftsmanship, which replaces the previous one and costs a hard point. Supreme adds two hard points and one free attachment of rarity 9 or less. Both choices are permanent.')}
          </p>
        </div>
      )}

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
            {SIGNATURE_WEAPON_PROFILES.map(p => {
              const spec = SIGNATURE_WEAPON_PROFILE_LABELS[p]
              return (
                <label key={p}>
                  <input type="radio" name={`weapon-profile-${sheet.id}`} checked={profile === p}
                    onChange={() => setProfile(p)} /> {spec}
                </label>
              )
            })}
          </div>
          <div className="inline-form">
            <select value={craftsmanship}
              onChange={e => setCraftsmanship(e.target.value as WeaponCraftsmanship)}>
              {SIGNATURE_WEAPON_CRAFTSMANSHIPS.map(c => (
                <option key={c} value={c}>{WEAPON_CRAFTSMANSHIP_LABELS[c]}</option>
              ))}
            </select>
            <input value={form} maxLength={200} placeholder={t('форма оружия', 'weapon form')}
              onChange={e => setForm(e.target.value)} />
          </div>
          <p className="hint small-text craftsmanship-hint">
            <b>{t('Что даёт качество:', 'What the craftsmanship gives:')}</b>{' '}
            {SIGNATURE_WEAPON_CRAFTSMANSHIP_HINTS[craftsmanship]}
          </p>
          <div className="inline-form">
            {CONFIRMABLE_WEAPON_TRAITS.map(trait => (
              <label key={trait}>
                <input type="checkbox" checked={traits.includes(trait)} onChange={() => toggleTrait(trait)} />
                {' '}{WEAPON_TRAIT_LABELS[trait]}
              </label>
            ))}
          </div>
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
            <button className="small primary" disabled={!form.trim() || !baseAttachmentId}
              onClick={() => run(() => (editable
                ? api.setHeroicConfiguration(sheet.id, {
                  weaponProfile: profile,
                  craftsmanship,
                  narrativeForm: form.trim(),
                  formTraits: formatWeaponTraits(traits),
                  baseAttachmentDefId: baseAttachmentId,
                })
                : api.replaceSignatureWeapon(sheet.id, {
                  lost: false,
                  weaponProfile: profile,
                  craftsmanship,
                  narrativeForm: form.trim(),
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
    </div>
  )
}
