import { useCallback, useEffect, useState, type FormEvent, type MouseEvent } from 'react'
import { api } from '../api/client'
import type {
  ActivateAbilityResult, CampaignMember, GameParticipant, GameSession, HeroicAbility,
  InitiativeSlotType, NpcListItem, RollLogEntry,
} from '../api/types'
import { SLOT_TYPE_LABELS } from '../utils/labels'
import { RollSymbolsView, type RollLogRequest } from './DiceRoller'
import type { RollSymbols } from '../utils/diceRoller'
import { useDiceRoller } from '../dice-roller-store'
import { t } from '../i18n'
import { GameTableNpcStatblock } from './GameTableNpcStatblock'
import { participantNameWithCount, participantRollPool } from '../utils/gameTable'
import {
  readRangeTrackerState, writeRangeTrackerState, writeSheetTab, type RangeZone,
} from '../utils/uiPreferences'
import { navigate } from '../router'

interface Props {
  campaignId: string
  isGm: boolean
  members: CampaignMember[]
  /** Read-only просмотр листа участника для мастера кампании. */
  onOpenMemberSheet?: (characterId: string, name: string) => Promise<void>
  /** Счётчик realtime-инвалидаций: при изменении сцена перечитывается (другой участник внёс правку). */
  refreshSignal?: number
}

export function GameTableTab({ campaignId, isGm, members, onOpenMemberSheet, refreshSignal }: Props) {
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
    } catch (e) { setError(e instanceof Error ? e.message : t('Ошибка', 'Error')); return null }
  }, [campaignId])

  const reload = useCallback(() =>
    api.session(campaignId)
      .then(s => { setSession(s); setLoaded(true) })
      .catch((e: unknown) => setError(e instanceof Error ? e.message : t('Ошибка загрузки', 'Failed to load'))),
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
    } catch (e) { setError(e instanceof Error ? e.message : t('Ошибка', 'Error')) }
  }, [reload])

  if (!loaded) return <p className="muted">{t('Загрузка сцены…', 'Loading scene…')}</p>

  if (!session) {
    return (
      <div className="game-table">
        {error && <div className="error">{error}</div>}
        {isGm
          ? <CreateSessionForm onCreate={(body) => run(() => api.createSession(campaignId, body))} />
          : <p className="muted">{t('Мастер ещё не запустил сцену.', 'The GM has not started a scene yet.')}</p>}
      </div>
    )
  }

  const currentSlot = session.slots[session.currentTurnIndex]
  const currentActor = currentSlot?.assignedParticipantId
    ? session.participants.find(p => p.id === currentSlot.assignedParticipantId) ?? null
    : null
  return (
    <div className="game-table table-shell">
      {error && <div className="error floating">{error}</div>}

      <section className="panel command-panel">
        <div className="scene-now">
          <h3>{session.name} <span className="badge danger">{t('активная сцена', 'active scene')}</span></h3>
          <div className="page-sub">
            {session.description || t('Описание сцены не задано', 'No scene description')}
            {currentSlot && (
              <> · {t('текущий слот:', 'current slot:')} <span className={`badge slot-${currentSlot.slotType}`}>{SLOT_TYPE_LABELS[currentSlot.slotType]}</span></>
            )}
            {currentActor && <> · <strong>{currentActor.displayName}</strong></>}
          </div>
        </div>

        <div className="round-box" aria-label={t('Раунд и ход', 'Round and turn')}>
          <div>
            <div className="small-text muted">{t('Раунд', 'Round')}</div>
            <div className="round-value">{session.currentRound}</div>
          </div>
          <div>
            <div className="small-text muted">{t('Ход', 'Turn')}</div>
            <div className="round-value">{currentSlot ? session.currentTurnIndex + 1 : '—'}</div>
          </div>
        </div>
        {isGm && <div className="scene-command-actions">
          <button className="primary" onClick={() => run(() => api.nextTurn(campaignId))}>{t('Следующий ход', 'Next turn')}</button>
          <button onClick={() => { if (confirm(t('Сбросить сцену (убрать участников и слоты)?', 'Reset the scene (remove participants and slots)?'))) void run(() => api.resetSession(campaignId)) }}>{t('Сбросить', 'Reset')}</button>
          <button className="danger" onClick={() => { if (confirm(t('Завершить сцену?', 'End the scene?'))) void run(() => api.endSession(campaignId)) }}>{t('Завершить сцену', 'End scene')}</button>
        </div>}
      </section>

      <aside className="left-rail">
        <StoryPoints session={session} isGm={isGm} onRun={run} campaignId={campaignId} />
        <InitiativeTracker session={session} isGm={isGm} onRun={run} campaignId={campaignId} />
      </aside>

      <section className="center-stage">
        <RangeBandTracker key={session.id} campaignId={campaignId} session={session} isGm={isGm} />
      </section>

      <aside className="right-rail">
        <RollSection campaignId={campaignId} isGm={isGm} refreshSignal={refreshSignal} />
        <NotesBlock session={session} isGm={isGm} onRun={run} campaignId={campaignId} />
        <QuickActionsPanel session={session} isGm={isGm} members={members}
          onRun={run} campaignId={campaignId} abilities={abilities} onActivate={activate} />
      </aside>

      <ParticipantsStrip session={session} campaignId={campaignId} isGm={isGm}
        members={members} onRun={run} onSessionChange={setSession}
        onOpenMemberSheet={onOpenMemberSheet} />
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
      <h3>{t('Создать сцену', 'Create a scene')}</h3>
      <label>{t('Название', 'Name')}<input value={name} onChange={e => setName(e.target.value)} required /></label>
      <label>{t('Описание', 'Description')}<textarea value={description} onChange={e => setDescription(e.target.value)} rows={2} /></label>
      <div className="form-row">
        <label className="char-input">{t('Сюжетные очки игроков', 'Player story points')}<input type="number" min={0} value={psp} onChange={e => setPsp(Math.max(0, +e.target.value))} /></label>
        <label className="char-input">{t('Сюжетные очки мастера', 'GM story points')}<input type="number" min={0} value={gsp} onChange={e => setGsp(Math.max(0, +e.target.value))} /></label>
      </div>
      <button className="primary" type="submit" disabled={!name.trim()}>{t('Запустить сцену', 'Start the scene')}</button>
    </form>
  )
}

