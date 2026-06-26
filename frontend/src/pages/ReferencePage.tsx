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

// Короткие подписи для переключателя разделов.
const KIND_TAB_LABELS: Record<RuleTableKind, string> = {
  difficulty: 'Сложности',
  symbolSpend: 'Траты',
  rangeBand: 'Дистанции',
  criticalInjury: 'Криты',
}

type Section = RuleTableKind | 'all'
type Polarity = 'all' | 'positive' | 'negative'

const RANGE_SUBS = ['Общая информация', 'Перемещение'] as const
type RangeSub = (typeof RANGE_SUBS)[number]

// Полярность траты определяем по символам в стоимости (Threat/Despair → негатив).
const isNegativeSpend = (e: RuleTableEntry) => /threat|despair|угроз|крах/i.test(e.symbolCost)

const HIT_GROUP_ORDER = ['Правила', 'Навыки', 'Таланты', 'Предметы', 'Качества',
  'Архетипы', 'Карьеры', 'Героика', 'NPC', 'Персонажи']

export function ReferencePage({ onNavigate }: { onNavigate: (to: string) => void }) {
  const [system, setSystem] = useState<GameSystem>('realmsOfTerrinoth')
  const [rules, setRules] = useState<RuleTableEntry[]>([])
  const [filter, setFilter] = useState('')
  const [section, setSection] = useState<Section>('difficulty')
  const [spendSituation, setSpendSituation] = useState<string>('all')
  const [spendPolarity, setSpendPolarity] = useState<Polarity>('all')
  const [rangeSub, setRangeSub] = useState<RangeSub>('Общая информация')
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

  // Счётчики по видам (по всему набору, не зависят от фильтра) — для подписей переключателя.
  const kindCounts = useMemo(() => {
    const m = new Map<RuleTableKind, number>()
    for (const e of rules) m.set(e.kind, (m.get(e.kind) ?? 0) + 1)
    return m
  }, [rules])

  // Список ситуаций для под-фильтра «Траты» (по данным, чтобы подписи совпадали с groupRu).
  const spendSituations = useMemo(() => {
    const s = new Set<string>()
    for (const e of rules) if (e.kind === 'symbolSpend' && e.groupRu) s.add(e.groupRu)
    return [...s]
  }, [rules])

  // Итоговые таблицы с учётом активного раздела и под-фильтров (траты/дистанции).
  const tables = useMemo(() => {
    const visibleKinds = KIND_ORDER.filter(k => section === 'all' || k === section)
    return visibleKinds.map(kind => {
      let entries = byKind.get(kind) ?? []
      let showCost = true
      if (kind === 'symbolSpend' && section === 'symbolSpend') {
        if (spendSituation !== 'all') entries = entries.filter(e => e.groupRu === spendSituation)
        if (spendPolarity !== 'all') entries = entries.filter(e =>
          spendPolarity === 'negative' ? isNegativeSpend(e) : !isNegativeSpend(e))
      }
      if (kind === 'rangeBand' && section === 'rangeBand') {
        entries = entries.filter(e => e.groupRu === rangeSub)
        showCost = rangeSub === 'Перемещение' // в «Общей информации» колонки стоимости нет
      }
      return { kind, entries, showCost }
    }).filter(t => t.entries.length > 0)
  }, [byKind, section, spendSituation, spendPolarity, rangeSub])

  const renderedCount = tables.reduce((n, t) => n + t.entries.length, 0)

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
        {rules.length > 0 && (
          <div className="section-switch" role="tablist">
            <button role="tab" aria-selected={section === 'all'}
                    className={section === 'all' ? 'tab active' : 'tab'} onClick={() => setSection('all')}>
              Все <span className="muted">({rules.length})</span>
            </button>
            {KIND_ORDER.map(kind => {
              const count = kindCounts.get(kind) ?? 0
              if (count === 0) return null
              return (
                <button key={kind} role="tab" aria-selected={section === kind}
                        className={section === kind ? 'tab active' : 'tab'} onClick={() => setSection(kind)}>
                  {KIND_TAB_LABELS[kind]} <span className="muted">({count})</span>
                </button>
              )
            })}
          </div>
        )}
        {section === 'symbolSpend' && (
          <>
            <div className="section-switch" role="tablist" aria-label="Ситуация">
              <button role="tab" aria-selected={spendSituation === 'all'}
                      className={spendSituation === 'all' ? 'tab active' : 'tab'}
                      onClick={() => setSpendSituation('all')}>Все ситуации</button>
              {spendSituations.map(s => (
                <button key={s} role="tab" aria-selected={spendSituation === s}
                        className={spendSituation === s ? 'tab active' : 'tab'}
                        onClick={() => setSpendSituation(s)}>{s}</button>
              ))}
            </div>
            <div className="section-switch" role="tablist" aria-label="Символы">
              <button role="tab" aria-selected={spendPolarity === 'all'}
                      className={spendPolarity === 'all' ? 'tab active' : 'tab'}
                      onClick={() => setSpendPolarity('all')}>Любые символы</button>
              <button role="tab" aria-selected={spendPolarity === 'positive'}
                      className={spendPolarity === 'positive' ? 'tab active' : 'tab'}
                      onClick={() => setSpendPolarity('positive')}>Преимущества и триумфы</button>
              <button role="tab" aria-selected={spendPolarity === 'negative'}
                      className={spendPolarity === 'negative' ? 'tab active' : 'tab'}
                      onClick={() => setSpendPolarity('negative')}>Угрозы и крахи</button>
            </div>
          </>
        )}
        {section === 'rangeBand' && (
          <div className="section-switch" role="tablist" aria-label="Раздел дистанций">
            {RANGE_SUBS.map(sub => (
              <button key={sub} role="tab" aria-selected={rangeSub === sub}
                      className={rangeSub === sub ? 'tab active' : 'tab'}
                      onClick={() => setRangeSub(sub)}>{sub}</button>
            ))}
          </div>
        )}
        <input
          className="search"
          type="search"
          placeholder="Фильтр по таблицам правил…"
          value={filter}
          onChange={e => setFilter(e.target.value)}
        />
        {rules.length === 0 && !error && <p className="muted">Загрузка таблиц…</p>}
        {tables.map(t => (
          <RuleTable key={t.kind} kind={t.kind} entries={t.entries} showCost={t.showCost} />
        ))}
        {rules.length > 0 && renderedCount === 0 && (
          <p className="muted">
            {filter.trim() ? `Нет строк по фильтру «${filter.trim()}».` : 'В этом разделе нет строк.'}
          </p>
        )}
      </div>
    </div>
  )
}

function RuleTable({ kind, entries, showCost = true }:
  { kind: RuleTableKind; entries: RuleTableEntry[]; showCost?: boolean }) {
  // Заголовок первой колонки зависит от вида таблицы.
  const firstCol = kind === 'criticalInjury' ? 'Бросок'
    : kind === 'symbolSpend' ? 'Ситуация'
    : kind === 'difficulty' ? 'Сложность'
    : kind === 'rangeBand' ? 'Название' : 'Диапазон'
  const costCol = kind === 'criticalInjury' ? 'Сложность'
    : kind === 'difficulty' ? 'Кубы'
    : kind === 'rangeBand' ? 'Манёвры' : 'Стоимость'

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
            {showCost && <th>{costCol}</th>}
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
              {showCost && <td className="ref-cost">{e.symbolCost}</td>}
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
