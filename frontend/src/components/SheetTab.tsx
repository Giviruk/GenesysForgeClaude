import { api } from '../api/client'
import type { CareerSkillSource, CharacterSheet, DefenseBreakdown, Derived, SkillKind } from '../api/types'
import {
  CHARACTERISTICS, CHARACTERISTIC_LABELS, CHARACTERISTIC_SHORT_LABELS, HEROIC_UPGRADE_LABELS,
  localizedDescription, localizedName, secondaryName, SKILL_KIND_LABELS,
} from '../utils/labels'
import { DicePoolView } from './DicePoolView'
import { CriticalInjuriesSection } from './CriticalInjuriesSection'
import { useDiceRoller } from '../dice-roller-store'
import { t } from '../i18n'

interface Props {
  sheet: CharacterSheet
  onError: (message: string) => void
  refresh: () => Promise<void>
}

// Левая колонка — крупный блок «общие»; правая — боевые, под ними знания/магия и
// социальные, чтобы плотно заполнить пространство и меньше скроллить.
const SKILL_COLUMNS: SkillKind[][] = [
  ['general'],
  ['combat', 'knowledge', 'magic', 'social'],
]

const CAREER_SOURCE_LABELS: Record<CareerSkillSource['source'], string> = t({
  Career: 'карьера', Species: 'вид', Talent: 'талант',
}, {
  Career: 'career', Species: 'species', Talent: 'talent',
})

/** Подсказка «почему навык карьерный»: перечисляет все источники статуса (ROT-CRE-01). */
function careerSourcesTitle(sources: CareerSkillSource[] | undefined): string | undefined {
  if (!sources?.length) return undefined
  return t('Карьерный навык: ', 'Career skill: ')
    + sources.map(s => `${CAREER_SOURCE_LABELS[s.source] ?? s.source} ${s.sourceName}`).join(', ')
}