/**
 * Сюжетные очки сцены в формате «пипсов» (как на обзорной вкладке): сразу видно,
 * сколько очков у игроков (солнце) и у мастера (луна), с переносом в обе стороны.
 */
function StoryPoints({ session, isGm, onRun, campaignId }: BlockProps) {
  const player = session.playerStoryPoints
  const gm = session.gmStoryPoints
  const total = Math.max(6, player + gm)
  const set = (patch: { playerStoryPoints?: number; gmStoryPoints?: number }) => onRun(() => api.updateSession(campaignId, patch))
  return (
    <section className="story-panel" aria-label={t('Сюжетные очки', 'Story points')}>
      <div className="story-title">{t('Сюжетные очки', 'Story points')} <span>{t('пул сцены', 'scene pool')}</span></div>
      <div className="story-controls">
        <div className="story-side">
          <b>{player}</b>
          <span>{t('игроки', 'players')}</span>
          {isGm && <button className="tiny" disabled={player <= 0} onClick={() => set({ playerStoryPoints: player - 1 })}>−</button>}
          {isGm && <button className="tiny" onClick={() => set({ playerStoryPoints: player + 1 })}>+</button>}
        </div>
        <div className="story-transfer">
          {isGm && (
            <>
              <button className="small" disabled={player <= 0}
                onClick={() => set({ playerStoryPoints: player - 1, gmStoryPoints: gm + 1 })}>{t('Игроки → Мастер', 'Players → GM')}</button>
              <button className="small" disabled={gm <= 0}
                onClick={() => set({ gmStoryPoints: gm - 1, playerStoryPoints: player + 1 })}>{t('Мастер → Игроки', 'GM → Players')}</button>
            </>
          )}
          {!isGm && (
            <div className="campaign-pips" aria-label={t(`Игроки ${player}, мастер ${gm}`, `Players ${player}, GM ${gm}`)}>
              {Array.from({ length: total }, (_, i) => (
                <span key={i} className={i < player ? 'campaign-pip player' : i < player + gm ? 'campaign-pip gm' : 'campaign-pip empty'} />
              ))}
            </div>
          )}
        </div>
        <div className="story-side">
          <b>{gm}</b>
          <span>{t('мастер', 'GM')}</span>
          {isGm && <button className="tiny" disabled={gm <= 0} onClick={() => set({ gmStoryPoints: gm - 1 })}>−</button>}
          {isGm && <button className="tiny" onClick={() => set({ gmStoryPoints: gm + 1 })}>+</button>}
        </div>
      </div>
      {isGm && (
        <div className="campaign-pips story-pips" aria-label={t(`Игроки ${player}, мастер ${gm}`, `Players ${player}, GM ${gm}`)}>
          {Array.from({ length: total }, (_, i) => (
            <span key={i} className={i < player ? 'campaign-pip player' : i < player + gm ? 'campaign-pip gm' : 'campaign-pip empty'} />
          ))}
        </div>
      )}
    </section>
  )
}

// ── Range Band Tracker (локальный инструмент мастера, без серверного состояния) ──

const RANGE_ZONES: { id: RangeZone; nameEn: string; nameRu: string; hint: string }[] = [
  { id: 'engaged', nameEn: 'Engaged', nameRu: 'Вплотную', hint: t('ближний бой', 'melee') },
  { id: 'short', nameEn: 'Short', nameRu: 'Ближняя', hint: t('лёгкие дальнобойные · 1 манёвр', 'light ranged · 1 maneuver') },
  { id: 'medium', nameEn: 'Medium', nameRu: 'Средняя', hint: t('дальнобойные · 1 манёвр', 'ranged · 1 maneuver') },
  { id: 'long', nameEn: 'Long', nameRu: 'Дальняя', hint: t('тяжёлые дальнобойные · 2 манёвра', 'heavy ranged · 2 maneuvers') },
  { id: 'extreme', nameEn: 'Extreme', nameRu: 'Предельная', hint: t('предел дистанции · 2 манёвра', 'extreme range · 2 maneuvers') },
]

