import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type {
  ActivateAbilityResult, CampaignMember, GameParticipant, GameSession, HeroicAbility,
  InitiativeSlotType, NpcListItem, RollLogEntry,
} from '../api/types'
import { PARTICIPANT_TYPE_LABELS, SLOT_TYPE_LABELS } from '../utils/labels'
import { DiceRoller, RollSymbolsView } from './DiceRoller'
import type { RollSymbols } from '../utils/diceRoller'

interface Props {
  campaignId: string
  isGm: boolean
  members: CampaignMember[]
  /** Счётчик realtime-инвалидаций: при изменении сцена перечитывается (другой участник внёс правку). */
  refreshSignal?: number
}

export function GameTableTab({ campaignId, isGm, members, refreshSignal }: Props) {
  const [session, setSession] = useState<GameSession | null>(null)
  const [loaded, setLoaded] = useState(false)
  const [error, setError] = useState<string | null>(null)
  // Способности с автоматизируемыми эффектами (U-18) — для кнопки «Активировать» у участника.
  const [abilities, setAbilities] = useState<HeroicAbility[]>([])

  useEffect(() => {
    let cancelled = false
    api.reference('realmsOfTerrinoth')
      .then(r => { if (!cancelled) setAbilities(r.heroicAbilities.filter(h => h.effects.length > 0)) })
      .catch(() => { /* без справочника список активируемых способностей будет пуст */ })
    return () => { cancelled = true }
  }, [])

  const activate = useCallback(async (participantId: string, code: string): Promise<ActivateAbilityResult | null> => {
    try {
      const r = await api.activateAbility(campaignId, participantId, code)
      setSession(r.session); setError(null)
      return r
    } catch (e) { setError(e instanceof Error ? e.message : 'Ошибка'); return null }
  }, [campaignId])

  const reload = useCallback(() =>
    api.session(campaignId)
      .then(s => { setSession(s); setLoaded(true) })
      .catch((e: unknown) => setError(e instanceof Error ? e.message : 'Ошибка загрузки')),
    [campaignId])

  useEffect(() => { void reload() }, [reload])
  // Перечитываем сцену по realtime-событию (правка другого участника).
  useEffect(() => { if (refreshSignal) void reload() }, [refreshSignal, reload])

  const run = useCallback(async (action: () => Promise<unknown>) => {
    try {
      const result = await action()
      if (result && typeof result === 'object' && 'participants' in result) setSession(result as GameSession)
      else await reload()
      setError(null)
    } catch (e) { setError(e instanceof Error ? e.message : 'Ошибка') }
  }, [reload])

  if (!loaded) return <p className="muted">Загрузка сцены…</p>

  if (!session) {
    return (
      <div className="game-table">
        {error && <div className="error">{error}</div>}
        {isGm
          ? <CreateSessionForm onCreate={(body) => run(() => api.createSession(campaignId, body))} />
          : <p className="muted">Мастер ещё не запустил сцену.</p>}
      </div>
    )
  }

  const currentSlot = session.slots[session.currentTurnIndex]

  return (
    <div className="game-table">
      {error && <div className="error floating">{error}</div>}

      <div className="gt-header panel">
        <div>
          <h3 className="inline-title">{session.name}</h3>
          {session.description && <span className="muted"> — {session.description}</span>}
          <div className="gt-round">
            Раунд <strong>{session.currentRound}</strong>
            {session.slots.length > 0 && currentSlot && (
              <> · Ход: <span className={`badge slot-${currentSlot.slotType}`}>{SLOT_TYPE_LABELS[currentSlot.slotType]}</span></>
            )}
          </div>
        </div>
        {isGm && (
          <div className="head-actions">
            <button className="primary" onClick={() => run(() => api.nextTurn(campaignId))}>Следующий ход →</button>
            <button onClick={() => { if (confirm('Сбросить сцену (убрать участников и слоты)?')) void run(() => api.resetSession(campaignId)) }}>Сбросить</button>
            <button className="danger" onClick={() => { if (confirm('Завершить сцену?')) void run(() => api.endSession(campaignId)) }}>Завершить</button>
          </div>
        )}
      </div>

      <StoryPoints session={session} isGm={isGm} onRun={run} campaignId={campaignId} />

      <InitiativeTracker session={session} isGm={isGm} onRun={run} campaignId={campaignId} />

      <ParticipantsBlock session={session} isGm={isGm} members={members} onRun={run} campaignId={campaignId}
        abilities={abilities} onActivate={activate} />

      <RollSection campaignId={campaignId} isGm={isGm} refreshSignal={refreshSignal} />

      <NotesBlock session={session} isGm={isGm} onRun={run} campaignId={campaignId} />
    </div>
  )
}

