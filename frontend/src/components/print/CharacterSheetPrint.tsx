import { useEffect, useState } from 'react'
import { api } from '../../api/client'
import type { CharacterNote, CharacterSheet, ItemState, Reference } from '../../api/types'
import { CHARACTERISTICS, CHARACTERISTIC_LABELS, ITEM_STATE_LABELS, SYSTEM_LABELS } from '../../utils/labels'
import { DicePoolView } from '../DicePoolView'

const ITEM_STATE_ORDER: ItemState[] = ['equipped', 'carried', 'backpack']

/** Полный печатный лист персонажа для PrintPreview (→ браузерная печать / сохранение в PDF). */
export function CharacterSheetPrint({ sheet, reference }: { sheet: CharacterSheet; reference: Reference }) {
  const [notes, setNotes] = useState<CharacterNote[]>([])
  useEffect(() => {
    // Заметки лежат в отдельном endpoint; для печати не критичны — ошибку игнорируем.
    api.notes(sheet.id).then(setNotes).catch(() => { /* пусто */ })
  }, [sheet.id])

  const skillRu = new Map(reference.skills.map(s => [s.id, s.nameRu]))
  const itemRu = new Map(reference.items.map(i => [i.id, i.nameRu]))
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

      <section className="sheet-section">
        <h2>Характеристики</h2>
        <div className="sheet-chars">
          {CHARACTERISTICS.map(c => (
            <div key={c} className="sheet-char">
              <span className="v">{sheet.characteristics[c]}</span>
              {CHARACTERISTIC_LABELS[c]}
            </div>
          ))}
        </div>
      </section>

      <section className="sheet-section">
        <h2>Производные характеристики</h2>
        <div className="sheet-derived">
          <span>Раны: {sheet.woundsCurrent}/{d.woundThreshold}</span>
          <span>Стрейн: {sheet.strainCurrent}/{d.strainThreshold}</span>
          <span>Поглощение: {d.soak}</span>
          <span>Защита ближняя: {d.meleeDefense}</span>
          <span>Защита дальняя: {d.rangedDefense}</span>
          <span>Порог нагрузки: {d.encumbranceThreshold}</span>
          <span>Текущая нагрузка: {d.encumbranceLoad}{d.encumbered ? ' — перегруз!' : ''}</span>
        </div>
      </section>

      <section className="sheet-section">
        <h2>Навыки</h2>
        <table className="sheet-table">
          <thead>
            <tr><th>Навык</th><th>Хар-ка</th><th>Карьерный</th><th>Ранги</th><th>Пул</th></tr>
          </thead>
          <tbody>
            {sheet.skills.map(s => {
              const ru = skillRu.get(s.skillDefId) || ''
              return (
                <tr key={s.skillDefId}>
                  <td>{ru ? `${ru} (${s.name})` : s.name}</td>
                  <td>{CHARACTERISTIC_LABELS[s.characteristic]}</td>
                  <td>{s.isCareer ? '✓' : ''}</td>
                  <td>{s.ranks}</td>
                  <td><DicePoolView pool={s.pool} /></td>
                </tr>
              )
            })}
          </tbody>
        </table>
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
                  </div>
                )
              })}
            </div>
          )
        })}
      </section>

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