/** Название зоны на языке интерфейса. */
const zoneName = (zone: { nameRu: string; nameEn: string }) => t(zone.nameRu, zone.nameEn)

const ZONE_INDEX: Record<RangeZone, number> = { engaged: 0, short: 1, medium: 2, long: 3, extreme: 4 }

/** Стартовая зона участника: персонажи игроков — ближняя, противники — средняя. */
const defaultZone = (p: GameParticipant): RangeZone =>
  p.participantType === 'playerCharacter' ? 'short' : 'medium'

/**
 * Трекер дистанций по прототипу range-band-tracker: зоны Engaged…Extreme, токены участников
 * сцены, перемещение перетаскиванием или кнопками, локальный лог перемещений.
 * Позиции не входят в серверную модель кампании и не синхронизируются между устройствами, но
 * сохраняются на этом устройстве отдельно для каждой сцены и переживают навигацию/перезагрузку.
 */
function RangeBandTracker({ campaignId, session, isGm }: {
  campaignId: string; session: GameSession; isGm: boolean
}) {
  const stored = () => readRangeTrackerState(campaignId, session.id)
  const [zones, setZones] = useState<Record<string, RangeZone>>(() => stored().zones)
  const [angles, setAngles] = useState<Record<string, number>>(() => stored().angles)
  const [log, setLog] = useState<string[]>(() => stored().log)
  const [focusParticipantId, setFocusParticipantId] = useState<string | null>(() => stored().focusParticipantId)
  const [dragId, setDragId] = useState<string | null>(null)
  const [showLog, setShowLog] = useState(false)

  const participants = session.participants.filter(p => !p.isDefeated)
  const fallbackFocus = participants.find(p => p.participantType === 'playerCharacter') ?? participants[0]
  const focus = participants.find(p => p.id === focusParticipantId) ?? fallbackFocus ?? null
  const zoneOf = (p: GameParticipant): RangeZone => zones[p.id] ?? defaultZone(p)
  const angleOf = (p: GameParticipant): number => angles[p.id]
    ?? ((participants.findIndex(candidate => candidate.id === p.id) * 137 + 210) % 360)

  const move = (p: GameParticipant, to: RangeZone, angle = angleOf(p)) => {
    const from = zoneOf(p)
    setZones(prev => ({ ...prev, [p.id]: to }))
    setAngles(prev => ({ ...prev, [p.id]: angle }))
    if (from === to) return
    const fromZone = RANGE_ZONES[ZONE_INDEX[from]]
    const toZone = RANGE_ZONES[ZONE_INDEX[to]]
    setLog(prev => [
      t(`Раунд ${session.currentRound}: ${p.displayName} — ${zoneName(fromZone)} → ${zoneName(toZone)}`,
        `Round ${session.currentRound}: ${p.displayName} — ${zoneName(fromZone)} → ${zoneName(toZone)}`),
      ...prev,
    ].slice(0, 20))
  }

  const shift = (p: GameParticipant, delta: 1 | -1) => {
    const next = RANGE_ZONES[ZONE_INDEX[zoneOf(p)] + delta]
    if (next) move(p, next.id)
  }

  useEffect(() => {
    writeRangeTrackerState(campaignId, session.id, {
      zones, angles, log, focusParticipantId: focus?.id ?? null,
    })
  }, [campaignId, session.id, zones, angles, log, focusParticipantId, focus?.id])

  if (participants.length === 0) return null

  const radiusByZone: Record<RangeZone, number> = {
    engaged: 8, short: 18, medium: 29, long: 39, extreme: 48,
  }
  const initials = (p: GameParticipant) => {
    if (p.participantType === 'minionGroup') return `${p.displayName.trim().charAt(0).toUpperCase()}×${p.remainingCount ?? p.count}`
    const words = p.displayName.trim().split(/\s+/).filter(Boolean)
    return words.slice(0, 2).map(word => word.charAt(0).toUpperCase()).join('') || '•'
  }
  const sideName = (angle: number) => {
    const normalized = ((angle % 360) + 360) % 360
    if (normalized >= 45 && normalized < 135) return t('тыл', 'rear')
    if (normalized >= 135 && normalized < 225) return t('слева', 'left')
    if (normalized >= 225 && normalized < 315) return t('фронт', 'front')
    return t('справа', 'right')
  }
  const positionStyle = (p: GameParticipant) => {
    const angle = angleOf(p)
    const radians = angle * Math.PI / 180
    const radius = radiusByZone[zoneOf(p)]
    return { left: `${50 + Math.cos(radians) * radius}%`, top: `${50 + Math.sin(radians) * radius}%` }
  }

  return (
    <section className="panel rb-tracker range-board ring-range-board">
      <div className="rb-head range-head">
        <h3>{t('Дистанции и позиции', 'Ranges and positions')}</h3>
        <span className="muted small-text">{t('сохранено на этом устройстве', 'saved on this device')}</span>
      </div>
      <div className="ring-focus-switch">
        <span className="muted small-text">{t('Отсчёт от:', 'Focus:')}</span>
        {participants.map(p => <button type="button" key={p.id}
          className={focus?.id === p.id ? 'tiny active' : 'tiny'}
          onClick={() => setFocusParticipantId(p.id)}>{participantNameWithCount(p)}</button>)}
      </div>
      <div className={`range-rings${dragId ? ' dragging' : ''}`}
        onDragOver={e => { if (isGm) { e.preventDefault(); e.dataTransfer.dropEffect = 'move' } }}
        onDrop={e => {
          e.preventDefault()
          if (!isGm) return
          const p = participants.find(x => x.id === dragId)
          if (!p || p.id === focus?.id) return setDragId(null)
          const rect = e.currentTarget.getBoundingClientRect()
          const x = e.clientX - rect.left - rect.width / 2
          const y = e.clientY - rect.top - rect.height / 2
          const normalizedRadius = Math.hypot(x, y) / (Math.min(rect.width, rect.height) / 2) * 100
          const zone = normalizedRadius < 13 ? 'engaged'
            : normalizedRadius < 24 ? 'short'
              : normalizedRadius < 34 ? 'medium'
                : normalizedRadius < 44 ? 'long' : 'extreme'
          move(p, zone, Math.atan2(y, x) * 180 / Math.PI)
          setDragId(null)
        }}>
        {RANGE_ZONES.slice().reverse().map(zone => (
          <div key={zone.id} className={`range-ring ring-${zone.id}`}>
            <span>{zoneName(zone)}</span>
          </div>
        ))}
        <span className="ring-side ring-left">{t('Левый фланг', 'Left flank')}</span>
        <span className="ring-side ring-right">{t('Правый фланг', 'Right flank')}</span>
        <span className="ring-side ring-rear">{t('Тыл', 'Rear')}</span>
        {focus && <div className="ring-token focus" title={`${participantNameWithCount(focus)} — ${t('фокус', 'focus')}`}>
          <span>{initials(focus)}</span>
        </div>}
        {participants.filter(p => p.id !== focus?.id).map(p => {
          const pc = p.participantType === 'playerCharacter'
          const zi = ZONE_INDEX[zoneOf(p)]
          return <div key={p.id} style={positionStyle(p)} draggable={isGm}
            className={`ring-token${pc ? ' pc' : ' npc'}${p.isHiddenFromPlayers ? ' hidden-token' : ''}`}
            title={`${participantNameWithCount(p)} — ${zoneName(RANGE_ZONES[zi])}, ${sideName(angleOf(p))}`}
            onDragStart={e => { setDragId(p.id); e.dataTransfer.effectAllowed = 'move' }}
            onDragEnd={() => setDragId(null)}>
            <span>{initials(p)}</span>
            {isGm && <span className="ring-token-actions">
              <button type="button" className="tiny" disabled={zi === 0}
                title={t('Ближе', 'Closer')} onClick={e => { e.stopPropagation(); shift(p, -1) }}>▲</button>
              <button type="button" className="tiny" disabled={zi === RANGE_ZONES.length - 1}
                title={t('Дальше', 'Farther')} onClick={e => { e.stopPropagation(); shift(p, 1) }}>▼</button>
            </span>}
          </div>
        })}
      </div>
      <div className="ring-legend">
        {participants.map(p => <span key={p.id} className={p.participantType === 'playerCharacter' ? 'pc' : 'npc'}>
          <i /> <strong>{initials(p)}</strong> {participantNameWithCount(p)} · {p.id === focus?.id
            ? t('фокус', 'focus')
            : `${zoneName(RANGE_ZONES[ZONE_INDEX[zoneOf(p)]])}, ${sideName(angleOf(p))}`}
        </span>)}
      </div>
      {log.length > 0 && (
        <div className="rb-log">
          <div className="rb-log-head">
            <span className="muted small-text">{t('Последние перемещения', 'Recent moves')}</span>
            {log.length > 3 && (
              <button type="button" className="tiny" onClick={() => setShowLog(true)}>
                {t('Вся история', 'Full history')}
              </button>
            )}
          </div>
          {log.slice(0, 3).map((entry, i) => <div key={i} className="rb-log-entry muted small-text">{entry}</div>)}
        </div>
      )}
      {showLog && (
        <div className="modal-backdrop" role="presentation" onClick={() => setShowLog(false)}>
          <div className="modal range-log-modal" role="dialog" aria-modal="true" aria-label={t('История перемещений', 'Movement history')}
            onClick={e => e.stopPropagation()}>
            <div className="modal-head">
              <h3>{t('История перемещений', 'Movement history')}</h3>
              <button type="button" className="small" onClick={() => setShowLog(false)}>{t('Закрыть', 'Close')}</button>
            </div>
            <div className="range-log-list">
              {log.map((entry, i) => <div key={i} className="note-row small-text">{entry}</div>)}
            </div>
          </div>
        </div>
      )}
    </section>
  )
}

