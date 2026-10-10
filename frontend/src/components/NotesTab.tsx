import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type { CharacterNote } from '../api/types'
import { lang, t } from '../i18n'
import { Icon } from './Icon'
import { SectionCard } from './content/ContentUi'

export function NotesTab({ characterId, onError }: { characterId: string; onError: (message: string) => void }) {
  const [notes, setNotes] = useState<CharacterNote[] | null>(null)
  const [editing, setEditing] = useState<CharacterNote | null>(null)
  const [open, setOpen] = useState(false)
  const [deleting, setDeleting] = useState<string | null>(null)
  const [title, setTitle] = useState('')
  const [body, setBody] = useState('')
  const [busy, setBusy] = useState(false)
  const reload = useCallback(() => api.notes(characterId).then(setNotes)
    .catch((err: unknown) => onError(err instanceof Error ? err.message : t('Ошибка загрузки заметок', 'Failed to load notes'))), [characterId, onError])
  useEffect(() => { void reload() }, [reload])
  function resetForm() { setEditing(null); setOpen(false); setTitle(''); setBody('') }
  async function submit(e: FormEvent) {
    e.preventDefault()
    if (busy || (!title.trim() && !body.trim())) return
    setBusy(true)
    try {
      const heading = title.trim() || t('Без заголовка', 'Untitled')
      if (editing) await api.updateNote(characterId, editing.id, heading, body)
      else await api.createNote(characterId, heading, body)
      resetForm(); await reload()
    } catch (err) { onError(err instanceof Error ? err.message : t('Ошибка сохранения', 'Failed to save')) }
    finally { setBusy(false) }
  }
  function startEdit(note: CharacterNote) { setEditing(note); setOpen(false); setTitle(note.title); setBody(note.body) }
  async function remove(note: CharacterNote) {
    setBusy(true)
    try { await api.deleteNote(characterId, note.id); if (editing?.id === note.id) resetForm(); setDeleting(null); await reload() }
    catch (err) { onError(err instanceof Error ? err.message : t('Ошибка удаления', 'Failed to delete')) }
    finally { setBusy(false) }
  }
  const fmt = (iso: string) => new Date(iso).toLocaleString(lang === 'ru' ? 'ru-RU' : 'en-US', { dateStyle: 'short', timeStyle: 'short' })
  const editor = <form className="note-editor" onSubmit={submit}>
    <label>{t('Заголовок', 'Title')}<input value={title} disabled={busy} onChange={e => setTitle(e.target.value)} maxLength={200} /></label>
    <label>{t('Текст', 'Text')}<textarea value={body} disabled={busy} onChange={e => setBody(e.target.value)} rows={4} /></label>
    <div className="form-actions"><button className="primary small" type="submit" disabled={busy || (!title.trim() && !body.trim())}>{editing ? t('Сохранить', 'Save') : t('Добавить', 'Add')}</button>
      <button type="button" className="small" disabled={busy} onClick={resetForm}>{t('Отмена', 'Cancel')}</button></div>
  </form>
  return <SectionCard title={<>{t('Заметки', 'Notes')} <small className="muted">{notes?.length ?? ''}</small></>} icon="book"
    actions={<button className="small" disabled={busy} onClick={() => { resetForm(); setOpen(!open) }}><Icon name="plus" className="button-icon" />{t('Новая', 'New')}</button>}>
    <p className="muted small-text"><Icon name="lock" className="button-icon" />{t('Видите только вы — ведущему и в кампании не показываются', 'Only you can see these — hidden from the GM and campaign')}</p>
    {open && editor}
    {notes === null && <p className="muted">{t('Загрузка…', 'Loading…')}</p>}
    {notes?.length === 0 && <p className="muted">{t('Заметок пока нет', 'No notes yet')}</p>}
    <div className="notes-list">{notes?.map(n => <div key={n.id} className="note-card">
      {editing?.id === n.id ? editor : <><div className="note-card-head"><strong>{n.title}</strong><span className="note-actions">
        <button className="small" disabled={busy} onClick={() => startEdit(n)}>{t('Изменить', 'Edit')}</button>
        <button className="small" disabled={busy} onClick={() => setDeleting(n.id)}>{t('Удалить', 'Delete')}</button></span></div>
        {n.body && <p className="note-body">{n.body}</p>}<div className="muted small-text">{t('обновлено', 'updated')} {fmt(n.updatedAt)}</div></>}
      {deleting === n.id && <div className="form-actions note-delete-confirm"><span>{t('Удалить заметку?', 'Delete note?')}</span>
        <button className="danger small" disabled={busy} onClick={() => void remove(n)}>{t('Удалить', 'Delete')}</button><button className="small" disabled={busy} onClick={() => setDeleting(null)}>{t('Отмена', 'Cancel')}</button></div>}
    </div>)}</div>
  </SectionCard>
}
