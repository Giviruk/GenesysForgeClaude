import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type {
  Characteristic, CreatureTemplate, GameSystem, NpcAttackEntry, NpcCombatStyle, NpcDetail, NpcInput,
  NpcKind, NpcListItem, NpcPowerLevel, NpcRole, NpcVisibility, Quality, QuickDraftRequest, Reference, SkillDef,
} from '../api/types'
import {
  CHARACTERISTICS, CHARACTERISTIC_LABELS, CREATURE_TEMPLATE_LABELS, CREATURE_TEMPLATES, dualName,
  ITEM_KIND_LABELS, NPC_COMBAT_STYLE_LABELS, NPC_KIND_LABELS, NPC_KINDS, NPC_POWER_LABELS, NPC_ROLE_LABELS,
  NPC_ROLES, NPC_VISIBILITY_LABELS, secondaryName, SYSTEM_LABELS,
} from '../utils/labels'
import {
  npcAttackViews, npcGearViews, npcSkillViews, skillIndex, syncAttacksWithEquipment, weaponsByLabel,
  type NpcGearView,
} from '../utils/npcStats'
import { DicePoolView } from '../components/DicePoolView'
import { resolveQualityCosts } from '../utils/combat'
import { PropertyTags } from '../components/PropertyTags'
import { PrintPreview } from '../components/print/PrintPreview'
import { AdversaryCard } from '../components/print/cards'
import { adversaryMarkdown } from '../components/print/markdown'
import { useDiceRoller } from '../dice-roller-store'

const SYSTEMS: GameSystem[] = ['genesysCore', 'realmsOfTerrinoth']

interface Props {
  openId: string | null
  onOpen: (id: string) => void
  onBack: () => void
}