function InitiativeTracker({ session, isGm, onRun, campaignId }: BlockProps) {
  const [slotType, setSlotType] = useState<InitiativeSlotType>('player')
  const nameOf = (pid: string | null) => session.participants.find(p => p.id === pid)?.displayName

  return (
    <section className="panel initiative-panel">
      <div className="panel-head">
        <h3>{t('Инициатива', 'Initiative')}</h3>
        {isGm && <button className="small" onClick={() => onRun(() => api.addSlot(campaignId, { slotType }))}>{t('+ Слот', '+ Slot')}</button>}
      </div>
      {session.slots.length === 0 && <p className="muted">{t('Слотов нет.', 'No slots.')}{isGm && t(' Добавьте слоты ниже.', ' Add slots below.')}</p>}
      <ol className="initiative-list">
        {session.slots.map((slot, i) => (
          <li key={slot.id} className={i === session.currentTurnIndex ? 'init-row current' : 'init-row'}>
            <span className="init-num">{i + 1}</span>
            {isGm ? (
              <select className="slot-assign" value={slot.assignedParticipantId ?? ''}
                onChange={e => onRun(() => api.updateSlot(campaignId, slot.id, { assignedParticipantId: e.target.value || '00000000-0000-0000-0000-000000000000' }))}>
                <option value="">{t('— абстрактный —', '— unassigned —')}</option>
                {session.participants.map(p => <option key={p.id} value={p.id}>{p.displayName}</option>)}
              </select>
            ) : (
              <span className="init-name">{nameOf(slot.assignedParticipantId) ?? t('— абстрактный —', '— unassigned —')}</span>
            )}
            <span className={`badge slot-${slot.slotType}`}>{SLOT_TYPE_LABELS[slot.slotType]}</span>
            {isGm && <button className="danger tiny" onClick={() => onRun(() => api.removeSlot(campaignId, slot.id))}>×</button>}
          </li>
        ))}
      </ol>
      {isGm && (
        <div className="form-row gt-slot-form">
          <select value={slotType} onChange={e => setSlotType(e.target.value as InitiativeSlotType)}>
            {(['player', 'npc', 'neutral'] as InitiativeSlotType[]).map(t => <option key={t} value={t}>{SLOT_TYPE_LABELS[t]}</option>)}
          </select>
        </div>
      )}
    </section>
  )
}

