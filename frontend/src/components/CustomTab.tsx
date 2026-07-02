import { useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type {
  Archetype, Career, CharacterSheet, CustomArchetypeInput, CustomCareerInput,
  HeroicAbility, HomebrewPackDocument, HomebrewPackListItem, ItemDef, Quality, Reference, SkillDef, TalentDef,
  TalentCategory,
} from '../api/types'
import {
  CHARACTERISTICS, CHARACTERISTIC_LABELS, dualName, ITEM_KIND_LABELS, SKILL_KIND_LABELS,
  TALENT_CATEGORIES, TALENT_CATEGORY_LABELS,
} from '../utils/labels'

interface Props {
  sheet: CharacterSheet
  reference: Reference
  onError: (message: string) => void
  refresh: () => Promise<void>
}

type Section = 'skill' | 'talent' | 'item' | 'heroic' | 'archetype' | 'career' | 'packs'

export function CustomTab({ sheet, reference, onError, refresh }: Props) {
  const [section, setSection] = useState<Section>('skill')
  const [notice, setNotice] = useState<string | null>(null)
  // редактируемый объект текущей секции (null — режим создания)
  const [editingSkill, setEditingSkill] = useState<SkillDef | null>(null)
  const [editingTalent, setEditingTalent] = useState<TalentDef | null>(null)
  const [editingItem, setEditingItem] = useState<ItemDef | null>(null)
  const [editingHeroic, setEditingHeroic] = useState<HeroicAbility | null>(null)
  const [editingArchetype, setEditingArchetype] = useState<Archetype | null>(null)
  const [editingCareer, setEditingCareer] = useState<Career | null>(null)

  async function run(action: () => Promise<unknown>, successMessage: string) {
    setNotice(null)
    try {
      await action()
      await refresh()
      setNotice(successMessage)
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Ошибка')
    }
  }

  const customSkills = reference.skills.filter(s => s.isCustom)
  const customTalents = reference.talents.filter(t => t.isCustom)
  const customItems = reference.items.filter(i => i.isCustom)
  const customHeroics = reference.heroicAbilities.filter(h => h.isCustom)
  const customArchetypes = reference.archetypes.filter(a => a.isCustom)
  const customCareers = reference.careers.filter(c => c.isCustom)

  return (
    <div>
      <section className="panel">
        <h3>Кастомный контент</h3>
        <p className="hint">
          Создавайте и редактируйте собственные навыки, таланты, предметы и героические способности для системы
          «{sheet.system === 'genesysCore' ? 'Genesys Core' : 'Realms of Terrinoth'}».
          Кастом привязан к вашему аккаунту, а не к этому персонажу: он виден только вам, но доступен
          во всех ваших персонажах и NPC этой системы. Удаление недоступно, пока контент используется персонажем.
        </p>
        <div className="tabs">
          <button className={section === 'skill' ? 'tab active' : 'tab'} onClick={() => setSection('skill')}>Навыки</button>
          <button className={section === 'talent' ? 'tab active' : 'tab'} onClick={() => setSection('talent')}>Таланты</button>
          <button className={section === 'item' ? 'tab active' : 'tab'} onClick={() => setSection('item')}>Предметы</button>
          <button className={section === 'archetype' ? 'tab active' : 'tab'} onClick={() => setSection('archetype')}>Архетипы</button>
          <button className={section === 'career' ? 'tab active' : 'tab'} onClick={() => setSection('career')}>Карьеры</button>
          <button className={section === 'packs' ? 'tab active' : 'tab'} onClick={() => setSection('packs')}>Наборы JSON</button>
          {sheet.system === 'realmsOfTerrinoth' && (
            <button className={section === 'heroic' ? 'tab active' : 'tab'} onClick={() => setSection('heroic')}>Героич. способности</button>
          )}
        </div>
        {notice && <div className="notice">{notice}</div>}

        {section === 'skill' && (
          <>
            <SkillForm key={editingSkill?.id ?? 'new'} sheet={sheet} run={run} editing={editingSkill}
              onDone={() => setEditingSkill(null)} />
            <CustomList items={customSkills.map(s => ({ id: s.id, label: `${s.name} · ${CHARACTERISTIC_LABELS[s.characteristic]} · ${SKILL_KIND_LABELS[s.kind]}` }))}
              onEdit={id => setEditingSkill(customSkills.find(s => s.id === id)!)}
              onDelete={id => run(() => api.deleteCustomSkill(id), 'Навык удалён.')} />
          </>
        )}
        {section === 'talent' && (
          <>
            <TalentForm key={editingTalent?.id ?? 'new'} sheet={sheet} run={run} editing={editingTalent}
              onDone={() => setEditingTalent(null)} />
            <CustomList items={customTalents.map(t => ({
              id: t.id,
              label: `${t.name} · ${TALENT_CATEGORY_LABELS[t.category]} · Тир ${t.tier}${t.isRanked ? ' · ранговый' : ''}`,
            }))}
              onEdit={id => setEditingTalent(customTalents.find(t => t.id === id)!)}
              onDelete={id => run(() => api.deleteCustomTalent(id), 'Талант удалён.')} />
          </>
        )}
        {section === 'item' && (
          <>
            <ItemForm key={editingItem?.id ?? 'new'} sheet={sheet} reference={reference} run={run} editing={editingItem}
              onDone={() => setEditingItem(null)} />
            <CustomList items={customItems.map(i => ({ id: i.id, label: `${i.name} · ${ITEM_KIND_LABELS[i.kind]} · вес ${i.encumbrance}` }))}
              onEdit={id => setEditingItem(customItems.find(i => i.id === id)!)}
              onDelete={id => run(() => api.deleteCustomItem(id), 'Предмет удалён.')} />
          </>
        )}
        {section === 'heroic' && sheet.system === 'realmsOfTerrinoth' && (
          <>
            <HeroicForm key={editingHeroic?.id ?? 'new'} run={run} editing={editingHeroic}
              onDone={() => setEditingHeroic(null)} />
            <CustomList items={customHeroics.map(h => ({ id: h.id, label: h.name }))}
              onEdit={id => setEditingHeroic(customHeroics.find(h => h.id === id)!)}
              onDelete={id => run(() => api.deleteCustomHeroicAbility(id), 'Способность удалена.')} />
          </>
        )}
        {section === 'archetype' && (
          <>
            <ArchetypeForm key={editingArchetype?.id ?? 'new'} sheet={sheet} run={run}
              editing={editingArchetype} onDone={() => setEditingArchetype(null)} />
            <CustomList items={customArchetypes.map(a => ({ id: a.id, label: `${a.nameRu || a.name} · XP ${a.startingXp}` }))}
              onEdit={id => setEditingArchetype(customArchetypes.find(a => a.id === id)!)}
              onDelete={id => run(() => api.deleteCustomArchetype(id), 'Архетип удалён.')} />
          </>
        )}
        {section === 'career' && (
          <>
            <CareerForm key={editingCareer?.id ?? 'new'} sheet={sheet} reference={reference} run={run}
              editing={editingCareer} onDone={() => setEditingCareer(null)} />
            <CustomList items={customCareers.map(c => ({ id: c.id, label: `${c.nameRu || c.name} · ${c.careerSkillNames.length} навыков` }))}
              onEdit={id => setEditingCareer(customCareers.find(c => c.id === id)!)}
              onDelete={id => run(() => api.deleteCustomCareer(id), 'Карьера удалена.')} />
          </>
        )}
        {section === 'packs' && (
          <HomebrewPackPanel sheet={sheet} onError={onError} refresh={refresh} />
        )}
      </section>
    </div>
  )
}

type Run = (action: () => Promise<unknown>, successMessage: string) => Promise<void>

function HomebrewPackPanel({ sheet, onError, refresh }: {
  sheet: CharacterSheet
  onError: (message: string) => void
  refresh: () => Promise<void>
}) {
  const [packs, setPacks] = useState<HomebrewPackListItem[]>([])
  const [jsonText, setJsonText] = useState('')
  const [shareText, setShareText] = useState('')
  const [exportText, setExportText] = useState('')
  const [busy, setBusy] = useState(false)

  async function load() {
    setPacks((await api.homebrewPacks()).filter(p => p.system === sheet.system))
  }

  async function act(fn: () => Promise<void>) {
    setBusy(true)
    setShareText('')
    try {
      await fn()
      await load()
      await refresh()
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Ошибка')
    } finally {
      setBusy(false)
    }
  }

  async function importJson() {
    const parsed = JSON.parse(jsonText) as HomebrewPackDocument
    await api.importHomebrewPack(parsed)
    setJsonText('')
  }

  async function exportPack(id: string) {
    const doc = await api.exportHomebrewPack(id)
    setExportText(JSON.stringify(doc, null, 2))
  }

  async function sharePack(id: string) {
    const share = await api.shareHomebrewPack(id)
    setShareText(`Токен: ${share.token}\nПуть: ${share.path}`)
  }

  return (
    <div className="custom-form">
      <p className="hint">
        Импортируйте переносимый JSON `genesysforge.homebrew-pack.v1`. Контент набора можно включать по умолчанию
        или отдельно для текущего персонажа.
      </p>
      <label>JSON набора
        <textarea value={jsonText} onChange={e => setJsonText(e.target.value)} rows={8}
          placeholder='{"format":"genesysforge.homebrew-pack.v1","name":"...","system":"genesysCore"}' />
      </label>
      <div className="form-actions">
        <button className="primary" type="button" disabled={busy || !jsonText.trim()} onClick={() => void act(importJson)}>
          Импортировать JSON
        </button>
        <button type="button" disabled={busy} onClick={() => void act(load)}>Обновить список</button>
      </div>

      <div className="custom-list">
        <div className="label-line">Ваши наборы ({packs.length}):</div>
        {packs.length === 0 && <p className="hint">Пока нет импортированных наборов для этой системы.</p>}
        {packs.map(pack => (
          <div key={pack.id} className="custom-list-row">
            <span>{pack.name} · {pack.entryCount} записей · {pack.isEnabledByDefault ? 'включён по умолчанию' : 'выключен по умолчанию'}</span>
            <span className="custom-list-actions">
              <button className="small" disabled={busy}
                onClick={() => void act(() => api.setHomebrewPackDefault(pack.id, !pack.isEnabledByDefault))}>
                {pack.isEnabledByDefault ? 'Выключить' : 'Включить'}
              </button>
              <button className="small" disabled={busy}
                onClick={() => void act(() => api.setCharacterHomebrewPack(sheet.id, pack.id, true))}>
                Для персонажа: вкл.
              </button>
              <button className="small" disabled={busy}
                onClick={() => void act(() => api.setCharacterHomebrewPack(sheet.id, pack.id, false))}>
                Для персонажа: выкл.
              </button>
              <button className="small" disabled={busy} onClick={() => void act(() => exportPack(pack.id))}>Экспорт</button>
              <button className="small" disabled={busy} onClick={() => void act(() => sharePack(pack.id))}>Поделиться</button>
            </span>
          </div>
        ))}
      </div>
      {shareText && <pre className="code-block">{shareText}</pre>}
      {exportText && (
        <label>Экспортированный JSON
          <textarea value={exportText} readOnly rows={8} />
        </label>
      )}
    </div>
  )
}

function CustomList({ items, onEdit, onDelete }: {
  items: { id: string; label: string }[]
  onEdit: (id: string) => void
  onDelete: (id: string) => void
}) {
  if (items.length === 0) return <p className="hint">Пока нет своего контента в этом разделе.</p>
  return (
    <div className="custom-list">
      <div className="label-line">Ваш контент ({items.length}):</div>
      {items.map(it => (
        <div key={it.id} className="custom-list-row">
          <span>{it.label}</span>
          <span className="custom-list-actions">
            <button className="small" onClick={() => onEdit(it.id)}>Изменить</button>
            <button className="danger small" onClick={() => { if (confirm(`Удалить «${it.label}»?`)) onDelete(it.id) }}>Удалить</button>
          </span>
        </div>
      ))}
    </div>
  )
}

function SkillForm({ sheet, run, editing, onDone }: { sheet: CharacterSheet; run: Run; editing: SkillDef | null; onDone: () => void }) {
  const [name, setName] = useState(editing?.name ?? '')
  const [characteristic, setCharacteristic] = useState<string>(editing?.characteristic ?? 'brawn')
  const [kind, setKind] = useState<string>(editing?.kind ?? 'general')

  function submit(e: FormEvent) {
    e.preventDefault()
    const payload = { system: sheet.system, name, characteristic, kind }
    if (editing) {
      void run(() => api.updateCustomSkill(editing.id, payload), `Навык «${name}» обновлён.`)
      onDone()
    } else {
      void run(() => api.createCustomSkill(payload), `Навык «${name}» создан — он появился в списке навыков листа.`)
      setName('')
    }
  }

  return (
    <form className="custom-form" onSubmit={submit}>
      {editing && <div className="editing-banner">Редактирование: {editing.name}</div>}
      <label>Название<input value={name} onChange={e => setName(e.target.value)} required /></label>
      <label>Характеристика
        <select value={characteristic} onChange={e => setCharacteristic(e.target.value)}>
          {CHARACTERISTICS.map(c => <option key={c} value={c}>{CHARACTERISTIC_LABELS[c]}</option>)}
        </select>
      </label>
      <label>Категория
        <select value={kind} onChange={e => setKind(e.target.value)}>
          <option value="general">Общий</option>
          <option value="combat">Боевой</option>
          <option value="social">Социальный</option>
          <option value="knowledge">Знание</option>
          <option value="magic">Магия</option>
        </select>
      </label>
      <div className="form-actions">
        <button className="primary" type="submit">{editing ? 'Сохранить' : 'Создать навык'}</button>
        {editing && <button type="button" onClick={onDone}>Отмена</button>}
      </div>
    </form>
  )
}

function TalentForm({ sheet, run, editing, onDone }: { sheet: CharacterSheet; run: Run; editing: TalentDef | null; onDone: () => void }) {
  const [name, setName] = useState(editing?.name ?? '')
  const [tier, setTier] = useState(editing?.tier ?? 1)
  const [isRanked, setIsRanked] = useState(editing?.isRanked ?? false)
  const [category, setCategory] = useState<TalentCategory>(editing?.category ?? 'general')
  const [activation, setActivation] = useState(editing?.activation ?? 'Пассивный')
  const [description, setDescription] = useState(editing?.description ?? '')
  const [bonuses, setBonuses] = useState({
    woundBonus: editing?.woundBonus ?? 0,
    strainBonus: editing?.strainBonus ?? 0,
    soakBonus: editing?.soakBonus ?? 0,
    meleeDefenseBonus: editing?.meleeDefenseBonus ?? 0,
    rangedDefenseBonus: editing?.rangedDefenseBonus ?? 0,
  })

  function submit(e: FormEvent) {
    e.preventDefault()
    const payload = { system: sheet.system, name, tier, isRanked, category, activation, description, ...bonuses }
    if (editing) {
      void run(() => api.updateCustomTalent(editing.id, payload), `Талант «${name}» обновлён.`)
      onDone()
    } else {
      void run(() => api.createCustomTalent(payload), `Талант «${name}» (тир ${tier}) создан — его можно купить на вкладке «Таланты».`)
      setName(''); setDescription('')
    }
  }

  const bonusFields: [keyof typeof bonuses, string][] = [
    ['woundBonus', 'Порог ран / ранг'],
    ['strainBonus', 'Порог усталости / ранг'],
    ['soakBonus', 'Поглощение / ранг'],
    ['meleeDefenseBonus', 'Защита ближ. / ранг'],
    ['rangedDefenseBonus', 'Защита дальн. / ранг'],
  ]

  return (
    <form className="custom-form" onSubmit={submit}>
      {editing && <div className="editing-banner">Редактирование: {editing.name}</div>}
      <label>Название<input value={name} onChange={e => setName(e.target.value)} required /></label>
      <label>Тир (1–5)
        <input type="number" min={1} max={5} value={tier} onChange={e => setTier(Number(e.target.value))} />
      </label>
      <label className="checkbox">
        <input type="checkbox" checked={isRanked} onChange={e => setIsRanked(e.target.checked)} />
        Ранговый (можно покупать несколько раз, каждый ранг — на тир выше)
      </label>
      <label>Категория
        <select value={category} onChange={e => setCategory(e.target.value as TalentCategory)}>
          {TALENT_CATEGORIES.map(c => <option key={c} value={c}>{TALENT_CATEGORY_LABELS[c]}</option>)}
        </select>
      </label>
      <label>Активация
        <select value={activation} onChange={e => setActivation(e.target.value)}>
          <option>Пассивный</option>
          <option>Действие</option>
          <option>Манёвр</option>
          <option>Инцидент</option>
        </select>
      </label>
      <label>Описание<textarea value={description} onChange={e => setDescription(e.target.value)} rows={3} /></label>
      <div className="label-line">Пассивные бонусы (применяются автоматически):</div>
      <div className="bonus-grid">
        {bonusFields.map(([key, label]) => (
          <label key={key}>{label}
            <input type="number" value={bonuses[key]}
              onChange={e => setBonuses(b => ({ ...b, [key]: Number(e.target.value) }))} />
          </label>
        ))}
      </div>
      <div className="form-actions">
        <button className="primary" type="submit">{editing ? 'Сохранить' : 'Создать талант'}</button>
        {editing && <button type="button" onClick={onDone}>Отмена</button>}
      </div>
    </form>
  )
}

function ItemForm({ sheet, reference, run, editing, onDone }: { sheet: CharacterSheet; reference: Reference; run: Run; editing: ItemDef | null; onDone: () => void }) {
  const [name, setName] = useState(editing?.name ?? '')
  const [kind, setKind] = useState<string>(editing?.kind ?? 'gear')
  const [description, setDescription] = useState(editing?.description ?? '')
  const [weapon, setWeapon] = useState({
    skillName: editing?.skillName ?? '',
    damage: editing?.damage ?? '',
    crit: editing?.crit ?? '',
    rangeBand: editing?.rangeBand ?? '',
    properties: editing?.properties ?? '',
  })
  const [numbers, setNumbers] = useState({
    encumbrance: editing?.encumbrance ?? 0,
    soakBonus: editing?.soakBonus ?? 0,
    meleeDefense: editing?.meleeDefense ?? 0,
    rangedDefense: editing?.rangedDefense ?? 0,
    encumbranceThresholdBonus: editing?.encumbranceThresholdBonus ?? 0,
    price: editing?.price ?? 0,
    rarity: editing?.rarity ?? 1,
  })

  function submit(e: FormEvent) {
    e.preventDefault()
    const payload = { system: sheet.system, name, kind, description, ...numbers, ...(kind === 'weapon' ? weapon : {}) }
    if (editing) {
      void run(() => api.updateCustomItem(editing.id, payload), `Предмет «${name}» обновлён.`)
      onDone()
    } else {
      void run(() => api.createCustomItem(payload), `Предмет «${name}» создан — его можно добавить в инвентарь.`)
      setName(''); setDescription('')
    }
  }

  const numberFields: [keyof typeof numbers, string][] = [
    ['encumbrance', 'Вес (encumbrance)'],
    ['soakBonus', 'Поглощение (надет)'],
    ['meleeDefense', 'Защита ближ. (надет)'],
    ['rangedDefense', 'Защита дальн. (надет)'],
    ['encumbranceThresholdBonus', 'Бонус порога веса (надет)'],
    ['price', 'Цена'],
    ['rarity', 'Редкость'],
  ]

  return (
    <form className="custom-form" onSubmit={submit}>
      {editing && <div className="editing-banner">Редактирование: {editing.name}</div>}
      <label>Название<input value={name} onChange={e => setName(e.target.value)} required /></label>
      <label>Тип
        <select value={kind} onChange={e => setKind(e.target.value)}>
          <option value="weapon">Оружие</option>
          <option value="armor">Броня</option>
          <option value="gear">Снаряжение</option>
        </select>
      </label>
      {kind === 'weapon' && (
        <>
          <div className="label-line">Боевые характеристики оружия:</div>
          <label>Навык броска
            <select value={weapon.skillName} onChange={e => setWeapon(w => ({ ...w, skillName: e.target.value }))}>
              <option value="">— не задан —</option>
              {sheet.skills.filter(s => s.kind === 'combat').map(s => (
                <option key={s.skillDefId} value={s.name}>{dualName(s)}</option>
              ))}
            </select>
          </label>
          <div className="bonus-grid">
            <label>Урон (например «+3» или «7»)
              <input value={weapon.damage} onChange={e => setWeapon(w => ({ ...w, damage: e.target.value }))} /></label>
            <label>Крит
              <input value={weapon.crit} onChange={e => setWeapon(w => ({ ...w, crit: e.target.value }))} /></label>
            <label>Дистанция
              <input value={weapon.rangeBand} onChange={e => setWeapon(w => ({ ...w, rangeBand: e.target.value }))} /></label>
          </div>
          <label>Свойства
            <input value={weapon.properties} onChange={e => setWeapon(w => ({ ...w, properties: e.target.value }))} /></label>
          <QualityPicker qualities={reference.qualities}
            onAdd={token => setWeapon(w => ({ ...w, properties: appendProperty(w.properties, token) }))} />
        </>
      )}
      <label>Описание<textarea value={description} onChange={e => setDescription(e.target.value)} rows={2} /></label>
      <div className="bonus-grid">
        {numberFields.map(([key, label]) => (
          <label key={key}>{label}
            <input type="number" min={0} value={numbers[key]}
              onChange={e => setNumbers(n => ({ ...n, [key]: Number(e.target.value) }))} />
          </label>
        ))}
      </div>
      <div className="form-actions">
        <button className="primary" type="submit">{editing ? 'Сохранить' : 'Создать предмет'}</button>
        {editing && <button type="button" onClick={onDone}>Отмена</button>}
      </div>
    </form>
  )
}

/** Добавляет канонический токен свойства («Точное 1») в строку properties без дублей по имени. */
function appendProperty(properties: string, token: string): string {
  const norm = (s: string) => s.toLowerCase().replace(/ё/g, 'е').replace(/\s*\d+\s*$/, '').trim()
  const existing = properties.split(',').map(s => s.trim()).filter(Boolean)
  if (existing.some(p => norm(p) === norm(token))) return properties
  return [...existing, token].join(', ')
}

/** Селектор справочного качества (+рейтинг) для формы кастом-предмета (U-10). */
function QualityPicker({ qualities, onAdd }: { qualities: Quality[]; onAdd: (token: string) => void }) {
  const items = qualities.filter(q => q.kind === 'itemQuality')
  const [code, setCode] = useState('')
  const [rating, setRating] = useState(1)
  const selected = items.find(q => q.code === code)

  function add() {
    if (!selected) return
    const token = selected.hasRating ? `${selected.nameRu} ${rating}` : selected.nameRu
    onAdd(token)
    setCode('')
    setRating(1)
  }

  if (items.length === 0) return null
  return (
    <div className="form-row quality-picker">
      <select className="grow" value={code} onChange={e => setCode(e.target.value)}>
        <option value="">— добавить свойство из справочника —</option>
        {items.map(q => (
          <option key={q.code} value={q.code} title={q.safeDescription}>
            {q.nameRu}{q.hasRating ? ' (рейтинг)' : ''}
          </option>
        ))}
      </select>
      {selected?.hasRating && (
        <input className="ranks-input" type="number" min={1} value={rating}
          onChange={e => setRating(Math.max(1, +e.target.value))} title="Рейтинг свойства" />
      )}
      <button type="button" className="small" onClick={add} disabled={!selected}>+ свойство</button>
    </div>
  )
}

function ArchetypeForm({ sheet, run, editing, onDone }: {
  sheet: CharacterSheet
  run: Run
  editing: Archetype | null
  onDone: () => void
}) {
  const [name, setName] = useState(editing?.name ?? '')
  const [nameRu, setNameRu] = useState(editing?.nameRu ?? '')
  const [description, setDescription] = useState(editing?.safeDescription || editing?.description || '')
  const [abilityNameRu, setAbilityNameRu] = useState(editing?.abilities[0]?.nameRu ?? '')
  const [abilityDescription, setAbilityDescription] = useState(editing?.abilities[0]?.safeDescription ?? '')
  const [stats, setStats] = useState({
    brawn: editing?.brawn ?? 2,
    agility: editing?.agility ?? 2,
    intellect: editing?.intellect ?? 2,
    cunning: editing?.cunning ?? 2,
    willpower: editing?.willpower ?? 2,
    presence: editing?.presence ?? 2,
    woundBase: editing?.woundBase ?? 10,
    strainBase: editing?.strainBase ?? 10,
    startingXp: editing?.startingXp ?? 100,
  })

  function submit(e: FormEvent) {
    e.preventDefault()
    const payload: CustomArchetypeInput = { system: sheet.system, name, nameRu, description, abilityNameRu, abilityDescription, ...stats }
    if (editing) {
      void run(() => api.updateCustomArchetype(editing.id, payload), `Архетип «${nameRu || name}» обновлён.`)
      onDone()
    } else {
      void run(() => api.createCustomArchetype(payload), `Архетип «${nameRu || name}» создан — он доступен при создании персонажа.`)
      setName(''); setNameRu(''); setDescription(''); setAbilityNameRu(''); setAbilityDescription('')
    }
  }

  const statFields: [keyof typeof stats, string, number, number][] = [
    ['brawn', 'Мощь', 1, 5],
    ['agility', 'Ловкость', 1, 5],
    ['intellect', 'Интеллект', 1, 5],
    ['cunning', 'Хитрость', 1, 5],
    ['willpower', 'Воля', 1, 5],
    ['presence', 'Присутствие', 1, 5],
    ['woundBase', 'База ран', 1, 30],
    ['strainBase', 'База усталости', 1, 30],
    ['startingXp', 'Стартовый XP', 0, 500],
  ]

  return (
    <form className="custom-form" onSubmit={submit}>
      {editing && <div className="editing-banner">Редактирование: {editing.nameRu || editing.name}</div>}
      <label>Название EN/кодовое<input value={name} onChange={e => setName(e.target.value)} required /></label>
      <label>Название RU<input value={nameRu} onChange={e => setNameRu(e.target.value)} /></label>
      <div className="bonus-grid">
        {statFields.map(([key, label, min, max]) => (
          <label key={key}>{label}
            <input type="number" min={min} max={max} value={stats[key]}
              onChange={e => setStats(s => ({ ...s, [key]: Number(e.target.value) }))} />
          </label>
        ))}
      </div>
      <label>Краткое описание<textarea value={description} onChange={e => setDescription(e.target.value)} rows={2} /></label>
      <label>Способность вида — название<input value={abilityNameRu} onChange={e => setAbilityNameRu(e.target.value)} /></label>
      <label>Способность вида — описание<textarea value={abilityDescription} onChange={e => setAbilityDescription(e.target.value)} rows={3} /></label>
      <div className="form-actions">
        <button className="primary" type="submit">{editing ? 'Сохранить' : 'Создать архетип'}</button>
        {editing && <button type="button" onClick={onDone}>Отмена</button>}
      </div>
    </form>
  )
}

function CareerForm({ sheet, reference, run, editing, onDone }: {
  sheet: CharacterSheet
  reference: Reference
  run: Run
  editing: Career | null
  onDone: () => void
}) {
  const [name, setName] = useState(editing?.name ?? '')
  const [nameRu, setNameRu] = useState(editing?.nameRu ?? '')
  const [description, setDescription] = useState(editing?.safeDescription || editing?.description || '')
  const [startingMoneyFixed, setStartingMoneyFixed] = useState(editing?.startingMoneyFixed ?? 0)
  const [startingMoneyDice, setStartingMoneyDice] = useState(editing?.startingMoneyDice ?? '')
  const [careerSkillNames, setCareerSkillNames] = useState<string[]>(editing?.careerSkillNames ?? [])
  const skills = reference.skills.filter(s => s.kind !== 'magic' || sheet.system === 'realmsOfTerrinoth' || s.name !== 'Verse')

  function toggleSkill(skillName: string) {
    setCareerSkillNames(prev => prev.includes(skillName)
      ? prev.filter(s => s !== skillName)
      : [...prev, skillName])
  }

  function submit(e: FormEvent) {
    e.preventDefault()
    const payload: CustomCareerInput = {
      system: sheet.system, name, nameRu, description,
      careerSkillNames, startingMoneyFixed, startingMoneyDice,
    }
    if (editing) {
      void run(() => api.updateCustomCareer(editing.id, payload), `Карьера «${nameRu || name}» обновлена.`)
      onDone()
    } else {
      void run(() => api.createCustomCareer(payload), `Карьера «${nameRu || name}» создана — она доступна при создании персонажа.`)
      setName(''); setNameRu(''); setDescription(''); setStartingMoneyFixed(0); setStartingMoneyDice(''); setCareerSkillNames([])
    }
  }

  return (
    <form className="custom-form" onSubmit={submit}>
      {editing && <div className="editing-banner">Редактирование: {editing.nameRu || editing.name}</div>}
      <label>Название EN/кодовое<input value={name} onChange={e => setName(e.target.value)} required /></label>
      <label>Название RU<input value={nameRu} onChange={e => setNameRu(e.target.value)} /></label>
      <label>Описание<textarea value={description} onChange={e => setDescription(e.target.value)} rows={2} /></label>
      <div className="bonus-grid">
        <label>Стартовые деньги фикс.
          <input type="number" min={0} value={startingMoneyFixed} onChange={e => setStartingMoneyFixed(Number(e.target.value))} />
        </label>
        <label>Бросок денег (например 1d100)
          <input value={startingMoneyDice} onChange={e => setStartingMoneyDice(e.target.value)} />
        </label>
      </div>
      <div className="label-line">Карьерные навыки ({careerSkillNames.length}):</div>
      <div className="chips">
        {skills.map(s => (
          <button key={s.id} type="button" className={careerSkillNames.includes(s.name) ? 'chip active' : 'chip'}
            onClick={() => toggleSkill(s.name)}>
            {dualName(s)}
          </button>
        ))}
      </div>
      <div className="form-actions">
        <button className="primary" type="submit" disabled={careerSkillNames.length === 0}>
          {editing ? 'Сохранить' : 'Создать карьеру'}
        </button>
        {editing && <button type="button" onClick={onDone}>Отмена</button>}
      </div>
    </form>
  )
}

function HeroicForm({ run, editing, onDone }: { run: Run; editing: HeroicAbility | null; onDone: () => void }) {
  const [name, setName] = useState(editing?.name ?? '')
  const [description, setDescription] = useState(editing?.description ?? '')

  function submit(e: FormEvent) {
    e.preventDefault()
    const payload = { name, description }
    if (editing) {
      void run(() => api.updateCustomHeroicAbility(editing.id, payload), `Способность «${name}» обновлена.`)
      onDone()
    } else {
      void run(() => api.createCustomHeroicAbility(payload), `Героическая способность «${name}» создана — её можно выбрать на вкладке «Лист».`)
      setName(''); setDescription('')
    }
  }

  return (
    <form className="custom-form" onSubmit={submit}>
      {editing && <div className="editing-banner">Редактирование: {editing.name}</div>}
      <label>Название<input value={name} onChange={e => setName(e.target.value)} required /></label>
      <label>Описание (активация, эффект, улучшения за XP)
        <textarea value={description} onChange={e => setDescription(e.target.value)} rows={4} />
      </label>
      <div className="form-actions">
        <button className="primary" type="submit">{editing ? 'Сохранить' : 'Создать способность'}</button>
        {editing && <button type="button" onClick={onDone}>Отмена</button>}
      </div>
    </form>
  )
}
