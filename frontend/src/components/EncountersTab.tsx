import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type {
  CampaignMember, EncounterDetail, EncounterInput, EncounterListItem, EncounterParticipant,
  EncounterType, GameSystem, NpcListItem, ParticipantType, SendToTableMode, ThreatLevel,
} from '../api/types'
import {
  ENCOUNTER_TYPE_LABELS, ENCOUNTER_TYPES, NPC_KIND_LABELS, NPC_ROLE_LABELS, PARTICIPANT_TYPE_LABELS,
  SLOT_TYPE_LABELS, SYSTEM_LABELS, THREAT_LEVEL_LABELS, THREAT_LEVELS,
} from '../utils/labels'
import { PrintPreview } from './print/PrintPreview'
import { EncounterSheet } from './print/cards'
import { encounterMarkdown } from './print/markdown'

interface Props {
  campaignId: string
  isGm: boolean
  members: CampaignMember[]
  /** Открытый энкаунтер из URL (/campaigns/:id/encounters/:eid), иначе список. */
  openEncounterId: string | null
  onOpenEncounter: (eid: string) => void
  onCloseEncounter: () => void
  onSentToTable?: () => void
}

const blankInput = (system: GameSystem): EncounterInput => ({
  name: '', system, type: 'combat', threatLevel: 'standard',
  gmDescription: '', playerDescription: '', playerGoals: '', npcGoals: '',
  location: '', environment: '', complications: '', rewards: '',
  isVisibleToPlayers: false, tags: [],
})

type VisibilityFilter = '' | 'visible' | 'hidden'

/**
 * Вкладка «Энкаунтеры» кампании по прототипу campaign-encounters: полоса состояния,
 * тулбар с поиском/фильтрами и master-detail «очередь сцен + рабочая область».
 */
