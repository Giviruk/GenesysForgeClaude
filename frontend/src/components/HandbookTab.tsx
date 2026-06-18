import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type {
  AllowedState, ContentEntryType, ContentPackDetail, ContentPackEntry, ContentPackEntryInput,
  ContentPackListItem, GameSystem, HouseRuleCategory,
} from '../api/types'
import {
  ALLOWED_STATE_LABELS, ALLOWED_STATES, CONTENT_ENTRY_TYPE_LABELS, CONTENT_ENTRY_TYPES,
  HOUSE_RULE_CATEGORIES, HOUSE_RULE_CATEGORY_LABELS, SYSTEM_LABELS,
} from '../utils/labels'

interface Props {
  campaignId: string
  isGm: boolean
}

/** Вкладка Handbook кампании: список Content Pack и просмотр/редактирование выбранного. */
export function HandbookTab({ campaignId, isGm }: Props) {
  const [list, setList] = useState<ContentPackListItem[] | null>(null)
  const [openId, setOpenId] = useState<string | null>(null)
  const [creating, setCreating] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const reload = useCallback(() =>
    api.contentPacks(campaignId).then(setList).catch((e: unknown) =>
      setError(e instanceof Error ? e.message : 'Ошибка загрузки')),
    [campaignId])
  useEffect(() => { void reload() }, [reload])

  if (openId) {
    return <ContentPackView packId={openId} isGm={isGm}
      onBack={() => { setOpenId(null); void reload() }} />
  }

  return (
    <div className="handbook">
      {error && <div className="error">{error}</div>}
      <div className="page-head">
        <h3 className="inline-title">Справочник кампании (Content Packs)</h3>
        {isGm && <button className="primary" onClick={() => setCreating(v => !v)}>{creating ? 'Отмена' : '+ Создать Content Pack'}</button>}
      </div>
      {!isGm && <p className="hint">Здесь — разрешённый контент и домашние правила, опубликованные мастером.</p>}

      {creating && isGm && (
        <CreatePackForm onCreate={async (body) => {
          try {
            const p = await api.createContentPack(campaignId, body)
            setCreating(false); await reload(); setOpenId(p.id)
          } catch (e) { setError(e instanceof Error ? e.message : 'Ошибка') }
        }} />
      )}

      {list === null && <p className="muted">Загрузка…</p>}
      {list?.length === 0 && <p className="muted">{isGm ? 'Пока нет ни одного Content Pack.' : 'Мастер ещё не опубликовал справочник.'}</p>}
      <div className="card-grid">
        {list?.map(p => (
          <div key={p.id} className="char-card" onClick={() => setOpenId(p.id)}>
            <div className="char-card-head">
              <strong>{p.name}</strong>
              <span className={p.isPublicToCampaign ? 'badge tier' : 'badge'}>{p.isPublicToCampaign ? 'опубликован' : 'черновик'}</span>
            </div>
            <div className="muted small-text">{SYSTEM_LABELS[p.system]} · записей: {p.entryCount}</div>
          </div>
        ))}
      </div>
    </div>
  )
}

function CreatePackForm({ onCreate }: { onCreate: (b: { name: string; description: string; system: GameSystem }) => void }) {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [system, setSystem] = useState<GameSystem>('realmsOfTerrinoth')
  return (
    <form className="panel custom-form" onSubmit={(e: FormEvent) => { e.preventDefault(); onCreate({ name: name.trim(), description, system }) }}>
      <label>Название<input value={name} onChange={e => setName(e.target.value)} required /></label>
      <label>Описание<textarea rows={2} value={description} onChange={e => setDescription(e.target.value)} /></label>
      <label>Система
        <select value={system} onChange={e => setSystem(e.target.value as GameSystem)}>
          {(['realmsOfTerrinoth', 'genesysCore'] as GameSystem[]).map(s => <option key={s} value={s}>{SYSTEM_LABELS[s]}</option>)}
        </select>
      </label>
      <button className="primary" type="submit" disabled={!name.trim()}>Создать и открыть</button>
    </form>
  )
}

