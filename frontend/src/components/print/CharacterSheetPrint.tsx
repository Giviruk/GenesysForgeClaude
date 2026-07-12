import { useEffect, useState } from 'react'
import { api } from '../../api/client'
import type { CharacterNote, CharacterSheet, ItemState, Reference, SheetSkill, SkillKind } from '../../api/types'
import {
  CHARACTERISTICS, CHARACTERISTIC_LABELS, ITEM_STATE_LABELS, resolveWeaponSkillName, secondaryName,
  SKILL_KIND_LABELS, SYSTEM_LABELS, localizedName,
} from '../../utils/labels'
import { DicePoolView } from '../DicePoolView'
import { t } from '../../i18n'

const ITEM_STATE_ORDER: ItemState[] = ['equipped', 'carried', 'backpack']
const SKILL_KIND_ORDER: SkillKind[] = ['general', 'combat', 'social', 'knowledge', 'magic']

/** Полный печатный лист персонажа для PrintPreview (→ браузерная печать / сохранение в PDF). */
export function CharacterSheetPrint({ sheet, loadNotes = true }: {
  sheet: CharacterSheet
  reference?: Reference
  /** false для публичного share-листа: заметки закрыты auth-endpoint и не входят в публичный DTO. */
  loadNotes?: boolean
}) {
  const [notes, setNotes] = useState<CharacterNote[]>([])
  useEffect(() => {
    if (!loadNotes) return
    // Заметки лежат в отдельном endpoint; для печати не критичны — ошибку игнорируем.
    api.notes(sheet.id).then(setNotes).catch(() => { /* пусто */ })
  }, [loadNotes, sheet.id])

  const printableNotes = loadNotes ? notes : []
  const skillNames = sheet.skills.map(s => s.name)
  const skillsByName = new Map(sheet.skills.map(s => [s.name, s]))
  const skillGroups = SKILL_KIND_ORDER
    .map(kind => ({ kind, skills: sheet.skills.filter(skill => skill.kind === kind) }))
    .filter(group => group.skills.length > 0)
  const skillColumns = balanceSkillGroups(skillGroups)
  const d = sheet.derived
  const h = sheet.heroicAbility

  return (
    <div className="sheet-doc">
      <header className="sheet-head">
        <h1>{sheet.name}</h1>
        <div className="sheet-sub">
          {SYSTEM_LABELS[sheet.system]} · {localizedName(sheet.archetype)} · {localizedName(sheet.career)}
          {sheet.isCreationPhase && t(' · фаза создания', ' · creation phase')}
        </div>
        <div className="sheet-xp">
          {t(`XP: всего ${sheet.totalXp} · потрачено ${sheet.spentXp} · доступно ${sheet.availableXp}`,
             `XP: total ${sheet.totalXp} · spent ${sheet.spentXp} · available ${sheet.availableXp}`)}
          {'   ·   '}{t('Деньги:', 'Money:')} {sheet.money}
        </div>
      </header>

      <section className="sheet-stat-block" aria-label={t('Характеристики и производные показатели', 'Characteristics and derived stats')}>
        <div className="sheet-stat-grid">
          {CHARACTERISTICS.map(c => (
            <div key={c} className="sheet-stat">
              <span className="sheet-stat-value">{sheet.characteristics[c]}</span>
              <span className="sheet-stat-label">{CHARACTERISTIC_LABELS[c]}</span>
            </div>
          ))}
        </div>
        <div className="sheet-stat-grid sheet-derived-grid">
          <DerivedStat value={`${sheet.woundsCurrent} / ${d.woundThreshold}`} label={t('Раны', 'Wounds')} />
          <DerivedStat value={`${sheet.strainCurrent} / ${d.strainThreshold}`} label={t('Усталость', 'Strain')} />
          <DerivedStat value={d.soak} label={t('Поглощение', 'Soak')} />
          <DerivedStat value={d.meleeDefense} label={t('Ближняя защита', 'Melee defense')} />
          <DerivedStat value={d.rangedDefense} label={t('Дальняя защита', 'Ranged defense')} />
          <DerivedStat value={`${d.encumbranceLoad} / ${d.encumbranceThreshold}`} label={t('Нагрузка', 'Encumbrance')}
            warning={d.encumbered} />
        </div>
      </section>

      <section className="sheet-section">
        <h2>{t('Навыки', 'Skills')}</h2>
        <div className="sheet-skill-columns">
          {skillColumns.map((column, index) => (
            <div key={index} className="sheet-skill-column">
              {column.map(group => (
                <SkillGroup key={group.kind} kind={group.kind} skills={group.skills} />
              ))}
            </div>
          ))}
        </div>
      </section>

      {sheet.talents.length > 0 && (
        <section className="sheet-section">
          <h2>{t('Таланты', 'Talents')}</h2>
          {sheet.talents.map(tal => (
            <div key={tal.talentDefId} className="sheet-entry">
              <strong>{localizedName(tal)}</strong>
              <span className="sheet-meta">
                {' · '}{t('уровень', 'tier')} {tal.tier}{tal.isRanked ? t(` · рангов ${tal.ranks}`, ` · ranks ${tal.ranks}`) : ''}
                {tal.activation ? ` · ${tal.activation}` : ''}
              </span>
              {tal.description && <div className="sheet-desc">{tal.description}</div>}
            </div>
          ))}
        </section>
      )}

      {h && (
        <section className="sheet-section">
          <h2>{t('Героическая способность', 'Heroic ability')}</h2>
          <div className="sheet-entry">
            <strong>{localizedName(h)}</strong>
            <span className="sheet-meta">
              {[h.activation, h.duration, h.frequency].filter(Boolean).map(x => ` · ${x}`).join('')}
              {sheet.heroicUpgradeRank > 0 && t(` · улучшение ${sheet.heroicUpgradeRank}`, ` · upgrade ${sheet.heroicUpgradeRank}`)}
            </span>
            {h.description && <div className="sheet-desc">{h.description}</div>}
            {h.upgrades.filter(u => u.level <= sheet.heroicUpgradeRank).map(u => (
              <div key={u.level} className="sheet-desc">
                ↑ {u.level === 1 ? t('Улучшенная', 'Improved') : t('Высшая', 'Supreme')}: {u.description}
              </div>
            ))}
          </div>
        </section>
      )}

      <section className="sheet-section">
        <h2>{t('Инвентарь', 'Inventory')}</h2>
        {sheet.items.length === 0 && <p className="muted">—</p>}
        {ITEM_STATE_ORDER.map(state => {
          const items = sheet.items.filter(i => i.state === state)
          if (items.length === 0) return null
          return (
            <div key={state} className="sheet-inv-group">
              <h3>{ITEM_STATE_LABELS[state]}</h3>
              {items.map(i => {
                const itemLabel = localizedName(i)
                const weaponSkillName = i.kind === 'weapon'
                  ? resolveWeaponSkillName(i.skillName, skillNames)
                  : null
                const weaponSkill = weaponSkillName ? skillsByName.get(weaponSkillName) : null
                const weaponSkillLabel = weaponSkill ? localizedName(weaponSkill) : ''
                const combat = [i.damage && t(`урон ${i.damage}`, `damage ${i.damage}`), i.crit && t(`крит ${i.crit}`, `crit ${i.crit}`), i.rangeBand, weaponSkillLabel]
                  .filter(Boolean).join(', ')
                const armor = [
                  i.soakBonus ? t(`поглощение +${i.soakBonus}`, `soak +${i.soakBonus}`) : '',
                  i.meleeDefense ? t(`защ. ближ. +${i.meleeDefense}`, `melee def. +${i.meleeDefense}`) : '',
                  i.rangedDefense ? t(`защ. дальн. +${i.rangedDefense}`, `ranged def. +${i.rangedDefense}`) : '',
                ].filter(Boolean).join(', ')
                const itemOriginal = secondaryName(i)
                return (
                  <div key={i.id} className="sheet-entry">
                    <strong>{itemLabel}</strong>
                    {itemOriginal && <span className="sheet-meta"> · {itemOriginal}</span>}
                    <span className="sheet-meta">
                      {' '}×{i.quantity} · {t('нагрузка', 'enc.')} {i.encumbrance}
                      {combat ? ` · ${combat}` : ''}
                      {armor ? ` · ${armor}` : ''}
                      {i.properties ? ` · ${i.properties}` : ''}
                    </span>
                    {i.kind === 'weapon' && (
                      <div className="sheet-weapon-pool">
                        <span className="sheet-weapon-pool-label">{t('Пул', 'Pool')}</span>
                        {weaponSkill
                          ? <><DicePoolView pool={weaponSkill.pool} /><span>{localizedName(weaponSkill)}</span></>
                          : <span className="muted">—{i.skillName ? t(` навык ${i.skillName} не найден`, ` skill ${i.skillName} not found`) : ''}</span>}
                      </div>
                    )}
                  </div>
                )
              })}
            </div>
          )
        })}
      </section>

      {sheet.criticalInjuries.length > 0 && (
        <section className="sheet-section">
          <h2>{t('Критические ранения', 'Critical injuries')}</h2>
          {sheet.criticalInjuries.map(ci => (
            <div key={ci.id} className="sheet-entry">
              <strong>{ci.nameRu}</strong>
              <span className="sheet-meta">
                {ci.severity ? ` · ${ci.severity}` : ''}
                {ci.rollResult != null ? t(` · бросок ${ci.rollResult}`, ` · roll ${ci.rollResult}`) : ''}
              </span>
              {ci.notes && <div className="sheet-desc">{ci.notes}</div>}
            </div>
          ))}
        </section>
      )}

      {(sheet.desire || sheet.fear || sheet.strength || sheet.flaw || sheet.background) && (
        <section className="sheet-section">
          <h2>{t('Образ персонажа', 'Character bio')}</h2>
          {([
            [t('Стремление', 'Desire'), sheet.desire],
            [t('Страх', 'Fear'), sheet.fear],
            [t('Сильная сторона', 'Strength'), sheet.strength],
            [t('Слабость', 'Flaw'), sheet.flaw],
          ] as const).filter(([, v]) => v).map(([label, value]) => (
            <div key={label} className="sheet-entry">
              <strong>{label}:</strong> <span className="sheet-meta">{value}</span>
            </div>
          ))}
          {sheet.background && (
            <div className="sheet-entry">
              <strong>{t('Предыстория', 'Background')}</strong>
              <div className="sheet-desc sheet-prewrap">{sheet.background}</div>
            </div>
          )}
        </section>
      )}

      {printableNotes.length > 0 && (
        <section className="sheet-section">
          <h2>{t('Заметки', 'Notes')}</h2>
          {printableNotes.map(n => (
            <div key={n.id} className="sheet-entry">
              <strong>{n.title}</strong>
              {n.body && <div className="sheet-desc">{n.body}</div>}
            </div>
          ))}
        </section>
      )}
    </div>
  )
}