function CreateSessionForm({ onCreate }: { onCreate: (b: { name: string; description: string; playerStoryPoints: number; gmStoryPoints: number }) => void }) {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [psp, setPsp] = useState(1)
  const [gsp, setGsp] = useState(1)
  return (
    <form className="panel custom-form" onSubmit={(e: FormEvent) => { e.preventDefault(); onCreate({ name, description, playerStoryPoints: psp, gmStoryPoints: gsp }) }}>
      <h3>Создать сцену</h3>
      <label>Название<input value={name} onChange={e => setName(e.target.value)} required /></label>
      <label>Описание<textarea value={description} onChange={e => setDescription(e.target.value)} rows={2} /></label>
      <div className="form-row">
        <label className="char-input">Story points игроков<input type="number" min={0} value={psp} onChange={e => setPsp(Math.max(0, +e.target.value))} /></label>
        <label className="char-input">Story points мастера<input type="number" min={0} value={gsp} onChange={e => setGsp(Math.max(0, +e.target.value))} /></label>
      </div>
      <button className="primary" type="submit" disabled={!name.trim()}>Запустить сцену</button>
    </form>
  )
}

function StoryPoints({ session, isGm, onRun, campaignId }: BlockProps) {
  const set = (patch: { playerStoryPoints?: number; gmStoryPoints?: number }) => onRun(() => api.updateSession(campaignId, patch))
  return (
    <section className="panel gt-story">
      <div className="story-counter">
        <span className="story-label">Story Points — Игроки</span>
        <div className="story-val">
          {isGm && <button className="small" onClick={() => set({ playerStoryPoints: session.playerStoryPoints - 1 })}>−</button>}
          <strong>{session.playerStoryPoints}</strong>
          {isGm && <button className="small" onClick={() => set({ playerStoryPoints: session.playerStoryPoints + 1 })}>+</button>}
        </div>
      </div>
      {isGm && (
        <div className="story-flip">
          <button className="small" title="Перевернуть игрокам → мастеру"
            onClick={() => set({ playerStoryPoints: session.playerStoryPoints - 1, gmStoryPoints: session.gmStoryPoints + 1 })}
            disabled={session.playerStoryPoints <= 0}>→ GM</button>
          <button className="small" title="Перевернуть мастеру → игрокам"
            onClick={() => set({ gmStoryPoints: session.gmStoryPoints - 1, playerStoryPoints: session.playerStoryPoints + 1 })}
            disabled={session.gmStoryPoints <= 0}>← Игрокам</button>
        </div>
      )}
      <div className="story-counter">
        <span className="story-label">Story Points — Мастер</span>
        <div className="story-val">
          {isGm && <button className="small" onClick={() => set({ gmStoryPoints: session.gmStoryPoints - 1 })}>−</button>}
          <strong>{session.gmStoryPoints}</strong>
          {isGm && <button className="small" onClick={() => set({ gmStoryPoints: session.gmStoryPoints + 1 })}>+</button>}
        </div>
      </div>
    </section>
  )
}

