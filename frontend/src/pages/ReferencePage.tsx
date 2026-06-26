import { useEffect, useMemo, useState } from 'react'
import { api } from '../api/client'
import type { GameSystem, RuleTableEntry, RuleTableKind, SearchHit } from '../api/types'
import { SYSTEM_LABELS } from '../utils/labels'

/**
 * Справочник правил (U-11): таблицы сложностей / трат символов / дистанций / критических ранений
 * + глобальный поиск по справочнику, контенту системы и сущностям пользователя.
 */

const KIND_ORDER: RuleTableKind[] = ['difficulty', 'symbolSpend', 'rangeBand', 'criticalInjury']

const KIND_LABELS: Record<RuleTableKind, string> = {
  difficulty: 'Сложности',
  symbolSpend: 'Траты символов',
  rangeBand: 'Дистанции',
  criticalInjury: 'Критические ранения (d100)',
}

const HIT_GROUP_ORDER = ['Правила', 'Навыки', 'Таланты', 'Предметы', 'Качества',
  'Архетипы', 'Карьеры', 'Героика', 'NPC', 'Персонажи']

export function ReferencePage({ onNavigate }: { onNavigate: (to: string) => void }) {
  const [system, setSystem] = useState<GameSystem>('realmsOfTerrinoth')
  const [rules, setRules] = useState<RuleTableEntry[]>([])
  const [filter, setFilter] = useState('')
  const [query, setQuery] = useState('')
  const [hits, setHits] = useState<SearchHit[] | null>(null)
  const [searching, setSearching] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api.rules().then(r => setRules(r.entries)).catch(e => setError(e.message))
  }, [])

  // Глобальный поиск с дебаунсом; короткий запрос (<2) сбрасывает результаты.
  // Все обновления состояния — в колбэке таймера (не в теле эффекта).
  useEffect(() => {
    const q = query.trim()
    let active = true
    const t = setTimeout(() => {
      if (q.length < 2) { setHits(null); setSearching(false); return }
      setSearching(true)
      api.search(system, q)
        .then(r => { if (active) setHits(r.hits) })
        .catch(e => { if (active) setError(e.message) })
        .finally(() => { if (active) setSearching(false) })
    }, 250)
    return () => { active = false; clearTimeout(t) }
  }, [query, system])

  const filteredRules = useMemo(() => {
    const f = filter.trim().toLowerCase()
    if (!f) return rules
    return rules.filter(r =>
      (r.nameRu + r.nameEn + r.groupRu + r.symbolCost + r.body + r.notes).toLowerCase().includes(f))
  }, [rules, filter])

  const byKind = useMemo(() => {
    const map = new Map<RuleTableKind, RuleTableEntry[]>()
    for (const e of filteredRules) {
      const arr = map.get(e.kind) ?? []
      arr.push(e)
      map.set(e.kind, arr)
    }
    return map
  }, [filteredRules])

  const groupedHits = useMemo(() => {
    if (!hits) return []
    const map = new Map<string, SearchHit[]>()
    for (const h of hits) {
      const arr = map.get(h.group) ?? []
      arr.push(h)
      map.set(h.group, arr)
    }
    return [...map.entries()].sort((a, b) =>
      HIT_GROUP_ORDER.indexOf(a[0]) - HIT_GROUP_ORDER.indexOf(b[0]))
  }, [hits])

  return (
    <div className="page">
      <div className="page-head">
        <h2>Справочник правил</h2>
        <div className="system-switch">
          {(['realmsOfTerrinoth', 'genesysCore'] as GameSystem[]).map(s => (
            <button key={s} className={system === s ? 'tab active' : 'tab'} onClick={() => setSystem(s)}>
              {SYSTEM_LABELS[s]}
            </button>
          ))}
        </div>
      </div>

      {error && <div className="error floating">{error}</div>}

      <div className="panel">
        <input
          className="search"
          type="search"
          placeholder="Глобальный поиск: правила, навыки, таланты, предметы, NPC, персонажи…"
          value={query}
          onChange={e => setQuery(e.target.value)}
        />
        {searching && <p className="muted">Поиск…</p>}
        {hits && !searching && hits.length === 0 && (
          <p className="muted">Ничего не найдено по запросу «{query.trim()}».</p>
        )}
        {hits && hits.length > 0 && (
          <div className="search-results">
            {groupedHits.map(([group, items]) => (
              <div key={group} className="search-group">
                <h4>{group} <span className="muted">({items.length})</span></h4>
                <ul className="search-hits">
                  {items.map((h, i) => (
                    <li key={`${h.type}-${i}`}
                        className={h.route ? 'search-hit clickable' : 'search-hit'}
                        onClick={() => h.route && onNavigate(h.route)}>
                      <span className="hit-title">{h.title}</span>
                      {h.subtitle && <span className="hit-subtitle"> · {h.subtitle}</span>}
                      {h.snippet && <span className="hit-snippet">{h.snippet}</span>}
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>
        )}
      </div>

      <div className="panel">
        <input
          className="search"
          type="search"
          placeholder="Фильтр по таблицам правил…"
          value={filter}
          onChange={e => setFilter(e.target.value)}
        />
        {rules.length === 0 && !error && <p className="muted">Загрузка таблиц…</p>}
        {KIND_ORDER.map(kind => {
          const entries = byKind.get(kind)
          if (!entries || entries.length === 0) return null
          return <RuleTable key={kind} kind={kind} entries={entries} />
        })}
        {rules.length > 0 && filteredRules.length === 0 && (
          <p className="muted">Нет строк по фильтру «{filter.trim()}».</p>
        )}
      </div>
    </div>
  )
}

function RuleTable({ kind, entries }: { kind: RuleTableKind; entries: RuleTableEntry[] }) {
  // Заголовок первой колонки зависит от вида таблицы.
  const firstCol = kind === 'criticalInjury' ? 'Бросок'
    : kind === 'symbolSpend' ? 'Ситуация'
    : kind === 'difficulty' ? 'Сложность' : 'Диапазон'
  const costCol = kind === 'criticalInjury' ? 'Сложность'
    : kind === 'difficulty' ? 'Кубы' : 'Стоимость'

  const firstValue = (e: RuleTableEntry) =>
    kind === 'criticalInjury' ? e.rollRange
    : kind === 'symbolSpend' ? e.groupRu
    : e.nameRu

  return (
    <section className="rule-table">
      <h3>{KIND_LABELS[kind]} <span className="muted">({entries.length})</span></h3>
      <table className="ref-table">
        <thead>
          <tr>
            <th>{firstCol}</th>
            {kind === 'criticalInjury' && <th>Название</th>}
            <th>{costCol}</th>
            <th>Описание</th>
          </tr>
        </thead>
        <tbody>
          {entries.map(e => (
            <tr key={e.code}>
              <td className="ref-first">{firstValue(e)}</td>
              {kind === 'criticalInjury' && (
                <td>{e.nameRu}{e.groupRu && <span className="muted"> · {e.groupRu}</span>}</td>
              )}
              <td className="ref-cost">{e.symbolCost}</td>
              <td>
                {e.body}
                {e.notes && <div className="muted ref-notes">{e.notes}</div>}
                {e.source && <div className="muted ref-source">{e.source}{e.sourcePage && `, с. ${e.sourcePage}`}</div>}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  )
}