function ParticipantsStrip({ session, campaignId, isGm, members, onRun, onSessionChange,
  onOpenMemberSheet }: {
  session: GameSession
  campaignId: string
  isGm: boolean
  members: CampaignMember[]
  onRun: (action: () => Promise<unknown>) => Promise<void>
  onSessionChange: (session: GameSession) => void
  onOpenMemberSheet?: (characterId: string, name: string) => Promise<void>
}) {
  const [openNpcId, setOpenNpcId] = useState<string | null>(null)
  const openNpc = session.participants.find(p => p.id === openNpcId) ?? null
  return (
    <>
      <section className="panel participants-strip">
        <div className="panel-head participants-head">
          <h3>{t('Участники сцены', 'Scene participants')}</h3>
          <span className="muted small-text">{t('раны · бусты · сетбеки — прямо на карточке', 'wounds · boosts · setbacks — directly on cards')}</span>
        </div>
        <div className="participants-grid">
          {session.participants.length === 0 && <p className="muted">{t('Участников пока нет.', 'No participants yet.')}</p>}
          {session.participants.map(p => {
            const member = p.characterId ? members.find(m => m.characterId === p.characterId) : undefined
            const onOpenCharacter = p.characterId && member && (isGm || member.isMine)
              ? () => {
                  if (isGm && onOpenMemberSheet) void onOpenMemberSheet(p.characterId!, member.characterName)
                  else if (member.isMine) {
                    writeSheetTab(p.characterId!, 'sheet')
                    navigate(`/characters/${p.characterId}`)
                  }
                }
              : undefined
            return <ParticipantCard key={p.id} p={p} campaignId={campaignId} isGm={isGm}
              canEditVitals={isGm || Boolean(session.allowPlayerEdits && member?.isMine)}
              onRun={onRun} onOpenCharacter={onOpenCharacter}
              onOpenNpc={p.npcId ? () => setOpenNpcId(p.id) : undefined} />
          })}
        </div>
      </section>
      {openNpc && <GameTableNpcStatblock participant={openNpc} campaignId={campaignId} isGm={isGm}
        onSessionChange={onSessionChange} onClose={() => setOpenNpcId(null)} />}
    </>
  )
}

