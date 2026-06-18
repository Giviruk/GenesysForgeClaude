import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type {
  Characteristic, GameSystem, NpcCombatStyle, NpcDetail, NpcInput, NpcKind, NpcListItem,
  NpcPowerLevel, NpcRole, NpcVisibility, QuickDraftRequest,
} from '../api/types'
import {
  CHARACTERISTICS, CHARACTERISTIC_LABELS, NPC_COMBAT_STYLE_LABELS, NPC_KIND_LABELS, NPC_KINDS,
  NPC_POWER_LABELS, NPC_ROLE_LABELS, NPC_ROLES, NPC_VISIBILITY_LABELS, SYSTEM_LABELS,
} from '../utils/labels'
import { PrintPreview } from '../components/print/PrintPreview'
import { AdversaryCard } from '../components/print/cards'
import { adversaryMarkdown } from '../components/print/markdown'

const SYSTEMS: GameSystem[] = ['genesysCore', 'realmsOfTerrinoth']

export function NpcsPage() {
  const [npcs, setNpcs] = useState<NpcListItem[] | null>(null)
  const [openId, setOpenId] = useState<string | null>(null)
  const [editing, setEditing] = useState<NpcDetail | 'new' | null>(null)
  const [drafting, setDrafting] = useState(false)
  const [error, setError] = useState<string | null>(null)

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

  useEffect(() => { void reload() }, [reload])

  if (openId) return <NpcDetailView npcId={openId} onBack={() => { setOpenId(null); void reload() }}
    onEdit={n => { setOpenId(null); setEditing(n) }} />

  async function run(action: () => Promise<unknown>) {
    try { await action(); await reload() }
    catch (e) { setError(e instanceof Error ? e.message : 'Ошибка') }
  }

  return (
    <div className="page">
      <div className="page-head">
        <h2>NPC / Бестиарий</h2>
        <div className="head-actions">
          <button onClick={() => setDrafting(true)}>⚡ Быстрый черновик</button>
          <button className="primary" onClick={() => setEditing('new')}>+ Создать NPC</button>
        </div>
      </div>
      {error && <div className="error">{error}</div>}

      <div className="npc-filters panel">
        <input placeholder="Поиск по имени…" value={search} onChange={e => setSearch(e.target.value)} />
        <select value={system} onChange={e => setSystem(e.target.value as GameSystem | '')}>
          <option value="">Все системы</option>
          {SYSTEMS.map(s => <option key={s} value={s}>{SYSTEM_LABELS[s]}</option>)}
        </select>
        <select value={kind} onChange={e => setKind(e.target.value as NpcKind | '')}>
          <option value="">Все типы</option>
          {NPC_KINDS.map(k => <option key={k} value={k}>{NPC_KIND_LABELS[k]}</option>)}
        </select>
        <select value={role} onChange={e => setRole(e.target.value as NpcRole | '')}>
          <option value="">Все роли</option>
          {NPC_ROLES.map(r => <option key={r} value={r}>{NPC_ROLE_LABELS[r]}</option>)}
        </select>
        <select value={sort} onChange={e => setSort(e.target.value as 'createdAt' | 'name')}>
          <option value="createdAt">Сначала новые</option>
          <option value="name">По имени</option>
        </select>
      </div>

      {npcs === null && <p className="muted">Загрузка…</p>}
      {npcs?.length === 0 && <p className="muted">Ничего не найдено — создайте NPC или быстрый черновик.</p>}
      <div className="card-grid">
        {npcs?.map(n => (
          <div key={n.id} className="char-card" onClick={() => setOpenId(n.id)}>
            <div className="char-card-head">
              <strong>{n.name}</strong>
              <span className={`badge ${n.system}`}>{SYSTEM_LABELS[n.system]}</span>
            </div>
            <div className="muted">{NPC_KIND_LABELS[n.kind]} · {NPC_ROLE_LABELS[n.role]}</div>
            <div className="npc-stats-line">
              Soak {n.soak} · Раны {n.woundThreshold}{n.strainThreshold != null ? ` · Стрейн ${n.strainThreshold}` : ''}
            </div>
            {n.skills.length > 0 && (
              <div className="muted small-text">
                {n.skills.slice(0, 4).map(s => `${s.name} ${s.ranks}`).join(', ')}
              </div>
            )}
            {n.tags.length > 0 && <div className="chips tiny">{n.tags.map(t => <span key={t} className="chip">{t}</span>)}</div>}
            {n.isMine && (
              <div className="card-actions">
                <button className="small" onClick={e => { e.stopPropagation(); void run(() => api.duplicateNpc(n.id)) }}>Дублировать</button>
                <button className="danger small" onClick={e => {
                  e.stopPropagation()
                  if (confirm(`Удалить NPC «${n.name}»?`)) void run(() => api.deleteNpc(n.id))
                }}>Удалить</button>
              </div>
            )}
            {!n.isMine && <span className="badge">из кампании</span>}
          </div>
        ))}
      </div>

      {editing && (
        <NpcEditor
          initial={editing === 'new' ? null : editing}
          onCancel={() => setEditing(null)}
          onSaved={() => { setEditing(null); void reload() }}
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

function NpcDetailView({ npcId, onBack, onEdit }: {
  npcId: string; onBack: () => void; onEdit: (n: NpcDetail) => void
}) {
  const [n, setN] = useState<NpcDetail | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [printing, setPrinting] = useState(false)

  useEffect(() => {
    api.npc(npcId).then(setN).catch((e: unknown) => setError(e instanceof Error ? e.message : 'Ошибка'))
  }, [npcId])

  if (!n) return <div className="page"><button onClick={onBack}>← Бестиарий</button>{error && <div className="error">{error}</div>}</div>

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

  return (
    <div className="page npc-detail">
      <div className="page-head no-print">
        <div>
          <button onClick={onBack}>← Бестиарий</button>
          <h2 className="inline-title">{n.name}</h2>
        </div>
        <div className="head-actions">
          <button onClick={() => setPrinting(true)}>🖨 Печать карточки</button>
          {n.isMine && <button className="primary" onClick={() => onEdit(n)}>Редактировать</button>}
        </div>
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

        {n.skills.length > 0 && (
          <div className="npc-section">
            <h4>Навыки</h4>
            <div>{n.skills.map(s => `${s.name} ${s.ranks}`).join(' · ')}</div>
          </div>
        )}
        {n.abilities.length > 0 && (
          <div className="npc-section">
            <h4>Способности</h4>
            <ul>{n.abilities.map((a, i) => <li key={i}><b>{a.name}.</b> {a.description}</li>)}</ul>
          </div>
        )}
        {n.talents.length > 0 && (
          <div className="npc-section"><h4>Таланты</h4><div>{n.talents.join(' · ')}</div></div>
        )}
        {n.equipment.length > 0 && (
          <div className="npc-section"><h4>Снаряжение</h4><div>{n.equipment.join(' · ')}</div></div>
        )}
        {n.tags.length > 0 && (
          <div className="npc-section"><h4>Теги</h4><div className="chips">{n.tags.map(t => <span key={t} className="chip">{t}</span>)}</div></div>
        )}
        {n.source && <div className="muted small-text npc-source">Источник: {n.source}</div>}
      </div>
    </div>
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
  initial: NpcDetail | null; onCancel: () => void; onSaved: () => void
}) {
  const [form, setForm] = useState<NpcInput>(initial ? toInput(initial) : EMPTY_INPUT)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const set = <K extends keyof NpcInput>(key: K, value: NpcInput[K]) => setForm(f => ({ ...f, [key]: value }))

  // Миньон обычно без стрейна; немезида обязан иметь.
  const minion = form.kind === 'minion'

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true); setError(null)
    const payload: NpcInput = { ...form, strainThreshold: minion ? null : form.strainThreshold }
    try {
      if (initial) await api.updateNpc(initial.id, payload)
      else await api.createNpc(payload)
      onSaved()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка сохранения')
      setBusy(false)
    }
  }

  return (
    <div className="modal-backdrop" onClick={onCancel}>
      <form className="modal wide" onClick={e => e.stopPropagation()} onSubmit={submit}>
        <h3>{initial ? 'Редактировать NPC' : 'Новый NPC'}</h3>

        <div className="form-row">
          <label className="grow">Имя<input value={form.name} onChange={e => set('name', e.target.value)} required /></label>
          <label>Система
            <select value={form.system} onChange={e => set('system', e.target.value as GameSystem)}>
              {SYSTEMS.map(s => <option key={s} value={s}>{SYSTEM_LABELS[s]}</option>)}
            </select>
          </label>
        </div>

        <div className="form-row">
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
        <div className="form-row chars-row">
          {CHARACTERISTICS.map(c => (
            <label key={c} className="char-input">{CHARACTERISTIC_LABELS[c]}
              <input type="number" min={1} max={6} value={form[c]}
                onChange={e => set(c, clamp(+e.target.value, 1, 6))} />
            </label>
          ))}
        </div>

        <div className="label-line">Производные параметры</div>
        <div className="form-row chars-row">
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

        <SkillsEditor skills={form.skills} onChange={s => set('skills', s)} />
        <AbilitiesEditor abilities={form.abilities} onChange={a => set('abilities', a)} />
        <StringListEditor label="Таланты" values={form.talents} onChange={v => set('talents', v)} placeholder="Название таланта" />
        <StringListEditor label="Снаряжение" values={form.equipment} onChange={v => set('equipment', v)} placeholder="Предмет / оружие" />
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

function SkillsEditor({ skills, onChange }: { skills: NpcInput['skills']; onChange: (s: NpcInput['skills']) => void }) {
  return (
    <div className="list-editor">
      <div className="label-line">Навыки</div>
      {skills.map((s, i) => (
        <div key={i} className="form-row">
          <input className="grow" placeholder="Навык" value={s.name}
            onChange={e => onChange(skills.map((x, j) => j === i ? { ...x, name: e.target.value } : x))} />
          <input type="number" min={0} max={5} className="ranks-input" value={s.ranks}
            onChange={e => onChange(skills.map((x, j) => j === i ? { ...x, ranks: clamp(+e.target.value, 0, 5) } : x))} />
          <button type="button" className="danger small" onClick={() => onChange(skills.filter((_, j) => j !== i))}>×</button>
        </div>
      ))}
      <button type="button" className="small" onClick={() => onChange([...skills, { name: '', ranks: 1 }])}>+ Навык</button>
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
