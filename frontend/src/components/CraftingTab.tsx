import { useCallback, useEffect, useMemo, useState } from 'react'
import { api } from '../api/client'
import type {
  CharacterSheet, CraftingKind, CraftingPreview, CraftingProject, CraftingProjectInput,
  CraftingSpend, CraftingSpendChoice, CraftingSymbol, ImplementMaterial, ItemDef, Reference,
  WeaponCraftsmanship,
} from '../api/types'
import {
  ITEM_KIND_LABELS,
  IMPLEMENT_MATERIAL_HINTS, IMPLEMENT_MATERIAL_LABELS, localizedName, localizedDescription,
  WEAPON_CRAFTSMANSHIPS, WEAPON_CRAFTSMANSHIP_HINTS, WEAPON_CRAFTSMANSHIP_LABELS,
} from '../utils/labels'
import { t } from '../i18n'
import { craftsmanshipApplies } from '../utils/craftsmanship'
import { IMPLEMENT_MATERIALS } from '../utils/implements'
import { Icon, type IconName } from './Icon'
import { FilterChip, CheckRow } from './content/ContentUi'
import { NumberStepper } from './content/NumberStepper'
import { useDiceRoller } from '../dice-roller-store'

interface Props {
  sheet: CharacterSheet
  reference: Reference
  onError: (message: string) => void
  refresh: () => Promise<void>
}

/** Доли стоимости компонентов — те же, что при покупке (ROT-ECO-01). */
const COST_PERCENTS = [50, 75, 100, 125, 150, 175, 200]

/** Рецепты ROT-ALCH-01; backend повторно проверяет kind, поэтому UI не является границей доверия. */
const POTION_CODES = new Set([
  'acid-flask', 'bottled-courage', 'health-elixir', 'immunity-elixir',
  'invisibility-potion', 'poison', 'power-potion', 'protective-tonic',
  'regeneration-elixir', 'smokebomb-vial', 'speed-potion', 'stamina-elixir',
])

const bareCode = (code: string) => code.slice(code.lastIndexOf('.') + 1)
const isPotion = (item: ItemDef) => POTION_CODES.has(bareCode(item.code))

const KIND_LABELS: Record<CraftingKind, string> = {
  item: t('Изготовление', 'Crafting'),
  potion: t('Варка зелья', 'Brewing'),
  enchantment: t('Зачарование', 'Enchanting'),
}

const SYMBOL_GLYPH: Record<CraftingSymbol, string> = {
  advantage: '▲', threat: '▼', triumph: '★', despair: '☠',
}

const SYMBOL_LABELS: Record<CraftingSymbol, string> = {
  advantage: t('Преимущества', 'Advantages'),
  threat: t('Угрозы', 'Threats'),
  triumph: t('Триумфы', 'Triumphs'),
  despair: t('Отчаяния', 'Despairs'),
}

const DIFFICULTY_LABELS = [
  t('Простая', 'Simple'), t('Лёгкая', 'Easy'), t('Средняя', 'Average'),
  t('Трудная', 'Hard'), t('Устрашающая', 'Daunting'), t('Грозная', 'Formidable'),
]

const RESOURCES_HINT = t(
  'Инструменты, компоненты и ингредиенты — описание: приложение их не списывает и наличия не '
  + 'проверяет. Стоимость считается и попадает в историю, но кошелька не касается.',
  'Tools, components and ingredients are description only: the app neither consumes them nor checks '
  + 'that you have them. The cost is computed and recorded in history, but never charged.',
)

const ROLL_HINT = t(
  'Бросок делаете вы в роллере, а сюда вписываете полученные символы — так же, как нетто-успехи '
  + 'при продаже. Сложность, время, стоимость и каждый эффект траты считает сервер.',
  'You roll in the dice roller and enter the symbols here — the same way net successes work for a '
  + 'sale. The server computes difficulty, time, cost and every spend effect.',
)

const ENCHANT_HINT = t(
  'Зачарование начинается с уже превосходной основы, а его способность согласуется заранее. '
  + 'Рекомендованная сложность — Грозная (5); для незначительного эффекта ведущий может опустить '
  + 'её до Трудной (3), указав причину. Именную реликвию этот путь не создаёт.',
  'Enchanting starts from a base that is already Superior, and its ability is agreed in advance. '
  + 'The recommended difficulty is Formidable (5); for a minor effect the GM may lower it to Hard (3) '
  + 'with a reason. This path never produces one of the named relics.',
)