function InitiativeTracker({ session, isGm, onRun, campaignId }: BlockProps) {
  const [slotType, setSlotType] = useState<InitiativeSlotType>('player')
  const nameOf = (pid: string | null) => session.participants.find(p => p.id === pid)?.displayName

  return (
    <section className="panel">
      <h3>Инициатива</h3>
      {session.slots.length === 0 && <p className="muted">Слотов нет.{isGm && ' Добавьте слоты ниже.'}</p>}
      <ol className="init-list">
        {session.slots.map((slot, i) => (
          <li key={slot.id} className={i === session.currentTurnIndex ? 'init-slot current' : 'init-slot'}>
            <span className={`badge slot-${slot.slotType}`}>{SLOT_TYPE_LABELS[slot.slotType]}</span>
            {isGm ? (
              <select className="slot-assign" value={slot.assignedParticipantId ?? ''}
                onChange={e => onRun(() => api.updateSlot(campaignId, slot.id, { assignedParticipantId: e.target.value || '00000000-0000-0000-0000-000000000000' }))}>
                <option value="">— абстрактный —</option>
                {session.participants.map(p => <option key={p.id} value={p.id}>{p.displayName}</option>)}
              </select>
            ) : (
              <span>{nameOf(slot.assignedParticipantId) ?? '— абстрактный —'}</span>
            )}
            {isGm && <button className="danger small" onClick={() => onRun(() => api.removeSlot(campaignId, slot.id))}>×</button>}
          </li>
        ))}
      </ol>
      {isGm && (
        <div className="form-row">
          <select value={slotType} onChange={e => setSlotType(e.target.value as InitiativeSlotType)}>
            {(['player', 'npc', 'neutral'] as InitiativeSlotType[]).map(t => <option key={t} value={t}>{SLOT_TYPE_LABELS[t]}</option>)}
          </select>
          <button className="small" onClick={() => onRun(() => api.addSlot(campaignId, { slotType }))}>+ Слот</button>
        </div>
      )}
    </section>
  )
}

function ParticipantsBlock({ session, isGm, members, onRun, campaignId, abilities, onActivate }:
  BlockProps & { members: CampaignMember[]; abilities: HeroicAbility[]
    onActivate: (participantId: string, code: string) => Promise<ActivateAbilityResult | null> }) {
  return (
    <section className="panel">
      <h3>Участники ({session.participants.length})</h3>
      {session.participants.length === 0 && <p className="muted">Участников пока нет.</p>}
      <div className="participant-grid">
        {session.participants.map(p => (
          <ParticipantCard key={p.id} p={p} isGm={isGm} allowPlayerEdits={session.allowPlayerEdits}
            onRun={onRun} campaignId={campaignId} abilities={abilities} onActivate={onActivate} />
        ))}
      </div>
      {isGm && <AddParticipant members={members} onRun={onRun} campaignId={campaignId} />}
    </section>
  )
}

function ParticipantCard({ p, isGm, allowPlayerEdits, onRun, campaignId, abilities, onActivate }: {
  p: GameParticipant; isGm: boolean; allowPlayerEdits: boolean
  onRun: (a: () => Promise<unknown>) => Promise<void>; campaignId: string
  abilities: HeroicAbility[]
  onActivate: (participantId: string, code: string) => Promise<ActivateAbilityResult | null>
}) {
  const patch = (body: Parameters<typeof api.updateParticipant>[2]) => onRun(() => api.updateParticipant(campaignId, p.id, body))
  // Игрок может крутить вайталы только своего персонажа, если мастер разрешил.
  const canEditVitals = isGm || (allowPlayerEdits && p.characterId != null)
  const [ability, setAbility] = useState('')
  const [outcome, setOutcome] = useState<ActivateAbilityResult | null>(null)

  async function activate() {
    const a = abilities.find(x => x.id === ability)
    if (!a) return
    const r = await onActivate(p.id, a.code)
    if (r) setOutcome(r)
  }

  return (
    <div className={`participant-card${p.isDefeated ? ' defeated' : ''}`}>
      <div className="pc-head">
        <strong>{p.displayName}</strong>
        <span className="badge">{PARTICIPANT_TYPE_LABELS[p.participantType]}</span>
        {p.isHiddenFromPlayers && <span className="badge tier">скрыт</span>}
      </div>
      <div className="pc-vitals">
        <Vital label="Раны" cur={p.woundsCurrent} max={p.woundsThreshold} editable={canEditVitals}
          onChange={v => patch({ woundsCurrent: v })} danger />
        {p.strainThreshold != null && (
          <Vital label="Стрейн" cur={p.strainCurrent} max={p.strainThreshold} editable={canEditVitals}
            onChange={v => patch({ strainCurrent: v })} />
        )}
      </div>
      <div className="pc-stats muted small-text">
        Soak {p.soak} · Бл.защ {p.meleeDefense} · Дал.защ {p.rangedDefense}{p.count > 1 ? ` · ×${p.count}` : ''}
      </div>
      <div className="pc-crits small-text">
        <span className={p.criticalInjuries > 0 ? 'crit-count warn' : 'crit-count muted'}>
          Криты: <b>{p.criticalInjuries}</b>
        </span>
        {canEditVitals && (
          <span className="crit-buttons">
            <button className="tiny" disabled={p.criticalInjuries === 0}
              onClick={() => patch({ criticalInjuries: Math.max(0, p.criticalInjuries - 1) })}>−</button>
            <button className="tiny" onClick={() => patch({ criticalInjuries: p.criticalInjuries + 1 })}>+</button>
          </span>
        )}
      </div>

      {canEditVitals && abilities.length > 0 && (
        <div className="pc-activate">
          <div className="form-row">
            <select className="grow" value={ability} onChange={e => setAbility(e.target.value)}>
              <option value="">— способность —</option>
              {abilities.map(a => <option key={a.id} value={a.id}>{a.nameRu || a.name}</option>)}
            </select>
            <button className="small" disabled={!ability} onClick={() => void activate()}>Активировать</button>
          </div>
          {outcome && (
            <div className="pc-activate-result small-text">
              <strong>{outcome.abilityName}.</strong>
              {outcome.applied.map((a, i) => <span key={`a${i}`}> {a}.</span>)}
              {outcome.manual.map((m, i) => <span key={`m${i}`} className="muted"> {m}</span>)}
            </div>
          )}
        </div>
      )}
      {isGm && (
        <>
          {p.notes && <div className="pc-notes small-text">{p.notes}</div>}
          <div className="card-actions">
            <button className="small" onClick={() => patch({ isDefeated: !p.isDefeated })}>{p.isDefeated ? 'Вернуть' : 'Повержен'}</button>
            <button className="small" onClick={() => patch({ isHiddenFromPlayers: !p.isHiddenFromPlayers })}>{p.isHiddenFromPlayers ? 'Показать' : 'Скрыть'}</button>
            <button className="danger small" onClick={() => { if (confirm(`Убрать «${p.displayName}»?`)) void onRun(() => api.removeParticipant(campaignId, p.id)) }}>Убрать</button>
          </div>
        </>
      )}
    </div>
  )
}

