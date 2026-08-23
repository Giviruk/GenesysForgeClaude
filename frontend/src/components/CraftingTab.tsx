import { useCallback, useEffect, useMemo, useState } from 'react'
import { api } from '../api/client'
import type {
  CharacterSheet, CraftingKind, CraftingPreview, CraftingProject, CraftingProjectInput,
  CraftingSpend, CraftingSpendChoice, CraftingSymbol, ImplementMaterial, ItemDef, Reference,
  WeaponCraftsmanship,
} from '../api/types'
import {
  IMPLEMENT_MATERIAL_HINTS, IMPLEMENT_MATERIAL_LABELS, localizedName,
  WEAPON_CRAFTSMANSHIPS, WEAPON_CRAFTSMANSHIP_HINTS, WEAPON_CRAFTSMANSHIP_LABELS,
} from '../utils/labels'
import { t } from '../i18n'
import { craftsmanshipApplies } from '../utils/craftsmanship'
import { IMPLEMENT_MATERIALS } from '../utils/implements'

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
    setTargetId('')
    setBaseItemId('')
    setRough(false)
    setCraftsmanship('steel')
    setMaterial('oak')
    setPreview(null)
  }

  return (
    <div className="crafting-tab">
      <p className="hint small-text">{RESOURCES_HINT}</p>

      <section className="card">
        <h3>{t('Новый проект', 'New project')}</h3>

        <div className="tabs" role="tablist">
          {(['item', 'potion', 'enchantment'] as CraftingKind[]).map(k => (
            <button key={k} role="tab" aria-selected={kind === k}
              className={kind === k ? 'tab active' : 'tab'} onClick={() => changeKind(k)}>
              {KIND_LABELS[k]}
            </button>
          ))}
        </div>

        {kind === 'enchantment' ? (
          <>
            <p className="hint small-text">{ENCHANT_HINT}</p>
            <label>{t('Основа из инвентаря', 'Base from inventory')}
              <select value={baseItemId} onChange={e => setBaseItemId(e.target.value)}>
                <option value="">{t('— выберите вещь —', '— pick an item —')}</option>
                {bases.map(i => (
                  <option key={i.id} value={i.id}>{localizedName(i)}</option>
                ))}
              </select>
            </label>
            <label>{t('Согласованная способность', 'Agreed ability')}
              <textarea value={intent} maxLength={2000} rows={2}
                placeholder={t('что именно должно получиться', 'what exactly the enchantment does')}
                onChange={e => setIntent(e.target.value)} />
            </label>
            <label>{t('Навык зачарования', 'Enchanting skill')}
              <select value={effectiveMagicSkillName} onChange={e => setMagicSkillName(e.target.value)}>
                <option value="">{t('— выберите магический навык —', '— pick a magic skill —')}</option>
                {magicSkills.map(s => (
                  <option key={s.skillDefId} value={s.name}>{localizedName(s)}</option>
                ))}
              </select>
            </label>
          </>
        ) : (
          <label>{t('Что делаем', 'What to make')}
            <select value={targetId} onChange={e => setTargetId(e.target.value)}>
              <option value="">{t('— выберите запись каталога —', '— pick a catalog entry —')}</option>
              <optgroup label={t('Можно создать', 'Craftable')}>
                {candidates.map(i => (
                  <option key={i.id} value={i.id}>{localizedName(i)}</option>
                ))}
              </optgroup>
              {unavailableCandidates.length > 0 && (
                <optgroup label={t('Нельзя создать ремеслом', 'Not craftable')}>
                  {unavailableCandidates.map(i => (
                    <option key={i.id} value={i.id} disabled>{localizedName(i)}</option>
                  ))}
                </optgroup>
              )}
            </select>
          </label>
        )}

        {kind === 'item' && (
          <>
            {canChooseCraftsmanship && (
              <label>{t('Материал / качество изготовления', 'Material / craftsmanship')}
                <select value={craftsmanship}
                  title={WEAPON_CRAFTSMANSHIP_HINTS[craftsmanship]}
                  onChange={e => setCraftsmanship(e.target.value as WeaponCraftsmanship)}>
                  {WEAPON_CRAFTSMANSHIPS.map(value => (
                    <option key={value} value={value}>{WEAPON_CRAFTSMANSHIP_LABELS[value]}</option>
                  ))}
                </select>
                <span className="muted small-text">{WEAPON_CRAFTSMANSHIP_HINTS[craftsmanship]}</span>
              </label>
            )}
            {canChooseMaterial && (
              <label>{t('Материал магического инструмента', 'Magic implement material')}
                <select value={material} title={IMPLEMENT_MATERIAL_HINTS[material]}
                  onChange={e => setMaterial(e.target.value as ImplementMaterial)}>
                  {IMPLEMENT_MATERIALS.map(value => (
                    <option key={value} value={value}>{IMPLEMENT_MATERIAL_LABELS[value]}</option>
                  ))}
                </select>
                <span className="muted small-text">{IMPLEMENT_MATERIAL_HINTS[material]}</span>
              </label>
            )}
            <label>
              <input type="checkbox" checked={rough} onChange={e => setRough(e.target.checked)} />
              {' '}{t('Грубая работа Выживанием (разрешение ведущего)', 'Rough work with Survival (GM permission)')}
            </label>
          </>
        )}

        <div className="price-control">
          <div className="price-mults">
            {COST_PERCENTS.map(m => (
              <button key={m} className={!hasOwnCost && percent === m ? 'chip active' : 'chip'}
                disabled={hasOwnCost}
                title={hasOwnCost ? t('Задана своя цена', 'An own price is set') : undefined}
                onClick={() => setPercent(m)}>{m}%</button>
            ))}
          </div>
          <div className="price-row crafting-override-row">
            <label>{t('Своя стоимость', 'Own cost')}
              <input className="crafting-override-input" type="number" min={0} value={ownCost}
                placeholder={t('по доле', 'by fraction')}
                onChange={e => setOwnCost(e.target.value)} />
            </label>
            <label>{t('Причина', 'Reason')}
              <input value={costReason} maxLength={200} disabled={!hasOwnCost}
                placeholder={t('например, свои материалы', 'e.g. own materials')}
                onChange={e => setCostReason(e.target.value)} />
            </label>
          </div>
        </div>

        <div className="price-row crafting-override-row">
          <label>{t('Своя сложность', 'Own difficulty')}
            <input className="crafting-override-input" type="number" min={0} max={5} value={difficulty}
              placeholder={t('по правилу', 'by rule')}
              onChange={e => setDifficulty(e.target.value)} />
          </label>
          <label>{t('Причина', 'Reason')}
            <input value={difficultyReason} maxLength={200} disabled={difficulty.trim() === ''}
              onChange={e => setDifficultyReason(e.target.value)} />
          </label>
        </div>

        <div className="price-row crafting-override-row">
          <label>{t('Своё время', 'Own time')}
            <input className="crafting-override-input" type="number" min={1} value={time}
              placeholder={t('по правилу', 'by rule')}
              onChange={e => setTime(e.target.value)} />
          </label>
          <label>{t('Причина', 'Reason')}
            <input value={timeReason} maxLength={200} disabled={time.trim() === ''}
              onChange={e => setTimeReason(e.target.value)} />
          </label>
        </div>

        <label>{t('Инструменты и компоненты', 'Tools and components')}
          <textarea value={requirements} maxLength={2000} rows={2}
            placeholder={t('кузница, слиток стали, мех — только описание',
              'a forge, a steel ingot, bellows — description only')}
            onChange={e => setRequirements(e.target.value)} />
        </label>

        {shownPreview && (
          <p className="price-total">
            {t('Сложность', 'Difficulty')} <strong>{DIFFICULTY_LABELS[shownPreview.difficulty] ?? shownPreview.difficulty}</strong>
            {shownPreview.difficulty !== shownPreview.baseDifficulty
              && <span className="muted"> ({t('по правилу', 'by rule')} {shownPreview.baseDifficulty})</span>}
            {' · '}{t('Навык', 'Skill')} {shownPreview.skillName}
            {' · '}{shownPreview.time} {shownPreview.timeUnit === 'hours' ? t('ч', 'h') : t('дн', 'd')}
            {' · '}{t('цена предмета', 'item price')} <strong>{shownPreview.targetPrice ?? '—'}</strong> 🪙
            {' · '}{t('компоненты', 'components')} <strong>{shownPreview.cost}</strong> 🪙
            {shownPreview.costOverride === null && shownPreview.costPercent !== 100
              && <span className="muted"> ({shownPreview.costPercent}% {t('от', 'of')} {shownPreview.listedCost})</span>}
          </p>
        )}

        <button className="primary" disabled={!canStart} onClick={() => void start()}>
          {t('Начать проект', 'Start the project')}
        </button>
      </section>

      {drafts.length > 0 && (
        <section className="card">
          <h3>{t('В работе', 'In progress')}</h3>
          <p className="hint small-text">{ROLL_HINT}</p>
          {drafts.map(p => (
            <ResolveForm key={p.id} project={p} sheet={sheet}
              onCancel={() => void cancel(p.id)}
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
              <li key={p.id}>
                <strong>{p.targetName}</strong> — {KIND_LABELS[p.kind]}
                {p.status === 'cancelled'
                  ? <span className="muted"> · {t('отменён', 'cancelled')}</span>
                  : <span className="muted">
                    {' · '}{p.netSuccesses > 0 ? t('успех', 'success') : t('провал', 'failure')}
                    {' · '}{t('сложность', 'difficulty')} {p.difficulty}
                    {' · '}{p.time} {p.timeUnit === 'hours' ? t('ч', 'h') : t('дн', 'd')}
                    {' · '}{p.cost} 🪙
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
  onCancel: () => void
  onResolved: () => Promise<void>
  onError: (message: string) => void
}) {
  const [successes, setSuccesses] = useState(0)
  const [symbols, setSymbols] = useState<Record<CraftingSymbol, number>>({
    advantage: 0, threat: 0, triumph: 0, despair: 0,
  })
  const [choices, setChoices] = useState<CraftingSpendChoice[]>([])
  const [table, setTable] = useState<CraftingSpend[]>([])
  const [busy, setBusy] = useState(false)

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

  async function resolve() {
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
      <header>
        <strong>{project.targetName}</strong> — {KIND_LABELS[project.kind]}
        <span className="muted">
          {' · '}{project.skillName}
          {' · '}{t('сложность', 'difficulty')} {project.difficulty}
          {' · '}{project.time} {project.timeUnit === 'hours' ? t('ч', 'h') : t('дн', 'd')}
          {' · '}{project.cost} 🪙
        </span>
      </header>
      {project.requirements && <p className="small-text muted">{project.requirements}</p>}

      <div className="price-row">
        <label>{t('Нетто-успехов', 'Net successes')}
          <input type="number" min={0} value={successes} style={{ width: '4rem' }}
            onChange={e => setSuccesses(Math.max(0, Math.trunc(Number(e.target.value)) || 0))} />
        </label>
        {(Object.keys(SYMBOL_LABELS) as CraftingSymbol[]).map(symbol => (
          <label key={symbol}>{SYMBOL_GLYPH[symbol]} {SYMBOL_LABELS[symbol]}
            <input type="number" min={0} value={symbols[symbol]} style={{ width: '4rem' }}
              onChange={e => setSymbols(s => ({
                ...s, [symbol]: Math.max(0, Math.trunc(Number(e.target.value)) || 0),
              }))} />
          </label>
        ))}
      </div>
      <p className="small-text">
        {successes > 0
          ? t('Успех: предмет будет создан.', 'Success: the item will be made.')
          : t('Провал: предмет не создаётся, но проект остаётся в истории.',
            'Failure: nothing is made, but the project stays in history.')}
      </p>

      {choices.length > 0 && (
        <ul className="crafting-choices">
          {choices.map(c => {
            const def = table.find(s => s.code === c.code)
            if (!def) return null
            return (
              <li key={c.code}>
                {SYMBOL_GLYPH[c.paidWith]} <strong>{def.nameRu}</strong>
                {(c.count ?? 1) > 1 && ` ×${c.count}`}
                {def.requiresParameter && (
                  <input value={c.parameter ?? ''} maxLength={400}
                    placeholder={def.effect === 'qualityRating'
                      ? t('код качества', 'quality code')
                      : def.effect === 'combineDose'
                        ? t('id второго зелья', 'the other potion id')
                        : t('формулировка', 'wording')}
                    onChange={e => setParameter(c.code, e.target.value)} />
                )}
                <button className="small" onClick={() => drop(c.code)}>×</button>
              </li>
            )
          })}
        </ul>
      )}

      <div className="crafting-table">
        {table.map(def => (
          <div key={def.code} className={def.isNegative ? 'crafting-row negative' : 'crafting-row'}>
            <span>
              {def.nameRu}
              {def.effect === 'descriptive' && (
                <span className="muted small-text">
                  {' '}({t('только описание', 'description only')})
                </span>
              )}
              <span className="muted small-text"> — {def.description}</span>
            </span>
            <span className="crafting-costs">
              {payments(def).map(([symbol, cost]) => (
                <button key={symbol} className="chip"
                  disabled={!affordable(def, symbol, cost)}
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
        <button className="primary" disabled={busy} onClick={() => void resolve()}>
          {t('Разрешить проект', 'Resolve the project')}
        </button>
        <button disabled={busy} onClick={onCancel}>{t('Отменить проект', 'Cancel the project')}</button>
      </div>
    </article>
  )
}
