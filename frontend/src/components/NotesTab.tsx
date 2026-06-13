import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type { CharacterNote } from '../api/types'

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
      .catch((err: unknown) => onError(err instanceof Error ? err.message : 'Ошибка загрузки заметок')),
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
      onError(err instanceof Error ? err.message : 'Ошибка сохранения')
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
    if (!confirm(`Удалить заметку «${note.title}»?`)) return
    try {
      await api.deleteNote(characterId, note.id)
      if (editing?.id === note.id) resetForm()
      await reload()
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Ошибка удаления')
    }
  }

  const fmt = (iso: string) => new Date(iso).toLocaleString('ru-RU', { dateStyle: 'short', timeStyle: 'short' })

  return (
    <div>
      <section className="panel">
        <h3>{editing ? 'Редактирование заметки' : 'Новая заметка'}</h3>
        <form className="custom-form" onSubmit={submit}>
          {editing && <div className="editing-banner">Редактирование: {editing.title}</div>}
          <label>Заголовок
            <input value={title} onChange={e => setTitle(e.target.value)} required maxLength={200} />
          </label>
          <label>Текст
            <textarea value={body} onChange={e => setBody(e.target.value)} rows={5}
              placeholder="Сюжетные события, NPC, зацепки, напоминания…" />
          </label>
          <div className="form-actions">
            <button className="primary" type="submit" disabled={busy}>{editing ? 'Сохранить' : 'Добавить'}</button>
            {editing && <button type="button" onClick={resetForm}>Отмена</button>}
          </div>
        </form>
      </section>

      <section className="panel">
        <h3>Заметки {notes ? `(${notes.length})` : ''}</h3>
        {notes === null && <p className="muted">Загрузка…</p>}
        {notes?.length === 0 && <p className="muted">Пока нет заметок — добавьте первую выше.</p>}
        <div className="notes-list">
          {notes?.map(n => (
            <div key={n.id} className="note-card">
              <div className="note-card-head">
                <strong>{n.title}</strong>
                <span className="note-actions">
                  <button className="small" onClick={() => startEdit(n)}>Изменить</button>
                  <button className="danger small" onClick={() => remove(n)}>Удалить</button>
                </span>
              </div>
              {n.body && <p className="note-body">{n.body}</p>}
              <div className="muted small-text">обновлено {fmt(n.updatedAt)}</div>
            </div>
          ))}
        </div>
      </section>
    </div>
  )
}
