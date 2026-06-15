import { useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type { CharacterSheet, HeroicAbility, ItemDef, Reference, SkillDef, TalentDef } from '../api/types'
import { CHARACTERISTICS, CHARACTERISTIC_LABELS, ITEM_KIND_LABELS, SKILL_KIND_LABELS } from '../utils/labels'

interface Props {
  sheet: CharacterSheet
  reference: Reference
  onError: (message: string) => void
  refresh: () => Promise<void>
}

type Section = 'skill' | 'talent' | 'item' | 'heroic'

export function CustomTab({ sheet, reference, onError, refresh }: Props) {
  const [section, setSection] = useState<Section>('skill')
  const [notice, setNotice] = useState<string | null>(null)
  // редактируемый объект текущей секции (null — режим создания)
  const [editingSkill, setEditingSkill] = useState<SkillDef | null>(null)
  const [editingTalent, setEditingTalent] = useState<TalentDef | null>(null)
  const [editingItem, setEditingItem] = useState<ItemDef | null>(null)
  const [editingHeroic, setEditingHeroic] = useState<HeroicAbility | null>(null)

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

  return (
    <div>
      <section className="panel">
        <h3>Кастомный контент</h3>
        <p className="hint">
          Создавайте и редактируйте собственные навыки, таланты, предметы и героические способности для системы
          «{sheet.system === 'genesysCore' ? 'Genesys Core' : 'Realms of Terrinoth'}».
          Они видны только вам и подчиняются всем правилам системы. Удаление недоступно, пока контент используется персонажем.
        </p>
        <div className="tabs">
          <button className={section === 'skill' ? 'tab active' : 'tab'} onClick={() => setSection('skill')}>Навыки</button>
          <button className={section === 'talent' ? 'tab active' : 'tab'} onClick={() => setSection('talent')}>Таланты</button>
          <button className={section === 'item' ? 'tab active' : 'tab'} onClick={() => setSection('item')}>Предметы</button>
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
            <CustomList items={customTalents.map(t => ({ id: t.id, label: `${t.name} · Тир ${t.tier}${t.isRanked ? ' · ранговый' : ''}` }))}
              onEdit={id => setEditingTalent(customTalents.find(t => t.id === id)!)}
              onDelete={id => run(() => api.deleteCustomTalent(id), 'Талант удалён.')} />
          </>
        )}
        {section === 'item' && (
          <>
            <ItemForm key={editingItem?.id ?? 'new'} sheet={sheet} run={run} editing={editingItem}
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
      </section>
    </div>
  )
}

type Run = (action: () => Promise<unknown>, successMessage: string) => Promise<void>

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
    const payload = { system: sheet.system, name, tier, isRanked, activation, description, ...bonuses }
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
    ['strainBonus', 'Порог стрейна / ранг'],
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

function ItemForm({ sheet, run, editing, onDone }: { sheet: CharacterSheet; run: Run; editing: ItemDef | null; onDone: () => void }) {
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
                <option key={s.skillDefId} value={s.name}>{s.name}</option>
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