function ParticipantCard({ p, campaignId, isGm, canEditVitals, onRun, onOpenNpc, onOpenCharacter }: {
  p: GameParticipant
  campaignId: string
  isGm: boolean
  canEditVitals: boolean
  onRun: (action: () => Promise<unknown>) => Promise<void>
  onOpenNpc?: () => void
  onOpenCharacter?: () => void
}) {
  const { openRoller } = useDiceRoller()
  const onOpen = onOpenNpc ?? onOpenCharacter
  const stop = (action: () => void) => (event: MouseEvent) => {
    event.stopPropagation()
    action()
  }
  const update = (patch: Parameters<typeof api.updateParticipant>[2]) =>
    void onRun(() => api.updateParticipant(campaignId, p.id, patch))
  const label = onOpenNpc
    ? t(`Открыть статблок NPC ${p.displayName}`, `Open NPC stat block ${p.displayName}`)
    : onOpenCharacter
      ? t(`Открыть лист персонажа ${p.displayName}`, `Open character sheet ${p.displayName}`)
      : undefined
  return (
    <article className={`pc-card participant-control-card${onOpen ? ' clickable' : ''}${p.isDefeated ? ' defeated' : ''}${p.criticalInjuries > 0 ? ' crit' : ''}`}
      role={onOpen ? 'button' : undefined} tabIndex={onOpen ? 0 : undefined} aria-label={label}
      onClick={onOpen}
      onKeyDown={e => { if (onOpen && (e.key === 'Enter' || e.key === ' ')) { e.preventDefault(); onOpen() } }}>
      <div className="pc-name">
        <span>{participantNameWithCount(p)}</span>
        <span className="pc-name-badges">
          {p.criticalInjuries > 0 && <span className="badge danger">{t('криты', 'crits')} {p.criticalInjuries}</span>}
          <span className={p.participantType === 'playerCharacter' ? 'badge slot-player' : 'badge slot-npc'}>
            {p.participantType === 'playerCharacter' ? 'PC' : 'NPC'}
          </span>
        </span>
      </div>
      <div className="pc-stats">
        <div className="mini-stat">{t('Погл.', 'Soak')} <b>{p.soak}</b></div>
        <div className="mini-stat">{t('Защ.', 'Def.')} <b>{p.meleeDefense}/{p.rangedDefense}</b></div>
      </div>
      <div className="participant-control-grid">
        <span>{t('Раны', 'Wounds')}</span>
        <div className="participant-vital"><b>{p.woundsCurrent} / {p.woundsThreshold}</b><div className="bar"><span className="wounds" style={{ width: `${ratio(p.woundsCurrent, p.woundsThreshold) * 100}%` }} /></div></div>
        <button type="button" className="tiny" disabled={!canEditVitals || p.woundsCurrent <= 0}
          aria-label={t(`Убрать рану у ${p.displayName}`, `Remove wound from ${p.displayName}`)}
          onClick={stop(() => update({ woundsCurrent: Math.max(0, p.woundsCurrent - 1) }))}>−</button>
        <button type="button" className="tiny" disabled={!canEditVitals}
          aria-label={t(`Добавить рану ${p.displayName}`, `Add wound to ${p.displayName}`)}
          onClick={stop(() => update({ woundsCurrent: p.woundsCurrent + 1 }))}>+</button>

        <span>{t('Устал.', 'Strain')}</span>
        <div className="participant-vital"><b>{p.strainThreshold == null ? '—' : `${p.strainCurrent} / ${p.strainThreshold}`}</b>{p.strainThreshold != null && <div className="bar"><span className="strain" style={{ width: `${ratio(p.strainCurrent, p.strainThreshold) * 100}%` }} /></div>}</div>
        <button type="button" className="tiny" disabled={!canEditVitals || p.strainThreshold == null || p.strainCurrent <= 0}
          aria-label={t(`Убрать усталость у ${p.displayName}`, `Remove strain from ${p.displayName}`)}
          onClick={stop(() => update({ strainCurrent: Math.max(0, p.strainCurrent - 1) }))}>−</button>
        <button type="button" className="tiny" disabled={!canEditVitals || p.strainThreshold == null}
          aria-label={t(`Добавить усталость ${p.displayName}`, `Add strain to ${p.displayName}`)}
          onClick={stop(() => update({ strainCurrent: p.strainCurrent + 1 }))}>+</button>

        <span className="boost-label">{t('Бусты', 'Boosts')}</span><b>{p.boostDice}</b>
        <button type="button" className="tiny" disabled={!isGm || p.boostDice <= 0}
          aria-label={t(`Убрать буст у ${p.displayName}`, `Remove boost from ${p.displayName}`)}
          onClick={stop(() => update({ boostDice: p.boostDice - 1 }))}>−</button>
        <button type="button" className="tiny" disabled={!isGm || p.boostDice >= 20}
          aria-label={t(`Добавить буст ${p.displayName}`, `Add boost to ${p.displayName}`)}
          onClick={stop(() => update({ boostDice: p.boostDice + 1 }))}>+</button>

        <span className="setback-label">{t('Сетбеки', 'Setbacks')}</span><b>{p.setbackDice}</b>
        <button type="button" className="tiny" disabled={!isGm || p.setbackDice <= 0}
          aria-label={t(`Убрать сетбек у ${p.displayName}`, `Remove setback from ${p.displayName}`)}
          onClick={stop(() => update({ setbackDice: p.setbackDice - 1 }))}>−</button>
        <button type="button" className="tiny" disabled={!isGm || p.setbackDice >= 20}
          aria-label={t(`Добавить сетбек ${p.displayName}`, `Add setback to ${p.displayName}`)}
          onClick={stop(() => update({ setbackDice: p.setbackDice + 1 }))}>+</button>
      </div>
      <div className="participant-card-footer" onClick={e => e.stopPropagation()}>
        {isGm && <label><input type="checkbox" checked={p.isDefeated}
          onChange={e => update({ isDefeated: e.target.checked })} />{t('повержен', 'defeated')}</label>}
        {isGm && <label><input type="checkbox" checked={p.isHiddenFromPlayers}
          onChange={e => update({ isHiddenFromPlayers: e.target.checked })} />{t('скрыт', 'hidden')}</label>}
        <button type="button" className="tiny participant-roll"
          aria-label={t(`Бросок участника ${p.displayName}`, `Roll for participant ${p.displayName}`)}
          onClick={() => openRoller({
          kind: 'roll', title: `${p.displayName} — ${t('бросок', 'roll')}`,
          label: t('Бросок участника', 'Participant roll'), initialPool: participantRollPool({}, p),
          onLog: req => { void onRun(() => api.createRoll(campaignId, { ...req, actorName: p.displayName })) },
          canSecret: isGm,
        })}>🎲</button>
        {onOpen && <button type="button" className="tiny" onClick={onOpenNpc ?? onOpenCharacter}>
          {onOpenNpc ? t('Статблок', 'Stat block') : t('Лист', 'Sheet')}
        </button>}
      </div>
    </article>
  )
}