export function SheetTab({ sheet, onError, refresh }: Props) {
  const { openRoller } = useDiceRoller()

  async function run(action: () => Promise<unknown>) {
    try {
      await action()
      await refresh()
    } catch (err) {
      onError(err instanceof Error ? err.message : t('Ошибка', 'Error'))
    }
  }

  const d = sheet.derived

  return (
    <div>
      <section className="stat-row">
        {CHARACTERISTICS.map(c => (
          <div key={c} className="stat-box characteristic">
            <div className="stat-value">{sheet.characteristics[c]}</div>
            <div className="stat-label">{CHARACTERISTIC_LABELS[c]}</div>
            {sheet.isCreationPhase && (
              <div className="buy-row">
                {sheet.characteristics[c] > sheet.archetype[c] && (
                  <button className="small" title={t(`Вернуть ${sheet.characteristics[c] * 10} XP`, `Refund ${sheet.characteristics[c] * 10} XP`)}
                    onClick={() => run(() => api.refundCharacteristic(sheet.id, c))}>
                    −
                  </button>
                )}
                <button className="small" title={t(`Повысить за ${(sheet.characteristics[c] + 1) * 10} XP`, `Increase for ${(sheet.characteristics[c] + 1) * 10} XP`)}
                  onClick={() => run(() => api.buyCharacteristic(sheet.id, c))}>
                  +{(sheet.characteristics[c] + 1) * 10} XP
                </button>
              </div>
            )}
          </div>
        ))}
      </section>

      <section className="stat-row derived">
        <DerivedBox label={t('Раны', 'Wounds')} value={`${sheet.woundsCurrent} / ${d.woundThreshold}`}
          onMinus={() => run(() => api.updateCharacter(sheet.id, { woundsCurrent: sheet.woundsCurrent - 1 }))}
          onPlus={() => run(() => api.updateCharacter(sheet.id, { woundsCurrent: sheet.woundsCurrent + 1 }))} />
        <DerivedBox label={t('Усталость', 'Strain')} value={`${sheet.strainCurrent} / ${d.strainThreshold}`}
          onMinus={() => run(() => api.updateCharacter(sheet.id, { strainCurrent: sheet.strainCurrent - 1 }))}
          onPlus={() => run(() => api.updateCharacter(sheet.id, { strainCurrent: sheet.strainCurrent + 1 }))} />
        <DerivedBox label={t('Поглощение', 'Soak')} value={String(d.soak)} />
        <DerivedBox label={t('Защита (ближ/дальн)', 'Defense (melee/ranged)')} value={`${d.meleeDefense} / ${d.rangedDefense}`}
          title={defenseTitle(d)} />
        <DerivedBox label={t('Переносимый вес', 'Encumbrance')} value={`${d.encumbranceLoad} / ${d.encumbranceThreshold}`}
          warning={d.encumbered ? t('Перегружен!', 'Encumbered!') : undefined} />
      </section>

      <CriticalInjuriesSection sheet={sheet} onError={onError} refresh={refresh} />

      {sheet.system === 'realmsOfTerrinoth' && <HeroicSummary sheet={sheet} />}

      <section className="panel">
        <h3>{t('Навыки', 'Skills')}</h3>
        <div className="skills-grid">
          {SKILL_COLUMNS.map((kinds, i) => (
            <div key={i} className="skill-column">
              {kinds.map(kind => {
                const skills = sheet.skills.filter(s => s.kind === kind)
                if (skills.length === 0) return null
                return (
                  <div key={kind} className="skill-block">
                    <h4 className="skill-kind">{SKILL_KIND_LABELS[kind]}</h4>
                    <table className="skills fixed">
                      {/* единые ширины колонок во всех разделах */}
                      <colgroup>
                        <col className="col-name" />
                        <col className="col-char" />
                        <col className="col-career" />
                        <col className="col-ranks" />
                        <col className="col-pool" />
                        <col className="col-action" />
                      </colgroup>
                      <thead>
                        <tr>
                          <th>{t('Навык', 'Skill')}</th>
                          <th>{t('Хар-ка', 'Char.')}</th>
                          <th className="centered" title={t('Карьерный навык', 'Career skill')}>{t('Карьерн.', 'Career')}</th>
                          <th>{t('Ранги', 'Ranks')}</th>
                          <th>{t('Пул кубов', 'Dice pool')}</th>
                          <th></th>
                        </tr>
                      </thead>
                      <tbody>
                        {skills.map(s => {
                          const label = localizedName(s)
                          const original = secondaryName(s)
                          return (
                            <tr key={s.skillDefId}>
                              <td className="ellipsis" title={original ? `${label} / ${original}` : label}>
                                {label}
                                {original && <span className="muted small-text name-secondary"> · {original}</span>}
                              </td>
                              <td className="muted" title={CHARACTERISTIC_LABELS[s.characteristic]}>
                                {CHARACTERISTIC_SHORT_LABELS[s.characteristic]}
                              </td>
                              <td className="centered" title={careerSourcesTitle(s.careerSources)}>{s.isCareer ? '✓' : ''}</td>
                              <td>{'●'.repeat(s.ranks)}{'○'.repeat(Math.max(0, 5 - s.ranks))}</td>
                              <td><DicePoolView pool={s.pool} /></td>
                              <td className="right">
                                <button className="small" title={t(`Бросить пул навыка «${label}»`, `Roll the "${label}" skill pool`)}
                                  onClick={() => openRoller({
                                    kind: 'roll',
                                    title: t('Бросок навыка', 'Skill check'),
                                    label,
                                    initialPool: { ability: s.pool.ability, proficiency: s.pool.proficiency },
                                  })}>
                                  🎲
                                </button>
                                {sheet.isCreationPhase && s.ranks > s.freeRanks && (
                                  <button className="small"
                                    title={t(`Вернуть ранг ${s.ranks} (+${s.ranks * 5 + (s.isCareer ? 0 : 5)} XP)`, `Refund rank ${s.ranks} (+${s.ranks * 5 + (s.isCareer ? 0 : 5)} XP)`)}
                                    onClick={() => run(() => api.refundSkillRank(sheet.id, s.skillDefId))}>
                                    −
                                  </button>
                                )}
                                {s.ranks < 5 && (
                                  <button className="small" disabled={s.nextRankCost > sheet.availableXp}
                                    title={s.nextRankCost > sheet.availableXp ? t('Недостаточно XP', 'Not enough XP') : t(`Купить ранг ${s.ranks + 1}`, `Buy rank ${s.ranks + 1}`)}
                                    onClick={() => run(() => api.buySkillRank(sheet.id, s.skillDefId))}>
                                    +{s.nextRankCost} XP
                                  </button>
                                )}
                              </td>
                            </tr>
                          )
                        })}
                      </tbody>
                    </table>
                  </div>
                )
              })}
            </div>
          ))}
        </div>
      </section>

    </div>
  )
}

function DerivedBox({ label, value, warning, title, onMinus, onPlus }: {
  label: string
  value: string
  warning?: string
  /** Подсказка при наведении: например, из чего сложилась защита. */
  title?: string
  onMinus?: () => void
  onPlus?: () => void
}) {
  return (
    <div className={warning ? 'stat-box warn' : 'stat-box'} title={title}>
      <div className="stat-value">
        {onMinus && <button className="tiny" onClick={onMinus}>−</button>}
        <span>{value}</span>
        {onPlus && <button className="tiny" onClick={onPlus}>+</button>}
      </div>
      <div className="stat-label">{label}</div>
      {warning && <div className="error small-text">{warning}</div>}
    </div>
  )
}

