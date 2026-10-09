import { useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type { Archetype, Career, CustomArchetypeInput, CustomCareerInput, GameSystem,
  HeroicAbility, ItemDef, Quality, Reference, SkillDef, TalentDef, TalentCategory } from '../api/types'
import { CHARACTERISTICS, CHARACTERISTIC_LABELS, dualName,
  TALENT_CATEGORIES, TALENT_CATEGORY_LABELS } from '../utils/labels'
import { t } from '../i18n'
export type Run = (action: () => Promise<unknown>, successMessage: string) => Promise<void | boolean>
interface FormOptions { packIds?: string[]; formId?: string }

export function SkillForm({ campaignId, system, run, editing, onDone, packIds, formId }: FormOptions & { campaignId?: string; system: GameSystem; run: Run; editing: SkillDef | null; onDone: () => void }) {
  const [name, setName] = useState(editing?.name ?? '')
  const [characteristic, setCharacteristic] = useState<string>(editing?.characteristic ?? 'brawn')
  const [kind, setKind] = useState<string>(editing?.kind ?? 'general')

  async function submit(e: FormEvent) {
    e.preventDefault()
    const payload = { packIds, system, name, characteristic, kind }
    if (editing) {
      const saved = await run(() => api.updateCustomSkill(campaignId, editing.id, payload), t(`Навык «${name}» обновлён.`, `Skill "${name}" updated.`))
      if (saved !== false) onDone()
    } else {
      const saved = await run(() => api.createCustomSkill(campaignId, payload), t(`Навык «${name}» создан — он появился в списке навыков листа.`, `Skill "${name}" created — it now appears in the sheet's skill list.`))
      if (saved !== false) { setName(''); onDone() }
    }
  }

  return (
    <form id={formId} className="custom-form" onSubmit={submit}>
      {editing && <div className="editing-banner">{t('Редактирование:', 'Editing:')} {editing.name}</div>}
      <label>{t('Название', 'Name')}<input value={name} onChange={e => setName(e.target.value)} required /></label>
      <label>{t('Характеристика', 'Characteristic')}
        <select value={characteristic} onChange={e => setCharacteristic(e.target.value)}>
          {CHARACTERISTICS.map(c => <option key={c} value={c}>{CHARACTERISTIC_LABELS[c]}</option>)}
        </select>
      </label>
      <label>{t('Категория', 'Category')}
        <select value={kind} onChange={e => setKind(e.target.value)}>
          <option value="general">{t('Общий', 'General')}</option>
          <option value="combat">{t('Боевой', 'Combat')}</option>
          <option value="social">{t('Социальный', 'Social')}</option>
          <option value="knowledge">{t('Знание', 'Knowledge')}</option>
          <option value="magic">{t('Магия', 'Magic')}</option>
        </select>
      </label>
      <div className="form-actions">
        <button className="primary" type="submit">{editing ? t('Сохранить', 'Save') : t('Создать навык', 'Create skill')}</button>
        {editing && <button type="button" onClick={onDone}>{t('Отмена', 'Cancel')}</button>}
      </div>
    </form>
  )
}

export function TalentForm({ campaignId, system, run, editing, onDone, packIds, formId }: FormOptions & { campaignId?: string; system: GameSystem; run: Run; editing: TalentDef | null; onDone: () => void }) {
  const [name, setName] = useState(editing?.name ?? '')
  const [tier, setTier] = useState(editing?.tier ?? 1)
  const [isRanked, setIsRanked] = useState(editing?.isRanked ?? false)
  const [category, setCategory] = useState<TalentCategory>(editing?.category ?? 'general')
  const [activation, setActivation] = useState(editing?.activation ?? t('Пассивный', 'Passive'))
  const [description, setDescription] = useState(editing?.description ?? '')
  const [bonuses, setBonuses] = useState({
    woundBonus: editing?.woundBonus ?? 0,
    strainBonus: editing?.strainBonus ?? 0,
    soakBonus: editing?.soakBonus ?? 0,
    meleeDefenseBonus: editing?.meleeDefenseBonus ?? 0,
    rangedDefenseBonus: editing?.rangedDefenseBonus ?? 0,
  })

  async function submit(e: FormEvent) {
    e.preventDefault()
    const payload = { packIds, system, name, tier, isRanked, category, activation, description, ...bonuses }
    if (editing) {
      const saved = await run(() => api.updateCustomTalent(campaignId, editing.id, payload), t(`Талант «${name}» обновлён.`, `Talent "${name}" updated.`))
      if (saved !== false) onDone()
    } else {
      const saved = await run(() => api.createCustomTalent(campaignId, payload), t(`Талант «${name}» (тир ${tier}) создан — его можно купить на вкладке «Таланты».`, `Talent "${name}" (tier ${tier}) created — you can buy it on the "Talents" tab.`))
      if (saved !== false) { setName(''); setDescription(''); onDone() }
    }
  }

  const bonusFields: [keyof typeof bonuses, string][] = [
    ['woundBonus', t('Порог ран / ранг', 'Wound threshold / rank')],
    ['strainBonus', t('Порог усталости / ранг', 'Strain threshold / rank')],
    ['soakBonus', t('Поглощение / ранг', 'Soak / rank')],
    ['meleeDefenseBonus', t('Защита ближ. / ранг', 'Melee defense / rank')],
    ['rangedDefenseBonus', t('Защита дальн. / ранг', 'Ranged defense / rank')],
  ]

  return (
    <form id={formId} className="custom-form" onSubmit={submit}>
      {editing && <div className="editing-banner">{t('Редактирование:', 'Editing:')} {editing.name}</div>}
      <label>{t('Название', 'Name')}<input value={name} onChange={e => setName(e.target.value)} required /></label>
      <label>{t('Тир (1–5)', 'Tier (1–5)')}
        <input type="number" min={1} max={5} value={tier} onChange={e => setTier(Number(e.target.value))} />
      </label>
      <label className="checkbox">
        <input type="checkbox" checked={isRanked} onChange={e => setIsRanked(e.target.checked)} />
        {t('Ранговый (можно покупать несколько раз, каждый ранг — на тир выше)', 'Ranked (can be bought multiple times, each rank one tier higher)')}
      </label>
      <label>{t('Категория', 'Category')}
        <select value={category} onChange={e => setCategory(e.target.value as TalentCategory)}>
          {TALENT_CATEGORIES.map(c => <option key={c} value={c}>{TALENT_CATEGORY_LABELS[c]}</option>)}
        </select>
      </label>
      <label>{t('Активация', 'Activation')}
        <select value={activation} onChange={e => setActivation(e.target.value)}>
          <option>{t('Пассивный', 'Passive')}</option>
          <option>{t('Действие', 'Action')}</option>
          <option>{t('Манёвр', 'Maneuver')}</option>
          <option>{t('Инцидент', 'Incidental')}</option>
        </select>
      </label>
      <label>{t('Описание', 'Description')}<textarea value={description} onChange={e => setDescription(e.target.value)} rows={3} /></label>
      <div className="label-line">{t('Пассивные бонусы (применяются автоматически):', 'Passive bonuses (applied automatically):')}</div>
      <div className="bonus-grid">
        {bonusFields.map(([key, label]) => (
          <label key={key}>{label}
            <input type="number" value={bonuses[key]}
              onChange={e => setBonuses(b => ({ ...b, [key]: Number(e.target.value) }))} />
          </label>
        ))}
      </div>
      <div className="form-actions">
        <button className="primary" type="submit">{editing ? t('Сохранить', 'Save') : t('Создать талант', 'Create talent')}</button>
        {editing && <button type="button" onClick={onDone}>{t('Отмена', 'Cancel')}</button>}
      </div>
    </form>
  )
}

export function ItemForm({ campaignId, system, reference, run, editing, onDone, packIds, formId }: FormOptions & { campaignId?: string; system: GameSystem; reference: Reference; run: Run; editing: ItemDef | null; onDone: () => void }) {
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

  async function submit(e: FormEvent) {
    e.preventDefault()
    const payload = { packIds, system, name, kind, description, ...numbers, ...(kind === 'weapon' ? weapon : {}) }
    if (editing) {
      const saved = await run(() => api.updateCustomItem(campaignId, editing.id, payload), t(`Предмет «${name}» обновлён.`, `Item "${name}" updated.`))
      if (saved !== false) onDone()
    } else {
      const saved = await run(() => api.createCustomItem(campaignId, payload), t(`Предмет «${name}» создан — его можно добавить в инвентарь.`, `Item "${name}" created — it can be added to the inventory.`))
      if (saved !== false) { setName(''); setDescription(''); onDone() }
    }
  }

  const numberFields: [keyof typeof numbers, string][] = [
    ['encumbrance', t('Вес (encumbrance)', 'Encumbrance')],
    ['soakBonus', t('Поглощение (надет)', 'Soak (equipped)')],
    ['meleeDefense', t('Защита ближ. (надет)', 'Melee defense (equipped)')],
    ['rangedDefense', t('Защита дальн. (надет)', 'Ranged defense (equipped)')],
    ['encumbranceThresholdBonus', t('Бонус порога веса (надет)', 'Encumbrance threshold bonus (equipped)')],
    ['price', t('Цена', 'Price')],
    ['rarity', t('Редкость', 'Rarity')],
  ]

  return (
    <form id={formId} className="custom-form" onSubmit={submit}>
      {editing && <div className="editing-banner">{t('Редактирование:', 'Editing:')} {editing.name}</div>}
      <label>{t('Название', 'Name')}<input value={name} onChange={e => setName(e.target.value)} required /></label>
      <label>{t('Тип', 'Type')}
        <select value={kind} onChange={e => setKind(e.target.value)}>
          <option value="weapon">{t('Оружие', 'Weapon')}</option>
          <option value="armor">{t('Броня', 'Armor')}</option>
          <option value="gear">{t('Снаряжение', 'Gear')}</option>
        </select>
      </label>
      {kind === 'weapon' && (
        <>
          <div className="label-line">{t('Боевые характеристики оружия:', 'Weapon combat stats:')}</div>
          <label>{t('Навык броска', 'Roll skill')}
            <select value={weapon.skillName} onChange={e => setWeapon(w => ({ ...w, skillName: e.target.value }))}>
              <option value="">{t('— не задан —', '— not set —')}</option>
              {reference.skills.filter(s => s.kind === 'combat').map(s => (
                <option key={s.id} value={s.name}>{dualName(s)}</option>
              ))}
            </select>
          </label>
          <div className="bonus-grid">
            <label>{t('Урон (например «+3» или «7»)', 'Damage (e.g. "+3" or "7")')}
              <input value={weapon.damage} onChange={e => setWeapon(w => ({ ...w, damage: e.target.value }))} /></label>
            <label>{t('Крит', 'Crit')}
              <input value={weapon.crit} onChange={e => setWeapon(w => ({ ...w, crit: e.target.value }))} /></label>
            <label>{t('Дистанция', 'Range')}
              <input value={weapon.rangeBand} onChange={e => setWeapon(w => ({ ...w, rangeBand: e.target.value }))} /></label>
          </div>
          <label>{t('Свойства', 'Properties')}
            <input value={weapon.properties} onChange={e => setWeapon(w => ({ ...w, properties: e.target.value }))} /></label>
          <QualityPicker qualities={reference.qualities}
            onAdd={token => setWeapon(w => ({ ...w, properties: appendProperty(w.properties, token) }))} />
        </>
      )}
      <label>{t('Описание', 'Description')}<textarea value={description} onChange={e => setDescription(e.target.value)} rows={2} /></label>
      <div className="bonus-grid">
        {numberFields.map(([key, label]) => (
          <label key={key}>{label}
            <input type="number" min={0} value={numbers[key]}
              onChange={e => setNumbers(n => ({ ...n, [key]: Number(e.target.value) }))} />
          </label>
        ))}
      </div>
      <div className="form-actions">
        <button className="primary" type="submit">{editing ? t('Сохранить', 'Save') : t('Создать предмет', 'Create item')}</button>
        {editing && <button type="button" onClick={onDone}>{t('Отмена', 'Cancel')}</button>}
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
    const token = selected.hasRating ? `${t(selected.nameRu, selected.nameEn || selected.nameRu)} ${rating}` : t(selected.nameRu, selected.nameEn || selected.nameRu)
    onAdd(token)
    setCode('')
    setRating(1)
  }

  if (items.length === 0) return null
  return (
    <div className="form-row quality-picker">
      <select className="grow" value={code} onChange={e => setCode(e.target.value)}>
        <option value="">{t('— добавить свойство из справочника —', '— add a property from the reference —')}</option>
        {items.map(q => (
          <option key={q.code} value={q.code} title={q.safeDescription}>
            {t(q.nameRu, q.nameEn || q.nameRu)}{q.hasRating ? t(' (рейтинг)', ' (rated)') : ''}
          </option>
        ))}
      </select>
      {selected?.hasRating && (
        <input className="ranks-input" type="number" min={1} value={rating}
          onChange={e => setRating(Math.max(1, +e.target.value))} title={t('Рейтинг свойства', 'Property rating')} />
      )}
      <button type="button" className="small" onClick={add} disabled={!selected}>{t('+ свойство', '+ property')}</button>
    </div>
  )
}

export function ArchetypeForm({ campaignId, system, run, editing, onDone, packIds, formId }: FormOptions & {
  campaignId?: string
  system: GameSystem
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

  async function submit(e: FormEvent) {
    e.preventDefault()
    const payload: CustomArchetypeInput = { packIds, system, name, nameRu, description, abilityNameRu, abilityDescription, ...stats }
    if (editing) {
      const saved = await run(() => api.updateCustomArchetype(campaignId, editing.id, payload), t(`Архетип «${nameRu || name}» обновлён.`, `Archetype "${nameRu || name}" updated.`))
      if (saved !== false) onDone()
    } else {
      const saved = await run(() => api.createCustomArchetype(campaignId, payload), t(`Архетип «${nameRu || name}» создан для кампании.`, `Archetype "${nameRu || name}" created for the campaign.`))
      if (saved !== false) { setName(''); setNameRu(''); setDescription(''); setAbilityNameRu(''); setAbilityDescription(''); onDone() }
    }
  }

  const statFields: [keyof typeof stats, string, number, number][] = [
    ['brawn', CHARACTERISTIC_LABELS.brawn, 1, 5],
    ['agility', CHARACTERISTIC_LABELS.agility, 1, 5],
    ['intellect', CHARACTERISTIC_LABELS.intellect, 1, 5],
    ['cunning', CHARACTERISTIC_LABELS.cunning, 1, 5],
    ['willpower', CHARACTERISTIC_LABELS.willpower, 1, 5],
    ['presence', CHARACTERISTIC_LABELS.presence, 1, 5],
    ['woundBase', t('База ран', 'Wound base'), 1, 30],
    ['strainBase', t('База усталости', 'Strain base'), 1, 30],
    ['startingXp', t('Стартовый XP', 'Starting XP'), 0, 500],
  ]

  return (
    <form id={formId} className="custom-form" onSubmit={submit}>
      {editing && <div className="editing-banner">{t('Редактирование:', 'Editing:')} {editing.nameRu || editing.name}</div>}
      <label>{t('Название EN/кодовое', 'Name EN/code')}<input value={name} onChange={e => setName(e.target.value)} required /></label>
      <label>{t('Название RU', 'Name RU')}<input value={nameRu} onChange={e => setNameRu(e.target.value)} /></label>
      <div className="bonus-grid">
        {statFields.map(([key, label, min, max]) => (
          <label key={key}>{label}
            <input type="number" min={min} max={max} value={stats[key]}
              onChange={e => setStats(s => ({ ...s, [key]: Number(e.target.value) }))} />
          </label>
        ))}
      </div>
      <label>{t('Краткое описание', 'Short description')}<textarea value={description} onChange={e => setDescription(e.target.value)} rows={2} /></label>
      <label>{t('Способность вида — название', 'Species ability — name')}<input value={abilityNameRu} onChange={e => setAbilityNameRu(e.target.value)} /></label>
      <label>{t('Способность вида — описание', 'Species ability — description')}<textarea value={abilityDescription} onChange={e => setAbilityDescription(e.target.value)} rows={3} /></label>
      <div className="form-actions">
        <button className="primary" type="submit">{editing ? t('Сохранить', 'Save') : t('Создать архетип', 'Create archetype')}</button>
        {editing && <button type="button" onClick={onDone}>{t('Отмена', 'Cancel')}</button>}
      </div>
    </form>
  )
}

export function CareerForm({ campaignId, system, reference, run, editing, onDone, packIds, formId }: FormOptions & {
  campaignId?: string
  system: GameSystem
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
  const skills = reference.skills.filter(s => s.kind !== 'magic' || system === 'realmsOfTerrinoth' || s.name !== 'Verse')

  function toggleSkill(skillName: string) {
    setCareerSkillNames(prev => prev.includes(skillName)
      ? prev.filter(s => s !== skillName)
      : [...prev, skillName])
  }

  async function submit(e: FormEvent) {
    e.preventDefault()
    const payload: CustomCareerInput = { packIds,
      system, name, nameRu, description,
      careerSkillNames, startingMoneyFixed, startingMoneyDice,
    }
    if (editing) {
      const saved = await run(() => api.updateCustomCareer(campaignId, editing.id, payload), t(`Карьера «${nameRu || name}» обновлена.`, `Career "${nameRu || name}" updated.`))
      if (saved !== false) onDone()
    } else {
      const saved = await run(() => api.createCustomCareer(campaignId, payload), t(`Карьера «${nameRu || name}» создана для кампании.`, `Career "${nameRu || name}" created for the campaign.`))
      if (saved !== false) { setName(''); setNameRu(''); setDescription(''); setStartingMoneyFixed(0); setStartingMoneyDice(''); setCareerSkillNames([]); onDone() }
    }
  }

  return (
    <form id={formId} className="custom-form" onSubmit={submit}>
      {editing && <div className="editing-banner">{t('Редактирование:', 'Editing:')} {editing.nameRu || editing.name}</div>}
      <label>{t('Название EN/кодовое', 'Name EN/code')}<input value={name} onChange={e => setName(e.target.value)} required /></label>
      <label>{t('Название RU', 'Name RU')}<input value={nameRu} onChange={e => setNameRu(e.target.value)} /></label>
      <label>{t('Описание', 'Description')}<textarea value={description} onChange={e => setDescription(e.target.value)} rows={2} /></label>
      <div className="bonus-grid">
        <label>{t('Стартовые деньги фикс.', 'Starting money (fixed)')}
          <input type="number" min={0} value={startingMoneyFixed} onChange={e => setStartingMoneyFixed(Number(e.target.value))} />
        </label>
        <label>{t('Бросок денег (например 1d100)', 'Money roll (e.g. 1d100)')}
          <input value={startingMoneyDice} onChange={e => setStartingMoneyDice(e.target.value)} />
        </label>
      </div>
      <div className="label-line">{t('Карьерные навыки', 'Career skills')} ({careerSkillNames.length}):</div>
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
          {editing ? t('Сохранить', 'Save') : t('Создать карьеру', 'Create career')}
        </button>
        {editing && <button type="button" onClick={onDone}>{t('Отмена', 'Cancel')}</button>}
      </div>
    </form>
  )
}

export function HeroicForm({ campaignId, run, editing, onDone, packIds, formId }: FormOptions & { campaignId?: string; run: Run; editing: HeroicAbility | null; onDone: () => void }) {
  const [name, setName] = useState(editing?.name ?? '')
  const [description, setDescription] = useState(editing?.description ?? '')

  async function submit(e: FormEvent) {
    e.preventDefault()
    const payload = { packIds, name, description }
    if (editing) {
      const saved = await run(() => api.updateCustomHeroicAbility(campaignId, editing.id, payload), t(`Способность «${name}» обновлена.`, `Ability "${name}" updated.`))
      if (saved !== false) onDone()
    } else {
      const saved = await run(() => api.createCustomHeroicAbility(campaignId, payload), t(`Героическая способность «${name}» создана — её можно выбрать на вкладке «Лист».`, `Heroic ability "${name}" created — you can pick it on the "Sheet" tab.`))
      if (saved !== false) { setName(''); setDescription(''); onDone() }
    }
  }

  return (
    <form id={formId} className="custom-form" onSubmit={submit}>
      {editing && <div className="editing-banner">{t('Редактирование:', 'Editing:')} {editing.name}</div>}
      <label>{t('Название', 'Name')}<input value={name} onChange={e => setName(e.target.value)} required /></label>
      <label>{t('Описание (активация, эффект, улучшения за XP)', 'Description (activation, effect, XP upgrades)')}
        <textarea value={description} onChange={e => setDescription(e.target.value)} rows={4} />
      </label>
      <div className="form-actions">
        <button className="primary" type="submit">{editing ? t('Сохранить', 'Save') : t('Создать способность', 'Create ability')}</button>
        {editing && <button type="button" onClick={onDone}>{t('Отмена', 'Cancel')}</button>}
      </div>
    </form>
  )
}
