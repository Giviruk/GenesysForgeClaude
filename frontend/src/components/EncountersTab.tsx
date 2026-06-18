import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type {
  CampaignMember, EncounterDetail, EncounterInput, EncounterListItem, EncounterParticipant,
  EncounterType, GameSystem, NpcListItem, ParticipantType, SendToTableMode, ThreatLevel,
} from '../api/types'
import {
  ENCOUNTER_TYPE_LABELS, ENCOUNTER_TYPES, PARTICIPANT_TYPE_LABELS, SLOT_TYPE_LABELS,
  SYSTEM_LABELS, THREAT_LEVEL_LABELS, THREAT_LEVELS,
} from '../utils/labels'

interface Props {
  campaignId: string
  isGm: boolean
  members: CampaignMember[]
  onSentToTable?: () => void
}

const blankInput = (system: GameSystem): EncounterInput => ({
  name: '', system, type: 'combat', threatLevel: 'standard',
  gmDescription: '', playerDescription: '', playerGoals: '', npcGoals: '',
  location: '', environment: '', complications: '', rewards: '',
  isVisibleToPlayers: false, tags: [],
})

export function EncountersTab({ campaignId, isGm, members, onSentToTable }: Props) {
  const [list, setList] = useState<EncounterListItem[] | null>(null)
  const [openId, setOpenId] = useState<string | null>(null)
  const [creating, setCreating] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const reload = useCallback(() =>
    api.encounters(campaignId)
      .then(setList)
      .catch((e: unknown) => setError(e instanceof Error ? e.message : 'Ошибка загрузки')),
    [campaignId])

  useEffect(() => { void reload() }, [reload])

  if (openId) {
    return <EncounterEditor encounterId={openId} isGm={isGm} members={members}
      onBack={() => { setOpenId(null); void reload() }} onSentToTable={onSentToTable} />
  }

  return (
    <div className="encounters">
      {error && <div className="error">{error}</div>}
      <div className="page-head">
        <h3 className="inline-title">Энкаунтеры</h3>
        {isGm && <button className="primary" onClick={() => setCreating(v => !v)}>{creating ? 'Отмена' : '+ Создать энкаунтер'}</button>}
      </div>

      {creating && isGm && (
        <CreateEncounterForm onCreate={async (input) => {
          try {
            const e = await api.createEncounter(campaignId, input)
            setCreating(false)
            await reload()
            setOpenId(e.id)
          } catch (err) { setError(err instanceof Error ? err.message : 'Ошибка') }
        }} />
      )}

      {list === null && <p className="muted">Загрузка…</p>}
      {list?.length === 0 && <p className="muted">Энкаунтеров пока нет.{isGm && ' Создайте первую сцену.'}</p>}
      <div className="card-grid">
        {list?.map(e => (
          <div key={e.id} className="char-card" onClick={() => setOpenId(e.id)}>
            <div className="char-card-head">
              <strong>{e.name}</strong>
              <span className="badge">{ENCOUNTER_TYPE_LABELS[e.type]}</span>
            </div>
            <div className="muted small-text">
              {THREAT_LEVEL_LABELS[e.threatLevel]} · участников: {e.participantCount}
              {' · '}{e.isVisibleToPlayers ? 'виден игрокам' : 'скрыт'}
            </div>
            {e.tags.length > 0 && <div className="tag-row">{e.tags.map(t => <span key={t} className="badge custom">{t}</span>)}</div>}
          </div>
        ))}
      </div>
    </div>
  )
}

function CreateEncounterForm({ onCreate }: { onCreate: (input: EncounterInput) => void }) {
  const [name, setName] = useState('')
  const [system, setSystem] = useState<GameSystem>('realmsOfTerrinoth')
  const [type, setType] = useState<EncounterType>('combat')
  const [threatLevel, setThreatLevel] = useState<ThreatLevel>('standard')

  return (
    <form className="panel custom-form" onSubmit={(e: FormEvent) => {
      e.preventDefault()
      onCreate({ ...blankInput(system), name: name.trim(), type, threatLevel })
    }}>
      <label>Название<input value={name} onChange={e => setName(e.target.value)} required /></label>
      <div className="form-row">
        <label className="grow">Система
          <select value={system} onChange={e => setSystem(e.target.value as GameSystem)}>
            {(['realmsOfTerrinoth', 'genesysCore'] as GameSystem[]).map(s => <option key={s} value={s}>{SYSTEM_LABELS[s]}</option>)}
          </select>
        </label>
        <label className="grow">Тип
          <select value={type} onChange={e => setType(e.target.value as EncounterType)}>
            {ENCOUNTER_TYPES.map(t => <option key={t} value={t}>{ENCOUNTER_TYPE_LABELS[t]}</option>)}
          </select>
        </label>
        <label className="grow">Сложность
          <select value={threatLevel} onChange={e => setThreatLevel(e.target.value as ThreatLevel)}>
            {THREAT_LEVELS.map(t => <option key={t} value={t}>{THREAT_LEVEL_LABELS[t]}</option>)}
          </select>
        </label>
      </div>
      <button className="primary" type="submit" disabled={!name.trim()}>Создать и открыть</button>
    </form>
  )
}