function QuickActionsPanel({ session, isGm, members, onRun, campaignId, abilities, onActivate }:
  BlockProps & { members: CampaignMember[]; abilities: HeroicAbility[]
    onActivate: (participantId: string, code: string) => Promise<ActivateAbilityResult | null> }) {
  const [removeParticipantId, setRemoveParticipantId] = useState('')
  const [abilityParticipantId, setAbilityParticipantId] = useState('')
  const [abilityId, setAbilityId] = useState('')
  const [outcome, setOutcome] = useState<ActivateAbilityResult | null>(null)
  const participant = session.participants.find(p => p.id === abilityParticipantId)

  async function activate() {
    if (!participant) return
    const ability = abilities.find(x => x.id === abilityId)
    if (!ability) return
    const result = await onActivate(participant.id, ability.code)
    if (result) setOutcome(result)
  }

  return (
    <section className="panel quick-panel">
      <div className="panel-head">
        <h3>{t('Быстрые действия', 'Quick actions')}</h3>
        <span className="muted small-text">{t('состав сцены', 'scene roster')}</span>
      </div>
      {isGm && <AddParticipant members={members} onRun={onRun} campaignId={campaignId} />}
      {isGm && <div className="quick-main remove-participant">
        <select className="grow" value={removeParticipantId} onChange={e => setRemoveParticipantId(e.target.value)}>
          <option value="">{t('— участник для удаления —', '— participant to remove —')}</option>
          {session.participants.map(p => <option key={p.id} value={p.id}>{participantNameWithCount(p)}</option>)}
        </select>
        <button className="danger small" disabled={!removeParticipantId}
          onClick={() => void onRun(() => api.removeParticipant(campaignId, removeParticipantId))}>{t('Убрать', 'Remove')}</button>
      </div>}
      {abilities.length > 0 && (
        <div className="quick-ability">
          <select className="grow" value={abilityParticipantId} onChange={e => setAbilityParticipantId(e.target.value)}>
            <option value="">{t('— участник для способности —', '— ability participant —')}</option>
            {session.participants.map(p => <option key={p.id} value={p.id}>{participantNameWithCount(p)}</option>)}
          </select>
          <select className="grow" value={abilityId} onChange={e => setAbilityId(e.target.value)}>
            <option value="">{t('— способность —', '— ability —')}</option>
            {abilities.map(a => <option key={a.id} value={a.id}>{t(a.nameRu || a.name, a.name || a.nameRu)}</option>)}
          </select>
          <button className="small" disabled={!abilityParticipantId || !abilityId} onClick={() => void activate()}>{t('Активировать', 'Activate')}</button>
        </div>
      )}
      {outcome && (
        <div className="pc-activate-result small-text">
          <strong>{outcome.abilityName}.</strong>
          {outcome.applied.map((a, i) => <span key={`a${i}`}> {a}.</span>)}
          {outcome.manual.map((m, i) => <span key={`m${i}`} className="muted"> {m}</span>)}
        </div>
      )}
    </section>
  )
}