/** Сколько символов уже расписано по тратам — чтобы не обещать больше, чем выпало. */
function spentSymbols(choices: CraftingSpendChoice[], spends: CraftingSpend[]): Record<CraftingSymbol, number> {
  const spent: Record<CraftingSymbol, number> = { advantage: 0, threat: 0, triumph: 0, despair: 0 }
  for (const choice of choices) {
    const def = spends.find(s => s.code === choice.code)
    if (!def) continue
    const unit = choice.paidWith === 'advantage' ? def.advantageCost
      : choice.paidWith === 'threat' ? def.threatCost
        : choice.paidWith === 'triumph' ? def.triumphCost : def.despairCost
    spent[choice.paidWith] += unit * (choice.count ?? 1)
  }
  return spent
}

/** Какими символами оплачивается строка таблицы. */
function payments(def: CraftingSpend): Array<[CraftingSymbol, number]> {
  const all: Array<[CraftingSymbol, number]> = [
    ['advantage', def.advantageCost], ['threat', def.threatCost],
    ['triumph', def.triumphCost], ['despair', def.despairCost],
  ]
  return all.filter(([, cost]) => cost > 0)
}

/**
 * Ремесло: изготовление, варка и зачарование (ROT-CRAFT-01, ROT-ALCH-02, ROT-CRAFT-MAGIC-01).
 *
 * <p>Вкладка доступна и игроку, и ведущему — отдельного gm-режима у ремесла нет. Требования по
 * ресурсам остаются текстом: приложение ничего не списывает и наличия не проверяет.</p>
 */