export function EncountersTab({
  campaignId, isGm, members, openEncounterId, onOpenEncounter, onCloseEncounter, onSentToTable,
}: Props) {
  const [list, setList] = useState<EncounterListItem[] | null>(null)
  const [creating, setCreating] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Фильтры очереди — клиентские поверх загруженного списка.
  const [search, setSearch] = useState('')
  const [typeFilter, setTypeFilter] = useState<EncounterType | ''>('')
  const [threatFilter, setThreatFilter] = useState<ThreatLevel | ''>('')
  const [visibilityFilter, setVisibilityFilter] = useState<VisibilityFilter>('')

  const reload = useCallback(() =>
    api.encounters(campaignId)
      .then(setList)
      .catch((e: unknown) => setError(e instanceof Error ? e.message : 'Ошибка загрузки')),
    [campaignId])

  useEffect(() => { void reload() }, [reload])

  const filtered = useMemo(() => {
    if (!list) return null
    const q = search.trim().toLowerCase()
    return list.filter(e =>
      (!q || e.name.toLowerCase().includes(q) || e.tags.some(t => t.toLowerCase().includes(q)))
      && (!typeFilter || e.type === typeFilter)
      && (!threatFilter || e.threatLevel === threatFilter)
      && (visibilityFilter === '' || (visibilityFilter === 'visible') === e.isVisibleToPlayers))
  }, [list, search, typeFilter, threatFilter, visibilityFilter])

  const stats = useMemo(() => ({
    total: list?.length ?? 0,
    ready: list?.filter(e => e.participantCount > 0).length ?? 0,
    visible: list?.filter(e => e.isVisibleToPlayers).length ?? 0,
    participants: list?.reduce((sum, e) => sum + e.participantCount, 0) ?? 0,
  }), [list])

  return (
    <div className="encounters">
      {error && <div className="error">{error}</div>}

      <div className="enc-strip" aria-label="Состояние энкаунтеров">
        <div className="enc-strip-card"><b>{stats.total}</b><span>всего сцен</span></div>
        <div className="enc-strip-card"><b>{stats.ready}</b><span>с участниками</span></div>
        <div className="enc-strip-card"><b>{stats.visible}</b><span>видны игрокам</span></div>
        <div className="enc-strip-card"><b>{stats.participants}</b><span>участников в подготовке</span></div>
      </div>

      <div className="enc-toolbar">
        <input type="search" placeholder="Поиск по названию или тегу…" value={search}
          onChange={e => setSearch(e.target.value)} aria-label="Поиск энкаунтеров" />
        <select value={typeFilter} onChange={e => setTypeFilter(e.target.value as EncounterType | '')} aria-label="Тип">
          <option value="">Все типы</option>
          {ENCOUNTER_TYPES.map(t => <option key={t} value={t}>{ENCOUNTER_TYPE_LABELS[t]}</option>)}
        </select>
        <select value={threatFilter} onChange={e => setThreatFilter(e.target.value as ThreatLevel | '')} aria-label="Сложность">
          <option value="">Любая сложность</option>
          {THREAT_LEVELS.map(t => <option key={t} value={t}>{THREAT_LEVEL_LABELS[t]}</option>)}
        </select>
        <select value={visibilityFilter} onChange={e => setVisibilityFilter(e.target.value as VisibilityFilter)} aria-label="Видимость">
          <option value="">Все</option>
          <option value="hidden">Скрытые</option>
          <option value="visible">Видны игрокам</option>
        </select>
        {isGm && (
          <button className="primary" onClick={() => setCreating(v => !v)}>
            {creating ? 'Отмена' : '+ Энкаунтер'}
          </button>
        )}
      </div>

      {creating && isGm && (
        <CreateEncounterForm onCreate={async (input) => {
          try {
            const e = await api.createEncounter(campaignId, input)
            setCreating(false)
            await reload()
            onOpenEncounter(e.id)
          } catch (err) { setError(err instanceof Error ? err.message : 'Ошибка') }
        }} />
      )}

      <div className="enc-workbench">
        <aside className="panel enc-queue" aria-label="Очередь сцен">
          <h4>Очередь сцен {filtered ? `(${filtered.length})` : ''}</h4>
          {filtered === null && <p className="muted">Загрузка…</p>}
          {filtered?.length === 0 && (
            <p className="muted">
              {list?.length === 0 ? <>Энкаунтеров пока нет.{isGm && ' Создайте первую сцену.'}</> : 'Ничего не найдено по фильтрам.'}
            </p>
          )}
          <div className="enc-queue-list">
            {filtered?.map(e => (
              <button type="button" key={e.id}
                className={`enc-row${e.id === openEncounterId ? ' active' : ''}`}
                onClick={() => onOpenEncounter(e.id)}>
                <span className="enc-row-main">
                  <span className="enc-row-title">{e.name}</span>
                  <span className="enc-row-meta muted">
                    {ENCOUNTER_TYPE_LABELS[e.type]} · {THREAT_LEVEL_LABELS[e.threatLevel]} · участников: {e.participantCount}
                  </span>
                  {(e.tags.length > 0 || !e.isVisibleToPlayers) && (
                    <span className="tag-row">
                      {!e.isVisibleToPlayers && <span className="badge">скрыт</span>}
                      {e.tags.map(t => <span key={t} className="badge custom">{t}</span>)}
                    </span>
                  )}
                </span>
                <span className={`enc-status-dot${e.participantCount > 0 ? ' ready' : ''}`}
                  title={e.participantCount > 0 ? 'Участники добавлены' : 'Без участников'} />
              </button>
            ))}
          </div>
        </aside>

        <section className="enc-detail">
          {openEncounterId
            ? <EncounterEditor key={openEncounterId} encounterId={openEncounterId} isGm={isGm} members={members}
                onBack={() => { onCloseEncounter(); void reload() }}
                onChanged={() => void reload()}
                onSentToTable={onSentToTable} />
            : <div className="panel enc-empty">
                <p className="muted">← Выберите сцену из очереди или создайте новую.</p>
              </div>}
        </section>
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

function EncounterEditor({ encounterId, isGm, members, onBack, onChanged, onSentToTable }: {
  encounterId: string; isGm: boolean; members: CampaignMember[]
  onBack: () => void; onChanged: () => void; onSentToTable?: () => void
}) {
  const [enc, setEnc] = useState<EncounterDetail | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [printing, setPrinting] = useState(false)

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
      onChanged()
    } catch (e) { setError(e instanceof Error ? e.message : 'Ошибка') }
  }, [reload, onChanged])

  if (!enc) {
    return <div className="panel enc-empty"><button onClick={onBack}>← К списку</button>{error && <div className="error">{error}</div>}</div>
  }

  if (printing) {
    const versions: ('gm' | 'player')[] = isGm ? ['gm', 'player'] : ['player']
    return (
      <PrintPreview title={`Энкаунтер — ${enc.name}`} versions={versions}
        markdown={(v) => encounterMarkdown(enc, v)} onClose={() => setPrinting(false)}>
        {(v) => <EncounterSheet enc={enc} version={v} />}
      </PrintPreview>
    )
  }

  return (
    <div className="encounter-editor">
      <div className="panel enc-scene-head">
        <div className="enc-scene-title">
          <button className="small" onClick={onBack}>←</button>
          <h3 className="inline-title">{enc.name}</h3>
          <span className="badge">{ENCOUNTER_TYPE_LABELS[enc.type]}</span>
          <span className="badge tier">{THREAT_LEVEL_LABELS[enc.threatLevel]}</span>
          {!enc.isVisibleToPlayers && <span className="badge">скрыт</span>}
        </div>
        <div className="row-actions">
          <button className="small" onClick={() => setPrinting(true)}>🖨 Печать</button>
        </div>
      </div>
      {error && <div className="error floating">{error}</div>}

      {isGm ? (
        <>
          <EncounterMeta key={enc.updatedAt} enc={enc} onRun={run} />
          <div className="enc-detail-grid">
            <ParticipantsSection enc={enc} isGm={isGm} members={members} onRun={run} />
            <div className="enc-side-panels">
              <SendToTableSection enc={enc} onRun={run} onSentToTable={onSentToTable} />
              <QuickBuildPanel enc={enc} onRun={run} />
            </div>
          </div>
        </>
      ) : (
        <>
          <PlayerEncounterView enc={enc} />
          <ParticipantsSection enc={enc} isGm={isGm} members={members} onRun={run} />
        </>
      )}
    </div>
  )
}

