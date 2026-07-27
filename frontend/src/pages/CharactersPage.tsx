import { useCallback, useEffect, useRef, useState, type ChangeEvent, type FormEvent } from 'react'
import { api } from '../api/client'
import type {
  CharacterExport, CharacterListItem, GameSystem, ImportPreview, Reference, StartingEquipmentMode,
} from '../api/types'
import { Icon } from '../components/Icon'
import { CHARACTERISTICS, CHARACTERISTIC_LABELS, dualName, localizedDescription, localizedName, SYSTEM_LABELS } from '../utils/labels'
import { MAX_FREE_CAREER_SKILLS, MAX_SKILL_RANK_AT_CREATION, MAX_STARTING_BUDGET } from '../utils/rules'
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
  // ROT-CRE-03: режимы взаимоисключающие, безопасный default — стандартные деньги.
  const [equipmentMode, setEquipmentMode] = useState<StartingEquipmentMode>('standardMoney')
  // ROT-SPECIES-01: у Half-Catfolk выбор обязателен и необратим — умолчания у него нет.
  const [speciesChoice, setSpeciesChoice] = useState('')
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
        setEquipmentMode('standardMoney')
        setSpeciesChoice('')
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

  // Способность вида, требующая обязательного выбора одной опции (Half-Catfolk).
  const speciesChoiceAbility = (archetype?.abilities ?? []).find(a => a.ruleKind === 'chooseOneAbility')
  const speciesChoiceOptions = (speciesChoiceAbility?.choiceOptions ?? [])
    .map(code => (reference?.archetypes ?? []).flatMap(a => a.abilities).find(a => a.code === code))
    .filter((a): a is NonNullable<typeof a> => a !== undefined)
  const speciesChoiceComplete = !speciesChoiceAbility || speciesChoice.length > 0

  const fixedStartingSkills = (archetype?.startingSkills ?? []).filter(s => !s.isChoice && s.skillName)
  const choiceGroups = (archetype?.startingSkills ?? []).filter(s => s.isChoice)

  // Эффективный набор карьерных навыков = навыки карьеры ∪ выдачи вида (ROT-CRE-01).
  // Тот же союз считает бэкенд; фронт лишь объясняет источники и не является источником истины.
  const careerSkillEntries = career
    ? (() => {
      const speciesGrants = fixedStartingSkills.filter(s => s.grantsCareerSkill)
      const names = [...new Set([...career.careerSkillNames, ...speciesGrants.map(s => s.skillName)])]
      return names.map(name => {
        const fromCareer = career.careerSkillNames.includes(name)
        const grant = speciesGrants.find(s => s.skillName === name)
        // Ранги, которые навык уже получает бесплатно от вида, до отметки карьерного ранга.
        const speciesRanks = fixedStartingSkills
          .filter(s => s.skillName === name)
          .reduce((sum, s) => sum + s.freeRanks, 0)
        const sources = [
          ...(fromCareer ? [t(`карьера ${localizedName(career)}`, `career ${localizedName(career)}`)] : []),
          ...(grant && archetype ? [t(`вид ${localizedName(archetype)}`, `species ${localizedName(archetype)}`)] : []),
        ]
        return { name, sources, speciesRanks, atCreationCap: speciesRanks >= MAX_SKILL_RANK_AT_CREATION }
      })
    })()
    : []

  // Кандидаты для выбора: для «any-noncareer» — навыки вне эффективного карьерного набора.
  const effectiveCareerNames = new Set(careerSkillEntries.map(e => e.name))
  const choiceCandidates = (group: string) => (reference?.skills ?? [])
    .filter(s => group !== 'any-noncareer' || !effectiveCareerNames.has(s.name))
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
  // В режиме стандартных денег выбор снаряжения не нужен и не отправляется вовсе.
  const gearComplete = equipmentMode !== 'careerPackage' || gearSlots.every(s => gearChoices[s.group] !== undefined)
  const moneyLabel = career
    ? [career.startingMoneyFixed || null, career.startingMoneyDice || null].filter(Boolean).join(' + ')
    : ''

  function toggleFreeSkill(skillName: string) {
    setFreeSkills(prev => prev.includes(skillName)
      ? prev.filter(s => s !== skillName)
      : prev.length < MAX_FREE_CAREER_SKILLS ? [...prev, skillName] : prev)
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
      const gear = equipmentMode === 'careerPackage'
        ? gearSlots.map(s => ({ choiceGroup: s.group, optionIndex: gearChoices[s.group] }))
        : []
      const { id } = await api.createCharacter(name, system, archetypeId, careerId, freeSkills, choices, gear,
        { desire, fear, strength, flaw, background }, equipmentMode, speciesChoice || undefined)
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
            onChange={e => { setArchetypeId(e.target.value); setSkillChoices({}); setSpeciesChoice('') }} required>
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
            {archetype.abilities.map(ab => {
              const desc = localizedDescription({ safeDescription: ab.safeDescription, descriptionEn: ab.descriptionEn })
              const abilityName = t(ab.nameRu, ab.nameEn || ab.nameRu)
              return <div key={ab.code}><strong>{abilityName}</strong>{desc ? `: ${desc.replace(new RegExp(`^${abilityName}:\\s*`), '')}` : ''}</div>
            })}
          </div>
        )}
        {speciesChoiceAbility && (
          <div>
            <div className="label-line">
              {t(
                `${speciesChoiceAbility.nameRu} — выберите одну способность (изменить после создания нельзя):`,
                `${speciesChoiceAbility.nameEn || speciesChoiceAbility.nameRu} — pick one ability (it cannot be changed later):`,
              )}
            </div>
            <div className="chips">
              {speciesChoiceOptions.map(option => (
                <button key={option.code} type="button"
                  className={speciesChoice === option.code ? 'chip active' : 'chip'}
                  title={localizedDescription({ safeDescription: option.safeDescription, descriptionEn: option.descriptionEn })}
                  onClick={() => setSpeciesChoice(option.code)}>
                  {t(option.nameRu, option.nameEn || option.nameRu)}
                </button>
              ))}
            </div>
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
          <select value={careerId} onChange={e => { setCareerId(e.target.value); setGearChoices({}); setEquipmentMode('standardMoney') }} required>
            <option value="" disabled>{t('— выберите —', '— select —')}</option>
            {reference?.careers.map(c => <option key={c.id} value={c.id}>{localizedName(c)}</option>)}
          </select>
        </label>

        {career && (
          <div>
            <div className="hint">{localizedDescription(career)}</div>
            <div className="label-line">{t(
              `Карьерные навыки — отметьте до 4 для бесплатного ранга (${freeSkills.length}/4):`,
              `Career skills — mark up to 4 for a free rank (${freeSkills.length}/4):`,
            )}</div>
            <div className="chips">
              {careerSkillEntries.map(entry => {
                const disabledReason = entry.atCreationCap
                  ? t(
                    `Вид уже даёт ранг ${entry.speciesRanks}; при создании ранг навыка не может быть выше ${MAX_SKILL_RANK_AT_CREATION}.`,
                    `Species already grants rank ${entry.speciesRanks}; a skill cannot exceed rank ${MAX_SKILL_RANK_AT_CREATION} at creation.`,
                  )
                  : null
                return (
                  <button key={entry.name} type="button"
                    className={freeSkills.includes(entry.name) ? 'chip active' : 'chip'}
                    disabled={entry.atCreationCap}
                    title={disabledReason ?? entry.sources.join(' · ')}
                    onClick={() => toggleFreeSkill(entry.name)}>
                    {skillRu(entry.name)}
                    {entry.sources.length > 1 && <span className="chip-badge"> ({entry.sources.length})</span>}
                  </button>
                )
              })}
            </div>
            {careerSkillEntries.some(e => e.sources.length > 1 || e.atCreationCap) && (
              <div className="hint">
                {careerSkillEntries
                  .filter(e => e.sources.length > 1 || e.atCreationCap)
                  .map(e => `${skillRu(e.name)} — ${e.sources.join(', ')}${e.atCreationCap
                    ? t(` (уже ранг ${e.speciesRanks}, выбрать нельзя)`, ` (already rank ${e.speciesRanks}, cannot pick)`)
                    : ''}`)
                  .join('; ')}
              </div>
            )}
          </div>
        )}

        {career && career.startingGear.length > 0 && (
          <div>
            <div className="label-line">{t('Стартовое снаряжение — режимы взаимоисключающие:', 'Starting equipment — the modes are mutually exclusive:')}</div>
            <div className="chips">
              <button type="button"
                className={equipmentMode === 'standardMoney' ? 'chip active' : 'chip'}
                onClick={() => setEquipmentMode('standardMoney')}>
                {t('Стандартные деньги', 'Standard money')}
              </button>
              <button type="button"
                className={equipmentMode === 'careerPackage' ? 'chip active' : 'chip'}
                onClick={() => setEquipmentMode('careerPackage')}>
                {t('Карьерный комплект (с разрешения ведущего)', 'Career package (with GM permission)')}
              </button>
            </div>

            {equipmentMode === 'standardMoney' ? (
              <div className="hint">
                {t(
                  `Бюджет ${MAX_STARTING_BUDGET} серебра на стартовые покупки и отдельно карманные 1d100. Карьерный комплект не выдаётся.`,
                  `A ${MAX_STARTING_BUDGET} silver budget for starting purchases plus separate 1d100 pocket money. No career package is granted.`,
                )}
              </div>
            ) : (
              <>
                <div className="hint">
                  {t(
                    `Вместо бюджета ${MAX_STARTING_BUDGET} — весь комплект карьеры и его деньги${moneyLabel ? `: ${moneyLabel} серебра` : ''}. Нужно выбрать вариант в каждой группе.`,
                    `Instead of the ${MAX_STARTING_BUDGET} budget — the whole career package and its money${moneyLabel ? `: ${moneyLabel} silver` : ''}. One option must be picked in every group.`,
                  )}
                </div>
                {fixedGear.length > 0 && <div className="hint">{t('Всегда входит:', 'Always included:')} {fixedGear.map(gearLabel).join(', ')}</div>}
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
              </>
            )}
            {career.rules.map(r => <div key={r.code} className="hint">{localizedDescription(r)}</div>)}
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
          <button className="primary" type="submit" disabled={busy || !archetypeId || !careerId || !choicesComplete || !gearComplete || !speciesChoiceComplete}>{t('Создать', 'Create')}</button>
        </div>
      </form>
    </div>
  )
}
