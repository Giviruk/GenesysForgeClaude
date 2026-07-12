import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type { CharacterNote } from '../api/types'
import { lang, t } from '../i18n'

interface Props {
  characterId: string
  onError: (message: string) => void
}

export function NotesTab({ characterId, onError }: Props) {
  const [notes, setNotes] = useState<CharacterNote[] | null>(null)
  const [editing, setEditing] = useState<CharacterNote | null>(null)
  const [title, setTitle] = useState('')
  const [body, setBody] = useState('')
  const [busy, setBusy] = useState(false)

  const reload = useCallback(
    () => api.notes(characterId)
      .then(setNotes)
      .catch((err: unknown) => onError(err instanceof Error ? err.message : t('Ошибка загрузки заметок', 'Failed to load notes'))),
    [characterId, onError])

  useEffect(() => { void reload() }, [reload])

  function resetForm() {
    setEditing(null)
    setTitle('')
    setBody('')
  }

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    try {
      if (editing) await api.updateNote(characterId, editing.id, title, body)
      else await api.createNote(characterId, title, body)
      resetForm()
      await reload()
    } catch (err) {
      onError(err instanceof Error ? err.message : t('Ошибка сохранения', 'Failed to save'))
    } finally {
      setBusy(false)
    }
  }

  function startEdit(note: CharacterNote) {
    setEditing(note)
    setTitle(note.title)
    setBody(note.body)
  }

  async function remove(note: CharacterNote) {
    if (!confirm(t(`Удалить заметку «${note.title}»?`, `Delete note "${note.title}"?`))) return
    try {
      await api.deleteNote(characterId, note.id)
      if (editing?.id === note.id) resetForm()
      await reload()
    } catch (err) {
      onError(err instanceof Error ? err.message : t('Ошибка удаления', 'Failed to delete'))
    }
  }

  const fmt = (iso: string) => new Date(iso).toLocaleString(lang === 'ru' ? 'ru-RU' : 'en-US', { dateStyle: 'short', timeStyle: 'short' })

  return (
    <div>
      <section className="panel">
        <h3>{editing ? t('Редактирование заметки', 'Edit note') : t('Новая заметка', 'New note')}</h3>
        <form className="custom-form" onSubmit={submit}>
          {editing && <div className="editing-banner">{t('Редактирование:', 'Editing:')} {editing.title}</div>}
          <label>{t('Заголовок', 'Title')}
            <input value={title} onChange={e => setTitle(e.target.value)} required maxLength={200} />
          </label>
          <label>{t('Текст', 'Text')}
            <textarea value={body} onChange={e => setBody(e.target.value)} rows={5}
              placeholder={t('Сюжетные события, NPC, зацепки, напоминания…', 'Plot events, NPCs, leads, reminders…')} />
          </label>
          <div className="form-actions">
            <button className="primary" type="submit" disabled={busy}>{editing ? t('Сохранить', 'Save') : t('Добавить', 'Add')}</button>
            {editing && <button type="button" onClick={resetForm}>{t('Отмена', 'Cancel')}</button>}
          </div>
        </form>
      </section>

      <section className="panel">
        <h3>{t('Заметки', 'Notes')} {notes ? `(${notes.length})` : ''}</h3>
        {notes === null && <p className="muted">{t('Загрузка…', 'Loading…')}</p>}
        {notes?.length === 0 && <p className="muted">{t('Пока нет заметок — добавьте первую выше.', 'No notes yet — add the first one above.')}</p>}
        <div className="notes-list">
          {notes?.map(n => (
            <div key={n.id} className="note-card">
              <div className="note-card-head">
                <strong>{n.title}</strong>
                <span className="note-actions">
                  <button className="small" onClick={() => startEdit(n)}>{t('Изменить', 'Edit')}</button>
                  <button className="danger small" onClick={() => remove(n)}>{t('Удалить', 'Delete')}</button>
                </span>
              </div>
              {n.body && <p className="note-body">{n.body}</p>}
              <div className="muted small-text">{t('обновлено', 'updated')} {fmt(n.updatedAt)}</div>
            </div>
          ))}
        </div>
      </section>
    </div>
  )
}
