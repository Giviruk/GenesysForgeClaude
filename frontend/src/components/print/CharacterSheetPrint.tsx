import { useEffect, useState } from 'react'
import { api } from '../../api/client'
import type { CharacterNote, CharacterSheet, ItemState, Reference, SheetSkill, SkillKind } from '../../api/types'
import {
  CHARACTERISTICS, CHARACTERISTIC_LABELS, ITEM_STATE_LABELS, resolveWeaponSkillName, SKILL_KIND_LABELS,
  SYSTEM_LABELS,
} from '../../utils/labels'
import { DicePoolView } from '../DicePoolView'

const ITEM_STATE_ORDER: ItemState[] = ['equipped', 'carried', 'backpack']
const SKILL_KIND_ORDER: SkillKind[] = ['general', 'combat', 'social', 'knowledge', 'magic']

/** Полный печатный лист персонажа для PrintPreview (→ браузерная печать / сохранение в PDF). */
export function CharacterSheetPrint({ sheet, reference }: { sheet: CharacterSheet; reference: Reference }) {
  const [notes, setNotes] = useState<CharacterNote[]>([])
  useEffect(() => {
    // Заметки лежат в отдельном endpoint; для печати не критичны — ошибку игнорируем.
    api.notes(sheet.id).then(setNotes).catch(() => { /* пусто */ })
  }, [sheet.id])

  const skillRu = new Map(reference.skills.map(s => [s.id, s.nameRu]))
  const itemRu = new Map(reference.items.map(i => [i.id, i.nameRu]))
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
          {SYSTEM_LABELS[sheet.system]} · {sheet.archetype.name} · {sheet.career.name}
          {sheet.isCreationPhase && ' · фаза создания'}
        </div>
        <div className="sheet-xp">
          XP: всего {sheet.totalXp} · потрачено {sheet.spentXp} · доступно {sheet.availableXp}
          {'   ·   '}Деньги: {sheet.money}
        </div>
      </header>

      <section className="sheet-stat-block" aria-label="Характеристики и производные показатели">
        <div className="sheet-stat-grid">
          {CHARACTERISTICS.map(c => (
            <div key={c} className="sheet-stat">
              <span className="sheet-stat-value">{sheet.characteristics[c]}</span>
              <span className="sheet-stat-label">{CHARACTERISTIC_LABELS[c]}</span>
            </div>
          ))}
        </div>
        <div className="sheet-stat-grid sheet-derived-grid">
          <DerivedStat value={`${sheet.woundsCurrent} / ${d.woundThreshold}`} label="Раны" />
          <DerivedStat value={`${sheet.strainCurrent} / ${d.strainThreshold}`} label="Стрейн" />
          <DerivedStat value={d.soak} label="Поглощение" />
          <DerivedStat value={d.meleeDefense} label="Ближняя защита" />
          <DerivedStat value={d.rangedDefense} label="Дальняя защита" />
          <DerivedStat value={`${d.encumbranceLoad} / ${d.encumbranceThreshold}`} label="Нагрузка"
            warning={d.encumbered} />
        </div>
      </section>

      <section className="sheet-section">
        <h2>Навыки</h2>
        <div className="sheet-skill-columns">
          {skillColumns.map((column, index) => (
            <div key={index} className="sheet-skill-column">
              {column.map(group => (
                <SkillGroup key={group.kind} kind={group.kind} skills={group.skills} skillRu={skillRu} />
              ))}
            </div>
          ))}
        </div>
      </section>

      {sheet.talents.length > 0 && (
        <section className="sheet-section">
          <h2>Таланты</h2>
          {sheet.talents.map(t => (
            <div key={t.talentDefId} className="sheet-entry">
              <strong>{t.nameRu || t.name}</strong>
              <span className="sheet-meta">
                {' · '}уровень {t.tier}{t.isRanked ? ` · рангов ${t.ranks}` : ''}
                {t.activation ? ` · ${t.activation}` : ''}
              </span>
              {t.description && <div className="sheet-desc">{t.description}</div>}
            </div>
          ))}
        </section>
      )}

      {h && (
        <section className="sheet-section">
          <h2>Героическая способность</h2>
          <div className="sheet-entry">
            <strong>{h.nameRu || h.name}</strong>
            <span className="sheet-meta">
              {[h.activation, h.duration, h.frequency].filter(Boolean).map(x => ` · ${x}`).join('')}
              {sheet.heroicUpgradeRank > 0 && ` · улучшение ${sheet.heroicUpgradeRank}`}
            </span>
            {h.description && <div className="sheet-desc">{h.description}</div>}
            {h.upgrades.filter(u => u.level <= sheet.heroicUpgradeRank).map(u => (
              <div key={u.level} className="sheet-desc">
                ↑ {u.level === 1 ? 'Improved' : 'Supreme'}: {u.description}
              </div>
            ))}
          </div>
        </section>
      )}

      <section className="sheet-section">
        <h2>Инвентарь</h2>
        {sheet.items.length === 0 && <p className="muted">—</p>}
        {ITEM_STATE_ORDER.map(state => {
          const items = sheet.items.filter(i => i.state === state)
          if (items.length === 0) return null
          return (
            <div key={state} className="sheet-inv-group">
              <h3>{ITEM_STATE_LABELS[state]}</h3>
              {items.map(i => {
                const ru = itemRu.get(i.itemDefId) || ''
                const weaponSkillName = i.kind === 'weapon'
                  ? resolveWeaponSkillName(i.skillName, skillNames)
                  : null
                const weaponSkill = weaponSkillName ? skillsByName.get(weaponSkillName) : null
                const combat = [i.damage && `урон ${i.damage}`, i.crit && `крит ${i.crit}`, i.rangeBand, i.skillName]
                  .filter(Boolean).join(', ')
                const armor = [
                  i.soakBonus ? `поглощение +${i.soakBonus}` : '',
                  i.meleeDefense ? `защ. ближ. +${i.meleeDefense}` : '',
                  i.rangedDefense ? `защ. дальн. +${i.rangedDefense}` : '',
                ].filter(Boolean).join(', ')
                return (
                  <div key={i.id} className="sheet-entry">
                    <strong>{ru ? `${ru} (${i.name})` : i.name}</strong>
                    <span className="sheet-meta">
                      {' '}×{i.quantity} · нагрузка {i.encumbrance}
                      {combat ? ` · ${combat}` : ''}
                      {armor ? ` · ${armor}` : ''}
                      {i.properties ? ` · ${i.properties}` : ''}
                    </span>
                    {i.kind === 'weapon' && (
                      <div className="sheet-weapon-pool">
                        <span className="sheet-weapon-pool-label">Пул</span>
                        {weaponSkill
                          ? <><DicePoolView pool={weaponSkill.pool} /><span>{weaponSkill.name}</span></>
                          : <span className="muted">—{i.skillName ? ` навык ${i.skillName} не найден` : ''}</span>}
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
          <h2>Критические ранения</h2>
          {sheet.criticalInjuries.map(ci => (
            <div key={ci.id} className="sheet-entry">
              <strong>{ci.nameRu}</strong>
              <span className="sheet-meta">
                {ci.severity ? ` · ${ci.severity}` : ''}
                {ci.rollResult != null ? ` · бросок ${ci.rollResult}` : ''}
              </span>
              {ci.notes && <div className="sheet-desc">{ci.notes}</div>}
            </div>
          ))}
        </section>
      )}

      {(sheet.desire || sheet.fear || sheet.strength || sheet.flaw || sheet.background) && (
        <section className="sheet-section">
          <h2>Образ персонажа</h2>
          {([
            ['Стремление', sheet.desire],
            ['Страх', sheet.fear],
            ['Сильная сторона', sheet.strength],
            ['Слабость', sheet.flaw],
          ] as const).filter(([, v]) => v).map(([label, value]) => (
            <div key={label} className="sheet-entry">
              <strong>{label}:</strong> <span className="sheet-meta">{value}</span>
            </div>
          ))}
          {sheet.background && (
            <div className="sheet-entry">
              <strong>Предыстория</strong>
              <div className="sheet-desc sheet-prewrap">{sheet.background}</div>
            </div>
          )}
        </section>
      )}

      {notes.length > 0 && (
        <section className="sheet-section">
          <h2>Заметки</h2>
          {notes.map(n => (
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

function SkillGroup({ kind, skills, skillRu }: SkillGroupData & { skillRu: Map<string, string> }) {
  return (
    <section className="sheet-skill-group">
      <h3>{SKILL_KIND_LABELS[kind]}</h3>
      <table className="sheet-table">
        <thead>
          <tr><th>Навык</th><th>Хар.</th><th>Кар.</th><th>Ранг</th><th>Пул</th></tr>
        </thead>
        <tbody>
          {skills.map(skill => {
            const ru = skillRu.get(skill.skillDefId) || ''
            return (
              <tr key={skill.skillDefId}>
                <td>{ru ? `${ru} (${skill.name})` : skill.name}</td>
                <td>{CHARACTERISTIC_LABELS[skill.characteristic]}</td>
                <td>{skill.isCareer ? '✓' : ''}</td>
                <td>{skill.ranks}</td>
                <td><DicePoolView pool={skill.pool} /></td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </section>
  )
}