/**
 * Краткая сводка героической способности на листе: базовый эффект и только уже купленные
 * улучшения. Полное описание, покупка и настройка живут на отдельной вкладке — лист не должен
 * тонуть в тексте ещё не приобретённых улучшений.
 */
function HeroicSummary({ sheet }: { sheet: CharacterSheet }) {
  const h = sheet.heroicAbility
  const upgrades = sheet.heroicUpgrades

  if (!h) {
    return (
      <section className="panel">
        <h3>{t('Героическая способность', 'Heroic ability')}</h3>
        <p className="hint">
          {t('Не выбрана — откройте вкладку «Героика».', 'Not chosen — open the “Heroic” tab.')}
        </p>
      </section>
    )
  }

  const meta = [
    upgrades.story ? t('1 очко сюжета', '1 Story Point') : h.activationCost,
    h.activation,
    upgrades.durationRanks > 0
      ? t(`${h.duration} · +${upgrades.durationRanks} ход.`, `${h.duration} · +${upgrades.durationRanks} turn(s)`)
      : h.duration,
    upgrades.frequencyRanks > 0
      ? t(`${1 + upgrades.frequencyRanks} раз за сессию`, `${1 + upgrades.frequencyRanks} times per session`)
      : h.frequency,
  ].filter(Boolean).join(' · ')

  const purchased = h.upgrades.filter(u => u.level <= upgrades.powerRank)

  return (
    <section className="panel">
      <h3>{t('Героическая способность', 'Heroic ability')}</h3>
      <div className="heroic">
        <strong>{sheet.heroicIdentity?.customName || localizedName(h)}</strong>
        {sheet.heroicIdentity?.customName && (
          <div className="hint small-text">{t('Эффект:', 'Effect:')} {localizedName(h)}</div>
        )}
        <p>{localizedDescription(h)}</p>
        {meta && <div className="hint small-text">{meta}</div>}

        {purchased.map(u => (
          <div key={u.level} className="heroic-upgrade bought">
            <div className="heroic-upgrade-head">
              <strong>{HEROIC_UPGRADE_LABELS[u.level] ?? t(`Уровень ${u.level}`, `Level ${u.level}`)}</strong>
            </div>
            <p>{localizedDescription(u)}</p>
          </div>
        ))}

        {upgrades.secondaryEffects.map(effect => (
          <div key={effect.id} className="heroic-upgrade bought">
            <div className="heroic-upgrade-head"><strong>{localizedName(effect)}</strong></div>
            <p>{localizedDescription(effect)}</p>
          </div>
        ))}

        {(sheet.heroicIdentityIncomplete || sheet.heroicConfigurationIncomplete) && (
          <p className="hint small-text">
            {t('Способность настроена не до конца — откройте вкладку «Героика».',
              'The ability is not fully set up — open the “Heroic” tab.')}
          </p>
        )}
      </div>
    </section>
  )
}

/**
 * Объяснение итоговой защиты (ROT-CMB-03): что её задало, что проигнорировано (источники
 * «получает Defense N» не складываются) и упёрлось ли значение в предел 4.
 */
function defenseTitle(d: Derived): string | undefined {
  const channel = (label: string, b: DefenseBreakdown | null) => {
    if (!b) return null
    const parts: string[] = []
    if (b.provider) parts.push(`${b.provider.sourceName} ${b.provider.value}`)
    for (const inc of b.increases) parts.push(`${inc.sourceName} ${inc.value > 0 ? '+' : ''}${inc.value}`)
    if (parts.length === 0) parts.push(t('источников нет', 'no sources'))
    const ignored = b.ignoredProviders.length > 0
      ? ` · ${t('не складывается с', 'does not stack with')} ${b.ignoredProviders.map(x => x.sourceName).join(', ')}`
      : ''
    const capped = b.capped ? ` · ${t(`предел 4 (было бы ${b.raw})`, `capped at 4 (raw ${b.raw})`)}` : ''
    return `${label}: ${parts.join(' ')} = ${b.effective}${ignored}${capped}`
  }

  const lines = [
    channel(t('Ближняя', 'Melee'), d.meleeDefenseBreakdown),
    channel(t('Дальняя', 'Ranged'), d.rangedDefenseBreakdown),
  ].filter(Boolean)
  return lines.length > 0 ? lines.join('\n') : undefined
}