/**
 * Содержание сцены по прототипу: база (тип/сложность/локация) + copy-grid
 * «Игрокам / Мастеру / Цели игроков / Цели NPC» + заметки (осложнения/награды).
 */
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
    <section className="panel custom-form enc-meta">
      <div className="panel-head-row">
        <h4>Содержание сцены</h4>
        <button className="primary small" onClick={save}>Сохранить</button>
      </div>
      <div className="form-row">
        <label className="grow">Название<input value={f.name} onChange={e => set('name', e.target.value)} /></label>
        <label>Тип
          <select value={f.type} onChange={e => set('type', e.target.value as EncounterType)}>
            {ENCOUNTER_TYPES.map(t => <option key={t} value={t}>{ENCOUNTER_TYPE_LABELS[t]}</option>)}
          </select>
        </label>
        <label>Сложность
          <select value={f.threatLevel} onChange={e => set('threatLevel', e.target.value as ThreatLevel)}>
            {THREAT_LEVELS.map(t => <option key={t} value={t}>{THREAT_LEVEL_LABELS[t]}</option>)}
          </select>
        </label>
      </div>
      <div className="form-row">
        <label className="grow">Локация<input value={f.location} onChange={e => set('location', e.target.value)} /></label>
        <label className="grow">Окружение<input value={f.environment} onChange={e => set('environment', e.target.value)} /></label>
      </div>

      <div className="copy-grid">
        <label className="copy-box">Игрокам
          <textarea rows={3} value={f.playerDescription} onChange={e => set('playerDescription', e.target.value)} />
        </label>
        <label className="copy-box private">Мастеру (приватно)
          <textarea rows={3} value={f.gmDescription} onChange={e => set('gmDescription', e.target.value)} />
        </label>
        <label className="copy-box">Цели игроков
          <textarea rows={2} value={f.playerGoals} onChange={e => set('playerGoals', e.target.value)} />
        </label>
        <label className="copy-box private">Цели NPC (приватно)
          <textarea rows={2} value={f.npcGoals} onChange={e => set('npcGoals', e.target.value)} />
        </label>
        <label className="copy-box private">Осложнения (приватно)
          <textarea rows={2} value={f.complications} onChange={e => set('complications', e.target.value)} />
        </label>
        <label className="copy-box">Награды
          <textarea rows={2} value={f.rewards} onChange={e => set('rewards', e.target.value)} />
        </label>
      </div>

      <div className="form-row">
        <label className="grow">Теги (через запятую)<input value={tagsText} onChange={e => setTagsText(e.target.value)} /></label>
        <label className="checkbox">
          <input type="checkbox" checked={f.isVisibleToPlayers} onChange={e => set('isVisibleToPlayers', e.target.checked)} />
          Видно игрокам
        </label>
      </div>
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

