import { useCallback, useEffect, useRef, useState, type ChangeEvent, type FormEvent } from 'react'
import { api } from '../api/client'
import type { CharacterExport, CharacterListItem, GameSystem, ImportPreview, Reference } from '../api/types'
import { CHARACTERISTICS, CHARACTERISTIC_LABELS, SYSTEM_LABELS } from '../utils/labels'

interface Props {
  onOpen: (id: string) => void
}

export function CharactersPage({ onOpen }: Props) {
  const [characters, setCharacters] = useState<CharacterListItem[] | null>(null)
  const [creating, setCreating] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [importState, setImportState] = useState<{ payload: CharacterExport; preview: ImportPreview } | null>(null)
  const fileRef = useRef<HTMLInputElement>(null)

  const reload = useCallback(
    () => api.characters()
      .then(setCharacters)
      .catch((err: unknown) => setError(err instanceof Error ? err.message : 'Ошибка загрузки')),
    [],
  )

  useEffect(() => {
    void reload()
  }, [reload])

  async function remove(id: string, name: string) {
    if (!confirm(`Удалить персонажа «${name}»?`)) return
    await api.deleteCharacter(id)
    await reload()
  }

  async function onFile(e: ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    e.target.value = '' // позволяем повторно выбрать тот же файл
    if (!file) return
    setError(null)
    try {
      const payload = JSON.parse(await file.text()) as CharacterExport
      const preview = await api.previewImport(payload)
      setImportState({ payload, preview })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Не удалось прочитать файл персонажа')
    }
  }

  return (
    <div className="page">
      <div className="page-head">
        <h2>Мои персонажи</h2>
        <div className="head-actions">
          <button onClick={() => fileRef.current?.click()}>Импорт JSON</button>
          <button className="primary" onClick={() => setCreating(true)}>+ Новый персонаж</button>
          <input ref={fileRef} type="file" accept="application/json,.json" hidden onChange={onFile} />
        </div>
      </div>
      {error && <div className="error">{error}</div>}
      {characters === null && <p className="muted">Загрузка…</p>}
      {characters?.length === 0 && !creating && <p className="muted">Пока нет персонажей — создайте первого!</p>}
      <div className="card-grid">
        {characters?.map(c => (
          <div key={c.id} className="char-card" onClick={() => onOpen(c.id)}>
            <div className="char-card-head">
              <strong>{c.name}</strong>
              <span className={`badge ${c.system}`}>{SYSTEM_LABELS[c.system]}</span>
            </div>
            <div className="muted">{c.archetype} · {c.career}</div>
            {c.isCreationPhase && <div className="badge creation">Создание</div>}
            <button className="danger small" onClick={e => { e.stopPropagation(); void remove(c.id, c.name) }}>
              Удалить
            </button>
          </div>
        ))}
      </div>
      {creating && (
        <CreateCharacterForm
          onCancel={() => setCreating(false)}
          onCreated={id => { setCreating(false); onOpen(id) }}
        />
      )}
      {importState && (
        <ImportCharacterModal
          payload={importState.payload}
          preview={importState.preview}
          onCancel={() => setImportState(null)}
          onImported={id => { setImportState(null); onOpen(id) }}
        />
      )}
    </div>
  )
}

function ImportCharacterModal({ payload, preview, onCancel, onImported }: {
  payload: CharacterExport
  preview: ImportPreview
  onCancel: () => void
  onImported: (id: string) => void
}) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function doImport(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      const result = await api.importCharacter(payload)
      onImported(result.characterId)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка импорта')
      setBusy(false)
    }
  }

  return (
    <div className="modal-backdrop" onClick={onCancel}>
      <form className="modal" onClick={e => e.stopPropagation()} onSubmit={doImport}>
        <h3>Импорт персонажа</h3>
        <div className="hint">
          <strong>{preview.name}</strong> · <span className={`badge ${preview.system}`}>{SYSTEM_LABELS[preview.system]}</span>
          <br />{preview.archetypeName} · {preview.careerName}
          <br />XP: {preview.totalXp} (потрачено {preview.spentXp})
          <br />Навыков {preview.skillCount} · талантов {preview.talentCount} · предметов {preview.itemCount} · заметок {preview.noteCount}
        </div>
        {preview.warnings.length > 0 && (
          <div className="notice warn">
            <strong>Предупреждения:</strong>
            <ul>{preview.warnings.map((w, i) => <li key={i}>{w}</li>)}</ul>
          </div>
        )}
        <p className="muted small-text">Будет создан новый персонаж; существующие не изменятся.</p>
        {error && <div className="error">{error}</div>}
        <div className="modal-actions">
          <button type="button" onClick={onCancel}>Отмена</button>
          <button className="primary" type="submit" disabled={busy}>Импортировать</button>
        </div>
      </form>
    </div>
  )
}