function ContentPackView({ packId, isGm, onBack }: { packId: string; isGm: boolean; onBack: () => void }) {
  const [pack, setPack] = useState<ContentPackDetail | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const [typeFilter, setTypeFilter] = useState<ContentEntryType | ''>('')
  const [stateFilter, setStateFilter] = useState<AllowedState | ''>('')
  const [adding, setAdding] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)

  const reload = useCallback(() =>
    api.contentPack(packId).then(setPack).catch((e: unknown) =>
      setError(e instanceof Error ? e.message : 'Ошибка загрузки')),
    [packId])
  useEffect(() => { void reload() }, [reload])

  const run = useCallback(async (action: () => Promise<unknown>) => {
    try {
      const r = await action()
      if (r && typeof r === 'object' && 'entries' in r) setPack(r as ContentPackDetail)
      else await reload()
      setError(null)
    } catch (e) { setError(e instanceof Error ? e.message : 'Ошибка') }
  }, [reload])

  const filtered = useMemo(() => {
    if (!pack) return []
    const term = search.trim().toLowerCase()
    return pack.entries.filter(e =>
      (!term || e.title.toLowerCase().includes(term))
      && (!typeFilter || e.contentType === typeFilter)
      && (!stateFilter || e.allowedState === stateFilter))
  }, [pack, search, typeFilter, stateFilter])

  if (!pack) {
    return <div><button onClick={onBack}>← К списку</button>{error && <div className="error">{error}</div>}</div>
  }

  return (
    <div className="content-pack">
      <div className="page-head">
        <div>
          <button onClick={onBack}>← К списку</button>
          <h3 className="inline-title">{pack.name}</h3>
          <span className={pack.isPublicToCampaign ? 'badge tier' : 'badge'}>{pack.isPublicToCampaign ? 'опубликован' : 'черновик'}</span>
        </div>
        {isGm && (
          <div className="head-actions">
            <button onClick={() => run(() => api.updateContentPack(pack.id, { isPublicToCampaign: !pack.isPublicToCampaign }))}>
              {pack.isPublicToCampaign ? 'Скрыть от игроков' : 'Опубликовать'}
            </button>
            <button className="danger" onClick={() => { if (confirm('Удалить Content Pack?')) { void run(() => api.deleteContentPack(pack.id)); onBack() } }}>Удалить</button>
          </div>
        )}
      </div>
      {error && <div className="error floating">{error}</div>}
      {pack.description && <p className="muted">{pack.description}</p>}

      <div className="filters">
        <input placeholder="Поиск по названию" value={search} onChange={e => setSearch(e.target.value)} />
        <select value={typeFilter} onChange={e => setTypeFilter(e.target.value as ContentEntryType | '')}>
          <option value="">Все типы</option>
          {CONTENT_ENTRY_TYPES.map(t => <option key={t} value={t}>{CONTENT_ENTRY_TYPE_LABELS[t]}</option>)}
        </select>
        <select value={stateFilter} onChange={e => setStateFilter(e.target.value as AllowedState | '')}>
          <option value="">Любой статус</option>
          {ALLOWED_STATES.map(s => <option key={s} value={s}>{ALLOWED_STATE_LABELS[s]}</option>)}
        </select>
        {isGm && <button className="primary small" onClick={() => { setAdding(v => !v); setEditingId(null) }}>{adding ? 'Отмена' : '+ Запись'}</button>}
      </div>

      {isGm && adding && (
        <EntryForm onSubmit={async (input) => { await run(() => api.addContentPackEntry(pack.id, input)); setAdding(false) }} />
      )}

      {filtered.length === 0 && <p className="muted">Записей нет.</p>}
      <div className="entry-cards">
        {filtered.map(e => editingId === e.id ? (
          <EntryForm key={e.id} initial={e}
            onSubmit={async (input) => { await run(() => api.updateContentPackEntry(pack.id, e.id, input)); setEditingId(null) }}
            onCancel={() => setEditingId(null)} />
        ) : (
          <EntryCard key={e.id} entry={e} isGm={isGm}
            onEdit={() => { setEditingId(e.id); setAdding(false) }}
            onDelete={() => { if (confirm(`Удалить «${e.title}»?`)) void run(() => api.removeContentPackEntry(pack.id, e.id)) }} />
        ))}
      </div>
    </div>
  )
}

function EntryCard({ entry, isGm, onEdit, onDelete }: {
  entry: ContentPackEntry; isGm: boolean; onEdit: () => void; onDelete: () => void
}) {
  const stateClass = entry.allowedState === 'allowed' ? 'badge tier'
    : entry.allowedState === 'disallowed' ? 'badge danger-badge' : 'badge'
  return (
    <div className="entry-card">
      <div className="entry-head">
        <strong>{entry.title}</strong>
        <span className="badge custom">{CONTENT_ENTRY_TYPE_LABELS[entry.contentType]}</span>
        <span className={stateClass}>{ALLOWED_STATE_LABELS[entry.allowedState]}</span>
        {entry.contentType === 'houseRule' && entry.category !== 'none' && (
          <span className="badge">{HOUSE_RULE_CATEGORY_LABELS[entry.category]}</span>
        )}
      </div>
      {entry.safeSummary && <p className="small-text">{entry.safeSummary}</p>}
      {(entry.source || entry.pageRef) && (
        <div className="muted small-text">Источник: {entry.source}{entry.pageRef && `, ${entry.pageRef}`}</div>
      )}
      {entry.playerNotes && <div className="small-text">📝 {entry.playerNotes}</div>}
      {isGm && entry.gmNotes && <div className="pc-notes small-text">GM: {entry.gmNotes}</div>}
      {entry.tags.length > 0 && <div className="tag-row">{entry.tags.map(t => <span key={t} className="badge custom">{t}</span>)}</div>}
      {isGm && (
        <div className="card-actions">
          <button className="small" onClick={onEdit}>Изменить</button>
          <button className="danger small" onClick={onDelete}>Удалить</button>
        </div>
      )}
    </div>
  )
}