/** Плотная таблица участников сцены (по прототипу): имя, тип, сторона, старт, действия. */
function ParticipantsSection({ enc, isGm, members, onRun }: {
  enc: EncounterDetail; isGm: boolean; members: CampaignMember[]; onRun: (a: () => Promise<unknown>) => Promise<void>
}) {
  return (
    <section className="panel enc-participants">
      <div className="panel-head-row">
        <h4>Участники ({enc.participants.length})</h4>
        {isGm && (
          <button className="small" onClick={() => onRun(() => api.addEncounterCharacters(enc.id, null))}>+ Все PC</button>
        )}
      </div>
      {enc.participants.length === 0 && <p className="muted">Участников пока нет.</p>}
      {enc.participants.length > 0 && (
        <div className="table-wrap">
          <table className="participant-table">
            <thead>
              <tr>
                <th>Имя</th>
                <th>Тип</th>
                <th>Сторона</th>
                <th className="nowrap">Старт</th>
                {isGm && <th className="right">Действия</th>}
              </tr>
            </thead>
            <tbody>
              {enc.participants.map(p => (
                <ParticipantRow key={p.id} enc={enc} p={p} isGm={isGm} onRun={onRun} />
              ))}
            </tbody>
          </table>
        </div>
      )}
      {isGm && <AddEncounterParticipant enc={enc} members={members} onRun={onRun} />}
    </section>
  )
}

function ParticipantRow({ enc, p, isGm, onRun }: {
  enc: EncounterDetail; p: EncounterParticipant; isGm: boolean; onRun: (a: () => Promise<unknown>) => Promise<void>
}) {
  const patch = (body: Parameters<typeof api.updateEncounterParticipant>[2]) =>
    onRun(() => api.updateEncounterParticipant(enc.id, p.id, body))
  const start = p.startsDefeated ? 'повержен' : p.startsHidden ? 'скрыт' : 'активен'
  return (
    <tr className={p.startsDefeated ? 'defeated' : undefined}>
      <td>
        <div className="participant-name">{p.displayName}{p.quantity > 1 ? ` ×${p.quantity}` : ''}</div>
        {isGm && p.notes && <div className="participant-note muted small-text">{p.notes}</div>}
      </td>
      <td><span className="badge">{PARTICIPANT_TYPE_LABELS[p.participantType]}</span></td>
      <td>{SLOT_TYPE_LABELS[p.initiativeSide]}</td>
      <td className="nowrap">{start}</td>
      {isGm && (
        <td className="right nowrap">
          <button className="small" title={p.startsHidden ? 'Показать в начале' : 'Скрыть в начале'}
            onClick={() => patch({ startsHidden: !p.startsHidden })}>{p.startsHidden ? 'Показать' : 'Скрыть'}</button>
          <button className="small" title={p.startsDefeated ? 'Сделать активным' : 'Повержен в начале'}
            onClick={() => patch({ startsDefeated: !p.startsDefeated })}>{p.startsDefeated ? 'Активен' : 'Повержен'}</button>
          <button className="danger small"
            onClick={() => { if (confirm(`Убрать «${p.displayName}»?`)) void onRun(() => api.removeEncounterParticipant(enc.id, p.id)) }}>×</button>
        </td>
      )}
    </tr>
  )
}