function CreateCharacterForm({ onCancel, onCreated }: { onCancel: () => void; onCreated: (id: string) => void }) {
  const [system, setSystem] = useState<GameSystem>('genesysCore')
  const [loaded, setLoaded] = useState<{ system: GameSystem; data: Reference } | null>(null)
  const [name, setName] = useState('')
  const [archetypeId, setArchetypeId] = useState('')
  const [careerId, setCareerId] = useState('')
  const [freeSkills, setFreeSkills] = useState<string[]>([])
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  // Справочник показывается только для текущей системы — при переключении стейл-данные скрываются сами
  const reference = loaded?.system === system ? loaded.data : null

  useEffect(() => {
    let cancelled = false
    api.reference(system)
      .then(data => {
        if (cancelled) return
        setLoaded({ system, data })
        setArchetypeId('')
        setCareerId('')
        setFreeSkills([])
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof Error ? err.message : 'Ошибка загрузки')
      })
    return () => { cancelled = true }
  }, [system])

  const archetype = reference?.archetypes.find(a => a.id === archetypeId)
  const career = reference?.careers.find(c => c.id === careerId)
  // EN-имя навыка → RU для подписей чипов (значение для бэкенда остаётся английским).
  const skillRu = (name: string) => reference?.skills.find(s => s.name === name)?.nameRu || name

  function toggleFreeSkill(skillName: string) {
    setFreeSkills(prev => prev.includes(skillName)
      ? prev.filter(s => s !== skillName)
      : prev.length < 4 ? [...prev, skillName] : prev)
  }

  async function submit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setBusy(true)
    try {
      const { id } = await api.createCharacter(name, system, archetypeId, careerId, freeSkills)
      onCreated(id)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка создания')
      setBusy(false)
    }
  }

  return (
    <div className="modal-backdrop" onClick={onCancel}>
      <form className="modal" onClick={e => e.stopPropagation()} onSubmit={submit}>
        <h3>Новый персонаж</h3>

        <label>
          Система
          <div className="system-switch">
            {(['genesysCore', 'realmsOfTerrinoth'] as GameSystem[]).map(s => (
              <button key={s} type="button"
                className={system === s ? 'tab active' : 'tab'}
                onClick={() => setSystem(s)}>
                {SYSTEM_LABELS[s]}
              </button>
            ))}
          </div>
        </label>

        <label>
          Имя персонажа
          <input value={name} onChange={e => setName(e.target.value)} required />
        </label>

        <label>
          {system === 'realmsOfTerrinoth' ? 'Раса (архетип)' : 'Архетип'}
          <select value={archetypeId} onChange={e => setArchetypeId(e.target.value)} required>
            <option value="" disabled>— выберите —</option>
            {reference?.archetypes.map(a => <option key={a.id} value={a.id}>{a.nameRu || a.name}</option>)}
          </select>
        </label>
        {archetype && (
          <div className="hint">
            {CHARACTERISTICS.map(c => `${CHARACTERISTIC_LABELS[c]} ${archetype[c]}`).join(' · ')}
            <br />Раны {archetype.woundBase}+Мощь · Стрейн {archetype.strainBase}+Воля · Старт. XP {archetype.startingXp}
            {archetype.safeDescription && <><br />{archetype.safeDescription}</>}
          </div>
        )}

        <label>
          Карьера
          <select value={careerId} onChange={e => setCareerId(e.target.value)} required>
            <option value="" disabled>— выберите —</option>
            {reference?.careers.map(c => <option key={c.id} value={c.id}>{c.nameRu || c.name}</option>)}
          </select>
        </label>

        {career && (
          <div>
            <div className="hint">{career.description}</div>
            <div className="label-line">Карьерные навыки — отметьте до 4 для бесплатного ранга ({freeSkills.length}/4):</div>
            <div className="chips">
              {career.careerSkillNames.map(s => (
                <button key={s} type="button"
                  className={freeSkills.includes(s) ? 'chip active' : 'chip'}
                  onClick={() => toggleFreeSkill(s)}>
                  {skillRu(s)}
                </button>
              ))}
            </div>
          </div>
        )}

        {error && <div className="error">{error}</div>}
        <div className="modal-actions">
          <button type="button" onClick={onCancel}>Отмена</button>
          <button className="primary" type="submit" disabled={busy || !archetypeId || !careerId}>Создать</button>
        </div>
      </form>
    </div>
  )
}