export function NpcsPage({ openId, onOpen, onBack }: Props) {
  const [npcs, setNpcs] = useState<NpcListItem[] | null>(null)
  const [editing, setEditing] = useState<NpcDetail | 'new' | null>(null)
  const [drafting, setDrafting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  // Счётчик принудительной перезагрузки открытой карточки: растёт после правки того же NPC
  // (его id не меняется, поэтому key/remount не срабатывает — обновляемся по этому токену).
  const [detailVersion, setDetailVersion] = useState(0)

  // фильтры
  const [search, setSearch] = useState('')
  const [system, setSystem] = useState<GameSystem | ''>('')
  const [kind, setKind] = useState<NpcKind | ''>('')
  const [role, setRole] = useState<NpcRole | ''>('')
  const [source, setSource] = useState<'' | 'mine' | 'builtin'>('')
  const [sort, setSort] = useState<'createdAt' | 'name'>('createdAt')

  const reload = useCallback(() => api.npcs({
    search: search || undefined,
    system: system || undefined,
    kind: kind || undefined,
    role: role || undefined,
    sort,
  }).then(setNpcs).catch((e: unknown) => setError(e instanceof Error ? e.message : 'Ошибка загрузки')),
    [search, system, kind, role, sort])

  // Список всегда виден (master-detail) и перезагружается по фильтрам.
  useEffect(() => { void reload() }, [reload])

  // Фильтр источника (мои / встроенный бестиарий) — клиентский поверх ответа сервера.
  const shown = useMemo(() => {
    if (!npcs) return npcs
    if (source === 'mine') return npcs.filter(n => n.isMine)
    if (source === 'builtin') return npcs.filter(n => n.isBuiltIn)
    return npcs
  }, [npcs, source])

  return (
    <div className="page npc-page">
      <div className="page-head">
        <h2>NPC / Бестиарий</h2>
        <div className="head-actions">
          <button onClick={() => setDrafting(true)}>⚡ Быстрый черновик</button>
          <button className="primary" onClick={() => setEditing('new')}>+ Создать NPC</button>
        </div>
      </div>
      {error && <div className="error">{error}</div>}

      <div className="bestiary-layout">
        <aside className="bestiary-list panel">
          <div className="npc-filters">
            <input placeholder="Поиск по имени…" value={search} onChange={e => setSearch(e.target.value)} />
            <div className="npc-filter-row">
              <select value={system} onChange={e => setSystem(e.target.value as GameSystem | '')}>
                <option value="">Все системы</option>
                {SYSTEMS.map(s => <option key={s} value={s}>{SYSTEM_LABELS[s]}</option>)}
              </select>
              <select value={kind} onChange={e => setKind(e.target.value as NpcKind | '')}>
                <option value="">Все типы</option>
                {NPC_KINDS.map(k => <option key={k} value={k}>{NPC_KIND_LABELS[k]}</option>)}
              </select>
            </div>
            <div className="npc-filter-row">
              <select value={role} onChange={e => setRole(e.target.value as NpcRole | '')}>
                <option value="">Все роли</option>
                {NPC_ROLES.map(r => <option key={r} value={r}>{NPC_ROLE_LABELS[r]}</option>)}
              </select>
              <select value={source} onChange={e => setSource(e.target.value as '' | 'mine' | 'builtin')}>
                <option value="">Все источники</option>
                <option value="mine">Мои</option>
                <option value="builtin">Встроенные</option>
              </select>
            </div>
            <div className="npc-filter-row">
              <select value={sort} onChange={e => setSort(e.target.value as 'createdAt' | 'name')}>
                <option value="createdAt">Сначала новые</option>
                <option value="name">По имени</option>
              </select>
            </div>
          </div>

          {shown === null && <p className="muted bestiary-hint">Загрузка…</p>}
          {shown?.length === 0 && <p className="muted bestiary-hint">Ничего не найдено — создайте NPC или быстрый черновик.</p>}
          <ul className="bestiary-items">
            {shown?.map(n => (
              <li key={n.id}>
                <button type="button" className={`bestiary-item${n.id === openId ? ' active' : ''}`}
                  onClick={() => onOpen(n.id)}>
                  <span className="bestiary-item-main">
                    <strong>{n.name}</strong>
                    <span className={`badge ${n.system}`}>{SYSTEM_LABELS[n.system]}</span>
                  </span>
                  <span className="bestiary-item-sub muted">
                    {NPC_KIND_LABELS[n.kind]} · {NPC_ROLE_LABELS[n.role]} · Поглощение {n.soak} · Раны {n.woundThreshold}
                  </span>
                  {n.isBuiltIn
                    ? <span className="bestiary-item-flag muted">встроенный</span>
                    : !n.isMine && <span className="bestiary-item-flag muted">из кампании</span>}
                </button>
              </li>
            ))}
          </ul>
        </aside>

        <section className="bestiary-detail">
          {openId
            ? <NpcDetailView key={openId} npcId={openId} reloadToken={detailVersion}
                onEdit={n => setEditing(n)}
                onDuplicated={id => { void reload(); onOpen(id) }}
                onDeleted={() => { onBack(); void reload() }} />
            : <div className="panel bestiary-empty">
                <p className="muted">← Выберите NPC из списка, чтобы увидеть статблок с пулами кубов.</p>
              </div>}
        </section>
      </div>

      {editing && (
        <NpcEditor
          initial={editing === 'new' ? null : editing}
          onCancel={() => setEditing(null)}
          onSaved={saved => { setEditing(null); void reload(); onOpen(saved.id); setDetailVersion(v => v + 1) }}
        />
      )}
      {drafting && (
        <QuickDraftForm
          onCancel={() => setDrafting(false)}
          onCreated={n => { setDrafting(false); setEditing(n) }}
        />
      )}
    </div>
  )
}

function NpcDetailView({ npcId, reloadToken, onEdit, onDuplicated, onDeleted }: {
  npcId: string
  reloadToken: number
  onEdit: (n: NpcDetail) => void
  onDuplicated: (id: string) => void
  onDeleted: () => void
}) {
  const [n, setN] = useState<NpcDetail | null>(null)
  const [reference, setReference] = useState<Reference | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [printing, setPrinting] = useState(false)
  const [busy, setBusy] = useState(false)
  const { openRoller } = useDiceRoller()

  // Грузим карточку при открытии и при росте reloadToken (после правки того же NPC).
  useEffect(() => {
    let cancelled = false
    api.npc(npcId)
      .then(d => { if (!cancelled) setN(d) })
      .catch((e: unknown) => { if (!cancelled) setError(e instanceof Error ? e.message : 'Ошибка') })
    return () => { cancelled = true }
  }, [npcId, reloadToken])

  // Справочник системы NPC нужен для характеристик навыков и боевых параметров оружия.
  const system = n?.system
  useEffect(() => {
    if (!system) return
    let cancelled = false
    api.reference(system)
      .then(r => { if (!cancelled) setReference(r) })
      .catch(() => { /* без справочника пулы кубов просто не покажем */ })
    return () => { cancelled = true }
  }, [system])

  // Пулы навыков и разбор снаряжения на оружие/прочее — пересчитываем при смене NPC/справочника.
  const index = useMemo(() => skillIndex(reference), [reference])
  const skills = useMemo(() => (n ? npcSkillViews(n, index) : []), [n, index])
  const attacks = useMemo(() => (n ? npcAttackViews(n, reference) : []), [n, reference])
  const gear = useMemo(() => (n ? npcGearViews(n, reference) : []), [n, reference])

  if (error) return <div className="panel bestiary-empty"><div className="error">{error}</div></div>
  if (!n) return <div className="panel bestiary-empty"><p className="muted">Загрузка…</p></div>

  if (printing) {
    // GM видит обе версии; владелец считается GM. Игрок без доступа печатает только player-версию.
    const versions: ('gm' | 'player')[] = n.isMine ? ['gm', 'player'] : ['player']
    return (
      <PrintPreview title={`Карточка NPC — ${n.name}`} versions={versions}
        markdown={(v) => adversaryMarkdown(n, v)} onClose={() => setPrinting(false)}>
        {(v) => <AdversaryCard npc={n} version={v} />}
      </PrintPreview>
    )
  }

  const chars: [Characteristic, number][] = [
    ['brawn', n.brawn], ['agility', n.agility], ['intellect', n.intellect],
    ['cunning', n.cunning], ['willpower', n.willpower], ['presence', n.presence],
  ]

  async function action(fn: () => Promise<void>) {
    setBusy(true); setError(null)
    try { await fn() }
    catch (e) { setError(e instanceof Error ? e.message : 'Ошибка') }
    finally { setBusy(false) }
  }

  return (
    <div className="npc-detail">
      <div className="npc-detail-actions no-print">
        <button onClick={() => setPrinting(true)}>🖨 Печать</button>
        {n.isMine && <>
          <button onClick={() => onEdit(n)}>Редактировать</button>
          <button disabled={busy}
            onClick={() => void action(async () => { const dup = await api.duplicateNpc(n.id); onDuplicated(dup.id) })}>
            Дублировать
          </button>
          <button className="danger" disabled={busy}
            onClick={() => { if (confirm(`Удалить NPC «${n.name}»?`)) void action(async () => { await api.deleteNpc(n.id); onDeleted() }) }}>
            Удалить
          </button>
        </>}
        {n.isBuiltIn && (
          <button className="primary" disabled={busy}
            onClick={() => void action(async () => { const dup = await api.duplicateNpc(n.id); onDuplicated(dup.id) })}>
            Клонировать в мою библиотеку
          </button>
        )}
      </div>

      <div className="npc-card">
        <div className="npc-card-head">
          <h3>{n.name}</h3>
          <span className="muted">
            {SYSTEM_LABELS[n.system]} · {NPC_KIND_LABELS[n.kind]} · {NPC_ROLE_LABELS[n.role]}
            {n.isBuiltIn && <span className="badge builtin"> встроенный · только чтение</span>}
          </span>
        </div>
        {n.description && <p className="npc-desc">{n.description}</p>}

        <div className="npc-char-row">
          {chars.map(([c, v]) => (
            <div key={c} className="npc-char">
              <span className="npc-char-val">{v}</span>
              <span className="npc-char-label">{CHARACTERISTIC_LABELS[c]}</span>
            </div>
          ))}
        </div>

        <div className="npc-derived">
          <span><b>Поглощение</b> {n.soak}</span>
          <span><b>Раны</b> {n.woundThreshold}</span>
          {n.strainThreshold != null && <span><b>Усталость</b> {n.strainThreshold}</span>}
          <span><b>Бл. защита</b> {n.meleeDefense}</span>
          <span><b>Дал. защита</b> {n.rangedDefense}</span>
          {n.silhouette !== 1 && <span><b>Силуэт</b> {n.silhouette}</span>}
        </div>

        {n.warnings.length > 0 && (
          <div className="npc-warnings">
            <b>Предупреждения:</b>
            <ul>{n.warnings.map((w, i) => <li key={i}>{w}</li>)}</ul>
          </div>
        )}

        {skills.length > 0 && (
          <div className="npc-section">
            <h4>{n.kind === 'minion' ? 'Групповые навыки' : 'Навыки'}</h4>
            <ul className="npc-skill-list">
              {skills.map((s, i) => (
                <li key={i} className="npc-skill-row">
                  <span className="npc-skill-name">
                    {s.name}
                    {(() => {
                      const original = index.get(s.name) ? secondaryName(index.get(s.name)!) : ''
                      return original && original !== s.name
                        ? <span className="muted small-text name-secondary"> · {original}</span>
                        : null
                    })()}
                    {' '}{n.kind !== 'minion' && <span className="muted">{s.ranks}</span>}
                    {s.characteristic && (
                      <span className="muted small-text"> · {CHARACTERISTIC_LABELS[s.characteristic]}</span>
                    )}
                  </span>
                  {s.pool
                    ? <DicePoolView pool={s.pool} />
                    : <span className="muted small-text">пул не определён</span>}
                </li>
              ))}
            </ul>
          </div>
        )}

        {attacks.length > 0 && (
          <div className="npc-section">
            <h4>Атаки</h4>
            <ul className="npc-weapon-list">
              {attacks.map((w, i) => (
                <li key={i} className="npc-weapon">
                  <div className="npc-weapon-head">
                    <strong>{w.name}</strong>
                    {w.pool
                      ? <span className="weapon-pool" title={w.skillLabel ? `Бросок: ${w.skillLabel}` : undefined}>
                          <DicePoolView pool={w.pool} />
                          {w.skillLabel && <span className="muted small-text">{w.skillLabel}</span>}
                        </span>
                      : w.skillLabel === null && <span className="muted small-text">навык не указан</span>}
                    <button type="button" className="small no-print" onClick={() => openRoller({
                      kind: 'combat',
                      title: w.name,
                      skillLabel: w.skillLabel,
                      basePool: w.pool ?? {},
                      damage: n.attacks[i].damage,
                      brawn: n.brawn,
                      crit: n.attacks[i].critical,
                      rangeBand: n.attacks[i].rangeBand,
                      qualities: resolveQualityCosts(
                        n.attacks[i].qualities.map(q => ({ code: q.qualityCode, label: q.nameRu || q.qualityCode, rating: q.rating })),
                        reference),
                      onLog: n.isMine && n.campaignId
                        ? (req => { void api.createRoll(n.campaignId!, req) })
                        : undefined,
                      canSecret: n.isMine,
                    })}>🎲 Атаковать</button>
                  </div>
                  <div className="npc-weapon-stats">
                    <span className="weapon-stat">Урон <strong>{w.damageText}</strong></span>
                    {w.crit && <span className="weapon-stat">Крит <strong>{w.crit}</strong></span>}
                    {w.rangeBand && <span className="weapon-stat">{w.rangeBand}</span>}
                  </div>
                  {w.qualities.length > 0 && (
                    <div className="chips weapon-props small-text">
                      {w.qualities.map((q, j) => <span key={j} className="chip">{q.label}</span>)}
                    </div>
                  )}
                  {w.notes && <div className="muted small-text">{w.notes}</div>}
                </li>
              ))}
            </ul>
          </div>
        )}

        {n.abilities.length > 0 && (
          <div className="npc-section">
            <h4>Способности</h4>
            <ul>{n.abilities.map((a, i) => <li key={i}><b>{a.name}.</b> {a.description}</li>)}</ul>
          </div>
        )}
        {n.talents.length > 0 && (
          <div className="npc-section"><h4>Таланты</h4><div className="chips">{n.talents.map((t, i) => <span key={i} className="chip">{t}</span>)}</div></div>
        )}
        {gear.length > 0 && (
          <div className="npc-section">
            <h4>Снаряжение</h4>
            <ul className="npc-gear-list">
              {gear.map((g, i) => <NpcGearRow key={i} gear={g} />)}
            </ul>
          </div>
        )}
        {n.tactics && (
          <div className="npc-section"><h4>Тактика</h4><p className="npc-desc">{n.tactics}</p></div>
        )}
        {n.tags.length > 0 && (
          <div className="npc-section"><h4>Теги</h4><div className="chips">{n.tags.map(t => <span key={t} className="chip">{t}</span>)}</div></div>
        )}
        {n.source && <div className="muted small-text npc-source">Источник: {n.source}</div>}
      </div>

    </div>
  )
}

/** Строка снаряжения NPC: имя, тип, бонусы брони и описание из каталога (если найдено). */
function NpcGearRow({ gear }: { gear: NpcGearView }) {
  const { name, item } = gear
  const bonuses = item ? [
    item.soakBonus > 0 && `Поглощение +${item.soakBonus}`,
    item.meleeDefense > 0 && `Бл. защита +${item.meleeDefense}`,
    item.rangedDefense > 0 && `Дал. защита +${item.rangedDefense}`,
    item.encumbranceThresholdBonus > 0 && `Порог веса +${item.encumbranceThresholdBonus}`,
  ].filter(Boolean) as string[] : []
  const description = item ? (item.description || item.safeDescription) : ''
  return (
    <li className="npc-gear">
      <div className="npc-gear-head">
        <strong>{name}</strong>
        {item && secondaryName(item) && <span className="muted small-text name-secondary">· {secondaryName(item)}</span>}
        {item && <span className="muted small-text">{ITEM_KIND_LABELS[item.kind]}</span>}
        {bonuses.length > 0 && <span className="npc-gear-bonus">{bonuses.join(' · ')}</span>}
      </div>
      {description && <div className="muted small-text npc-gear-desc">{description}</div>}
      {item?.properties && <PropertyTags properties={item.properties} className="small-text" />}
    </li>
  )
}

const EMPTY_INPUT: NpcInput = {
  name: '', system: 'realmsOfTerrinoth', kind: 'rival', role: 'brute', description: '', source: '',
  brawn: 2, agility: 2, intellect: 2, cunning: 2, willpower: 2, presence: 2,
  woundThreshold: 10, strainThreshold: 10, soak: 2, meleeDefense: 0, rangedDefense: 0,
  silhouette: 1, tactics: '',
  visibility: 'private', campaignId: null,
  skills: [], abilities: [], attacks: [], talents: [], equipment: [], tags: [],
}

function toInput(n: NpcDetail): NpcInput {
  const { id, isMine, createdAt, updatedAt, warnings, ...rest } = n
  void id; void isMine; void createdAt; void updatedAt; void warnings
  return rest
}

function NpcEditor({ initial, onCancel, onSaved }: {
  initial: NpcDetail | null; onCancel: () => void; onSaved: (saved: NpcDetail) => void
}) {
  const [form, setForm] = useState<NpcInput>(initial ? toInput(initial) : EMPTY_INPUT)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  // Тип существа — параметр применения шаблона (теги/способности/природная атака), не поле модели.
  const [template, setTemplate] = useState<CreatureTemplate>('none')
  const [applying, setApplying] = useState(false)
  // Справочник выбранной системы — источник доступных навыков, талантов и снаряжения.
  // Храним вместе с системой, для которой загружен: при смене системы старые опции
  // сразу считаются неактуальными (ref === null), пока не подтянется новый справочник.
  const [loaded, setLoaded] = useState<{ system: GameSystem; data: Reference } | null>(null)
  const set = <K extends keyof NpcInput>(key: K, value: NpcInput[K]) => setForm(f => ({ ...f, [key]: value }))

  // Перезагружаем справочник при смене системы; уже выбранные значения сохраняем.
  useEffect(() => {
    let cancelled = false
    api.reference(form.system)
      .then(r => { if (!cancelled) setLoaded({ system: form.system, data: r }) })
      .catch(() => { /* справочник не загрузился — списки останутся пустыми */ })
    return () => { cancelled = true }
  }, [form.system])
  const reference = loaded?.system === form.system ? loaded.data : null

  // Миньон обычно без усталости; немезида обязан иметь.
  const minion = form.kind === 'minion'

  // Оружие каталога по подписи — источник производных атак из снаряжения.
  const weaponMap = useMemo(() => weaponsByLabel(reference), [reference])

  // Снаряжение меняется → пересобираем производные атаки (оружие → атака), кастомные сохраняем.
  const setEquipment = (equipment: string[]) =>
    setForm(f => ({ ...f, equipment, attacks: syncAttacksWithEquipment(equipment, f.attacks, weaponMap) }))

  // Применяет шаблон типа существа к текущей форме на сервере (единый источник правды с генератором).
  async function applyTemplate() {
    if (template === 'none') return
    setApplying(true); setError(null)
    try {
      const payload: NpcInput = { ...form, strainThreshold: minion ? null : form.strainThreshold }
      setForm(toInput(await api.applyNpcTemplate(payload, template)))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка применения шаблона')
    } finally {
      setApplying(false)
    }
  }

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true); setError(null)
    const payload: NpcInput = { ...form, strainThreshold: minion ? null : form.strainThreshold }
    try {
      const saved = initial ? await api.updateNpc(initial.id, payload) : await api.createNpc(payload)
      onSaved(saved)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка сохранения')
      setBusy(false)
    }
  }

  return (
    <div className="modal-backdrop" onClick={onCancel}>
      <form className="modal wide xwide npc-editor" onClick={e => e.stopPropagation()} onSubmit={submit}>
        <h3>{initial ? 'Редактировать NPC' : 'Новый NPC'}</h3>

        <div className="npc-editor-grid">
          <div className="npc-editor-form">
            <section className="npc-editor-section">
              <h4>Основа</h4>
              <div className="form-row npc-main-grid">
                <label className="grow">Имя<input value={form.name} onChange={e => set('name', e.target.value)} required /></label>
                <label>Система
                  <select value={form.system} onChange={e => set('system', e.target.value as GameSystem)}>
                    {SYSTEMS.map(s => <option key={s} value={s}>{SYSTEM_LABELS[s]}</option>)}
                  </select>
                </label>
              </div>

              <div className="form-row npc-meta-grid">
                <label>Тип
                  <select value={form.kind} onChange={e => set('kind', e.target.value as NpcKind)}>
                    {NPC_KINDS.map(k => <option key={k} value={k}>{NPC_KIND_LABELS[k]}</option>)}
                  </select>
                </label>
                <label>Роль
                  <select value={form.role} onChange={e => set('role', e.target.value as NpcRole)}>
                    {NPC_ROLES.map(r => <option key={r} value={r}>{NPC_ROLE_LABELS[r]}</option>)}
                  </select>
                </label>
                <label>Видимость
                  <select value={form.visibility} onChange={e => set('visibility', e.target.value as NpcVisibility)}>
                    {(['private', 'campaignVisible', 'publicTemplate'] as NpcVisibility[]).map(v =>
                      <option key={v} value={v}>{NPC_VISIBILITY_LABELS[v]}</option>)}
                  </select>
                </label>
              </div>

              <label>Описание<textarea value={form.description} onChange={e => set('description', e.target.value)} rows={2} /></label>

              <div className="list-editor">
                <div className="label-line">Тип существа (шаблон)</div>
                <div className="muted small-text">Добавит к NPC теги, способности и природную атаку выбранного типа. Шаблон не хранится — после применения всё редактируемо.</div>
                <div className="form-row">
                  <select className="grow" value={template} onChange={e => setTemplate(e.target.value as CreatureTemplate)}>
                    {CREATURE_TEMPLATES.map(t => <option key={t} value={t}>{CREATURE_TEMPLATE_LABELS[t]}</option>)}
                  </select>
                  <button type="button" className="small" disabled={template === 'none' || applying}
                    onClick={() => void applyTemplate()}>Применить шаблон</button>
                </div>
              </div>
            </section>

            <section className="npc-editor-section">
              <h4>Характеристики и пороги</h4>
              <div className="label-line">Характеристики (1–6)</div>
              <div className="form-row chars-row npc-stat-grid">
                {CHARACTERISTICS.map(c => (
                  <label key={c} className="char-input">{CHARACTERISTIC_LABELS[c]}
                    <input type="number" min={1} max={6} value={form[c]}
                      onChange={e => set(c, clamp(+e.target.value, 1, 6))} />
                  </label>
                ))}
              </div>

              <div className="label-line">Производные параметры</div>
              <div className="form-row chars-row npc-derived-grid">
                <label className="char-input">Раны<input type="number" min={1} value={form.woundThreshold}
                  onChange={e => set('woundThreshold', Math.max(1, +e.target.value))} /></label>
                <label className="char-input">Усталость<input type="number" min={0} value={form.strainThreshold ?? ''}
                  disabled={minion} placeholder={minion ? '—' : ''}
                  onChange={e => set('strainThreshold', e.target.value === '' ? null : Math.max(0, +e.target.value))} /></label>
                <label className="char-input">Поглощение<input type="number" min={0} value={form.soak}
                  onChange={e => set('soak', Math.max(0, +e.target.value))} /></label>
                <label className="char-input">Бл. защита<input type="number" min={0} value={form.meleeDefense}
                  onChange={e => set('meleeDefense', Math.max(0, +e.target.value))} /></label>
                <label className="char-input">Дал. защита<input type="number" min={0} value={form.rangedDefense}
                  onChange={e => set('rangedDefense', Math.max(0, +e.target.value))} /></label>
                <label className="char-input">Силуэт<input type="number" min={0} max={10} value={form.silhouette}
                  onChange={e => set('silhouette', clamp(+e.target.value, 0, 10))} /></label>
              </div>
              {minion && <div className="muted small-text">Для миньона усталость считается ранами — поле отключено.</div>}
            </section>

            <section className="npc-editor-section">
              <h4>Навыки и атаки</h4>
              <SkillsEditor skills={form.skills} available={reference?.skills ?? []} groupSkills={minion}
                onChange={s => set('skills', s)} />
              <AttacksEditor attacks={form.attacks} skills={reference?.skills ?? []} qualities={reference?.qualities ?? []}
                onChange={a => set('attacks', a)} />
            </section>

            <section className="npc-editor-section">
              <h4>Контент и заметки</h4>
              <AbilitiesEditor abilities={form.abilities} onChange={a => set('abilities', a)} />
              <PickListEditor label="Таланты" values={form.talents} options={reference?.talents ?? []}
                onChange={v => set('talents', v)} placeholder="Выберите талант из списка" />
              <PickListEditor label="Снаряжение" values={form.equipment} options={reference?.items ?? []}
                onChange={setEquipment} placeholder="Выберите предмет из списка" />
              <StringListEditor label="Теги" values={form.tags} onChange={v => set('tags', v)} placeholder="Тег" />

              <label>Тактика (1–3 раунда)<textarea value={form.tactics} rows={2}
                onChange={e => set('tactics', e.target.value)} placeholder="Что NPC делает в бою" /></label>

              <div className="form-row">
                <label className="grow">Источник<input value={form.source} onChange={e => set('source', e.target.value)} /></label>
              </div>
            </section>
          </div>

          <aside className="npc-editor-summary">
            <div className="label-line">Сводка</div>
            <div className="qd-summary-rail vertical">
              <div className="summary-tile"><b>{form.skills.length}</b><span className="muted small-text">навыков</span></div>
              <div className="summary-tile"><b>{form.attacks.length}</b><span className="muted small-text">атак</span></div>
              <div className="summary-tile"><b>{form.abilities.length}</b><span className="muted small-text">способностей</span></div>
              <div className="summary-tile"><b>{form.equipment.length}</b><span className="muted small-text">предметов</span></div>
            </div>

            <div className="npc-card qd-preview-card">
              <div className="npc-card-head">
                <h3>{form.name.trim() || 'Без имени'}</h3>
                <span className="muted small-text">
                  {NPC_KIND_LABELS[form.kind]} · {NPC_ROLE_LABELS[form.role]} · {NPC_VISIBILITY_LABELS[form.visibility]}
                </span>
              </div>
              <div className="npc-char-row">
                {CHARACTERISTICS.map(c => (
                  <div key={c} className="npc-char">
                    <span className="npc-char-val">{form[c]}</span>
                    <span className="npc-char-label">{CHARACTERISTIC_LABELS[c]}</span>
                  </div>
                ))}
              </div>
              <div className="npc-derived">
                <span><b>Раны</b> {form.woundThreshold}</span>
                {!minion && form.strainThreshold != null && <span><b>Усталость</b> {form.strainThreshold}</span>}
                <span><b>Погл.</b> {form.soak}</span>
                <span><b>Защита</b> {form.meleeDefense}/{form.rangedDefense}</span>
              </div>
              {form.skills.filter(s => s.name).length > 0 && (
                <div className="npc-section">
                  <h4>Навыки</h4>
                  <ul className="npc-skill-list">
                    {form.skills.filter(s => s.name).map((s, i) => (
                      <li key={i} className="npc-skill-row">
                        <span className="npc-skill-name">{s.name}{!minion && <span className="muted"> {s.ranks}</span>}</span>
                      </li>
                    ))}
                  </ul>
                </div>
              )}
              {form.attacks.filter(a => a.name).length > 0 && (
                <div className="npc-section">
                  <h4>Атаки</h4>
                  <ul className="npc-weapon-list">
                    {form.attacks.filter(a => a.name).map((a, i) => (
                      <li key={i} className="npc-weapon">
                        <div className="npc-weapon-stats">
                          <strong>{a.name}</strong>
                          {a.damage && <span className="weapon-stat">урон {a.damage}</span>}
                          {a.critical && <span className="weapon-stat">крит {a.critical}</span>}
                          {a.rangeBand && <span className="weapon-stat">{a.rangeBand}</span>}
                        </div>
                      </li>
                    ))}
                  </ul>
                </div>
              )}
              {form.equipment.length > 0 && (
                <div className="npc-section">
                  <h4>Снаряжение</h4>
                  <div className="chips">{form.equipment.map((g, i) => <span key={i} className="chip">{g}</span>)}</div>
                </div>
              )}
              {form.tags.length > 0 && (
                <div className="chips">{form.tags.map((t, i) => <span key={i} className="chip">{t}</span>)}</div>
              )}
            </div>
          </aside>
        </div>

        {error && <div className="error">{error}</div>}
        <div className="modal-actions">
          <button type="button" onClick={onCancel}>Отмена</button>
          <button className="primary" type="submit" disabled={busy || !form.name.trim()}>Сохранить</button>
        </div>
      </form>
    </div>
  )
}

