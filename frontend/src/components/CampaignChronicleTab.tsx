import { useCallback, useEffect, useId, useMemo, useRef, useState, type KeyboardEvent } from 'react'
import { api } from '../api/client'
import type { CampaignChronicleChapter, CampaignChronicleRevision, CampaignMember, NpcListItem } from '../api/types'
import { t } from '../i18n'
import { navigate } from '../router'
import { MarkdownContent, type MarkdownEntityLink } from './MarkdownContent'
import { markdownHeadings } from '../utils/markdown'
import { findChronicleMention, replaceChronicleMention, type ChronicleMention } from '../utils/chronicleMentions'

interface Props {
  campaignId: string
  members: CampaignMember[]
  refreshSignal: number
  onOpenCharacter: (characterId: string, name: string) => void
  onError: (message: string) => void
}

type ViewMode = 'write' | 'preview' | 'split'
type MentionOption = { kind: 'character' | 'npc'; id: string; name: string }

export function CampaignChronicleTab({ campaignId, members, refreshSignal, onOpenCharacter, onError }: Props) {
  const [chapters, setChapters] = useState<CampaignChronicleChapter[]>([])
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [title, setTitle] = useState('')
  const [content, setContent] = useState('')
  const [dirty, setDirty] = useState(false)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [creating, setCreating] = useState(false)
  const [mode, setMode] = useState<ViewMode>('split')
  const [history, setHistory] = useState<CampaignChronicleRevision[] | null>(null)
  const [historyOpen, setHistoryOpen] = useState(false)
  const [npcs, setNpcs] = useState<NpcListItem[]>([])
  const [characterTarget, setCharacterTarget] = useState('')
  const [npcTarget, setNpcTarget] = useState('')
  const [npcPickerReset, setNpcPickerReset] = useState(0)
  const [mention, setMention] = useState<ChronicleMention | null>(null)
  const [mentionIndex, setMentionIndex] = useState(0)
  const textarea = useRef<HTMLTextAreaElement>(null)
  const selectedIdRef = useRef<string | null>(null)

  const selected = chapters.find(chapter => chapter.id === selectedId) ?? null
  const mentionOptions = useMemo<MentionOption[]>(() => {
    if (!mention) return []
    const query = mention.query.trim().toLocaleLowerCase()
    return [
      ...members.map(member => ({ kind: 'character' as const, id: member.characterId, name: member.characterName })),
      ...npcs.map(npc => ({ kind: 'npc' as const, id: npc.id, name: npc.name })),
    ].filter(option => !query || option.name.toLocaleLowerCase().includes(query)).slice(0, 8)
  }, [members, npcs, mention])

  const load = useCallback(async () => {
    try {
      const data = await api.campaignChronicle(campaignId)
      setChapters(data)
      const nextId = selectedIdRef.current && data.some(chapter => chapter.id === selectedIdRef.current)
        ? selectedIdRef.current : data[0]?.id ?? null
      const chapter = data.find(item => item.id === nextId)
      selectedIdRef.current = nextId
      setSelectedId(nextId)
      if (chapter) { setTitle(chapter.title); setContent(chapter.content); setHistory(null) }
      setMention(null)
      setLoading(false)
    } catch (error) {
      setLoading(false)
      onError(error instanceof Error ? error.message : t('Не удалось загрузить хронику', 'Could not load chronicle'))
    }
  }, [campaignId, onError])

  useEffect(() => {
    const timer = window.setTimeout(() => { void load() }, 0)
    return () => window.clearTimeout(timer)
  }, [load])
  useEffect(() => {
    if (dirty || refreshSignal <= 0) return
    const timer = window.setTimeout(() => { void load() }, 0)
    return () => window.clearTimeout(timer)
  }, [refreshSignal, dirty, load])
  useEffect(() => {
    void api.npcs().then(setNpcs)
      .catch(() => setNpcs([]))
  }, [])

  function chooseChapter(id: string) {
    if (dirty && !confirm(t('Отменить несохранённые изменения?', 'Discard unsaved changes?'))) return
    const chapter = chapters.find(item => item.id === id)
    setDirty(false)
    selectedIdRef.current = id
    setSelectedId(id)
    if (chapter) { setTitle(chapter.title); setContent(chapter.content) }
    setHistoryOpen(false)
    setHistory(null)
    setMention(null)
  }

  async function createChapter() {
    const name = prompt(t('Название новой главы', 'New chapter title'), t('Новая глава', 'New chapter'))?.trim()
    if (!name) return
    try {
      setCreating(true)
      const chapter = await api.createChronicleChapter(campaignId, { title: name, content: `# ${name}\n\n` })
      setChapters(current => [...current, chapter])
      selectedIdRef.current = chapter.id
      setSelectedId(chapter.id)
      setTitle(chapter.title)
      setContent(chapter.content)
      setDirty(false)
      setMention(null)
    } catch (error) {
      onError(error instanceof Error ? error.message : t('Не удалось создать главу', 'Could not create chapter'))
    } finally { setCreating(false) }
  }

  async function save() {
    if (!selectedId || !title.trim()) return
    try {
      setSaving(true)
      const chapter = await api.updateChronicleChapter(campaignId, selectedId, {
        title, content, expectedVersion: selected?.currentVersion ?? 1,
      })
      setChapters(current => current.map(item => item.id === chapter.id ? chapter : item))
      setTitle(chapter.title)
      setContent(chapter.content)
      setDirty(false)
      setHistory(null)
    } catch (error) {
      onError(error instanceof Error ? error.message : t('Не удалось сохранить главу', 'Could not save chapter'))
    } finally { setSaving(false) }
  }

  function insert(markdown: string, selectPlaceholder?: string) {
    const element = textarea.current
    const start = element?.selectionStart ?? content.length
    const end = element?.selectionEnd ?? start
    const next = content.slice(0, start) + markdown + content.slice(end)
    setContent(next)
    setDirty(true)
    requestAnimationFrame(() => {
      element?.focus()
      const placeholderAt = selectPlaceholder ? markdown.indexOf(selectPlaceholder) : -1
      const from = start + (placeholderAt >= 0 ? placeholderAt : markdown.length)
      element?.setSelectionRange(from, from + (placeholderAt >= 0 ? selectPlaceholder!.length : 0))
    })
  }

  function insertWebLink() {
    const label = prompt(t('Текст ссылки', 'Link text'), t('ссылка', 'link'))
    if (!label) return
    const href = prompt(t('Адрес ссылки (https://…)', 'Link address (https://…)'), 'https://')
    if (href) insert(`[${label}](${href})`)
  }

  function insertCharacter() {
    const member = members.find(item => item.characterId === characterTarget)
    if (!member) return
    insert(`[${member.characterName}](character:${member.characterId})`)
    setCharacterTarget('')
  }

  function insertNpc() {
    const npc = npcs.find(item => item.id === npcTarget)
    if (!npc) return
    insert(`[${npc.name}](npc:${npc.id})`)
    setNpcTarget('')
    setNpcPickerReset(current => current + 1)
  }

  function updateMention(nextContent: string, cursor: number) {
    setMention(findChronicleMention(nextContent, cursor))
    setMentionIndex(0)
  }

  function selectMention(option: MentionOption) {
    if (!mention) return
    const target = option.kind === 'character' ? `character:${option.id}` : `npc:${option.id}`
    const result = replaceChronicleMention(content, mention, option.name, target)
    setContent(result.text)
    setDirty(true)
    setMention(null)
    requestAnimationFrame(() => {
      textarea.current?.focus()
      textarea.current?.setSelectionRange(result.cursor, result.cursor)
    })
  }

  function handleEditorKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (!mention) return
    if (event.key === 'Escape') { event.preventDefault(); setMention(null); return }
    if (mentionOptions.length === 0) {
      if (event.key === 'Enter' || event.key === 'Tab') setMention(null)
      return
    }
    if (event.key === 'ArrowDown') {
      event.preventDefault()
      setMentionIndex(index => (index + 1) % mentionOptions.length)
    } else if (event.key === 'ArrowUp') {
      event.preventDefault()
      setMentionIndex(index => (index - 1 + mentionOptions.length) % mentionOptions.length)
    } else if (event.key === 'Enter' || event.key === 'Tab') {
      event.preventDefault()
      selectMention(mentionOptions[mentionIndex] ?? mentionOptions[0])
    }
  }

  function openEntity(link: MarkdownEntityLink) {
    if (link.kind === 'npc') { navigate(`/npcs/${link.id}`); return }
    const member = members.find(item => item.characterId === link.id)
    if (member) onOpenCharacter(member.characterId, member.characterName)
  }

  async function toggleHistory() {
    const open = !historyOpen
    setHistoryOpen(open)
    if (!open || history || !selectedId) return
    try { setHistory(await api.chronicleHistory(campaignId, selectedId)) }
    catch (error) { onError(error instanceof Error ? error.message : t('Не удалось загрузить историю', 'Could not load history')) }
  }

  async function restore(revision: CampaignChronicleRevision) {
    if (!selectedId || !confirm(t(`Восстановить версию ${revision.version}? Текущее состояние сохранится в истории.`,
      `Restore version ${revision.version}? The current state will remain in history.`))) return
    try {
      const chapter = await api.restoreChronicleRevision(campaignId, selectedId, revision.id)
      setChapters(current => current.map(item => item.id === chapter.id ? chapter : item))
      setTitle(chapter.title); setContent(chapter.content); setDirty(false)
      setHistory(await api.chronicleHistory(campaignId, selectedId))
    } catch (error) { onError(error instanceof Error ? error.message : t('Не удалось восстановить версию', 'Could not restore version')) }
  }

  return <div className="chronicle-layout">
    <aside className="chronicle-toc">
      <div className="chronicle-toc-head"><b>{t('Оглавление', 'Contents')}</b>
        <button className="small primary" onClick={() => void createChapter()} disabled={creating}>＋ {t('Глава', 'Chapter')}</button>
      </div>
      {chapters.map((chapter, index) => <div key={chapter.id}>
        <button className={chapter.id === selectedId ? 'chronicle-chapter active' : 'chronicle-chapter'}
          onClick={() => chooseChapter(chapter.id)}>{index + 1}. {chapter.title}</button>
        {chapter.id === selectedId && markdownHeadings(content).filter(item => item.level > 1).map(heading =>
          <a className="chronicle-heading" style={{ paddingLeft: `${(heading.level - 1) * 10 + 12}px` }}
            key={`${heading.level}-${heading.id}`} href={`#${heading.id}`}>{heading.text}</a>)}
      </div>)}
      {!loading && chapters.length === 0 && <p className="muted">{t('Хроника пока пуста.', 'The chronicle is empty.')}</p>}
    </aside>

    <section className="chronicle-workspace">
      {!selected && !loading ? <div className="campaign-empty chronicle-empty">
        <h3>{t('Начните хронику приключения', 'Start the adventure chronicle')}</h3>
        <p>{t('Создайте первую главу — редактировать её смогут все участники кампании.',
          'Create the first chapter — every campaign member will be able to edit it.')}</p>
        <button className="primary" onClick={() => void createChapter()}>＋ {t('Создать главу', 'Create chapter')}</button>
      </div> : selected && <>
        <div className="chronicle-title-row">
          <input aria-label={t('Название главы', 'Chapter title')} value={title}
            onChange={event => { setTitle(event.target.value); setDirty(true) }} />
          <span className="muted">v{selected.currentVersion} · {selected.updatedBy}</span>
          <button onClick={() => void toggleHistory()}>{t('История', 'History')}</button>
          <button className="primary" onClick={() => void save()} disabled={!dirty || saving || !title.trim()}>
            {saving ? t('Сохранение…', 'Saving…') : t('Сохранить', 'Save')}
          </button>
        </div>

        <div className="chronicle-toolbar" aria-label={t('Быстрые действия Markdown', 'Markdown quick actions')}>
          <button onClick={() => insert('\n## Новая сцена\n', 'Новая сцена')}>H2 {t('Раздел', 'Section')}</button>
          <button onClick={() => insert('\n\nНовый абзац\n\n', 'Новый абзац')}>¶ {t('Абзац', 'Paragraph')}</button>
          <button onClick={() => insert('\n- Первый пункт\n- Второй пункт\n', 'Первый пункт')}>• {t('Список', 'List')}</button>
          <button onClick={insertWebLink}>🔗 {t('Ссылка', 'Link')}</button>
          <span className="chronicle-picker"><select aria-label={t('Персонаж для ссылки', 'Character link')}
            value={characterTarget} onChange={event => setCharacterTarget(event.target.value)}>
            <option value="">{t('Персонаж…', 'Character…')}</option>
            {members.map(member => <option key={member.characterId} value={member.characterId}>{member.characterName}</option>)}
          </select><button onClick={insertCharacter} disabled={!characterTarget}>＋</button></span>
          <span className="chronicle-picker chronicle-npc-picker">
            <NpcCombobox key={npcPickerReset} items={npcs} value={npcTarget} onChange={setNpcTarget} />
            <button onClick={insertNpc} disabled={!npcTarget}>＋</button>
          </span>
          <span className="chronicle-mode">
            {(['write', 'split', 'preview'] as ViewMode[]).map(value => <button key={value}
              className={mode === value ? 'active' : ''} onClick={() => setMode(value)}>
              {value === 'write' ? t('Текст', 'Write') : value === 'split' ? t('Оба', 'Split') : t('Просмотр', 'Preview')}
            </button>)}
          </span>
        </div>

        <div className={`chronicle-editor mode-${mode}`}>
          {mode !== 'preview' && <div className="chronicle-write-pane"><textarea ref={textarea} value={content} spellCheck
            aria-label={t('Markdown-текст главы', 'Chapter Markdown')}
            onChange={event => {
              setContent(event.target.value); setDirty(true)
              updateMention(event.target.value, event.target.selectionStart)
            }}
            onClick={event => updateMention(event.currentTarget.value, event.currentTarget.selectionStart)}
            onKeyUp={event => {
              if (!['ArrowDown', 'ArrowUp', 'Enter', 'Tab', 'Escape'].includes(event.key))
                updateMention(event.currentTarget.value, event.currentTarget.selectionStart)
            }}
            onKeyDown={handleEditorKeyDown} />
            {mention && <div className="chronicle-mentions" role="listbox" aria-label={t('Упоминания', 'Mentions')}>
              {mentionOptions.map((option, index) => <button type="button" role="option"
                aria-selected={index === mentionIndex} className={index === mentionIndex ? 'active' : ''}
                key={`${option.kind}-${option.id}`} onMouseDown={event => event.preventDefault()}
                onClick={() => selectMention(option)}>
                <span className="chronicle-mention-kind">{option.kind === 'character' ? t('Персонаж', 'Character') : 'NPC'}</span>
                <b>{option.name}</b>
              </button>)}
              {mentionOptions.length === 0 && <div className="chronicle-mention-empty">{t('Совпадений нет', 'No matches')}</div>}
            </div>}
          </div>}
          {mode !== 'write' && <div className="chronicle-preview"><MarkdownContent markdown={content} onEntityLink={openEntity} /></div>}
        </div>

        {historyOpen && <div className="chronicle-history">
          <h3>{t('История изменений', 'Change history')}</h3>
          {!history && <p className="muted">{t('Загрузка…', 'Loading…')}</p>}
          {history?.map(revision => <details key={revision.id}>
            <summary><b>v{revision.version}</b> · {revision.editedBy} · {new Date(revision.editedAt).toLocaleString()}</summary>
            <div className="chronicle-history-actions"><span>{revision.title}</span>
              {revision.version !== selected.currentVersion && <button onClick={() => void restore(revision)}>{t('Восстановить', 'Restore')}</button>}
            </div>
            <MarkdownContent markdown={revision.content} onEntityLink={openEntity} />
          </details>)}
        </div>}
      </>}
    </section>
  </div>
}

