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

export function CreateCharacterForm({ onCancel, onCreated }: { onCancel: () => void; onCreated: (id: string) => void }) {
  const [system, setSystem] = useState<GameSystem>('genesysCore')
  const [loaded, setLoaded] = useState<{ system: GameSystem; data: Reference } | null>(null)
  const [name, setName] = useState('')
  const [archetypeId, setArchetypeId] = useState('')
  const [careerId, setCareerId] = useState('')
  const [freeSkills, setFreeSkills] = useState<string[]>([])
  // Выборы стартовых навыков вида: choiceGroup → выбранные EN-имена навыков.
  const [skillChoices, setSkillChoices] = useState<Record<string, string[]>>({})
  // Выборы стартового снаряжения карьеры: choiceGroup → индекс выбранного варианта.
  const [gearChoices, setGearChoices] = useState<Record<string, number>>({})
  // Мотивации и предыстория (U-22) — все опциональны, можно заполнить позже на листе.
  const [desire, setDesire] = useState('')
  const [fear, setFear] = useState('')
  const [strength, setStrength] = useState('')
  const [flaw, setFlaw] = useState('')
  const [background, setBackground] = useState('')
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
        setSkillChoices({})
        setGearChoices({})
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

  const fixedStartingSkills = (archetype?.startingSkills ?? []).filter(s => !s.isChoice && s.skillName)
  const choiceGroups = (archetype?.startingSkills ?? []).filter(s => s.isChoice)
  // Кандидаты для выбора: для «any-noncareer» — навыки вне карьерных (как валидирует бэкенд).
  const choiceCandidates = (group: string) => (reference?.skills ?? [])
    .filter(s => group !== 'any-noncareer' || !career?.careerSkillNames.includes(s.name))
  const choicesComplete = choiceGroups.every(g => (skillChoices[g.choiceGroup]?.length ?? 0) === g.choiceCount)

  // Стартовое снаряжение карьеры: фиксированное и слоты выбора (вариант = набор предметов).
  const gearLabel = (g: { itemNameRu: string; quantity: number }) => g.quantity > 1 ? `${g.itemNameRu} ×${g.quantity}` : g.itemNameRu
  const fixedGear = (career?.startingGear ?? []).filter(g => !g.isChoice)
  const gearSlots = [...new Set((career?.startingGear ?? []).filter(g => g.isChoice).map(g => g.choiceGroup))]
    .map(group => ({
      group,
      options: [...new Set(career!.startingGear.filter(g => g.isChoice && g.choiceGroup === group).map(g => g.choiceOption))]
        .sort((a, b) => a - b)
        .map(index => ({
          index,
          label: career!.startingGear
            .filter(g => g.isChoice && g.choiceGroup === group && g.choiceOption === index)
            .map(gearLabel).join(' + '),
        })),
    }))
  const gearComplete = gearSlots.every(s => gearChoices[s.group] !== undefined)
  const moneyLabel = career
    ? [career.startingMoneyFixed || null, career.startingMoneyDice || null].filter(Boolean).join(' + ')
    : ''

  function toggleFreeSkill(skillName: string) {
    setFreeSkills(prev => prev.includes(skillName)
      ? prev.filter(s => s !== skillName)
      : prev.length < 4 ? [...prev, skillName] : prev)
  }

  function toggleChoiceSkill(group: string, skillName: string, max: number) {
    setSkillChoices(prev => {
      const cur = prev[group] ?? []
      const next = cur.includes(skillName)
        ? cur.filter(s => s !== skillName)
        : cur.length < max ? [...cur, skillName] : cur
      return { ...prev, [group]: next }
    })
  }

  async function submit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setBusy(true)
    try {
      const choices = choiceGroups.map(g => ({ choiceGroup: g.choiceGroup, skillNames: skillChoices[g.choiceGroup] ?? [] }))
      const gear = gearSlots.map(s => ({ choiceGroup: s.group, optionIndex: gearChoices[s.group] }))
      const { id } = await api.createCharacter(name, system, archetypeId, careerId, freeSkills, choices, gear,
        { desire, fear, strength, flaw, background })
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
          <select value={archetypeId}
            onChange={e => { setArchetypeId(e.target.value); setSkillChoices({}) }} required>
            <option value="" disabled>— выберите —</option>
            {reference?.archetypes.map(a => <option key={a.id} value={a.id}>{a.nameRu || a.name}</option>)}
          </select>
        </label>
        {archetype && (
          <div className="hint">
            {CHARACTERISTICS.map(c => `${CHARACTERISTIC_LABELS[c]} ${archetype[c]}`).join(' · ')}
            <br />Раны {archetype.woundBase}+Мощь · Стрейн {archetype.strainBase}+Воля · Старт. XP {archetype.startingXp}
            {fixedStartingSkills.length > 0 && (
              <><br />Стартовые навыки: {fixedStartingSkills
                .map(s => `${s.nameRu || skillRu(s.skillName)}${s.freeRanks > 1 ? ` ${s.freeRanks}` : ''}`)
                .join(', ')}</>
            )}
            {archetype.abilities.map(ab => (
              <div key={ab.code}><strong>{ab.nameRu}</strong>{ab.safeDescription ? `: ${ab.safeDescription.replace(new RegExp(`^${ab.nameRu}:\\s*`), '')}` : ''}</div>
            ))}
          </div>
        )}
        {archetype && choiceGroups.map(g => {
          const picked = skillChoices[g.choiceGroup] ?? []
          return (
            <div key={g.choiceGroup}>
              <div className="label-line">
                Стартовые навыки вида — выберите {g.choiceCount} разных некарьерных ({picked.length}/{g.choiceCount}):
              </div>
              {g.choiceGroup === 'any-noncareer' && !career && <div className="hint">Сначала выберите карьеру.</div>}
              <div className="chips">
                {choiceCandidates(g.choiceGroup).map(s => (
                  <button key={s.id} type="button"
                    className={picked.includes(s.name) ? 'chip active' : 'chip'}
                    onClick={() => toggleChoiceSkill(g.choiceGroup, s.name, g.choiceCount)}>
                    {s.nameRu || s.name}
                  </button>
                ))}
              </div>
            </div>
          )
        })}

        <label>
          Карьера
          <select value={careerId} onChange={e => { setCareerId(e.target.value); setGearChoices({}) }} required>
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

        {career && career.startingGear.length > 0 && (
          <div>
            {moneyLabel && <div className="hint">Стартовые деньги: {moneyLabel} серебра</div>}
            {fixedGear.length > 0 && <div className="hint">Снаряжение: {fixedGear.map(gearLabel).join(', ')}</div>}
            {gearSlots.map(slot => (
              <div key={slot.group}>
                <div className="label-line">Снаряжение — выберите вариант:</div>
                <div className="chips">
                  {slot.options.map(o => (
                    <button key={o.index} type="button"
                      className={gearChoices[slot.group] === o.index ? 'chip active' : 'chip'}
                      onClick={() => setGearChoices(prev => ({ ...prev, [slot.group]: o.index }))}>
                      {o.label}
                    </button>
                  ))}
                </div>
              </div>
            ))}
            {career.rules.map(r => <div key={r.code} className="hint">{r.description}</div>)}
          </div>
        )}

        <details className="create-bio">
          <summary>Мотивации и предыстория (необязательно)</summary>
          <div className="hint">Можно заполнить позже на вкладке «Образ» листа персонажа.</div>
          <label>Стремление
            <input value={desire} onChange={e => setDesire(e.target.value)} maxLength={300} />
          </label>
          <label>Страх
            <input value={fear} onChange={e => setFear(e.target.value)} maxLength={300} />
          </label>
          <label>Сильная сторона
            <input value={strength} onChange={e => setStrength(e.target.value)} maxLength={300} />
          </label>
          <label>Слабость
            <input value={flaw} onChange={e => setFlaw(e.target.value)} maxLength={300} />
          </label>
          <label>Предыстория
            <textarea value={background} onChange={e => setBackground(e.target.value)} rows={4} maxLength={8000} />
          </label>
        </details>

        {error && <div className="error">{error}</div>}
        <div className="modal-actions">
          <button type="button" onClick={onCancel}>Отмена</button>
          <button className="primary" type="submit" disabled={busy || !archetypeId || !careerId || !choicesComplete || !gearComplete}>Создать</button>
        </div>
      </form>
    </div>
  )
}