// Отображаемое имя справочной записи: русское, с откатом на оригинальное.
const refLabel = (o: { name: string; nameRu: string }) => o.nameRu || o.name

function SkillsEditor({ skills, available, groupSkills = false, onChange }: {
  skills: NpcInput['skills']; available: { name: string; nameRu: string }[]
  groupSkills?: boolean
  onChange: (s: NpcInput['skills']) => void
}) {
  const names = available.map(refLabel)
  return (
    <div className="list-editor">
      <div className="label-line">{groupSkills ? 'Групповые навыки' : 'Навыки'}</div>
      {groupSkills && <div className="muted small-text">Миньоны используют групповые навыки без рангов (ранг = размер группы − 1).</div>}
      {skills.map((s, i) => {
        // Текущее значение оставляем выбираемым, даже если его нет в справочнике (кастом/смена системы).
        const orphan = s.name !== '' && !names.includes(s.name)
        return (
          <div key={i} className="form-row">
            <select className="grow" value={s.name}
              onChange={e => onChange(skills.map((x, j) => j === i ? { ...x, name: e.target.value } : x))}>
              <option value="">— выберите навык —</option>
              {orphan && <option value={s.name}>{s.name} (вне справочника)</option>}
              {available.map(sk => <option key={sk.name} value={refLabel(sk)}>{dualName(sk)}</option>)}
            </select>
            {!groupSkills && <input type="number" min={0} max={5} className="ranks-input" value={s.ranks}
              onChange={e => onChange(skills.map((x, j) => j === i ? { ...x, ranks: clamp(+e.target.value, 0, 5) } : x))} />}
            <button type="button" className="danger small" onClick={() => onChange(skills.filter((_, j) => j !== i))}>×</button>
          </div>
        )
      })}
      <button type="button" className="small" disabled={available.length === 0}
        onClick={() => onChange([...skills, { name: '', ranks: groupSkills ? 0 : 1 }])}>
        {groupSkills ? '+ Групповой навык' : '+ Навык'}
      </button>
    </div>
  )
}