function EncounterEditor({ encounterId, isGm, members, onBack, onSentToTable }: {
  encounterId: string; isGm: boolean; members: CampaignMember[]; onBack: () => void; onSentToTable?: () => void
}) {
  const [enc, setEnc] = useState<EncounterDetail | null>(null)
  const [error, setError] = useState<string | null>(null)

  const reload = useCallback(() =>
    api.encounter(encounterId).then(setEnc).catch((e: unknown) =>
      setError(e instanceof Error ? e.message : 'Ошибка загрузки')),
    [encounterId])
  useEffect(() => { void reload() }, [reload])

  const run = useCallback(async (action: () => Promise<unknown>) => {
    try {
      const r = await action()
      if (r && typeof r === 'object' && 'participants' in r && 'threatLevel' in r) setEnc(r as EncounterDetail)
      else await reload()
      setError(null)
    } catch (e) { setError(e instanceof Error ? e.message : 'Ошибка') }
  }, [reload])

  if (!enc) {
    return <div><button onClick={onBack}>← К списку</button>{error && <div className="error">{error}</div>}</div>
  }

  return (
    <div className="encounter-editor">
      <div className="page-head">
        <div>
          <button onClick={onBack}>← К списку</button>
          <h3 className="inline-title">{enc.name}</h3>
          <span className="badge">{ENCOUNTER_TYPE_LABELS[enc.type]}</span>
          <span className="badge tier">{THREAT_LEVEL_LABELS[enc.threatLevel]}</span>
          {!enc.isVisibleToPlayers && <span className="badge">скрыт</span>}
        </div>
      </div>
      {error && <div className="error floating">{error}</div>}

      {isGm
        ? <EncounterMeta key={enc.updatedAt} enc={enc} onRun={run} />
        : <PlayerEncounterView enc={enc} />}

      <ParticipantsSection enc={enc} isGm={isGm} members={members} onRun={run} />

      {isGm && <SendToTableSection enc={enc} onRun={run} onSentToTable={onSentToTable} />}
    </div>
  )
}

function EncounterMeta({ enc, onRun }: { enc: EncounterDetail; onRun: (a: () => Promise<unknown>) => Promise<void> }) {
  // Локальная копия формы; компонент перемонтируется по key=updatedAt после сохранения,
  // поэтому начального состояния из props достаточно (без синхронизации через эффект).
  const [f, setF] = useState<EncounterInput>(toInput(enc))
  const [tagsText, setTagsText] = useState(enc.tags.join(', '))
  const set = <K extends keyof EncounterInput>(k: K, v: EncounterInput[K]) => setF(prev => ({ ...prev, [k]: v }))

  const save = () => onRun(() => api.updateEncounter(enc.id, {
    ...f,
    tags: tagsText.split(',').map(t => t.trim()).filter(Boolean),
  }))

  return (
    <section className="panel custom-form">
      <h4>Основная информация</h4>
      <label>Название<input value={f.name} onChange={e => set('name', e.target.value)} /></label>
      <div className="form-row">
        <label className="grow">Тип
          <select value={f.type} onChange={e => set('type', e.target.value as EncounterType)}>
            {ENCOUNTER_TYPES.map(t => <option key={t} value={t}>{ENCOUNTER_TYPE_LABELS[t]}</option>)}
          </select>
        </label>
        <label className="grow">Сложность
          <select value={f.threatLevel} onChange={e => set('threatLevel', e.target.value as ThreatLevel)}>
            {THREAT_LEVELS.map(t => <option key={t} value={t}>{THREAT_LEVEL_LABELS[t]}</option>)}
          </select>
        </label>
      </div>
      <div className="form-row">
        <label className="grow">Локация<input value={f.location} onChange={e => set('location', e.target.value)} /></label>
        <label className="grow">Окружение<input value={f.environment} onChange={e => set('environment', e.target.value)} /></label>
      </div>

      <h4>Описание для мастера</h4>
      <textarea rows={2} value={f.gmDescription} onChange={e => set('gmDescription', e.target.value)} />
      <h4>Описание для игроков</h4>
      <textarea rows={2} value={f.playerDescription} onChange={e => set('playerDescription', e.target.value)} />

      <div className="form-row">
        <label className="grow">Цели игроков<textarea rows={2} value={f.playerGoals} onChange={e => set('playerGoals', e.target.value)} /></label>
        <label className="grow">Цели NPC (приватно)<textarea rows={2} value={f.npcGoals} onChange={e => set('npcGoals', e.target.value)} /></label>
      </div>
      <div className="form-row">
        <label className="grow">Осложнения (приватно)<textarea rows={2} value={f.complications} onChange={e => set('complications', e.target.value)} /></label>
        <label className="grow">Награды<textarea rows={2} value={f.rewards} onChange={e => set('rewards', e.target.value)} /></label>
      </div>

      <label>Теги (через запятую)<input value={tagsText} onChange={e => setTagsText(e.target.value)} /></label>
      <label className="checkbox">
        <input type="checkbox" checked={f.isVisibleToPlayers} onChange={e => set('isVisibleToPlayers', e.target.checked)} />
        Видно игрокам (открыть публичную часть)
      </label>

      <button className="primary" onClick={save}>Сохранить</button>
    </section>
  )
}