export function CraftingTab({ sheet, reference, onError, refresh }: Props) {
  const [projects, setProjects] = useState<CraftingProject[]>([])
  const [kind, setKind] = useState<CraftingKind>('item')
  const [targetId, setTargetId] = useState('')
  const [baseItemId, setBaseItemId] = useState('')
  const [percent, setPercent] = useState(100)
  const [ownCost, setOwnCost] = useState('')
  const [costReason, setCostReason] = useState('')
  const [difficulty, setDifficulty] = useState('')
  const [difficultyReason, setDifficultyReason] = useState('')
  const [time, setTime] = useState('')
  const [timeReason, setTimeReason] = useState('')
  const [requirements, setRequirements] = useState('')
  const [intent, setIntent] = useState('')
  const [magicSkillName, setMagicSkillName] = useState(
    () => sheet.skills.find(s => s.kind === 'magic')?.name ?? '')
  const [rough, setRough] = useState(false)
  const [craftsmanship, setCraftsmanship] = useState<WeaponCraftsmanship>('steel')
  const [material, setMaterial] = useState<ImplementMaterial>('oak')
  const [preview, setPreview] = useState<CraftingPreview | null>(null)
  const [busy, setBusy] = useState(false)
  const [search, setSearch] = useState('')

  const loadProjects = useCallback(
    () => api.crafting(sheet.id)
      .then(setProjects)
      .catch((err: unknown) => onError(err instanceof Error ? err.message : String(err))),
    [sheet.id, onError])
  useEffect(() => { void loadProjects() }, [loadProjects])

  // Зачарование идёт от вещи в инвентаре, всё остальное — от записи каталога.
  const catalogCandidates: ItemDef[] = useMemo(() => reference.items
    .filter(i => i.price !== null && i.rarity !== null)
    .filter(i => kind === 'potion' ? isPotion(i) : !isPotion(i)),
  [reference.items, kind])
  const candidates = useMemo(() => catalogCandidates
    .filter(i => kind === 'potion' || i.shopCategory !== 'service'),
  [catalogCandidates, kind])
  const unavailableCandidates = useMemo(() => kind === 'item'
    ? catalogCandidates.filter(i => i.shopCategory === 'service')
    : [], [catalogCandidates, kind])
  // Руну и каталожную реликвию зачаровывают не в мастерской: книга их не создаёт (backend
  // повторяет проверку). Отбор исключением — незнакомая своя запись основой остаётся.
  const notEnchantableDefIds = useMemo(() => new Set(reference.items
    .filter(i => i.shard || i.shopCategory === 'magicItem')
    .map(i => i.id)), [reference.items])
  const bases = useMemo(() => (sheet.items ?? [])
    .filter(i => !notEnchantableDefIds.has(i.itemDefId)),
  [sheet.items, notEnchantableDefIds])
  const magicSkills = useMemo(() => sheet.skills.filter(s => s.kind === 'magic'), [sheet.skills])
  const effectiveMagicSkillName = magicSkills.some(s => s.name === magicSkillName)
    ? magicSkillName
    : magicSkills[0]?.name ?? ''
  const target = kind === 'item' ? candidates.find(i => i.id === targetId) ?? null : null
  const canChooseCraftsmanship = target ? craftsmanshipApplies(target.kind) : false
  const canChooseMaterial = target?.implement != null

  const input: CraftingProjectInput | null = useMemo(() => {
    if (kind === 'enchantment') {
      const base = bases.find(i => i.id === baseItemId)
      if (!base) return null
      return buildInput(base.itemDefId, base.id)
    }
    if (!targetId) return null
    return buildInput(targetId, null)

    function buildInput(itemDefId: string, baseId: string | null): CraftingProjectInput {
      const own = ownCost.trim() === '' ? null : Math.max(0, Math.trunc(Number(ownCost)) || 0)
      return {
        itemDefId,
        baseCharacterItemId: baseId,
        kind,
        skillName: kind === 'enchantment' ? effectiveMagicSkillName || undefined : undefined,
        costPercent: own === null ? percent : 100,
        costOverride: own,
        costOverrideReason: costReason.trim() || undefined,
        difficultyOverride: difficulty.trim() === '' ? null : Math.trunc(Number(difficulty)) || 0,
        difficultyReason: difficultyReason.trim() || undefined,
        timeOverride: time.trim() === '' ? null : Math.max(1, Math.trunc(Number(time)) || 1),
        timeReason: timeReason.trim() || undefined,
        requirements: requirements.trim() || undefined,
        intent: intent.trim() || undefined,
        roughSurvival: rough,
        craftsmanship: canChooseCraftsmanship ? craftsmanship : 'steel',
        material: canChooseMaterial ? material : 'oak',
      }
    }
  }, [kind, targetId, baseItemId, bases, effectiveMagicSkillName, percent, ownCost, costReason,
    difficulty, difficultyReason, time, timeReason, requirements, intent, rough,
    canChooseCraftsmanship, craftsmanship, canChooseMaterial, material])

  // Предпросмотр обновляется сам: числа должны быть видны до подтверждения, а не после.
  // Пока цель не выбрана, запроса нет — и показывать нечего, поэтому старый ответ просто не
  // рисуется (см. `shownPreview`), а не гасится состоянием прямо в теле эффекта.
  useEffect(() => {
    if (!input) return
    let cancelled = false
    void api.craftingPreview(sheet.id, input)
      .then(p => { if (!cancelled) setPreview(p) })
      .catch(() => { if (!cancelled) setPreview(null) })
    return () => { cancelled = true }
  }, [sheet.id, input])
  const shownPreview = input ? preview : null

  async function start() {
    if (!input) return
    setBusy(true)
    try {
      await api.startCrafting(sheet.id, input)
      await loadProjects()
      // Правка персонажа возвращает свежие части листа, и они одноразовые: не забрать их здесь —
      // значит подсунуть устаревший инвентарь следующему обновлению, уже после создания предмета.
      await refresh()
    } catch (e) {
      onError((e as Error).message)
    } finally {
      setBusy(false)
    }
  }

  async function cancel(projectId: string) {
    try {
      await api.cancelCrafting(sheet.id, projectId)
      await loadProjects()
      await refresh()
    } catch (e) {
      onError((e as Error).message)
    }
  }

  const drafts = projects.filter(p => p.status === 'draft')
  const done = projects.filter(p => p.status !== 'draft')
  const hasOwnCost = ownCost.trim() !== ''
  const canStart = !!input && !busy
    && (!hasOwnCost || costReason.trim() !== '')
    && (difficulty.trim() === '' || difficultyReason.trim() !== '')
    && (time.trim() === '' || timeReason.trim() !== '')
    && (kind !== 'enchantment' || (intent.trim() !== '' && effectiveMagicSkillName !== ''))

  function changeKind(next: CraftingKind) {
    setKind(next)
    setSearch('')
    setTargetId('')
    setBaseItemId('')
    setRough(false)
    setCraftsmanship('steel')
    setMaterial('oak')
    setPreview(null)
  }

  const corrections = [ownCost, difficulty, time].filter(v => v.trim()).length + Number(rough)
  const startReason = !input ? t('Выберите, что создать', 'Choose what to create')
    : (hasOwnCost && !costReason.trim()) || (difficulty.trim() && !difficultyReason.trim()) || (time.trim() && !timeReason.trim())
      ? t('Укажите причину поправки', 'Give a reason for the adjustment')
      : kind === 'enchantment' && !intent.trim() ? t('Опишите согласованную способность', 'Describe the agreed ability')
        : kind === 'enchantment' && !effectiveMagicSkillName ? t('Выберите магический навык', 'Choose a magic skill') : ''
  const kindIcons: Record<CraftingKind, IconName> = { item: 'hammer', potion: 'flask', enchantment: 'sparkles' }
  return (
    <div className="crafting-tab">
      <div className="crafting-kind-grid" role="tablist" aria-label={t('Вид работы', 'Work type')}>
        {(['item', 'potion', 'enchantment'] as CraftingKind[]).map(k => <button key={k} role="tab" aria-selected={kind === k}
          className={`sheet-choice-card crafting-kind${kind === k ? ' selected' : ''}`} onClick={() => changeKind(k)}>
          <Icon name={kindIcons[k]} /><span><strong>{KIND_LABELS[k]}</strong><small>{k === 'item' ? t('Оружие, броня, снаряжение', 'Weapons, armor and gear') : k === 'potion' ? t('Зелья и эликсиры', 'Potions and elixirs') : t('Чары на вещь из инвентаря', 'Enchant an inventory item')}</small></span>
        </button>)}
      </div>
      <div className="crafting-new-layout sheet-two-column">
        <section className="panel crafting-new-form">
          {kind === 'enchantment' && <p className="hint small-text">{ENCHANT_HINT}</p>}
          <h4>{kind === 'enchantment' ? t('Основа из инвентаря', 'Base from inventory') : kind === 'potion' ? t('Что варим', 'What to brew') : t('Что создаём', 'What to create')}</h4>
          {((kind === 'enchantment' ? bases.length : catalogCandidates.length) > 12 || search !== '') && <input type="search" aria-label={t('Поиск проекта', 'Search project')} placeholder={t('Поиск по названию…', 'Search by name…')} value={search} onChange={e => setSearch(e.target.value)} />}
          <div className="sheet-choice-grid crafting-targets">
            {kind === 'enchantment' ? bases.filter(i => localizedName(i).toLowerCase().includes(search.toLowerCase())).map(i => <button key={i.id} aria-pressed={baseItemId === i.id}
              className={`sheet-choice-card${baseItemId === i.id ? ' selected' : ''}`} onClick={() => setBaseItemId(i.id)}><strong>{localizedName(i)}</strong><small>{ITEM_KIND_LABELS[i.kind]} · {t('из инвентаря', 'from inventory')}</small></button>)
              : [...candidates, ...unavailableCandidates].filter(i => localizedName(i).toLowerCase().includes(search.toLowerCase())).map(i => <button key={i.id} aria-pressed={targetId === i.id}
                disabled={unavailableCandidates.includes(i)} title={unavailableCandidates.includes(i) ? t('Услуги не создаются ремеслом', 'Services cannot be crafted') : undefined}
                className={`sheet-choice-card${targetId === i.id ? ' selected' : ''}`} onClick={() => setTargetId(i.id)}><strong>{localizedName(i)}</strong><small>{ITEM_KIND_LABELS[i.kind]} · {t(`${i.price} зол. · редк. ${i.rarity}`, `${i.price} coins · rarity ${i.rarity}`)}</small></button>)}
          </div>
          {kind === 'enchantment' && <>
            <label>{t('Согласованная способность', 'Agreed ability')}<textarea value={intent} maxLength={2000} rows={2} placeholder={t('что именно должно получиться', 'what exactly the enchantment does')} onChange={e => setIntent(e.target.value)} /></label>
            <h4>{t('Навык зачарования', 'Enchanting skill')}</h4><div className="sheet-chips">{magicSkills.map(sk => <FilterChip key={sk.skillDefId} active={effectiveMagicSkillName === sk.name} onClick={() => setMagicSkillName(sk.name)}>{localizedName(sk)} · {sk.ranks ?? 0}</FilterChip>)}
              {!magicSkills.length && <FilterChip active={false} disabled onClick={() => {}}>{t('Нет магического навыка', 'No magic skill')}</FilterChip>}</div>
          </>}
          {kind === 'item' && canChooseCraftsmanship && <><h4>{t('Работа', 'Craftsmanship')}</h4><div className="sheet-chips">{WEAPON_CRAFTSMANSHIPS.map(v => <FilterChip key={v} active={craftsmanship === v} onClick={() => setCraftsmanship(v)}>{WEAPON_CRAFTSMANSHIP_LABELS[v]}</FilterChip>)}</div><p className="muted small-text">{WEAPON_CRAFTSMANSHIP_HINTS[craftsmanship]}</p></>}
          {kind === 'item' && canChooseMaterial && <><h4>{t('Материал инструмента', 'Implement material')}</h4><div className="sheet-chips">{IMPLEMENT_MATERIALS.map(v => <FilterChip key={v} active={material === v} onClick={() => setMaterial(v)}>{IMPLEMENT_MATERIAL_LABELS[v]}</FilterChip>)}</div><p className="muted small-text">{IMPLEMENT_MATERIAL_HINTS[material]}</p></>}
          <h4>{t('Стоимость компонентов', 'Component cost')}</h4><div className="sheet-chips">{COST_PERCENTS.map(m => <FilterChip key={m} active={!hasOwnCost && percent === m} disabled={hasOwnCost} onClick={() => setPercent(m)}>{m}%</FilterChip>)}</div>
          <label>{t('Инструменты и компоненты · только описание', 'Tools and components · description only')}<input value={requirements} maxLength={2000} placeholder={t('кузница, слиток стали, мех', 'a forge, a steel ingot, bellows')} onChange={e => setRequirements(e.target.value)} /></label>
          <details className="crafting-corrections"><summary>{t('Поправки ведущего', 'GM adjustments')} {corrections || ''}</summary>
            {[
              { label: t('Своя стоимость', 'Own cost'), value: ownCost, set: setOwnCost, reason: costReason, setReason: setCostReason, min: 0 },
              { label: t('Своя сложность', 'Own difficulty'), value: difficulty, set: setDifficulty, reason: difficultyReason, setReason: setDifficultyReason, min: 0, max: 5 },
              { label: t('Своё время', 'Own time'), value: time, set: setTime, reason: timeReason, setReason: setTimeReason, min: 1 },
            ].map(row => <div className="crafting-override-row" key={row.label}><label>{row.label}<input type="number" min={row.min} max={row.max} value={row.value} onChange={e => row.set(e.target.value)} /></label>
              <label>{t('Причина', 'Reason')}<input value={row.reason} maxLength={200} disabled={!row.value.trim()} className={row.value.trim() && !row.reason.trim() ? 'needs-reason' : ''} onChange={e => row.setReason(e.target.value)} /></label></div>)}
            {kind === 'item' && <CheckRow checked={rough} onChange={setRough} label={t('Грубая работа Выживанием (разрешение ведущего)', 'Rough work with Survival (GM permission)')} />}
          </details>
        </section>
        <aside className="sheet-sticky sheet-result-card crafting-calculation">
          <h4>{t('Расчёт', 'Calculation')}</h4><h3>{shownPreview?.targetName ?? t('Новый проект', 'New project')}</h3>
          {shownPreview && <><div className="sheet-difficulty"><div><small>{t('Сложность', 'Difficulty')}</small><strong>{DIFFICULTY_LABELS[shownPreview.difficulty] ?? shownPreview.difficulty} ({shownPreview.difficulty})</strong>
            {shownPreview.difficulty !== shownPreview.baseDifficulty && <small>{t('по правилу', 'by rule')} — {shownPreview.baseDifficulty}</small>}</div><span aria-hidden="true">{'◆'.repeat(shownPreview.difficulty)}</span></div>
            <dl className="sheet-result-stats"><dt>{t('Навык', 'Skill')}</dt><dd>{shownPreview.skillName}</dd><dt>{t('Время', 'Time')}</dt><dd>{shownPreview.time} {shownPreview.timeUnit === 'hours' ? t('ч', 'h') : t('дн', 'd')}</dd>
              <dt>{t('Цена предмета', 'Item price')}</dt><dd>{shownPreview.targetPrice ?? '—'}</dd><dt>{t('Компоненты', 'Components')}</dt><dd>{shownPreview.cost} {shownPreview.costOverride === null && shownPreview.costPercent !== 100 && <small>({shownPreview.costPercent}% {t('от', 'of')} {shownPreview.listedCost})</small>}</dd></dl></>}
          <button className="primary" disabled={!canStart} onClick={() => void start()}>{t('Начать проект', 'Start the project')}</button>
          {startReason && <p className="warn-text small-text">{startReason}</p>}
          <p className="hint small-text"><Icon name="info-circle" className="button-icon" />{RESOURCES_HINT}</p>
        </aside>
      </div>

      {drafts.length > 0 && (
        <section className="card">
          <h3>{t('В работе', 'In progress')}</h3>
          <p className="hint small-text">{ROLL_HINT}</p>
          {drafts.map(p => (
            <ResolveForm key={p.id} project={p} sheet={sheet}
              onCancel={() => cancel(p.id)}
              onResolved={async () => { await loadProjects(); await refresh() }}
              onError={onError} />
          ))}
        </section>
      )}

      {done.length > 0 && (
        <section className="card">
          <h3>{t('История ремесла', 'Crafting history')}</h3>
          <ul className="crafting-history">
            {done.map(p => (
              <li key={p.id} className={p.status === 'cancelled' ? 'cancelled' : p.netSuccesses > 0 ? 'success' : 'failure'}>
                <strong>{p.targetName}</strong> — {KIND_LABELS[p.kind]}
                {p.status === 'cancelled'
                  ? <span className="muted"> · {t('отменён', 'cancelled')}</span>
                  : <span className="muted">
                    {' · '}{p.netSuccesses > 0 ? t('успех', 'success') : t('провал', 'failure')}
                    {' · '}{t('сложность', 'difficulty')} {p.difficulty}
                    {' · '}{p.time} {p.timeUnit === 'hours' ? t('ч', 'h') : t('дн', 'd')}
                    {' · '}{p.cost} <Icon name="coin" className="button-icon" />
                  </span>}
                {p.outcome && <pre className="crafting-outcome small-text">{p.outcome}</pre>}
              </li>
            ))}
          </ul>
        </section>
      )}
    </div>
  )
}