function NpcCombobox({ items, value, onChange }: {
  items: NpcListItem[]
  value: string
  onChange: (value: string) => void
}) {
  const listId = useId()
  const selected = items.find(item => item.id === value)
  const [query, setQuery] = useState(selected?.name ?? '')
  const [open, setOpen] = useState(false)
  const [activeIndex, setActiveIndex] = useState(0)
  const matches = useMemo(() => {
    const normalized = query.trim().toLocaleLowerCase()
    return items.filter(item => !normalized || item.name.toLocaleLowerCase().includes(normalized)).slice(0, 12)
  }, [items, query])

  function choose(item: NpcListItem) {
    onChange(item.id)
    setQuery(item.name)
    setOpen(false)
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'Escape') { setOpen(false); return }
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault()
      setOpen(true)
      setActiveIndex(index => event.key === 'ArrowDown'
        ? (index + 1) % Math.max(matches.length, 1)
        : (index - 1 + Math.max(matches.length, 1)) % Math.max(matches.length, 1))
    } else if (event.key === 'Enter' && open && matches.length > 0) {
      event.preventDefault()
      choose(matches[activeIndex] ?? matches[0])
    }
  }

  return <span className="chronicle-combobox">
    <input type="search" role="combobox" aria-label={t('NPC для ссылки', 'NPC link')}
      aria-expanded={open} aria-controls={listId} aria-autocomplete="list"
      aria-activedescendant={open && matches[activeIndex] ? `${listId}-${matches[activeIndex].id}` : undefined}
      placeholder={t('Найти NPC…', 'Find NPC…')} value={query}
      onFocus={event => { event.currentTarget.select(); setOpen(true); setActiveIndex(0) }}
      onBlur={() => setOpen(false)} onKeyDown={handleKeyDown}
      onChange={event => { setQuery(event.target.value); onChange(''); setOpen(true); setActiveIndex(0) }} />
    {open && <span className="chronicle-combobox-options" role="listbox" id={listId}
      aria-label={t('Результаты поиска NPC', 'NPC search results')}>
      {matches.map((item, index) => <button type="button" role="option" id={`${listId}-${item.id}`}
        aria-selected={item.id === value} className={index === activeIndex ? 'active' : ''} key={item.id}
        onMouseDown={event => event.preventDefault()} onClick={() => choose(item)}>
        <span>{item.name}</span>
        {!item.isBuiltIn && <small>{item.isMine ? t('Свой', 'Mine') : t('Кампанийный', 'Campaign')}</small>}
      </button>)}
      {matches.length === 0 && <span className="chronicle-combobox-empty">{t('NPC не найдены', 'No NPC found')}</span>}
    </span>}
  </span>
}