function Vital({ label, cur, max, editable, onChange, danger }: {
  label: string; cur: number; max: number; editable: boolean; onChange: (v: number) => void; danger?: boolean
}) {
  const over = cur >= max
  return (
    <div className={`vital${danger && over ? ' vital-over' : ''}`}>
      <span className="vital-label">{label}</span>
      <span className="vital-nums">
        {editable && <button className="tiny" onClick={() => onChange(Math.max(0, cur - 1))}>−</button>}
        <b>{cur}</b><span className="muted">/{max}</span>
        {editable && <button className="tiny" onClick={() => onChange(cur + 1)}>+</button>}
      </span>
    </div>
  )
}

function AddParticipant({ members, onRun, campaignId }: {
  members: CampaignMember[]; onRun: (a: () => Promise<unknown>) => Promise<void>; campaignId: string
}) {
  const [mode, setMode] = useState<'character' | 'npc' | 'manual'>('character')
  const [npcs, setNpcs] = useState<NpcListItem[]>([])
  const [characterId, setCharacterId] = useState('')
  const [npcId, setNpcId] = useState('')
  const [count, setCount] = useState(1)
  const [manualName, setManualName] = useState('')
  const [manualWt, setManualWt] = useState(10)

  useEffect(() => { api.npcs().then(setNpcs).catch(() => { /* список NPC не критичен */ }) }, [])

  function add() {
    if (mode === 'character' && characterId) void onRun(() => api.addParticipant(campaignId, { characterId }))
    else if (mode === 'npc' && npcId) void onRun(() => api.addParticipant(campaignId, { npcId, count, participantType: count > 1 ? 'minionGroup' : 'npc' }))
    else if (mode === 'manual' && manualName.trim()) void onRun(() => api.addParticipant(campaignId, { displayName: manualName.trim(), participantType: 'hazard', woundsThreshold: manualWt }))
  }

  return (
    <div className="add-participant">
      <div className="label-line">Добавить участника</div>
      <div className="system-switch">
        {(['character', 'npc', 'manual'] as const).map(m => (
          <button key={m} type="button" className={mode === m ? 'tab active' : 'tab'} onClick={() => setMode(m)}>
            {m === 'character' ? 'Персонаж' : m === 'npc' ? 'NPC' : 'Вручную'}
          </button>
        ))}
      </div>
      <div className="form-row">
        {mode === 'character' && (
          <select className="grow" value={characterId} onChange={e => setCharacterId(e.target.value)}>
            <option value="">— выберите персонажа —</option>
            {members.map(m => <option key={m.characterId} value={m.characterId}>{m.characterName}</option>)}
          </select>
        )}
        {mode === 'npc' && (
          <>
            <select className="grow" value={npcId} onChange={e => setNpcId(e.target.value)}>
              <option value="">— выберите NPC —</option>
              {npcs.map(n => <option key={n.id} value={n.id}>{n.name}</option>)}
            </select>
            <input className="ranks-input" type="number" min={1} value={count} onChange={e => setCount(Math.max(1, +e.target.value))} title="Количество (группа миньонов)" />
          </>
        )}
        {mode === 'manual' && (
          <>
            <input className="grow" placeholder="Название" value={manualName} onChange={e => setManualName(e.target.value)} />
            <input className="ranks-input" type="number" min={1} value={manualWt} onChange={e => setManualWt(Math.max(1, +e.target.value))} title="Порог ран" />
          </>
        )}
        <button className="primary small" onClick={add}>Добавить</button>
      </div>
    </div>
  )
}

