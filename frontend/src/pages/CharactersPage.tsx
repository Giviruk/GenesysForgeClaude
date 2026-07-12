import { useCallback, useEffect, useRef, useState, type ChangeEvent, type FormEvent } from 'react'
import { api } from '../api/client'
import type { CharacterExport, CharacterListItem, GameSystem, ImportPreview, Reference } from '../api/types'
import { Icon } from '../components/Icon'
import { CHARACTERISTICS, CHARACTERISTIC_LABELS, dualName, localizedName, SYSTEM_LABELS } from '../utils/labels'
import { t } from '../i18n'

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
      .catch((err: unknown) => setError(err instanceof Error ? err.message : t('Ошибка загрузки', 'Failed to load'))),
    [],
  )

  useEffect(() => {
    void reload()
  }, [reload])

  async function remove(id: string, name: string) {
    if (!confirm(t(`Удалить персонажа «${name}»?`, `Delete character "${name}"?`))) return
    await api.deleteCharacter(id)
    await reload()
  }

  async function duplicate(id: string) {
    setError(null)
    try {
      const copy = await api.duplicateCharacter(id)
      await reload()
      onOpen(copy.id)
    } catch (err) {
      setError(err instanceof Error ? err.message : t('Ошибка клонирования', 'Failed to duplicate'))
    }
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
      setError(err instanceof Error ? err.message : t('Не удалось прочитать файл персонажа', 'Could not read the character file'))
    }
  }

  const isLoading = characters === null && !error
  const isEmpty = characters?.length === 0 && !creating

  return (
    <div className="page">
      <div className="page-head">
        <div>
          <h2>{t('Ваши персонажи', 'Your characters')}</h2>
          <div className="page-sub">{t('Genesys Core и Realms of Terrinoth в одном месте', 'Genesys Core and Realms of Terrinoth in one place')}</div>
        </div>
        <div className="head-actions">
          <button onClick={() => fileRef.current?.click()}>
            <Icon name="file-import" className="button-icon" />
            {t('Импорт JSON', 'Import JSON')}
          </button>
          <button className="primary" onClick={() => setCreating(true)}>
            <Icon name="plus" className="button-icon" />
            {t('Новый персонаж', 'New character')}
          </button>
          <input ref={fileRef} type="file" accept="application/json,.json" hidden onChange={onFile} />
        </div>
      </div>

      {isLoading && (
        <div className="card-grid character-grid">
          {[0, 1, 2].map(i => <div key={i} className="char-card skeleton-card" />)}
        </div>
      )}

      {error && (
        <div className="state-panel error-state">
          <Icon name="alert" className="state-icon" />
          <h3>{t('Не удалось загрузить персонажей', 'Failed to load characters')}</h3>
          <p>{t('Проверьте соединение и попробуйте снова. Если ошибка повторяется, обратитесь в поддержку.', 'Check your connection and try again. If the error persists, contact support.')}</p>
          <button onClick={() => { setError(null); void reload() }}>{t('Повторить', 'Retry')}</button>
          <div className="small-text muted">{error}</div>
        </div>
      )}

      {isEmpty && (
        <div className="state-panel empty-state">
          <Icon name="user-plus" className="state-icon" />
          <h3>{t('Персонажей пока нет', 'No characters yet')}</h3>
          <p>{t('Создайте первого героя или импортируйте готовый лист в формате JSON.', 'Create your first hero or import an existing sheet from JSON.')}</p>
          <div className="head-actions">
            <button className="primary" onClick={() => setCreating(true)}>
              <Icon name="plus" className="button-icon" />
              {t('Новый персонаж', 'New character')}
            </button>
            <button onClick={() => fileRef.current?.click()}>{t('Импорт JSON', 'Import JSON')}</button>
          </div>
        </div>
      )}

      <div className="card-grid">
        {characters?.map(c => (
          <div key={c.id} className="char-card" onClick={() => onOpen(c.id)}>
            <div className="char-card-identity">
              <div className="char-portrait">{initials(c.name)}</div>
              <div className="char-title-block">
                <strong>{c.name}</strong>
                <div className="muted">{c.archetype} · {c.career}</div>
              </div>
            </div>
            <div className="tag-row compact">
              <span className={`badge ${c.system}`}>{SYSTEM_LABELS[c.system]}</span>
              {c.isCreationPhase && <span className="badge creation">{t('Создание', 'Creation')}</span>}
            </div>
            <VitalBar label={t('Раны', 'Wounds')} current={c.woundsCurrent} threshold={c.woundThreshold} tone="wound" />
            <VitalBar label={t('Стресс', 'Strain')} current={c.strainCurrent} threshold={c.strainThreshold} tone="strain" />
            <div className="char-xp-row">
              <span>{t('Доступно XP', 'Available XP')}</span>
              <b>{c.availableXp}</b>
            </div>
            <div className="card-actions">
              <button className="small" onClick={e => { e.stopPropagation(); void duplicate(c.id) }}>
                <Icon name="copy" className="button-icon" />
                {t('Клонировать', 'Duplicate')}
              </button>
              <button className="danger small" onClick={e => { e.stopPropagation(); void remove(c.id, c.name) }}>
                <Icon name="trash" className="button-icon" />
                {t('Удалить', 'Delete')}
              </button>
            </div>
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

function pct(current: number, threshold: number) {
  if (threshold <= 0) return 0
  return Math.max(0, Math.min(100, Math.round((current / threshold) * 100)))
}

function initials(name: string) {
  return name.trim().split(/\s+/).slice(0, 2).map(part => part[0]?.toUpperCase() ?? '').join('') || 'PC'
}

function VitalBar({ label, current, threshold, tone }: {
  label: string
  current: number
  threshold: number
  tone: 'wound' | 'strain'
}) {
  return (
    <div className="vital-bar">
      <div className="vital-meta">
        <span>{label}</span>
        <b>{current}/{threshold}</b>
      </div>
      <div className="vital-track">
        <div className={`vital-fill ${tone}`} style={{ width: `${pct(current, threshold)}%` }} />
      </div>
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
      setError(err instanceof Error ? err.message : t('Ошибка импорта', 'Import failed'))
      setBusy(false)
    }
  }

  return (
    <div className="modal-backdrop" onClick={onCancel}>
      <form className="modal" onClick={e => e.stopPropagation()} onSubmit={doImport}>
        <h3>{t('Импорт персонажа', 'Import character')}</h3>
        <div className="hint">
          <strong>{preview.name}</strong> · <span className={`badge ${preview.system}`}>{SYSTEM_LABELS[preview.system]}</span>
          <br />{preview.archetypeName} · {preview.careerName}
          <br />{t(`XP: ${preview.totalXp} (потрачено ${preview.spentXp})`, `XP: ${preview.totalXp} (${preview.spentXp} spent)`)}
          <br />{t(
            `Навыков ${preview.skillCount} · талантов ${preview.talentCount} · предметов ${preview.itemCount} · заметок ${preview.noteCount}`,
            `${preview.skillCount} skills · ${preview.talentCount} talents · ${preview.itemCount} items · ${preview.noteCount} notes`,
          )}
        </div>
        {preview.warnings.length > 0 && (
          <div className="notice warn">
            <strong>{t('Предупреждения:', 'Warnings:')}</strong>
            <ul>{preview.warnings.map((w, i) => <li key={i}>{w}</li>)}</ul>
          </div>
        )}
        <p className="muted small-text">{t('Будет создан новый персонаж; существующие не изменятся.', 'A new character will be created; existing ones will not change.')}</p>
        {error && <div className="error">{error}</div>}
        <div className="modal-actions">
          <button type="button" onClick={onCancel}>{t('Отмена', 'Cancel')}</button>
          <button className="primary" type="submit" disabled={busy}>{t('Импортировать', 'Import')}</button>
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
        if (!cancelled) setError(err instanceof Error ? err.message : t('Ошибка загрузки', 'Failed to load'))
      })
    return () => { cancelled = true }
  }, [system])

  const archetype = reference?.archetypes.find(a => a.id === archetypeId)
  const career = reference?.careers.find(c => c.id === careerId)
  // EN-имя навыка → RU/ENG подпись чипа (значение для бэкенда остаётся английским).
  const skillRu = (name: string) => {
    const def = reference?.skills.find(s => s.name === name)
    return def ? dualName(def) : name
  }

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
      setError(err instanceof Error ? err.message : t('Ошибка создания', 'Failed to create'))
      setBusy(false)
    }
  }

  return (
    <div className="modal-backdrop" onClick={onCancel}>
      <form className="modal" onClick={e => e.stopPropagation()} onSubmit={submit}>
        <h3>{t('Новый персонаж', 'New character')}</h3>

        <label>
          {t('Система', 'System')}
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
          {t('Имя персонажа', 'Character name')}
          <input value={name} onChange={e => setName(e.target.value)} required />
        </label>

        <label>
          {system === 'realmsOfTerrinoth' ? t('Раса (архетип)', 'Species (archetype)') : t('Архетип', 'Archetype')}
          <select value={archetypeId}
            onChange={e => { setArchetypeId(e.target.value); setSkillChoices({}) }} required>
            <option value="" disabled>{t('— выберите —', '— select —')}</option>
            {reference?.archetypes.map(a => <option key={a.id} value={a.id}>{localizedName(a)}</option>)}
          </select>
        </label>
        {archetype && (
          <div className="hint">
            {CHARACTERISTICS.map(c => `${CHARACTERISTIC_LABELS[c]} ${archetype[c]}`).join(' · ')}
            <br />{t(
              `Раны ${archetype.woundBase}+Мощь · Усталость ${archetype.strainBase}+Воля · Старт. XP ${archetype.startingXp}`,
              `Wounds ${archetype.woundBase}+Brawn · Strain ${archetype.strainBase}+Willpower · Starting XP ${archetype.startingXp}`,
            )}
            {fixedStartingSkills.length > 0 && (
              <><br />{t('Стартовые навыки:', 'Starting skills:')} {fixedStartingSkills
                .map(s => `${t(s.nameRu || skillRu(s.skillName), skillRu(s.skillName))}${s.freeRanks > 1 ? ` ${s.freeRanks}` : ''}`)
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
                {t(
                  `Стартовые навыки вида — выберите ${g.choiceCount} разных некарьерных (${picked.length}/${g.choiceCount}):`,
                  `Species starting skills — pick ${g.choiceCount} different non-career skills (${picked.length}/${g.choiceCount}):`,
                )}
              </div>
              {g.choiceGroup === 'any-noncareer' && !career && <div className="hint">{t('Сначала выберите карьеру.', 'Pick a career first.')}</div>}
              <div className="chips">
                {choiceCandidates(g.choiceGroup).map(s => (
                  <button key={s.id} type="button"
                    className={picked.includes(s.name) ? 'chip active' : 'chip'}
                    onClick={() => toggleChoiceSkill(g.choiceGroup, s.name, g.choiceCount)}>
                    {dualName(s)}
                  </button>
                ))}
              </div>
            </div>
          )
        })}

        <label>
          {t('Карьера', 'Career')}
          <select value={careerId} onChange={e => { setCareerId(e.target.value); setGearChoices({}) }} required>
            <option value="" disabled>{t('— выберите —', '— select —')}</option>
            {reference?.careers.map(c => <option key={c.id} value={c.id}>{localizedName(c)}</option>)}
          </select>
        </label>

        {career && (
          <div>
            <div className="hint">{career.description}</div>
            <div className="label-line">{t(
              `Карьерные навыки — отметьте до 4 для бесплатного ранга (${freeSkills.length}/4):`,
              `Career skills — mark up to 4 for a free rank (${freeSkills.length}/4):`,
            )}</div>
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
            {moneyLabel && <div className="hint">{t(`Стартовые деньги: ${moneyLabel} серебра`, `Starting money: ${moneyLabel} silver`)}</div>}
            {fixedGear.length > 0 && <div className="hint">{t('Снаряжение:', 'Gear:')} {fixedGear.map(gearLabel).join(', ')}</div>}
            {gearSlots.map(slot => (
              <div key={slot.group}>
                <div className="label-line">{t('Снаряжение — выберите вариант:', 'Gear — pick an option:')}</div>
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
          <summary>{t('Мотивации и предыстория (необязательно)', 'Motivations and background (optional)')}</summary>
          <div className="hint">{t('Можно заполнить позже на вкладке «Образ» листа персонажа.', 'You can fill this in later on the sheet’s "Bio" tab.')}</div>
          <label>{t('Стремление', 'Desire')}
            <input value={desire} onChange={e => setDesire(e.target.value)} maxLength={300} />
          </label>
          <label>{t('Страх', 'Fear')}
            <input value={fear} onChange={e => setFear(e.target.value)} maxLength={300} />
          </label>
          <label>{t('Сильная сторона', 'Strength')}
            <input value={strength} onChange={e => setStrength(e.target.value)} maxLength={300} />
          </label>
          <label>{t('Слабость', 'Flaw')}
            <input value={flaw} onChange={e => setFlaw(e.target.value)} maxLength={300} />
          </label>
          <label>{t('Предыстория', 'Background')}
            <textarea value={background} onChange={e => setBackground(e.target.value)} rows={4} maxLength={8000} />
          </label>
        </details>

        {error && <div className="error">{error}</div>}
        <div className="modal-actions">
          <button type="button" onClick={onCancel}>{t('Отмена', 'Cancel')}</button>
          <button className="primary" type="submit" disabled={busy || !archetypeId || !careerId || !choicesComplete || !gearComplete}>{t('Создать', 'Create')}</button>
        </div>
      </form>
    </div>
  )
}