/** Выбор значений из справочника (таланты, снаряжение): чипы выбранного + выпадающий список доступного. */
function PickListEditor({ label, values, options, onChange, placeholder }: {
  label: string; values: string[]; options: { name: string; nameRu: string }[]
  onChange: (v: string[]) => void; placeholder: string
}) {
  const remaining = options.filter(o => !values.includes(refLabel(o)))
  return (
    <div className="list-editor">
      <div className="label-line">{label}</div>
      <div className="chips">
        {values.map((v, i) => (
          <span key={i} className="chip removable">
            {v}<button type="button" onClick={() => onChange(values.filter((_, j) => j !== i))}>×</button>
          </span>
        ))}
      </div>
      <select className="grow" value="" disabled={remaining.length === 0}
        onChange={e => { if (e.target.value) onChange([...values, e.target.value]) }}>
        <option value="">
          {options.length === 0 ? 'Справочник загружается…'
            : remaining.length === 0 ? 'Все доступные уже добавлены' : placeholder}
        </option>
        {remaining.map(o => <option key={o.name} value={refLabel(o)}>{dualName(o)}</option>)}
      </select>
    </div>
  )
}

function AbilitiesEditor({ abilities, onChange }: { abilities: NpcInput['abilities']; onChange: (a: NpcInput['abilities']) => void }) {
  return (
    <div className="list-editor">
      <div className="label-line">Способности</div>
      {abilities.map((a, i) => (
        <div key={i} className="ability-edit">
          <div className="form-row">
            <input className="grow" placeholder="Название способности" value={a.name}
              onChange={e => onChange(abilities.map((x, j) => j === i ? { ...x, name: e.target.value } : x))} />
            <button type="button" className="danger small" onClick={() => onChange(abilities.filter((_, j) => j !== i))}>×</button>
          </div>
          <textarea placeholder="Описание" rows={2} value={a.description}
            onChange={e => onChange(abilities.map((x, j) => j === i ? { ...x, description: e.target.value } : x))} />
        </div>
      ))}
      <button type="button" className="small" onClick={() => onChange([...abilities, { name: '', description: '' }])}>+ Способность</button>
    </div>
  )
}