function DerivedStat({ value, label, warning = false }: { value: string | number; label: string; warning?: boolean }) {
  return (
    <div className={`sheet-stat${warning ? ' warning' : ''}`}>
      <span className="sheet-stat-value">{value}</span>
      <span className="sheet-stat-label">{label}</span>
    </div>
  )
}

interface SkillGroupData {
  kind: SkillKind
  skills: SheetSkill[]
}

function balanceSkillGroups(groups: SkillGroupData[]): SkillGroupData[][] {
  if (groups.length < 2) return [groups]

  const total = groups.reduce((sum, group) => sum + group.skills.length, 0)
  let leftCount = 0
  let splitAt = 1
  let smallestDifference = Number.POSITIVE_INFINITY

  for (let index = 1; index < groups.length; index += 1) {
    leftCount += groups[index - 1].skills.length
    const difference = Math.abs(total - leftCount * 2)
    if (difference < smallestDifference) {
      smallestDifference = difference
      splitAt = index
    }
  }

  return [groups.slice(0, splitAt), groups.slice(splitAt)]
}

function SkillGroup({ kind, skills }: SkillGroupData) {
  return (
    <section className="sheet-skill-group">
      <h3>{SKILL_KIND_LABELS[kind]}</h3>
      <table className="sheet-table">
        <thead>
          <tr><th>{t('Навык', 'Skill')}</th><th>{t('Хар.', 'Char.')}</th><th>{t('Кар.', 'Career')}</th><th>{t('Ранг', 'Rank')}</th><th>{t('Пул', 'Pool')}</th></tr>
        </thead>
        <tbody>
          {skills.map(skill => (
            <tr key={skill.skillDefId}>
              <td>
                {localizedName(skill)}
                {secondaryName(skill) && <span className="sheet-meta"> · {secondaryName(skill)}</span>}
              </td>
              <td>{CHARACTERISTIC_LABELS[skill.characteristic]}</td>
              <td>{skill.isCareer ? '✓' : ''}</td>
              <td>{skill.ranks}</td>
              <td><DicePoolView pool={skill.pool} /></td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  )
}