/**
 * Разрешение проекта: символы броска и интерактивный выбор трат. Каждая выбранная трата попадёт
 * в описание созданного предмета, поэтому здесь же видно, что именно будет записано.
 */
function ResolveForm({ project, sheet, onCancel, onResolved, onError }: {
  project: CraftingProject
  sheet: CharacterSheet
  onCancel: () => Promise<void>
  onResolved: () => Promise<void>
  onError: (message: string) => void
}) {
  const { openRoller } = useDiceRoller()
  const [successes, setSuccesses] = useState(0)
  const [symbols, setSymbols] = useState<Record<CraftingSymbol, number>>({
    advantage: 0, threat: 0, triumph: 0, despair: 0,
  })
  const [choices, setChoices] = useState<CraftingSpendChoice[]>([])
  const [table, setTable] = useState<CraftingSpend[]>([])
  const [busy, setBusy] = useState(false)
  const [confirmCancel, setConfirmCancel] = useState(false)

  // Таблица трат приезжает тем же предпросмотром: она зависит от вида работы, а не от вкладки.
  useEffect(() => {
    let cancelled = false
    void api.craftingPreview(sheet.id, {
      itemDefId: project.itemDefId,
      baseCharacterItemId: project.baseCharacterItemId,
      kind: project.kind,
    })
      .then(p => { if (!cancelled) setTable(p.spends) })
      .catch(() => { if (!cancelled) setTable([]) })
    return () => { cancelled = true }
  }, [sheet.id, project.itemDefId, project.baseCharacterItemId, project.kind])

  const spent = spentSymbols(choices, table)
  const usedRows = new Set(choices.map(c => table.find(s => s.code === c.code)?.rowCode))

  function add(def: CraftingSpend, paidWith: CraftingSymbol) {
    setChoices(prev => {
      const existing = prev.find(c => c.code === def.code && c.paidWith === paidWith)
      if (existing && def.repeatable) {
        return prev.map(c => c === existing ? { ...c, count: (c.count ?? 1) + 1 } : c)
      }
      if (existing) return prev
      return [...prev, { code: def.code, count: 1, paidWith, parameter: '' }]
    })
  }

  function setParameter(code: string, parameter: string) {
    setChoices(prev => prev.map(c => c.code === code ? { ...c, parameter } : c))
  }

  function drop(code: string) {
    setChoices(prev => prev.filter(c => c.code !== code))
  }

  const validBudget = Object.keys(symbols).every(key => symbols[key as CraftingSymbol] >= spent[key as CraftingSymbol])

  async function resolve() {
    if (busy || !validBudget) return
    setBusy(true)
    try {
      await api.resolveCrafting(sheet.id, project.id, {
        netSuccesses: successes,
        advantages: symbols.advantage,
        threats: symbols.threat,
        triumphs: symbols.triumph,
        despairs: symbols.despair,
        spends: choices,
      })
      await onResolved()
    } catch (e) {
      onError((e as Error).message)
    } finally {
      setBusy(false)
    }
  }

  const affordable = (def: CraftingSpend, symbol: CraftingSymbol, cost: number) => {
    if (cost <= 0) return false
    if (symbols[symbol] - spent[symbol] < cost) return false
    // Из строки таблицы берут один эффект — кроме повторяемых, которые набирают сами себя.
    const chosen = choices.find(c => c.code === def.code)
    return !usedRows.has(def.rowCode) || (!!chosen && def.repeatable)
  }

  return (
    <article className="crafting-project">
      <header className="sheet-card-heading">
        <Icon name={project.kind === 'item' ? 'hammer' : project.kind === 'potion' ? 'flask' : 'sparkles'} className="button-icon" />
        <strong>{project.targetName}</strong> — {KIND_LABELS[project.kind]}
        <span className="muted">
          {' · '}{project.skillName}
          {' · '}{t('сложность', 'difficulty')} {project.difficulty}
          {' · '}{project.time} {project.timeUnit === 'hours' ? t('ч', 'h') : t('дн', 'd')}
          {' · '}{project.cost} <Icon name="coin" className="button-icon" />
        </span>
        <div className="form-actions"><button className="small" onClick={() => {
          const sk = sheet.skills.find(s => s.name === project.skillName)
          openRoller({ kind: 'roll', title: t('Проверка ремесла', 'Crafting check'), label: project.targetName,
            difficultyUpgrades: sk?.difficultyUpgrades ?? 0,
            initialPool: { ability: sk?.pool?.ability ?? 0, proficiency: sk?.pool?.proficiency ?? 0,
              boost: sk?.removeBoosts ? 0 : sk?.boostDice ?? 0, setback: sk?.setbackDice ?? 0,
              difficulty: project.difficulty + (sk?.difficultyDice ?? 0) } })
        }}><Icon name="dice-5" className="button-icon" />{t('Роллер', 'Roller')}</button>
          <button className="small danger" disabled={busy} onClick={() => setConfirmCancel(true)}><Icon name="trash" className="button-icon" />{t('Отменить проект', 'Cancel the project')}</button></div>
      </header>
      {confirmCancel && <div className="hint" role="group" aria-label={t('Подтверждение отмены проекта', 'Confirm project cancellation')}>
        <p>{t('Отменить проект навсегда? Он останется в истории, восстановить его нельзя.', 'Cancel this project permanently? It stays in history and cannot be restored.')}</p>
        <div className="form-actions"><button className="danger small" disabled={busy} onClick={async () => {
          setBusy(true)
          try { await onCancel() } finally { setBusy(false); setConfirmCancel(false) }
        }}>{t('Да, отменить проект', 'Yes, cancel the project')}</button>
          <button className="small" disabled={busy} onClick={() => setConfirmCancel(false)}>{t('Продолжить работу', 'Keep working')}</button></div>
      </div>}
      {project.requirements && <p className="small-text muted">{project.requirements}</p>}

      <div className="crafting-symbols">
        <NumberStepper label={t('Нетто-успехов', 'Net successes')} glyph="✓" className="successes" value={successes} onChange={setSuccesses} disabled={busy} />
        {(Object.keys(SYMBOL_LABELS) as CraftingSymbol[]).map(symbol => <NumberStepper key={symbol} label={SYMBOL_LABELS[symbol]} glyph={SYMBOL_GLYPH[symbol]} className={symbol} value={symbols[symbol]} min={spent[symbol]} disabled={busy}
          onChange={value => setSymbols(prev => ({ ...prev, [symbol]: value }))} />)}
      </div>
      <h4 className="sheet-section-title">{t('Траты символов', 'Symbol spends')}<small>{t('Осталось:', 'Remaining:')} {Object.keys(symbols).map(key => `${SYMBOL_GLYPH[key as CraftingSymbol]} ${symbols[key as CraftingSymbol] - spent[key as CraftingSymbol]}`).join(' · ')}</small></h4>
      {!Object.values(symbols).some(v => v > 0) && <p className="muted small-text">{t('Впишите ▲ ▼ ★ ☠ — здесь появятся доступные траты.', 'Enter ▲ ▼ ★ ☠ to enable symbol spends.')}</p>}
      {choices.length > 0 && (
        <ul className="crafting-choices">
          {choices.map(c => {
            const def = table.find(s => s.code === c.code)
            if (!def) return null
            return (
              <li key={c.code}>
                {SYMBOL_GLYPH[c.paidWith]} <strong>{t(def.nameRu, def.nameEn)}</strong>
                {` ×${c.count ?? 1}`}
                {def.requiresParameter && (
                  <input value={c.parameter ?? ''} maxLength={400}
                    placeholder={def.effect === 'qualityRating'
                      ? t('код качества', 'quality code')
                      : def.effect === 'combineDose'
                        ? t('id второго зелья', 'the other potion id')
                        : t('формулировка', 'wording')}
                    onChange={e => setParameter(c.code, e.target.value)} />
                )}
                <button className="small" disabled={busy} aria-label={t(`Убрать трату: ${def.nameRu}`, `Remove spend: ${def.nameEn}`)} onClick={() => drop(c.code)}><Icon name="close" className="button-icon" /></button>
              </li>
            )
          })}
        </ul>
      )}

      <div className="crafting-table">
        {table.map(def => (
          <div key={def.code} className={`crafting-row${def.isNegative ? ' negative' : ''}${choices.some(c => c.code === def.code) ? ' selected' : ''}`}>
            <span>
              {t(def.nameRu, def.nameEn)}
              {def.effect === 'descriptive' && (
                <span className="muted small-text">
                  {' '}({t('только описание', 'description only')})
                </span>
              )}
              <span className="muted small-text"> — {localizedDescription(def)}</span>
            </span>
            <span className="crafting-costs">
              {payments(def).map(([symbol, cost]) => (
                <button key={symbol} className="chip"
                  disabled={busy || !affordable(def, symbol, cost)}
                  title={t('Оплатить', 'Pay with')}
                  onClick={() => add(def, symbol)}>
                  {SYMBOL_GLYPH[symbol]} {cost}
                </button>
              ))}
            </span>
          </div>
        ))}
      </div>

      <div className="price-row">
        <button className="primary" disabled={busy || !validBudget} onClick={() => void resolve()}>
          {t('Разрешить проект', 'Resolve the project')}
        </button>
        <span className={successes > 0 ? 'success-text' : 'danger-text'}><Icon name={successes > 0 ? 'circle-check' : 'circle-x'} className="button-icon" />{successes > 0 ? t('Успех: предмет будет создан.', 'Success: the item will be made.') : t('Провал: предмет не создаётся, но проект остаётся в истории.', 'Failure: nothing is made, but the project stays in history.')}</span>
      </div>
    </article>
  )
}