const EMPTY_ATTACK: NpcAttackEntry = {
  name: '', skillName: '', damage: '', critical: '', rangeBand: '', notes: '', qualities: [], sourceWeapon: '',
}

/** Редактор структурных атак NPC: производные из снаряжения (бейдж) + кастомные (имя, навык, урон…). */
function AttacksEditor({ attacks, skills, qualities, onChange }: {
  attacks: NpcAttackEntry[]; skills: SkillDef[]; qualities: Quality[]
  onChange: (a: NpcAttackEntry[]) => void
}) {
  const upd = (i: number, patch: Partial<NpcAttackEntry>) =>
    onChange(attacks.map((x, j) => j === i ? { ...x, ...patch } : x))
  return (
    <div className="list-editor">
      <div className="label-line">Атаки</div>
      <div className="muted small-text">Оружие из «Снаряжения» добавляет атаку автоматически; «+ Атака» — кастомная.</div>
      {attacks.map((a, i) => {
        const remaining = qualities.filter(q => !a.qualities.some(x => x.qualityCode === q.code))
        const derived = a.sourceWeapon !== ''
        return (
          <div key={i} className="ability-edit">
            {derived && <div className="muted small-text">🗡 из снаряжения: «{a.sourceWeapon}» (убрать — удалить оружие из инвентаря)</div>}
            <div className="form-row">
              <input className="grow" placeholder="Название атаки/оружия" value={a.name}
                onChange={e => upd(i, { name: e.target.value })} />
              {!derived && <button type="button" className="danger small" onClick={() => onChange(attacks.filter((_, j) => j !== i))}>×</button>}
            </div>
            <div className="form-row">
              <select className="grow" value={a.skillName} onChange={e => upd(i, { skillName: e.target.value })}>
                <option value="">— навык броска —</option>
                {a.skillName !== '' && !skills.some(s => s.name === a.skillName) &&
                  <option value={a.skillName}>{a.skillName} (вне справочника)</option>}
                {skills.filter(s => s.kind === 'combat' || s.kind === 'magic').map(s => (
                  <option key={s.name} value={s.name}>{dualName(s)}</option>
                ))}
              </select>
            </div>
            <div className="form-row">
              <label className="char-input">Урон<input value={a.damage} placeholder="+3 / 7"
                onChange={e => upd(i, { damage: e.target.value })} /></label>
              <label className="char-input">Крит<input value={a.critical} placeholder="2"
                onChange={e => upd(i, { critical: e.target.value })} /></label>
              <label className="char-input grow">Дистанция<input value={a.rangeBand} placeholder="Вплотную"
                onChange={e => upd(i, { rangeBand: e.target.value })} /></label>
            </div>
            <div className="chips">
              {a.qualities.map((q, k) => (
                <span key={k} className="chip removable">
                  {q.nameRu || q.qualityCode}
                  {q.rating != null && <input type="number" min={1} className="ranks-input" value={q.rating}
                    onChange={e => upd(i, { qualities: a.qualities.map((x, m) => m === k ? { ...x, rating: Math.max(1, +e.target.value) } : x) })} />}
                  <button type="button" onClick={() => upd(i, { qualities: a.qualities.filter((_, m) => m !== k) })}>×</button>
                </span>
              ))}
            </div>
            <select className="grow" value="" disabled={remaining.length === 0}
              onChange={e => {
                const q = qualities.find(x => x.code === e.target.value)
                if (q) upd(i, { qualities: [...a.qualities, { qualityCode: q.code, nameRu: q.nameRu || q.nameEn, rating: q.hasRating ? 1 : null }] })
              }}>
              <option value="">{qualities.length === 0 ? 'Справочник качеств загружается…' : remaining.length === 0 ? 'Все качества добавлены' : 'Добавить качество'}</option>
              {remaining.map(q => <option key={q.code} value={q.code}>{q.nameRu || q.nameEn}</option>)}
            </select>
            <input placeholder="Заметки по атаке" value={a.notes}
              onChange={e => upd(i, { notes: e.target.value })} />
          </div>
        )
      })}
      <button type="button" className="small" onClick={() => onChange([...attacks, { ...EMPTY_ATTACK }])}>+ Атака</button>
    </div>
  )
}