function PlayerEncounterView({ enc }: { enc: EncounterDetail }) {
  return (
    <section className="panel">
      {enc.playerDescription && <p>{enc.playerDescription}</p>}
      {enc.location && <div className="muted">Локация: {enc.location}</div>}
      {enc.playerGoals && <><h4>Цели</h4><p className="note-body">{enc.playerGoals}</p></>}
      {enc.rewards && <><h4>Награды</h4><p className="note-body">{enc.rewards}</p></>}
    </section>
  )
}

function ParticipantsSection({ enc, isGm, members, onRun }: {
  enc: EncounterDetail; isGm: boolean; members: CampaignMember[]; onRun: (a: () => Promise<unknown>) => Promise<void>
}) {
  return (
    <section className="panel">
      <h4>Участники ({enc.participants.length})</h4>
      {enc.participants.length === 0 && <p className="muted">Участников пока нет.</p>}
      <div className="participant-grid">
        {enc.participants.map(p => (
          <EncounterParticipantCard key={p.id} enc={enc} p={p} isGm={isGm} onRun={onRun} />
        ))}
      </div>
      {isGm && <AddEncounterParticipant enc={enc} members={members} onRun={onRun} />}
    </section>
  )
}

function EncounterParticipantCard({ enc, p, isGm, onRun }: {
  enc: EncounterDetail; p: EncounterParticipant; isGm: boolean; onRun: (a: () => Promise<unknown>) => Promise<void>
}) {
  const patch = (body: Parameters<typeof api.updateEncounterParticipant>[2]) =>
    onRun(() => api.updateEncounterParticipant(enc.id, p.id, body))
  return (
    <div className={`participant-card${p.startsDefeated ? ' defeated' : ''}`}>
      <div className="pc-head">
        <strong>{p.displayName}{p.quantity > 1 ? ` ×${p.quantity}` : ''}</strong>
        <span className="badge">{PARTICIPANT_TYPE_LABELS[p.participantType]}</span>
        {p.startsHidden && <span className="badge tier">скрыт</span>}
      </div>
      <div className="muted small-text">Сторона: {SLOT_TYPE_LABELS[p.initiativeSide]}</div>
      {isGm && p.notes && <div className="pc-notes small-text">{p.notes}</div>}
      {isGm && (
        <div className="card-actions">
          <button className="small" onClick={() => patch({ startsHidden: !p.startsHidden })}>{p.startsHidden ? 'Показать' : 'Скрыть в начале'}</button>
          <button className="small" onClick={() => patch({ startsDefeated: !p.startsDefeated })}>{p.startsDefeated ? 'Активен' : 'Повержен в начале'}</button>
          <button className="danger small" onClick={() => { if (confirm(`Убрать «${p.displayName}»?`)) void onRun(() => api.removeEncounterParticipant(enc.id, p.id)) }}>Убрать</button>
        </div>
      )}
    </div>
  )
}

