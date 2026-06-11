import { useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type { CharacterSheet } from '../api/types'
import { CHARACTERISTICS, CHARACTERISTIC_LABELS } from '../utils/labels'

interface Props {
  sheet: CharacterSheet
  onError: (message: string) => void
  refresh: () => Promise<void>
}

type Section = 'skill' | 'talent' | 'item' | 'heroic'

export function CustomTab({ sheet, onError, refresh }: Props) {
  const [section, setSection] = useState<Section>('skill')
  const [notice, setNotice] = useState<string | null>(null)

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

  return (
    <div>
      <section className="panel">
        <h3>Кастомный контент</h3>
        <p className="hint">
          Создавайте собственные навыки, таланты, предметы и героические способности для системы
          «{sheet.system === 'genesysCore' ? 'Genesys Core' : 'Realms of Terrinoth'}».
          Они видны только вам и подчиняются всем правилам системы (пирамида талантов, стоимость XP, пересчёт характеристик).
        </p>
        <div className="tabs">
          <button className={section === 'skill' ? 'tab active' : 'tab'} onClick={() => setSection('skill')}>Навык</button>
          <button className={section === 'talent' ? 'tab active' : 'tab'} onClick={() => setSection('talent')}>Талант</button>
          <button className={section === 'item' ? 'tab active' : 'tab'} onClick={() => setSection('item')}>Предмет</button>
          {sheet.system === 'realmsOfTerrinoth' && (
            <button className={section === 'heroic' ? 'tab active' : 'tab'} onClick={() => setSection('heroic')}>Героич. способность</button>
          )}
        </div>
        {notice && <div className="notice">{notice}</div>}
        {section === 'skill' && <SkillForm sheet={sheet} run={run} />}
        {section === 'talent' && <TalentForm sheet={sheet} run={run} />}
        {section === 'item' && <ItemForm sheet={sheet} run={run} />}
        {section === 'heroic' && sheet.system === 'realmsOfTerrinoth' && <HeroicForm run={run} />}
      </section>
    </div>
  )
}

type Run = (action: () => Promise<unknown>, successMessage: string) => Promise<void>

function SkillForm({ sheet, run }: { sheet: CharacterSheet; run: Run }) {
  const [name, setName] = useState('')
  const [characteristic, setCharacteristic] = useState('brawn')
  const [kind, setKind] = useState('general')

  function submit(e: FormEvent) {
    e.preventDefault()
    void run(() => api.createCustomSkill({ system: sheet.system, name, characteristic, kind }),
      `Навык «${name}» создан — он появился в списке навыков листа.`)
    setName('')
  }

  return (
    <form className="custom-form" onSubmit={submit}>
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
      <button className="primary" type="submit">Создать навык</button>
    </form>
  )
}

function TalentForm({ sheet, run }: { sheet: CharacterSheet; run: Run }) {
  const [name, setName] = useState('')
  const [tier, setTier] = useState(1)
  const [isRanked, setIsRanked] = useState(false)
  const [activation, setActivation] = useState('Пассивный')
  const [description, setDescription] = useState('')
  const [bonuses, setBonuses] = useState({ woundBonus: 0, strainBonus: 0, soakBonus: 0, meleeDefenseBonus: 0, rangedDefenseBonus: 0 })

  function submit(e: FormEvent) {
    e.preventDefault()
    void run(() => api.createCustomTalent({
      system: sheet.system, name, tier, isRanked, activation, description, ...bonuses,
    }), `Талант «${name}» (тир ${tier}) создан — его можно купить на вкладке «Таланты».`)
    setName('')
    setDescription('')
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
      <button className="primary" type="submit">Создать талант</button>
    </form>
  )
}

function ItemForm({ sheet, run }: { sheet: CharacterSheet; run: Run }) {
  const [name, setName] = useState('')
  const [kind, setKind] = useState('gear')
  const [description, setDescription] = useState('')
  const [numbers, setNumbers] = useState({
    encumbrance: 0, soakBonus: 0, meleeDefense: 0, rangedDefense: 0,
    encumbranceThresholdBonus: 0, price: 0, rarity: 1,
  })

  function submit(e: FormEvent) {
    e.preventDefault()
    void run(() => api.createCustomItem({ system: sheet.system, name, kind, description, ...numbers }),
      `Предмет «${name}» создан — его можно добавить в инвентарь.`)
    setName('')
    setDescription('')
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
      <label>Название<input value={name} onChange={e => setName(e.target.value)} required /></label>
      <label>Тип
        <select value={kind} onChange={e => setKind(e.target.value)}>
          <option value="weapon">Оружие</option>
          <option value="armor">Броня</option>
          <option value="gear">Снаряжение</option>
        </select>
      </label>
      <label>Описание (урон, крит, свойства…)<textarea value={description} onChange={e => setDescription(e.target.value)} rows={2} /></label>
      <div className="bonus-grid">
        {numberFields.map(([key, label]) => (
          <label key={key}>{label}
            <input type="number" min={0} value={numbers[key]}
              onChange={e => setNumbers(n => ({ ...n, [key]: Number(e.target.value) }))} />
          </label>
        ))}
      </div>
      <button className="primary" type="submit">Создать предмет</button>
    </form>
  )
}

function HeroicForm({ run }: { run: Run }) {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')

  function submit(e: FormEvent) {
    e.preventDefault()
    void run(() => api.createCustomHeroicAbility({ name, description }),
      `Героическая способность «${name}» создана — её можно выбрать на вкладке «Лист».`)
    setName('')
    setDescription('')
  }

  return (
    <form className="custom-form" onSubmit={submit}>
      <label>Название<input value={name} onChange={e => setName(e.target.value)} required /></label>
      <label>Описание (активация, эффект, улучшения за XP)
        <textarea value={description} onChange={e => setDescription(e.target.value)} rows={4} />
      </label>
      <button className="primary" type="submit">Создать способность</button>
    </form>
  )
}