function StringListEditor({ label, values, onChange, placeholder }: {
  label: string; values: string[]; onChange: (v: string[]) => void; placeholder: string
}) {
  const [draft, setDraft] = useState('')
  function add() {
    const v = draft.trim()
    if (!v) return
    onChange([...values, v]); setDraft('')
  }
  return (
    <div className="list-editor">
      <div className="label-line">{label}</div>
      <div className="chips">
        {values.map((v, i) => (
          <span key={i} className="chip removable">
            {v}<button type="button" onClick={() => onChange(values.filter((_, j) => j !== i))}>×</button>
          </span>
        ))}
      </div>
      <div className="form-row">
        <input className="grow" placeholder={placeholder} value={draft} onChange={e => setDraft(e.target.value)}
          onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); add() } }} />
        <button type="button" className="small" onClick={add}>+</button>
      </div>
    </div>
  )
}

/** Подсказки ролевых профилей: чем занята роль и какой боевой стиль ей соответствует. */
const ROLE_HINTS: Record<NpcRole, { sub: string; style: NpcCombatStyle }> = {
  brute: { sub: 'ближний бой, давление, живучесть', style: 'melee' },
  skirmisher: { sub: 'мобильность, фланги, защита', style: 'melee' },
  archer: { sub: 'дистанция, укрытие, фокус', style: 'ranged' },
  caster: { sub: 'заклинания, воля, дальняя угроза', style: 'magic' },
  leader: { sub: 'лидерство, приказы, координация', style: 'melee' },
  social: { sub: 'обман, переговоры, влияние', style: 'social' },
  support: { sub: 'лечение, поддержка, помощь', style: 'magic' },
  monster: { sub: 'природные атаки, шаблон существа', style: 'melee' },
  custom: { sub: 'свой профиль — настройте вручную', style: 'melee' },
}