function EntryForm({ initial, onSubmit, onCancel }: {
  initial?: ContentPackEntry; onSubmit: (input: ContentPackEntryInput) => void; onCancel?: () => void
}) {
  const [contentType, setContentType] = useState<ContentEntryType>(initial?.contentType ?? 'talent')
  const [title, setTitle] = useState(initial?.title ?? '')
  const [allowedState, setAllowedState] = useState<AllowedState>(initial?.allowedState ?? 'allowed')
  const [category, setCategory] = useState<HouseRuleCategory>(
    initial?.category && initial.category !== 'none' ? initial.category : 'custom')
  const [safeSummary, setSafeSummary] = useState(initial?.safeSummary ?? '')
  const [source, setSource] = useState(initial?.source ?? '')
  const [pageRef, setPageRef] = useState(initial?.pageRef ?? '')
  const [gmNotes, setGmNotes] = useState(initial?.gmNotes ?? '')
  const [playerNotes, setPlayerNotes] = useState(initial?.playerNotes ?? '')
  const [tagsText, setTagsText] = useState(initial?.tags.join(', ') ?? '')

  const submit = (e: FormEvent) => {
    e.preventDefault()
    onSubmit({
      contentType, title: title.trim(), allowedState,
      category: contentType === 'houseRule' ? category : null,
      safeSummary, source, pageRef, gmNotes, playerNotes,
      tags: tagsText.split(',').map(t => t.trim()).filter(Boolean),
    })
  }

  return (
    <form className="panel custom-form" onSubmit={submit}>
      <div className="form-row">
        <label className="grow">Тип
          <select value={contentType} onChange={e => setContentType(e.target.value as ContentEntryType)}>
            {CONTENT_ENTRY_TYPES.map(t => <option key={t} value={t}>{CONTENT_ENTRY_TYPE_LABELS[t]}</option>)}
          </select>
        </label>
        <label className="grow">Статус
          <select value={allowedState} onChange={e => setAllowedState(e.target.value as AllowedState)}>
            {ALLOWED_STATES.map(s => <option key={s} value={s}>{ALLOWED_STATE_LABELS[s]}</option>)}
          </select>
        </label>
        {contentType === 'houseRule' && (
          <label className="grow">Категория
            <select value={category} onChange={e => setCategory(e.target.value as HouseRuleCategory)}>
              {HOUSE_RULE_CATEGORIES.map(c => <option key={c} value={c}>{HOUSE_RULE_CATEGORY_LABELS[c]}</option>)}
            </select>
          </label>
        )}
      </div>
      <label>Название<input value={title} onChange={e => setTitle(e.target.value)} required /></label>
      <label>Краткое описание (без копирования текста книг)<textarea rows={2} value={safeSummary} onChange={e => setSafeSummary(e.target.value)} /></label>
      <div className="form-row">
        <label className="grow">Источник<input value={source} onChange={e => setSource(e.target.value)} /></label>
        <label className="grow">Страница<input value={pageRef} onChange={e => setPageRef(e.target.value)} /></label>
      </div>
      <div className="form-row">
        <label className="grow">Заметки для игроков<textarea rows={2} value={playerNotes} onChange={e => setPlayerNotes(e.target.value)} /></label>
        <label className="grow">Заметки GM (приватно)<textarea rows={2} value={gmNotes} onChange={e => setGmNotes(e.target.value)} /></label>
      </div>
      <label>Теги (через запятую)<input value={tagsText} onChange={e => setTagsText(e.target.value)} /></label>
      <div className="card-actions">
        <button className="primary" type="submit" disabled={!title.trim()}>{initial ? 'Сохранить' : 'Добавить запись'}</button>
        {onCancel && <button type="button" onClick={onCancel}>Отмена</button>}
      </div>
    </form>
  )
}