function ratio(value: number, max: number): number {
  if (!Number.isFinite(value) || !Number.isFinite(max) || max <= 0) return 0
  return Math.max(0, Math.min(1, value / max))
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
      <div className="system-switch">
        {(['character', 'npc', 'manual'] as const).map(m => (
          <button key={m} type="button" className={mode === m ? 'tab active' : 'tab'} onClick={() => setMode(m)}>
            {m === 'character' ? t('Персонаж', 'Character') : m === 'npc' ? 'NPC' : t('Вручную', 'Manual')}
          </button>
        ))}
      </div>
      <div className="form-row">
        {mode === 'character' && (
          <select className="grow" value={characterId} onChange={e => setCharacterId(e.target.value)}>
            <option value="">{t('— выберите персонажа —', '— pick a character —')}</option>
            {members.map(m => <option key={m.characterId} value={m.characterId}>{m.characterName}</option>)}
          </select>
        )}
        {mode === 'npc' && (
          <>
            <select className="grow" value={npcId} onChange={e => setNpcId(e.target.value)}>
              <option value="">{t('— выберите NPC —', '— pick an NPC —')}</option>
              {npcs.map(n => <option key={n.id} value={n.id}>{n.name}</option>)}
            </select>
            <input className="ranks-input" type="number" min={1} value={count} onChange={e => setCount(Math.max(1, +e.target.value))} title={t('Количество (группа миньонов)', 'Count (minion group)')} />
          </>
        )}
        {mode === 'manual' && (
          <>
            <input className="grow" placeholder={t('Название', 'Name')} value={manualName} onChange={e => setManualName(e.target.value)} />
            <input className="ranks-input" type="number" min={1} value={manualWt} onChange={e => setManualWt(Math.max(1, +e.target.value))} title={t('Порог ран', 'Wound threshold')} />
          </>
        )}
        <button className="primary small" onClick={add}>{t('Добавить', 'Add')}</button>
      </div>
    </div>
  )
}

function NotesBlock({ session, isGm, onRun, campaignId }: BlockProps) {
  const [pub, setPub] = useState(session.publicNotes)
  const [gm, setGm] = useState(session.gmNotes ?? '')
  return (
    <section className="panel scene-notes-panel">
      <div className="panel-head">
        <h3>{t('Заметки сцены', 'Scene notes')}</h3>
        {isGm && (
          <label className="checkbox compact-checkbox">
            <input type="checkbox" checked={session.allowPlayerEdits}
              onChange={e => onRun(() => api.updateSession(campaignId, { allowPlayerEdits: e.target.checked }))} />
            {t('Игроки меняют раны/усталость', 'Players edit wounds/strain')}
          </label>
        )}
      </div>
      <label>{t('Публичные (видят игроки)', 'Public (players can see)')}
        <textarea rows={2} value={pub} disabled={!isGm} onChange={e => setPub(e.target.value)}
          onBlur={() => isGm && pub !== session.publicNotes && onRun(() => api.updateSession(campaignId, { publicNotes: pub }))} />
      </label>
      {isGm && (
        <label>{t('Приватные (только мастер)', 'Private (GM only)')}
          <textarea rows={2} value={gm} onChange={e => setGm(e.target.value)}
            onBlur={() => gm !== (session.gmNotes ?? '') && onRun(() => api.updateSession(campaignId, { gmNotes: gm }))} />
        </label>
      )}
    </section>
  )
}

function RollSection({ campaignId, isGm, refreshSignal }: { campaignId: string; isGm: boolean; refreshSignal?: number }) {
  const [rolls, setRolls] = useState<RollLogEntry[]>([])
  const [error, setError] = useState<string | null>(null)
  const { openRoller } = useDiceRoller()

  const reload = useCallback(() =>
    api.rolls(campaignId).then(setRolls).catch(() => { /* лог не критичен */ }),
    [campaignId])

  useEffect(() => { void reload() }, [reload])
  // Перечитываем лог по realtime-событию (чужой бросок).
  useEffect(() => { if (refreshSignal) void reload() }, [refreshSignal, reload])

  async function log(req: RollLogRequest) {
    try {
      await api.createRoll(campaignId, req)
      setError(null)
      await reload()
    } catch (e) { setError(e instanceof Error ? e.message : t('Ошибка броска', 'Roll failed')) }
  }

  return (
    <section className="panel gt-rolls">
      <div className="panel-head">
        <h3>{t('Броски', 'Rolls')}</h3>
        <button type="button" className="small" onClick={() => openRoller({
          kind: 'roll',
          title: t('Бросок стола', 'Table roll'),
          onLog: log,
          canSecret: isGm,
        })}>
          {t('Открыть справа', 'Open on the right')}
        </button>
      </div>
      {error && <div className="error">{error}</div>}
      <div className="roll-log">
        {rolls.length === 0 && <p className="muted">{t('Бросков пока нет.', 'No rolls yet.')}</p>}
        {rolls.map(r => (
          <div key={r.id} className="roll-entry">
            <span className="roll-actor"><strong>{r.actorName}</strong>{r.label && <span className="muted"> · {r.label}</span>}</span>
            <RollSymbolsView symbols={parseSymbols(r.resultJson)} />
            {r.isSecret && <span className="badge tier" title={t('Виден только мастеру', 'Visible only to the GM')}>{t('секретно', 'secret')}</span>}
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