function AddEncounterParticipant({ enc, members, onRun }: {
  enc: EncounterDetail; members: CampaignMember[]; onRun: (a: () => Promise<unknown>) => Promise<void>
}) {
  const [mode, setMode] = useState<'npc' | 'character' | 'manual'>('npc')
  const [npcs, setNpcs] = useState<NpcListItem[]>([])
  const [npcId, setNpcId] = useState('')
  const [quantity, setQuantity] = useState(1)
  const [characterId, setCharacterId] = useState('')
  const [manualName, setManualName] = useState('')
  const [manualType, setManualType] = useState<ParticipantType>('hazard')

  useEffect(() => { api.npcs().then(setNpcs).catch(() => { /* список NPC не критичен */ }) }, [])

  const add = () => {
    if (mode === 'npc' && npcId) void onRun(() => api.addEncounterParticipant(enc.id, { npcId, quantity }))
    else if (mode === 'character' && characterId) void onRun(() => api.addEncounterParticipant(enc.id, { characterId }))
    else if (mode === 'manual' && manualName.trim())
      void onRun(() => api.addEncounterParticipant(enc.id, { displayName: manualName.trim(), participantType: manualType }))
  }

  return (
    <div className="add-participant">
      <div className="label-line">Добавить участника</div>
      <div className="system-switch">
        {(['npc', 'character', 'manual'] as const).map(m => (
          <button key={m} type="button" className={mode === m ? 'tab active' : 'tab'} onClick={() => setMode(m)}>
            {m === 'npc' ? 'NPC' : m === 'character' ? 'Персонаж' : 'Вручную'}
          </button>
        ))}
      </div>
      <div className="form-row">
        {mode === 'npc' && (
          <>
            <select className="grow" value={npcId} onChange={e => setNpcId(e.target.value)}>
              <option value="">— выберите NPC —</option>
              {npcs.map(n => <option key={n.id} value={n.id}>{n.name}</option>)}
            </select>
            <input className="ranks-input" type="number" min={1} value={quantity} onChange={e => setQuantity(Math.max(1, +e.target.value))} title="Количество (группа миньонов)" />
          </>
        )}
        {mode === 'character' && (
          <select className="grow" value={characterId} onChange={e => setCharacterId(e.target.value)}>
            <option value="">— выберите персонажа —</option>
            {members.map(m => <option key={m.characterId} value={m.characterId}>{m.characterName}</option>)}
          </select>
        )}
        {mode === 'manual' && (
          <>
            <input className="grow" placeholder="Название" value={manualName} onChange={e => setManualName(e.target.value)} />
            <select value={manualType} onChange={e => setManualType(e.target.value as ParticipantType)}>
              {(['hazard', 'npc'] as ParticipantType[]).map(t => <option key={t} value={t}>{PARTICIPANT_TYPE_LABELS[t]}</option>)}
            </select>
          </>
        )}
        <button className="primary small" onClick={add}>Добавить</button>
      </div>
      <div className="form-row">
        <button className="small" onClick={() => onRun(() => api.addEncounterCharacters(enc.id, null))}>+ Добавить всех PC</button>
      </div>
    </div>
  )
}

function SendToTableSection({ enc, onRun, onSentToTable }: {
  enc: EncounterDetail; onRun: (a: () => Promise<unknown>) => Promise<void>; onSentToTable?: () => void
}) {
  const send = async (mode: SendToTableMode) => {
    await onRun(async () => {
      await api.sendEncounterToTable(enc.id, mode)
      onSentToTable?.()
    })
  }
  return (
    <section className="panel">
      <h4>Отправить в Game Table</h4>
      <p className="hint">Если активной сцены нет — создаётся новая. Если есть — выберите режим.</p>
      <div className="form-row">
        <button className="primary" onClick={() => send('replace')}>Заменить активную сцену</button>
        <button onClick={() => send('append')}>Добавить в активную</button>
      </div>
    </section>
  )
}

function toInput(e: EncounterDetail): EncounterInput {
  return {
    name: e.name, system: e.system, type: e.type, threatLevel: e.threatLevel,
    gmDescription: e.gmDescription ?? '', playerDescription: e.playerDescription,
    playerGoals: e.playerGoals, npcGoals: e.npcGoals ?? '',
    location: e.location, environment: e.environment,
    complications: e.complications ?? '', rewards: e.rewards,
    isVisibleToPlayers: e.isVisibleToPlayers, tags: e.tags,
  }
}