function NotesBlock({ session, isGm, onRun, campaignId }: BlockProps) {
  const [pub, setPub] = useState(session.publicNotes)
  const [gm, setGm] = useState(session.gmNotes ?? '')
  return (
    <section className="panel">
      <h3>Заметки сцены</h3>
      <label>Публичные (видят игроки)
        <textarea rows={2} value={pub} disabled={!isGm} onChange={e => setPub(e.target.value)}
          onBlur={() => isGm && pub !== session.publicNotes && onRun(() => api.updateSession(campaignId, { publicNotes: pub }))} />
      </label>
      {isGm && (
        <label>Приватные (только мастер)
          <textarea rows={2} value={gm} onChange={e => setGm(e.target.value)}
            onBlur={() => gm !== (session.gmNotes ?? '') && onRun(() => api.updateSession(campaignId, { gmNotes: gm }))} />
        </label>
      )}
      {isGm && (
        <label className="checkbox">
          <input type="checkbox" checked={session.allowPlayerEdits}
            onChange={e => onRun(() => api.updateSession(campaignId, { allowPlayerEdits: e.target.checked }))} />
          Разрешить игрокам менять раны/стрейн своих персонажей
        </label>
      )}
    </section>
  )
}

function RollSection({ campaignId, isGm, refreshSignal }: { campaignId: string; isGm: boolean; refreshSignal?: number }) {
  const [rolls, setRolls] = useState<RollLogEntry[]>([])
  const [error, setError] = useState<string | null>(null)

  const reload = useCallback(() =>
    api.rolls(campaignId).then(setRolls).catch(() => { /* лог не критичен */ }),
    [campaignId])

  useEffect(() => { void reload() }, [reload])
  // Перечитываем лог по realtime-событию (чужой бросок).
  useEffect(() => { if (refreshSignal) void reload() }, [refreshSignal, reload])

  async function log(req: { poolJson: string; resultJson: string; summary: string; label: string; isSecret: boolean }) {
    try {
      await api.createRoll(campaignId, req)
      setError(null)
      await reload()
    } catch (e) { setError(e instanceof Error ? e.message : 'Ошибка броска') }
  }

  return (
    <section className="panel gt-rolls">
      <h3>Броски кубов</h3>
      {error && <div className="error">{error}</div>}
      <DiceRoller onLog={log} canSecret={isGm} />

      <div className="roll-log">
        {rolls.length === 0 && <p className="muted">Бросков пока нет.</p>}
        {rolls.map(r => (
          <div key={r.id} className="roll-entry">
            <span className="roll-actor"><strong>{r.actorName}</strong>{r.label && <span className="muted"> · {r.label}</span>}</span>
            <RollSymbolsView symbols={parseSymbols(r.resultJson)} />
            {r.isSecret && <span className="badge tier" title="Виден только мастеру">секретно</span>}
          </div>
        ))}
      </div>
    </section>
  )
}

function parseSymbols(json: string): RollSymbols {
  const empty: RollSymbols = { success: 0, failure: 0, advantage: 0, threat: 0, triumph: 0, despair: 0 }
  try {
    return { ...empty, ...(JSON.parse(json) as Partial<RollSymbols>) }
  } catch {
    return empty
  }
}

interface BlockProps {
  session: GameSession
  isGm: boolean
  onRun: (a: () => Promise<unknown>) => Promise<void>
  campaignId: string
}
