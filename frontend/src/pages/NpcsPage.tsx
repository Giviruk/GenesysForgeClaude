import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type {
  Characteristic, GameSystem, NpcCombatStyle, NpcDetail, NpcInput, NpcKind, NpcListItem,
  NpcPowerLevel, NpcRole, NpcVisibility, QuickDraftRequest, Reference,
} from '../api/types'
import {
  CHARACTERISTICS, CHARACTERISTIC_LABELS, ITEM_KIND_LABELS, NPC_COMBAT_STYLE_LABELS, NPC_KIND_LABELS,
  NPC_KINDS, NPC_POWER_LABELS, NPC_ROLE_LABELS, NPC_ROLES, NPC_VISIBILITY_LABELS, SYSTEM_LABELS,
} from '../utils/labels'
import { npcSkillViews, skillIndex, splitEquipment, type NpcGearView } from '../utils/npcStats'
import { DicePoolView } from '../components/DicePoolView'
import { PropertyTags } from '../components/PropertyTags'
import { PrintPreview } from '../components/print/PrintPreview'
import { AdversaryCard } from '../components/print/cards'
import { adversaryMarkdown } from '../components/print/markdown'

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
              <select value={sort} onChange={e => setSort(e.target.value as 'createdAt' | 'name')}>
                <option value="createdAt">Сначала новые</option>
                <option value="name">По имени</option>
              </select>
            </div>
          </div>

          {npcs === null && <p className="muted bestiary-hint">Загрузка…</p>}
          {npcs?.length === 0 && <p className="muted bestiary-hint">Ничего не найдено — создайте NPC или быстрый черновик.</p>}
          <ul className="bestiary-items">
            {npcs?.map(n => (
              <li key={n.id}>
                <button type="button" className={`bestiary-item${n.id === openId ? ' active' : ''}`}
                  onClick={() => onOpen(n.id)}>
                  <span className="bestiary-item-main">
                    <strong>{n.name}</strong>
                    <span className={`badge ${n.system}`}>{SYSTEM_LABELS[n.system]}</span>
                  </span>
                  <span className="bestiary-item-sub muted">
                    {NPC_KIND_LABELS[n.kind]} · {NPC_ROLE_LABELS[n.role]} · Soak {n.soak} · Раны {n.woundThreshold}
                  </span>
                  {!n.isMine && <span className="bestiary-item-flag muted">из кампании</span>}
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
  const { weapons, gear } = useMemo(() => (
    n ? splitEquipment(n, reference) : { weapons: [], gear: [] }
  ), [n, reference])

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
      </div>

      <div className="npc-card">
        <div className="npc-card-head">
          <h3>{n.name}</h3>
          <span className="muted">
            {SYSTEM_LABELS[n.system]} · {NPC_KIND_LABELS[n.kind]} · {NPC_ROLE_LABELS[n.role]}
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
          <span><b>Soak</b> {n.soak}</span>
          <span><b>Раны</b> {n.woundThreshold}</span>
          {n.strainThreshold != null && <span><b>Стрейн</b> {n.strainThreshold}</span>}
          <span><b>Бл. защита</b> {n.meleeDefense}</span>
          <span><b>Дал. защита</b> {n.rangedDefense}</span>
        </div>

        {skills.length > 0 && (
          <div className="npc-section">
            <h4>Навыки</h4>
            <ul className="npc-skill-list">
              {skills.map((s, i) => (
                <li key={i} className="npc-skill-row">
                  <span className="npc-skill-name">
                    {s.name} <span className="muted">{s.ranks}</span>
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

        {weapons.length > 0 && (
          <div className="npc-section">
            <h4>Оружие</h4>
            <ul className="npc-weapon-list">
              {weapons.map((w, i) => (
                <li key={i} className="npc-weapon">
                  <div className="npc-weapon-head">
                    <strong>{w.name}</strong>
                    {w.pool
                      ? <span className="weapon-pool" title={w.skillLabel ? `Бросок: ${w.skillLabel}` : undefined}>
                          <DicePoolView pool={w.pool} />
                          {w.skillLabel && <span className="muted small-text">{w.skillLabel}</span>}
                        </span>
                      : <span className="muted small-text">навык не освоен</span>}
                  </div>
                  <div className="npc-weapon-stats">
                    <span className="weapon-stat">Урон <strong>{w.damageText}</strong></span>
                    {w.crit && <span className="weapon-stat">Крит <strong>{w.crit}</strong></span>}
                    {w.rangeBand && <span className="weapon-stat">{w.rangeBand}</span>}
                  </div>
                  {w.properties && <PropertyTags properties={w.properties} className="weapon-props small-text" />}
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
  visibility: 'private', campaignId: null,
  skills: [], abilities: [], talents: [], equipment: [], tags: [],
}

function toInput(n: NpcDetail): NpcInput {
  const { id, isMine, createdAt, updatedAt, ...rest } = n
  void id; void isMine; void createdAt; void updatedAt
  return rest
}

function NpcEditor({ initial, onCancel, onSaved }: {
  initial: NpcDetail | null; onCancel: () => void; onSaved: (saved: NpcDetail) => void
}) {
  const [form, setForm] = useState<NpcInput>(initial ? toInput(initial) : EMPTY_INPUT)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
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

  // Миньон обычно без стрейна; немезида обязан иметь.
  const minion = form.kind === 'minion'

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
      <form className="modal wide" onClick={e => e.stopPropagation()} onSubmit={submit}>
        <h3>{initial ? 'Редактировать NPC' : 'Новый NPC'}</h3>

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
          <label className="char-input">Стрейн<input type="number" min={0} value={form.strainThreshold ?? ''}
            disabled={minion} placeholder={minion ? '—' : ''}
            onChange={e => set('strainThreshold', e.target.value === '' ? null : Math.max(0, +e.target.value))} /></label>
          <label className="char-input">Soak<input type="number" min={0} value={form.soak}
            onChange={e => set('soak', Math.max(0, +e.target.value))} /></label>
          <label className="char-input">Бл. защита<input type="number" min={0} value={form.meleeDefense}
            onChange={e => set('meleeDefense', Math.max(0, +e.target.value))} /></label>
          <label className="char-input">Дал. защита<input type="number" min={0} value={form.rangedDefense}
            onChange={e => set('rangedDefense', Math.max(0, +e.target.value))} /></label>
        </div>

        <SkillsEditor skills={form.skills} available={reference?.skills ?? []} onChange={s => set('skills', s)} />
        <AbilitiesEditor abilities={form.abilities} onChange={a => set('abilities', a)} />
        <PickListEditor label="Таланты" values={form.talents} options={reference?.talents ?? []}
          onChange={v => set('talents', v)} placeholder="Выберите талант из списка" />
        <PickListEditor label="Снаряжение" values={form.equipment} options={reference?.items ?? []}
          onChange={v => set('equipment', v)} placeholder="Выберите предмет из списка" />
        <StringListEditor label="Теги" values={form.tags} onChange={v => set('tags', v)} placeholder="Тег" />

        <div className="form-row">
          <label className="grow">Источник<input value={form.source} onChange={e => set('source', e.target.value)} /></label>
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

function SkillsEditor({ skills, available, onChange }: {
  skills: NpcInput['skills']; available: { name: string; nameRu: string }[]
  onChange: (s: NpcInput['skills']) => void
}) {
  const names = available.map(refLabel)
  return (
    <div className="list-editor">
      <div className="label-line">Навыки</div>
      {skills.map((s, i) => {
        // Текущее значение оставляем выбираемым, даже если его нет в справочнике (кастом/смена системы).
        const orphan = s.name !== '' && !names.includes(s.name)
        return (
          <div key={i} className="form-row">
            <select className="grow" value={s.name}
              onChange={e => onChange(skills.map((x, j) => j === i ? { ...x, name: e.target.value } : x))}>
              <option value="">— выберите навык —</option>
              {orphan && <option value={s.name}>{s.name} (вне справочника)</option>}
              {available.map(sk => <option key={sk.name} value={refLabel(sk)}>{refLabel(sk)}</option>)}
            </select>
            <input type="number" min={0} max={5} className="ranks-input" value={s.ranks}
              onChange={e => onChange(skills.map((x, j) => j === i ? { ...x, ranks: clamp(+e.target.value, 0, 5) } : x))} />
            <button type="button" className="danger small" onClick={() => onChange(skills.filter((_, j) => j !== i))}>×</button>
          </div>
        )
      })}
      <button type="button" className="small" disabled={available.length === 0}
        onClick={() => onChange([...skills, { name: '', ranks: 1 }])}>+ Навык</button>
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
        {remaining.map(o => <option key={o.name} value={refLabel(o)}>{refLabel(o)}</option>)}
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

function QuickDraftForm({ onCancel, onCreated }: { onCancel: () => void; onCreated: (n: NpcDetail) => void }) {
  const [req, setReq] = useState<QuickDraftRequest>({
    system: 'realmsOfTerrinoth', kind: 'rival', role: 'brute', powerLevel: 'standard',
    primaryCharacteristic: null, combatStyle: 'melee', name: null,
  })
  const [name, setName] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const set = <K extends keyof QuickDraftRequest>(k: K, v: QuickDraftRequest[K]) => setReq(r => ({ ...r, [k]: v }))
  const powerLevels = useMemo<NpcPowerLevel[]>(() => ['weak', 'standard', 'strong', 'elite'], [])

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

  return (
    <div className="modal-backdrop" onClick={onCancel}>
      <form className="modal" onClick={e => e.stopPropagation()} onSubmit={submit}>
        <h3>⚡ Быстрый черновик NPC</h3>
        <p className="hint">Детерминированный генератор соберёт заготовку статблока — затем вы её отредактируете.</p>

        <label>Имя (необязательно)<input value={name} onChange={e => setName(e.target.value)} placeholder="Сгенерируется автоматически" /></label>
        <div className="form-row">
          <label>Система
            <select value={req.system} onChange={e => set('system', e.target.value as GameSystem)}>
              {SYSTEMS.map(s => <option key={s} value={s}>{SYSTEM_LABELS[s]}</option>)}
            </select>
          </label>
          <label>Тип
            <select value={req.kind} onChange={e => set('kind', e.target.value as NpcKind)}>
              {NPC_KINDS.map(k => <option key={k} value={k}>{NPC_KIND_LABELS[k]}</option>)}
            </select>
          </label>
        </div>
        <div className="form-row">
          <label>Роль
            <select value={req.role} onChange={e => set('role', e.target.value as NpcRole)}>
              {NPC_ROLES.map(r => <option key={r} value={r}>{NPC_ROLE_LABELS[r]}</option>)}
            </select>
          </label>
          <label>Уровень силы
            <select value={req.powerLevel} onChange={e => set('powerLevel', e.target.value as NpcPowerLevel)}>
              {powerLevels.map(p => <option key={p} value={p}>{NPC_POWER_LABELS[p]}</option>)}
            </select>
          </label>
        </div>
        <div className="form-row">
          <label>Боевой стиль
            <select value={req.combatStyle} onChange={e => set('combatStyle', e.target.value as NpcCombatStyle)}>
              {(['melee', 'ranged', 'magic', 'social'] as NpcCombatStyle[]).map(s =>
                <option key={s} value={s}>{NPC_COMBAT_STYLE_LABELS[s]}</option>)}
            </select>
          </label>
          <label>Основная характеристика
            <select value={req.primaryCharacteristic ?? ''}
              onChange={e => set('primaryCharacteristic', (e.target.value || null) as Characteristic | null)}>
              <option value="">По роли</option>
              {CHARACTERISTICS.map(c => <option key={c} value={c}>{CHARACTERISTIC_LABELS[c]}</option>)}
            </select>
          </label>
        </div>

        {error && <div className="error">{error}</div>}
        <div className="modal-actions">
          <button type="button" onClick={onCancel}>Отмена</button>
          <button className="primary" type="submit" disabled={busy}>Сгенерировать</button>
        </div>
      </form>
    </div>
  )
}

function clamp(v: number, min: number, max: number) {
  return Number.isNaN(v) ? min : Math.min(max, Math.max(min, v))
}