function AddEncounterParticipant({ enc, members, onRun }: {
  enc: EncounterDetail; members: CampaignMember[]; onRun: (a: () => Promise<unknown>) => Promise<void>
}) {
  const [mode, setMode] = useState<'character' | 'manual'>('character')
  const [characterId, setCharacterId] = useState('')
  const [manualName, setManualName] = useState('')
  const [manualType, setManualType] = useState<ParticipantType>('hazard')

  const add = () => {
    if (mode === 'character' && characterId) void onRun(() => api.addEncounterParticipant(enc.id, { characterId }))
    else if (mode === 'manual' && manualName.trim())
      void onRun(() => api.addEncounterParticipant(enc.id, { displayName: manualName.trim(), participantType: manualType }))
  }

  return (
    <div className="add-participant">
      <div className="label-line">Добавить участника (NPC — через быструю сборку)</div>
      <div className="system-switch">
        {(['character', 'manual'] as const).map(m => (
          <button key={m} type="button" className={mode === m ? 'tab active' : 'tab'} onClick={() => setMode(m)}>
            {m === 'character' ? 'Персонаж' : 'Вручную'}
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
    </div>
  )
}

/** Быстрая сборка сцены из бестиария: поиск NPC + источник + добавление в один клик. */
function QuickBuildPanel({ enc, onRun }: {
  enc: EncounterDetail; onRun: (a: () => Promise<unknown>) => Promise<void>
}) {
  const [npcs, setNpcs] = useState<NpcListItem[]>([])
  const [search, setSearch] = useState('')
  const [source, setSource] = useState<'' | 'mine' | 'builtin'>('')
  const [quantity, setQuantity] = useState(1)

  useEffect(() => { api.npcs().then(setNpcs).catch(() => { /* список NPC не критичен */ }) }, [])

  const shown = useMemo(() => {
    const q = search.trim().toLowerCase()
    return npcs
      .filter(n => n.system === enc.system)
      .filter(n => !q || n.name.toLowerCase().includes(q) || n.tags.some(t => t.toLowerCase().includes(q)))
      .filter(n => source === '' || (source === 'mine' ? n.isMine : n.isBuiltIn))
      .slice(0, 8)
  }, [npcs, search, source, enc.system])

  return (
    <section className="panel enc-quick-build">
      <h4>Быстрая сборка</h4>
      <div className="form-row">
        <input className="grow" type="search" placeholder="Поиск NPC…" value={search}
          onChange={e => setSearch(e.target.value)} aria-label="Поиск NPC" />
        <select value={source} onChange={e => setSource(e.target.value as '' | 'mine' | 'builtin')} aria-label="Источник">
          <option value="">Все источники</option>
          <option value="builtin">Встроенный бестиарий</option>
          <option value="mine">Свои NPC</option>
        </select>
      </div>
      <div className="form-row">
        <label className="inline-label">Количество (для группы миньонов)
          <input className="ranks-input" type="number" min={1} value={quantity}
            onChange={e => setQuantity(Math.max(1, +e.target.value))} />
        </label>
      </div>
      <div className="enc-library-list">
        {shown.length === 0 && <p className="muted small-text">Ничего не найдено.</p>}
        {shown.map(n => (
          <div key={n.id} className="enc-library-row">
            <div className="enc-library-info">
              <div className="enc-library-title">{n.name}</div>
              <div className="muted small-text">
                {NPC_KIND_LABELS[n.kind]} · {NPC_ROLE_LABELS[n.role]}
                {n.isBuiltIn ? ' · встроенный' : n.isMine ? ' · мой' : ''}
              </div>
            </div>
            <button className="small" title="Добавить в сцену"
              onClick={() => void onRun(() => api.addEncounterParticipant(enc.id, { npcId: n.id, quantity }))}>
              +
            </button>
          </div>
        ))}
      </div>
    </section>
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
    <section className="panel enc-run">
      <h4>Запуск</h4>
      <p className="hint">Если активной сцены нет — создаётся новая. Если есть — выберите режим.</p>
      <div className="quick-run">
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