// Приоритет характеристик роли — зеркалит NpcDraftGenerator.PrimaryOf/SecondaryOf для подписи профиля.
const ROLE_PRIMARY: Record<NpcRole, Characteristic> = {
  brute: 'brawn', monster: 'brawn', skirmisher: 'agility', archer: 'agility',
  caster: 'willpower', support: 'willpower', leader: 'presence', social: 'presence', custom: 'brawn',
}
const ROLE_SECONDARY: Record<NpcRole, Characteristic> = {
  brute: 'agility', monster: 'agility', skirmisher: 'cunning', archer: 'cunning',
  caster: 'intellect', support: 'intellect', leader: 'cunning', social: 'cunning', custom: 'agility',
}

/**
 * Быстрый черновик NPC по прототипу npc-quick-draft: ролевые карточки с профилем,
 * параметры генерации и live preview результата (что именно будет создано и почему).
 */
function QuickDraftForm({ onCancel, onCreated }: { onCancel: () => void; onCreated: (n: NpcDetail) => void }) {
  const [req, setReq] = useState<QuickDraftRequest>({
    system: 'realmsOfTerrinoth', kind: 'rival', role: 'brute', powerLevel: 'standard',
    primaryCharacteristic: null, combatStyle: 'melee', name: null,
    template: 'none', magicSkill: null, environment: null,
  })
  const [name, setName] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const set = <K extends keyof QuickDraftRequest>(k: K, v: QuickDraftRequest[K]) => setReq(r => ({ ...r, [k]: v }))
  const powerLevels = useMemo<NpcPowerLevel[]>(() => ['weak', 'standard', 'strong', 'elite'], [])

  // Выбор роли автоматически согласует боевой стиль профиля (можно переопределить ниже).
  const pickRole = (role: NpcRole) => setReq(r => ({ ...r, role, combatStyle: ROLE_HINTS[role].style }))

  // Справочник системы — для магшкол и RU/ENG подписей навыков/предметов в preview.
  const [loaded, setLoaded] = useState<{ system: GameSystem; data: Reference } | null>(null)
  useEffect(() => {
    let cancelled = false
    api.reference(req.system)
      .then(r => { if (!cancelled) setLoaded({ system: req.system, data: r }) })
      .catch(() => { /* без справочника список магшкол будет пуст */ })
    return () => { cancelled = true }
  }, [req.system])
  const reference = loaded?.system === req.system ? loaded.data : null
  const magicSkills = (reference?.skills ?? []).filter(s => s.kind === 'magic')

  // Live preview: генератор без сохранения, с дебаунсом по параметрам.
  const [preview, setPreview] = useState<NpcDetail | null>(null)
  const [previewBusy, setPreviewBusy] = useState(false)
  useEffect(() => {
    let cancelled = false
    const t = setTimeout(() => {
      setPreviewBusy(true)
      api.previewQuickDraftNpc({ ...req, name: name.trim() || null })
        .then(p => { if (!cancelled) { setPreview(p); setError(null) } })
        .catch((e: unknown) => { if (!cancelled) setError(e instanceof Error ? e.message : 'Ошибка предпросмотра') })
        .finally(() => { if (!cancelled) setPreviewBusy(false) })
    }, 300)
    return () => { cancelled = true; clearTimeout(t) }
  }, [req, name])

  const index = useMemo(() => skillIndex(reference), [reference])
  const previewSkills = useMemo(() => (preview ? npcSkillViews(preview, index) : []), [preview, index])
  const previewAttacks = useMemo(() => (preview ? npcAttackViews(preview, reference) : []), [preview, reference])
  const previewGear = useMemo(() => (preview ? npcGearViews(preview, reference) : []), [preview, reference])

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true); setError(null)
    try {
      const npc = await api.quickDraftNpc({ ...req, name: name.trim() || null })
      onCreated(npc)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка')
      setBusy(false)
    }
  }

  const chars: [Characteristic, number][] = preview ? [
    ['brawn', preview.brawn], ['agility', preview.agility], ['intellect', preview.intellect],
    ['cunning', preview.cunning], ['willpower', preview.willpower], ['presence', preview.presence],
  ] : []

  return (
    <div className="modal-backdrop" onClick={onCancel}>
      <form className="modal wide xwide quick-draft" onClick={e => e.stopPropagation()} onSubmit={submit}>
        <h3>⚡ Быстрый черновик NPC</h3>
        <p className="hint">
          Ролевой профиль задаёт характеристики, стиль боя, ключевые навыки, снаряжение и способности.
          Справа — предпросмотр того, что будет создано; после создания всё можно править.
        </p>

        <div className="quick-draft-grid">
          <aside className="qd-roles">
            <div className="label-line">Ролевой профиль</div>
            <div className="role-list">
              {NPC_ROLES.map(r => (
                <button key={r} type="button" className={`role-card${req.role === r ? ' active' : ''}`}
                  onClick={() => pickRole(r)}>
                  <span className="role-name">{NPC_ROLE_LABELS[r]}</span>
                  <span className="role-sub muted small-text">{ROLE_HINTS[r].sub}</span>
                </button>
              ))}
            </div>
            <div className="qd-profile">
              <div className="label-line">Профиль: {NPC_ROLE_LABELS[req.role]}</div>
              <div className="small-text">
                <div><b>Стиль:</b> {NPC_COMBAT_STYLE_LABELS[req.combatStyle]}</div>
                <div><b>Приоритет:</b> {CHARACTERISTIC_LABELS[req.primaryCharacteristic ?? ROLE_PRIMARY[req.role]]}, {CHARACTERISTIC_LABELS[ROLE_SECONDARY[req.role]]}</div>
              </div>
            </div>
          </aside>

          <section className="qd-params">
            <label>Имя (необязательно)
              <input value={name} onChange={e => setName(e.target.value)} placeholder="Сгенерируется автоматически" />
            </label>
            <label>Система
              <select value={req.system} onChange={e => set('system', e.target.value as GameSystem)}>
                {SYSTEMS.map(s => <option key={s} value={s}>{SYSTEM_LABELS[s]}</option>)}
              </select>
            </label>

            <div className="label-line">Тип и сила</div>
            <div className="qd-choice-grid">
              {NPC_KINDS.map(k => (
                <button key={k} type="button" className={`choice${req.kind === k ? ' active' : ''}`}
                  onClick={() => set('kind', k)}>
                  <strong>{NPC_KIND_LABELS[k]}</strong>
                  <span className="muted small-text">
                    {k === 'minion' ? 'группа, без усталости' : k === 'rival' ? 'одиночный противник' : 'босс, усталость'}
                  </span>
                </button>
              ))}
            </div>
            <div className="qd-choice-grid">
              {powerLevels.map(p => (
                <button key={p} type="button" className={`choice${req.powerLevel === p ? ' active' : ''}`}
                  onClick={() => set('powerLevel', p)}>
                  <strong>{NPC_POWER_LABELS[p]}</strong>
                </button>
              ))}
            </div>

            <div className="form-row">
              <label className="grow">Боевой стиль
                <select value={req.combatStyle} onChange={e => set('combatStyle', e.target.value as NpcCombatStyle)}>
                  {(['melee', 'ranged', 'magic', 'social'] as NpcCombatStyle[]).map(s =>
                    <option key={s} value={s}>{NPC_COMBAT_STYLE_LABELS[s]}</option>)}
                </select>
              </label>
              {req.combatStyle === 'magic' && (
                <label className="grow">Магшкола
                  <select value={req.magicSkill ?? ''} onChange={e => set('magicSkill', e.target.value || null)}>
                    <option value="">По умолчанию</option>
                    {magicSkills.map(s => <option key={s.name} value={refLabel(s)}>{dualName(s)}</option>)}
                  </select>
                </label>
              )}
            </div>
            <div className="form-row">
              <label className="grow">Основная характеристика
                <select value={req.primaryCharacteristic ?? ''}
                  onChange={e => set('primaryCharacteristic', (e.target.value || null) as Characteristic | null)}>
                  <option value="">По роли: {CHARACTERISTIC_LABELS[ROLE_PRIMARY[req.role]]}</option>
                  {CHARACTERISTICS.map(c => <option key={c} value={c}>{CHARACTERISTIC_LABELS[c]}</option>)}
                </select>
              </label>
              <label className="grow">Тип существа
                <select value={req.template ?? 'none'} onChange={e => set('template', e.target.value as CreatureTemplate)}>
                  {CREATURE_TEMPLATES.map(t => <option key={t} value={t}>{CREATURE_TEMPLATE_LABELS[t]}</option>)}
                </select>
              </label>
            </div>
            <label>Окружение / тег (необязательно)
              <input value={req.environment ?? ''} onChange={e => set('environment', e.target.value || null)}
                placeholder="например, лес, подземелье" />
            </label>

            {preview && (
              <>
                <div className="label-line">Будет добавлено ролью и стилем</div>
                <div className="qd-summary-rail">
                  <div className="summary-tile">
                    <b>{preview.skills.length} нав.</b>
                    <span className="muted small-text">{previewSkills.map(s => s.name).join(', ') || '—'}</span>
                  </div>
                  <div className="summary-tile">
                    <b>{preview.equipment.length} предм.</b>
                    <span className="muted small-text">{preview.equipment.join(', ') || '—'}</span>
                  </div>
                  <div className="summary-tile">
                    <b>{preview.abilities.length} способн.</b>
                    <span className="muted small-text">{preview.abilities.map(a => a.name).join(', ') || '—'}</span>
                  </div>
                </div>
              </>
            )}
          </section>

          <aside className="qd-preview">
            <div className="label-line">
              Предпросмотр {previewBusy && <span className="muted small-text">обновляется…</span>}
            </div>
            {!preview && <p className="muted">Предпросмотр загружается…</p>}
            {preview && (
              <div className="npc-card qd-preview-card">
                <div className="npc-card-head">
                  <h3>{preview.name}</h3>
                  <span className="muted small-text">
                    {NPC_KIND_LABELS[preview.kind]} · {NPC_ROLE_LABELS[preview.role]} · {SYSTEM_LABELS[preview.system]}
                  </span>
                </div>
                <div className="npc-char-row">
                  {chars.map(([c, v]) => (
                    <div key={c} className="npc-char">
                      <span className="npc-char-val">{v}</span>
                      <span className="npc-char-label">{CHARACTERISTIC_LABELS[c]}</span>
                    </div>
                  ))}
                </div>
                <div className="npc-derived">
                  <span><b>Раны</b> {preview.woundThreshold}</span>
                  {preview.strainThreshold != null && <span><b>Усталость</b> {preview.strainThreshold}</span>}
                  <span><b>Погл.</b> {preview.soak}</span>
                  <span><b>Защита</b> {preview.meleeDefense}/{preview.rangedDefense}</span>
                </div>
                {previewSkills.length > 0 && (
                  <div className="npc-section">
                    <h4>Навыки</h4>
                    <ul className="npc-skill-list">
                      {previewSkills.map((s, i) => {
                        const def = index.get(s.name)
                        const original = def ? secondaryName(def) : ''
                        return (
                          <li key={i} className="npc-skill-row">
                            <span className="npc-skill-name">
                              {s.name}
                              {original && original !== s.name && <span className="muted small-text name-secondary"> · {original}</span>}
                              {' '}{preview.kind !== 'minion' && <span className="muted">{s.ranks}</span>}
                            </span>
                            {s.pool && <DicePoolView pool={s.pool} />}
                          </li>
                        )
                      })}
                    </ul>
                  </div>
                )}
                {previewAttacks.length > 0 && (
                  <div className="npc-section">
                    <h4>Атаки</h4>
                    <ul className="npc-weapon-list">
                      {previewAttacks.map((w, i) => (
                        <li key={i} className="npc-weapon">
                          <div className="npc-weapon-head"><strong>{w.name}</strong>
                            {w.pool && <DicePoolView pool={w.pool} />}
                          </div>
                          <div className="npc-weapon-stats">
                            <span className="weapon-stat">Урон <strong>{w.damageText}</strong></span>
                            {w.crit && <span className="weapon-stat">Крит <strong>{w.crit}</strong></span>}
                            {w.rangeBand && <span className="weapon-stat">{w.rangeBand}</span>}
                          </div>
                        </li>
                      ))}
                    </ul>
                  </div>
                )}
                {previewGear.length > 0 && (
                  <div className="npc-section">
                    <h4>Снаряжение</h4>
                    <ul className="npc-gear-list">
                      {previewGear.map((g, i) => (
                        <li key={i} className="npc-gear">
                          <strong>{g.name}</strong>
                          {g.item && secondaryName(g.item) && (
                            <span className="muted small-text name-secondary"> · {secondaryName(g.item)}</span>
                          )}
                        </li>
                      ))}
                    </ul>
                  </div>
                )}
                {preview.abilities.length > 0 && (
                  <div className="npc-section">
                    <h4>Способности</h4>
                    <ul>{preview.abilities.map((a, i) => <li key={i} className="small-text"><b>{a.name}.</b> {a.description}</li>)}</ul>
                  </div>
                )}
                {preview.tags.length > 0 && (
                  <div className="chips">{preview.tags.map(t => <span key={t} className="chip">{t}</span>)}</div>
                )}
              </div>
            )}
          </aside>
        </div>

        {error && <div className="error">{error}</div>}
        <div className="modal-actions">
          <button type="button" onClick={onCancel}>Отмена</button>
          <button className="primary" type="submit" disabled={busy}>Создать черновик</button>
        </div>
      </form>
    </div>
  )
}

function clamp(v: number, min: number, max: number) {
  return Number.isNaN(v) ? min : Math.min(max, Math.max(min, v))
}
