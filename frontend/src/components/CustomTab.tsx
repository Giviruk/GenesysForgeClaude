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
import { t } from '../i18n'

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
      onError(err instanceof Error ? err.message : t('Ошибка', 'Error'))
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
        <h3>{t('Кастомный контент', 'Custom content')}</h3>
        <p className="hint">
          {t(
            `Создавайте и редактируйте собственные навыки, таланты, предметы и героические способности для системы «${sheet.system === 'genesysCore' ? 'Genesys Core' : 'Realms of Terrinoth'}». ` +
            'Кастом привязан к вашему аккаунту, а не к этому персонажу: он виден только вам, но доступен ' +
            'во всех ваших персонажах и NPC этой системы. Удаление недоступно, пока контент используется персонажем.',
            `Create and edit your own skills, talents, items and heroic abilities for the “${sheet.system === 'genesysCore' ? 'Genesys Core' : 'Realms of Terrinoth'}” system. ` +
            'Custom content belongs to your account, not to this character: only you can see it, but it is available ' +
            'to all your characters and NPCs of this system. Deletion is unavailable while the content is in use by a character.',
          )}
        </p>
        <div className="tabs">
          <button className={section === 'skill' ? 'tab active' : 'tab'} onClick={() => setSection('skill')}>{t('Навыки', 'Skills')}</button>
          <button className={section === 'talent' ? 'tab active' : 'tab'} onClick={() => setSection('talent')}>{t('Таланты', 'Talents')}</button>
          <button className={section === 'item' ? 'tab active' : 'tab'} onClick={() => setSection('item')}>{t('Предметы', 'Items')}</button>
          <button className={section === 'archetype' ? 'tab active' : 'tab'} onClick={() => setSection('archetype')}>{t('Архетипы', 'Archetypes')}</button>
          <button className={section === 'career' ? 'tab active' : 'tab'} onClick={() => setSection('career')}>{t('Карьеры', 'Careers')}</button>
          <button className={section === 'packs' ? 'tab active' : 'tab'} onClick={() => setSection('packs')}>{t('Наборы JSON', 'JSON packs')}</button>
          {sheet.system === 'realmsOfTerrinoth' && (
            <button className={section === 'heroic' ? 'tab active' : 'tab'} onClick={() => setSection('heroic')}>{t('Героич. способности', 'Heroic abilities')}</button>
          )}
        </div>
        {notice && <div className="notice">{notice}</div>}

        {section === 'skill' && (
          <>
            <SkillForm key={editingSkill?.id ?? 'new'} sheet={sheet} run={run} editing={editingSkill}
              onDone={() => setEditingSkill(null)} />
            <CustomList items={customSkills.map(s => ({ id: s.id, label: `${s.name} · ${CHARACTERISTIC_LABELS[s.characteristic]} · ${SKILL_KIND_LABELS[s.kind]}` }))}
              onEdit={id => setEditingSkill(customSkills.find(s => s.id === id)!)}
              onDelete={id => run(() => api.deleteCustomSkill(id), t('Навык удалён.', 'Skill deleted.'))} />
          </>
        )}
        {section === 'talent' && (
          <>
            <TalentForm key={editingTalent?.id ?? 'new'} sheet={sheet} run={run} editing={editingTalent}
              onDone={() => setEditingTalent(null)} />
            <CustomList items={customTalents.map(tal => ({
              id: tal.id,
              label: `${tal.name} · ${TALENT_CATEGORY_LABELS[tal.category]} · ${t('Тир', 'Tier')} ${tal.tier}${tal.isRanked ? t(' · ранговый', ' · ranked') : ''}`,
            }))}
              onEdit={id => setEditingTalent(customTalents.find(tal => tal.id === id)!)}
              onDelete={id => run(() => api.deleteCustomTalent(id), t('Талант удалён.', 'Talent deleted.'))} />
          </>
        )}
        {section === 'item' && (
          <>
            <ItemForm key={editingItem?.id ?? 'new'} sheet={sheet} reference={reference} run={run} editing={editingItem}
              onDone={() => setEditingItem(null)} />
            <CustomList items={customItems.map(i => ({ id: i.id, label: `${i.name} · ${ITEM_KIND_LABELS[i.kind]} · ${t('вес', 'enc.')} ${i.encumbrance}` }))}
              onEdit={id => setEditingItem(customItems.find(i => i.id === id)!)}
              onDelete={id => run(() => api.deleteCustomItem(id), t('Предмет удалён.', 'Item deleted.'))} />
          </>
        )}
        {section === 'heroic' && sheet.system === 'realmsOfTerrinoth' && (
          <>
            <HeroicForm key={editingHeroic?.id ?? 'new'} run={run} editing={editingHeroic}
              onDone={() => setEditingHeroic(null)} />
            <CustomList items={customHeroics.map(h => ({ id: h.id, label: h.name }))}
              onEdit={id => setEditingHeroic(customHeroics.find(h => h.id === id)!)}
              onDelete={id => run(() => api.deleteCustomHeroicAbility(id), t('Способность удалена.', 'Ability deleted.'))} />
          </>
        )}
        {section === 'archetype' && (
          <>
            <ArchetypeForm key={editingArchetype?.id ?? 'new'} sheet={sheet} run={run}
              editing={editingArchetype} onDone={() => setEditingArchetype(null)} />
            <CustomList items={customArchetypes.map(a => ({ id: a.id, label: `${a.nameRu || a.name} · XP ${a.startingXp}` }))}
              onEdit={id => setEditingArchetype(customArchetypes.find(a => a.id === id)!)}
              onDelete={id => run(() => api.deleteCustomArchetype(id), t('Архетип удалён.', 'Archetype deleted.'))} />
          </>
        )}
        {section === 'career' && (
          <>
            <CareerForm key={editingCareer?.id ?? 'new'} sheet={sheet} reference={reference} run={run}
              editing={editingCareer} onDone={() => setEditingCareer(null)} />
            <CustomList items={customCareers.map(c => ({ id: c.id, label: `${c.nameRu || c.name} · ${c.careerSkillNames.length} ${t('навыков', 'skills')}` }))}
              onEdit={id => setEditingCareer(customCareers.find(c => c.id === id)!)}
              onDelete={id => run(() => api.deleteCustomCareer(id), t('Карьера удалена.', 'Career deleted.'))} />
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
      onError(err instanceof Error ? err.message : t('Ошибка', 'Error'))
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
    setShareText(t(`Токен: ${share.token}\nПуть: ${share.path}`, `Token: ${share.token}\nPath: ${share.path}`))
  }

  return (
    <div className="custom-form">
      <p className="hint">
        {t(
          'Импортируйте переносимый JSON `genesysforge.homebrew-pack.v1`. Контент набора можно включать по умолчанию ' +
          'или отдельно для текущего персонажа.',
          'Import a portable `genesysforge.homebrew-pack.v1` JSON. Pack content can be enabled by default ' +
          'or individually for the current character.',
        )}
      </p>
      <label>{t('JSON набора', 'Pack JSON')}
        <textarea value={jsonText} onChange={e => setJsonText(e.target.value)} rows={8}
          placeholder='{"format":"genesysforge.homebrew-pack.v1","name":"...","system":"genesysCore"}' />
      </label>
      <div className="form-actions">
        <button className="primary" type="button" disabled={busy || !jsonText.trim()} onClick={() => void act(importJson)}>
          {t('Импортировать JSON', 'Import JSON')}
        </button>
        <button type="button" disabled={busy} onClick={() => void act(load)}>{t('Обновить список', 'Refresh list')}</button>
      </div>

      <div className="custom-list">
        <div className="label-line">{t('Ваши наборы', 'Your packs')} ({packs.length}):</div>
        {packs.length === 0 && <p className="hint">{t('Пока нет импортированных наборов для этой системы.', 'No imported packs for this system yet.')}</p>}
        {packs.map(pack => (
          <div key={pack.id} className="custom-list-row">
            <span>{pack.name} · {pack.entryCount} {t('записей', 'entries')} · {pack.isEnabledByDefault ? t('включён по умолчанию', 'enabled by default') : t('выключен по умолчанию', 'disabled by default')}</span>
            <span className="custom-list-actions">
              <button className="small" disabled={busy}
                onClick={() => void act(() => api.setHomebrewPackDefault(pack.id, !pack.isEnabledByDefault))}>
                {pack.isEnabledByDefault ? t('Выключить', 'Disable') : t('Включить', 'Enable')}
              </button>
              <button className="small" disabled={busy}
                onClick={() => void act(() => api.setCharacterHomebrewPack(sheet.id, pack.id, true))}>
                {t('Для персонажа: вкл.', 'For character: on')}
              </button>
              <button className="small" disabled={busy}
                onClick={() => void act(() => api.setCharacterHomebrewPack(sheet.id, pack.id, false))}>
                {t('Для персонажа: выкл.', 'For character: off')}
              </button>
              <button className="small" disabled={busy} onClick={() => void act(() => exportPack(pack.id))}>{t('Экспорт', 'Export')}</button>
              <button className="small" disabled={busy} onClick={() => void act(() => sharePack(pack.id))}>{t('Поделиться', 'Share')}</button>
            </span>
          </div>
        ))}
      </div>
      {shareText && <pre className="code-block">{shareText}</pre>}
      {exportText && (
        <label>{t('Экспортированный JSON', 'Exported JSON')}
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
  if (items.length === 0) return <p className="hint">{t('Пока нет своего контента в этом разделе.', 'No custom content in this section yet.')}</p>
  return (
    <div className="custom-list">
      <div className="label-line">{t('Ваш контент', 'Your content')} ({items.length}):</div>
      {items.map(it => (
        <div key={it.id} className="custom-list-row">
          <span>{it.label}</span>
          <span className="custom-list-actions">
            <button className="small" onClick={() => onEdit(it.id)}>{t('Изменить', 'Edit')}</button>
            <button className="danger small" onClick={() => { if (confirm(t(`Удалить «${it.label}»?`, `Delete "${it.label}"?`))) onDelete(it.id) }}>{t('Удалить', 'Delete')}</button>
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
      void run(() => api.updateCustomSkill(editing.id, payload), t(`Навык «${name}» обновлён.`, `Skill "${name}" updated.`))
      onDone()
    } else {
      void run(() => api.createCustomSkill(payload), t(`Навык «${name}» создан — он появился в списке навыков листа.`, `Skill "${name}" created — it now appears in the sheet's skill list.`))
      setName('')
    }
  }

  return (
    <form className="custom-form" onSubmit={submit}>
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

function TalentForm({ sheet, run, editing, onDone }: { sheet: CharacterSheet; run: Run; editing: TalentDef | null; onDone: () => void }) {
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

  function submit(e: FormEvent) {
    e.preventDefault()
    const payload = { system: sheet.system, name, tier, isRanked, category, activation, description, ...bonuses }
    if (editing) {
      void run(() => api.updateCustomTalent(editing.id, payload), t(`Талант «${name}» обновлён.`, `Talent "${name}" updated.`))
      onDone()
    } else {
      void run(() => api.createCustomTalent(payload), t(`Талант «${name}» (тир ${tier}) создан — его можно купить на вкладке «Таланты».`, `Talent "${name}" (tier ${tier}) created — you can buy it on the "Talents" tab.`))
      setName(''); setDescription('')
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
    <form className="custom-form" onSubmit={submit}>
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
      void run(() => api.updateCustomItem(editing.id, payload), t(`Предмет «${name}» обновлён.`, `Item "${name}" updated.`))
      onDone()
    } else {
      void run(() => api.createCustomItem(payload), t(`Предмет «${name}» создан — его можно добавить в инвентарь.`, `Item "${name}" created — it can be added to the inventory.`))
      setName(''); setDescription('')
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
    <form className="custom-form" onSubmit={submit}>
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
              {sheet.skills.filter(s => s.kind === 'combat').map(s => (
                <option key={s.skillDefId} value={s.name}>{dualName(s)}</option>
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
      void run(() => api.updateCustomArchetype(editing.id, payload), t(`Архетип «${nameRu || name}» обновлён.`, `Archetype "${nameRu || name}" updated.`))
      onDone()
    } else {
      void run(() => api.createCustomArchetype(payload), t(`Архетип «${nameRu || name}» создан — он доступен при создании персонажа.`, `Archetype "${nameRu || name}" created — it is available during character creation.`))
      setName(''); setNameRu(''); setDescription(''); setAbilityNameRu(''); setAbilityDescription('')
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
    <form className="custom-form" onSubmit={submit}>
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
      void run(() => api.updateCustomCareer(editing.id, payload), t(`Карьера «${nameRu || name}» обновлена.`, `Career "${nameRu || name}" updated.`))
      onDone()
    } else {
      void run(() => api.createCustomCareer(payload), t(`Карьера «${nameRu || name}» создана — она доступна при создании персонажа.`, `Career "${nameRu || name}" created — it is available during character creation.`))
      setName(''); setNameRu(''); setDescription(''); setStartingMoneyFixed(0); setStartingMoneyDice(''); setCareerSkillNames([])
    }
  }

  return (
    <form className="custom-form" onSubmit={submit}>
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

function HeroicForm({ run, editing, onDone }: { run: Run; editing: HeroicAbility | null; onDone: () => void }) {
  const [name, setName] = useState(editing?.name ?? '')
  const [description, setDescription] = useState(editing?.description ?? '')

  function submit(e: FormEvent) {
    e.preventDefault()
    const payload = { name, description }
    if (editing) {
      void run(() => api.updateCustomHeroicAbility(editing.id, payload), t(`Способность «${name}» обновлена.`, `Ability "${name}" updated.`))
      onDone()
    } else {
      void run(() => api.createCustomHeroicAbility(payload), t(`Героическая способность «${name}» создана — её можно выбрать на вкладке «Лист».`, `Heroic ability "${name}" created — you can pick it on the "Sheet" tab.`))
      setName(''); setDescription('')
    }
  }

  return (
    <form className="custom-form" onSubmit={submit}>
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
