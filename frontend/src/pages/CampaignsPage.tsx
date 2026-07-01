import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type {
  CampaignDetail, CampaignListItem, CampaignMember, CharacterListItem, CharacterSheet, GameSession, Reference,
} from '../api/types'
import { PARTICIPANT_TYPE_LABELS, SLOT_TYPE_LABELS, SYSTEM_LABELS } from '../utils/labels'
import { GameTableTab } from '../components/GameTableTab'
import { EncountersTab } from '../components/EncountersTab'
import { HandbookTab } from '../components/HandbookTab'
import { PrintPreview } from '../components/print/PrintPreview'
import { CharacterSheetPrint } from '../components/print/CharacterSheetPrint'
import { useCampaignHub, type CampaignHubStatus } from '../useCampaignHub'

export type CampaignView = 'overview' | 'handbook' | 'encounters' | 'table'

interface Props {
  openId: string | null
  view: CampaignView
  openEncounterId: string | null
  onOpen: (id: string) => void
  onBack: () => void
  onView: (view: CampaignView) => void
  onOpenEncounter: (eid: string) => void
  onCloseEncounter: () => void
}

export function CampaignsPage({
  openId, view, openEncounterId, onOpen, onBack, onView, onOpenEncounter, onCloseEncounter,
}: Props) {
  const [campaigns, setCampaigns] = useState<CampaignListItem[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  const reload = useCallback(
    () => api.campaigns().then(setCampaigns).catch((e: unknown) =>
      setError(e instanceof Error ? e.message : 'Ошибка загрузки')),
    [])

  // Перезагружаем список при показе (в т.ч. при возврате из карточки кампании).
  useEffect(() => { if (!openId) void reload() }, [openId, reload])

  if (openId) return <CampaignDetailView campaignId={openId} view={view} openEncounterId={openEncounterId}
    onBack={onBack} onView={onView} onOpenEncounter={onOpenEncounter} onCloseEncounter={onCloseEncounter} />

  return (
    <div className="page">
      <div className="page-head">
        <h2>Кампании</h2>
      </div>
      {error && <div className="error">{error}</div>}

      <div className="campaign-forms">
        <CreateCampaignForm onDone={reload} onError={setError} />
        <JoinCampaignForm onDone={reload} onError={setError} />
      </div>

      {campaigns === null && <p className="muted">Загрузка…</p>}
      {campaigns?.length === 0 && <p className="muted">Пока нет кампаний — создайте свою или присоединитесь по коду.</p>}
      <div className="card-grid">
        {campaigns?.map(c => (
          <div key={c.id} className="char-card" onClick={() => onOpen(c.id)}>
            <div className="char-card-head">
              <strong>{c.name}</strong>
              <span className={c.isGm ? 'badge tier' : 'badge'}>{c.isGm ? 'Мастер' : 'Игрок'}</span>
            </div>
            <div className="muted">Персонажей: {c.characterCount}</div>
          </div>
        ))}
      </div>
    </div>
  )
}

function CreateCampaignForm({ onDone, onError }: { onDone: () => void; onError: (m: string) => void }) {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')

  async function submit(e: FormEvent) {
    e.preventDefault()
    try {
      await api.createCampaign(name, description)
      setName(''); setDescription('')
      onDone()
    } catch (err) { onError(err instanceof Error ? err.message : 'Ошибка') }
  }

  return (
    <form className="panel custom-form" onSubmit={submit}>
      <h3>Создать кампанию (вы — мастер)</h3>
      <label>Название<input value={name} onChange={e => setName(e.target.value)} required /></label>
      <label>Описание<textarea value={description} onChange={e => setDescription(e.target.value)} rows={2} /></label>
      <button className="primary" type="submit">Создать</button>
    </form>
  )
}

function JoinCampaignForm({ onDone, onError }: { onDone: () => void; onError: (m: string) => void }) {
  const [code, setCode] = useState('')
  const [characters, setCharacters] = useState<CharacterListItem[]>([])
  const [characterId, setCharacterId] = useState('')

  useEffect(() => {
    api.characters().then(setCharacters).catch(() => { /* список персонажей не критичен здесь */ })
  }, [])

  async function submit(e: FormEvent) {
    e.preventDefault()
    try {
      await api.joinCampaign(code, characterId)
      setCode(''); setCharacterId('')
      onDone()
    } catch (err) { onError(err instanceof Error ? err.message : 'Ошибка') }
  }

  return (
    <form className="panel custom-form" onSubmit={submit}>
      <h3>Присоединиться по коду (своим персонажем)</h3>
      <label>Код кампании<input value={code} onChange={e => setCode(e.target.value)} required /></label>
      <label>Мой персонаж
        <select value={characterId} onChange={e => setCharacterId(e.target.value)} required>
          <option value="" disabled>— выберите —</option>
          {characters.map(c => <option key={c.id} value={c.id}>{c.name} ({SYSTEM_LABELS[c.system]})</option>)}
        </select>
      </label>
      <button className="primary" type="submit" disabled={!characterId}>Присоединиться</button>
    </form>
  )
}

function CampaignDetailView({ campaignId, view, openEncounterId, onBack, onView, onOpenEncounter, onCloseEncounter }: {
  campaignId: string
  view: CampaignView
  openEncounterId: string | null
  onBack: () => void
  onView: (view: CampaignView) => void
  onOpenEncounter: (eid: string) => void
  onCloseEncounter: () => void
}) {
  const [c, setC] = useState<CampaignDetail | null>(null)
  const [error, setError] = useState<string | null>(null)
  // Счётчик realtime-инвалидаций: меняется на событиях хаба, вкладки перечитывают данные.
  const [liveSignal, setLiveSignal] = useState(0)
  const [hubStatus, setHubStatus] = useState<CampaignHubStatus>('connecting')
  // GM открыл read-only лист персонажа участника (U-20).
  const [memberSheet, setMemberSheet] = useState<{ name: string; sheet: CharacterSheet; reference: Reference } | null>(null)
  const [memberSheets, setMemberSheets] = useState<Record<string, CharacterSheet>>({})
  const [session, setSession] = useState<GameSession | null>(null)
  const [sessionLoaded, setSessionLoaded] = useState(false)

  async function openMemberSheet(characterId: string, name: string) {
    try {
      const sheet = await api.campaignMemberSheet(campaignId, characterId)
      const reference = await api.reference(sheet.system)
      setMemberSheet({ name, sheet, reference })
    } catch (err) { setError(err instanceof Error ? err.message : 'Не удалось открыть лист') }
  }

  const reload = useCallback(
    () => api.campaign(campaignId).then(setC).catch((e: unknown) =>
      setError(e instanceof Error ? e.message : 'Ошибка загрузки')),
    [campaignId])
  useEffect(() => { void reload() }, [reload])

  const reloadSession = useCallback(
    () => api.session(campaignId)
      .then(s => { setSession(s); setSessionLoaded(true) })
      .catch(() => { setSession(null); setSessionLoaded(true) }),
    [campaignId])

  useEffect(() => {
    if (view === 'overview') void reloadSession()
  }, [view, reloadSession, liveSignal])

  useEffect(() => {
    let cancelled = false
    if (!c?.isGm || view !== 'overview' || c.members.length === 0) return () => { cancelled = true }

    Promise.all(c.members.map(async m => {
      try {
        const sheet = await api.campaignMemberSheet(campaignId, m.characterId)
        return [m.characterId, sheet] as const
      } catch {
        return null
      }
    })).then(rows => {
      if (cancelled) return
      setMemberSheets(Object.fromEntries(rows.filter((row): row is readonly [string, CharacterSheet] => row !== null)))
    })

    return () => { cancelled = true }
  }, [c, campaignId, view, liveSignal])

  // Подписка на события кампании на время открытой карточки.
  useCampaignHub(campaignId, {
    onGameTableChanged: () => setLiveSignal(v => v + 1),
    onRollAdded: () => setLiveSignal(v => v + 1),
    onCampaignChanged: () => { setLiveSignal(v => v + 1); void reload() },
    onStatus: setHubStatus,
  })

  async function run(action: () => Promise<unknown>) {
    try { await action(); await reload() }
    catch (err) { setError(err instanceof Error ? err.message : 'Ошибка') }
  }

  async function runSession(action: () => Promise<unknown>) {
    try {
      const result = await action()
      if (result && typeof result === 'object' && 'participants' in result) setSession(result as GameSession)
      else await reloadSession()
      setError(null)
    } catch (err) { setError(err instanceof Error ? err.message : 'Ошибка') }
  }

  if (!c) {
    return <div className="page"><button onClick={onBack}>← Кампании</button>{error && <div className="error">{error}</div>}</div>
  }

  return (
    <div className="page">
      <div className="page-head">
        <div>
          <button onClick={onBack}>← Кампании</button>
          <h2 className="inline-title">{c.name}</h2>
          <span className={c.isGm ? 'badge tier' : 'badge'}>{c.isGm ? 'Мастер' : 'Игрок'}</span>
          {/* Состояние связи показываем, только когда оно влияет на ожидания (нет live-обновлений). */}
          {hubStatus !== 'connected' && (
            <span className="badge warn live-badge" title="Обновления в реальном времени недоступны — обновите вручную">
              {hubStatus === 'connecting' ? 'подключение…' : 'офлайн'}
            </span>
          )}
        </div>
      </div>
      {error && <div className="error floating">{error}</div>}

      <div className="system-switch campaign-tabs">
        <button className={view === 'overview' ? 'tab active' : 'tab'} onClick={() => onView('overview')}>Обзор</button>
        <button className={view === 'handbook' ? 'tab active' : 'tab'} onClick={() => onView('handbook')}>Материалы</button>
        <button className={view === 'encounters' ? 'tab active' : 'tab'} onClick={() => onView('encounters')}>Энкаунтеры</button>
        <button className={view === 'table' ? 'tab active' : 'tab'} onClick={() => onView('table')}>Игровой стол</button>
      </div>

      {view === 'table' ? (
        <GameTableTab campaignId={c.id} isGm={c.isGm} members={c.members} refreshSignal={liveSignal} />
      ) : view === 'handbook' ? (
        <HandbookTab campaignId={c.id} isGm={c.isGm} />
      ) : view === 'encounters' ? (
        <EncountersTab campaignId={c.id} isGm={c.isGm} members={c.members}
          openEncounterId={openEncounterId} onOpenEncounter={onOpenEncounter} onCloseEncounter={onCloseEncounter}
          onSentToTable={() => onView('table')} />
      ) : (
        <CampaignOverview
          campaign={c}
          session={session}
          sessionLoaded={sessionLoaded}
          memberSheets={memberSheets}
          onView={onView}
          onOpenMemberSheet={openMemberSheet}
          onRemoveMember={characterId => run(() => api.removeCampaignCharacter(c.id, characterId))}
          onCampaignRun={run}
          onSessionRun={runSession}
        />
      )}

      {memberSheet && (
        <PrintPreview title={`Лист персонажа — ${memberSheet.name}`} onClose={() => setMemberSheet(null)}>
          {() => <CharacterSheetPrint sheet={memberSheet.sheet} reference={memberSheet.reference} />}
        </PrintPreview>
      )}
    </div>
  )
}

function CampaignOverview({ campaign, session, sessionLoaded, memberSheets, onView, onOpenMemberSheet,
  onRemoveMember, onCampaignRun, onSessionRun }: {
  campaign: CampaignDetail
  session: GameSession | null
  sessionLoaded: boolean
  memberSheets: Record<string, CharacterSheet>
  onView: (view: CampaignView) => void
  onOpenMemberSheet: (characterId: string, name: string) => Promise<void>
  onRemoveMember: (characterId: string) => Promise<void>
  onCampaignRun: (a: () => Promise<unknown>) => Promise<void>
  onSessionRun: (a: () => Promise<unknown>) => Promise<void>
}) {
  const sheets = campaign.members
    .map(m => memberSheets[m.characterId])
    .filter((s): s is CharacterSheet => Boolean(s))
  const totalXp = sheets.reduce((sum, s) => sum + s.totalXp, 0)
  const availableXp = sheets.reduce((sum, s) => sum + s.availableXp, 0)
  const woundRatios = sheets
    .map(s => ratio(s.woundsCurrent, s.derived.woundThreshold))
    .filter(n => Number.isFinite(n))
  const avgWounds = woundRatios.length > 0
    ? Math.round(woundRatios.reduce((sum, n) => sum + n, 0) / woundRatios.length * 100)
    : null
  const critical = campaign.members
    .map(m => ({ member: m, sheet: memberSheets[m.characterId] }))
    .find(x => x.sheet && ratio(x.sheet.woundsCurrent, x.sheet.derived.woundThreshold) >= 0.9)

  return (
    <div className="campaign-dashboard">
      {critical?.sheet && (
        <div className="campaign-alert">
          <strong>{critical.member.characterName}</strong> почти на пороге ран —
          {` ${critical.sheet.woundsCurrent}/${critical.sheet.derived.woundThreshold}`}. Проверьте критические ранения.
        </div>
      )}

      <div className="campaign-dash-head">
        <div className="campaign-dash-title">
          <h3>{campaign.description || 'Кампания без описания'}</h3>
          <div className="campaign-sub">
            {campaign.isGm && campaign.joinCode
              ? <>Код: <code className="join-code compact">{campaign.joinCode}</code> · </>
              : null}
            {campaign.members.length} участник(ов)
            {session ? ` · Раунд ${session.currentRound}` : ''}
          </div>
        </div>
        <div className="head-actions">
          <button className="small" onClick={() => onView('encounters')}>Энкаунтеры</button>
          <button className="primary small" onClick={() => onView('table')}>→ Игровой стол</button>
        </div>
      </div>

      <div className="campaign-section-label">Персонажи группы</div>
      <div className="campaign-players-grid">
        {campaign.members.length === 0 && <div className="campaign-empty">Пока никто не присоединился.</div>}
        {campaign.members.map(m => (
          <CampaignMemberCard key={m.characterId} member={m} sheet={memberSheets[m.characterId]}
            isGm={campaign.isGm} onOpenSheet={onOpenMemberSheet} onRemove={onRemoveMember} />
        ))}
      </div>

      <div className="campaign-bottom-grid">
        <CurrentSceneBlock session={session} sessionLoaded={sessionLoaded} onView={onView} />
        <StoryPointsBlock session={session} isGm={campaign.isGm} onSessionRun={onSessionRun} />
        <InitiativeBlock session={session} isGm={campaign.isGm} onView={onView} onSessionRun={onSessionRun} />
      </div>

      <div className="campaign-wide-grid">
        <CampaignNotesSection campaign={campaign} onRun={onCampaignRun} variant="dashboard" />
        <div className="campaign-dash-block">
          <h4>Статистика группы</h4>
          <div className="campaign-stats-grid">
            <div className="campaign-stat"><div className="campaign-stat-val">{sheets.length > 0 ? totalXp : '—'}</div><div className="campaign-stat-lbl">суммарный XP</div></div>
            <div className="campaign-stat"><div className="campaign-stat-val">{sheets.length > 0 ? availableXp : '—'}</div><div className="campaign-stat-lbl">свободный XP</div></div>
            <div className="campaign-stat"><div className={avgWounds !== null && avgWounds >= 70 ? 'campaign-stat-val red' : 'campaign-stat-val'}>{avgWounds !== null ? `${avgWounds}%` : '—'}</div><div className="campaign-stat-lbl">сред. раны</div></div>
            <div className="campaign-stat"><div className="campaign-stat-val">{session?.currentRound ?? '—'}</div><div className="campaign-stat-lbl">текущий раунд</div></div>
          </div>
        </div>
      </div>
    </div>
  )
}

function CampaignMemberCard({ member, sheet, isGm, onOpenSheet, onRemove }: {
  member: CampaignMember
  sheet?: CharacterSheet
  isGm: boolean
  onOpenSheet: (characterId: string, name: string) => Promise<void>
  onRemove: (characterId: string) => Promise<void>
}) {
  const woundRatio = sheet ? ratio(sheet.woundsCurrent, sheet.derived.woundThreshold) : 0
  const strainRatio = sheet ? ratio(sheet.strainCurrent, sheet.derived.strainThreshold) : 0
  const cardClass = woundRatio >= 0.9 ? 'campaign-pc-card crit' : woundRatio >= 0.7 || strainRatio >= 0.75 ? 'campaign-pc-card warn' : 'campaign-pc-card'
  return (
    <div className={cardClass}>
      <div className="campaign-pc-name">{member.characterName}{member.isMine && <span className="badge custom">мой</span>}</div>
      <div className="campaign-pc-role">{member.career} · {member.archetype}</div>
      {sheet ? (
        <div className="campaign-bars">
          <CampaignBar label="Раны" value={sheet.woundsCurrent} max={sheet.derived.woundThreshold} tone="wound" />
          <CampaignBar label="Стресс" value={sheet.strainCurrent} max={sheet.derived.strainThreshold} tone="strain" />
        </div>
      ) : (
        <div className="campaign-pc-fallback">{SYSTEM_LABELS[member.system]}</div>
      )}
      <div className="campaign-pc-foot">
        <span>Свободно XP: <b>{sheet?.availableXp ?? '—'}</b></span>
        <span className="campaign-pc-actions">
          {isGm && <button className="small" onClick={() => void onOpenSheet(member.characterId, member.characterName)}>Лист</button>}
          {(isGm || member.isMine) && <button className="danger small" onClick={() => void onRemove(member.characterId)}>Убрать</button>}
        </span>
      </div>
    </div>
  )
}

function CampaignBar({ label, value, max, tone }: { label: string; value: number; max: number; tone: 'wound' | 'strain' }) {
  return (
    <div className="campaign-bar-row">
      <div className="campaign-bar-meta"><span>{label}</span><b>{value}/{max}</b></div>
      <div className="campaign-bar-track"><div className={`campaign-bar-fill ${tone}`} style={{ width: `${Math.round(ratio(value, max) * 100)}%` }} /></div>
    </div>
  )
}

function CurrentSceneBlock({ session, sessionLoaded, onView }: {
  session: GameSession | null
  sessionLoaded: boolean
  onView: (view: CampaignView) => void
}) {
  const npcs = session?.participants.filter(p => p.participantType !== 'playerCharacter') ?? []
  return (
    <div className="campaign-dash-block">
      <div className="campaign-block-head">
        <h4>Текущая сцена</h4>
        <button className="small" onClick={() => onView('table')}>→ Открыть</button>
      </div>
      {!sessionLoaded ? (
        <p className="muted">Загрузка сцены…</p>
      ) : !session ? (
        <p className="muted">Активная сцена не запущена.</p>
      ) : (
        <>
          <div className="campaign-scene-sub">{session.name} · <span>Раунд {session.currentRound}</span></div>
          <div className="campaign-npc-list">
            {npcs.length === 0 && <p className="muted">НПС и угрозы ещё не добавлены.</p>}
            {npcs.slice(0, 4).map(p => <ParticipantMiniRow key={p.id} participant={p} />)}
          </div>
        </>
      )}
    </div>
  )
}

function ParticipantMiniRow({ participant }: { participant: GameSession['participants'][number] }) {
  const hpMax = participant.woundsThreshold || 1
  const hp = Math.max(0, hpMax - participant.woundsCurrent)
  const participantTypeLabel = participant.participantType === 'npc' ? 'НПС' : PARTICIPANT_TYPE_LABELS[participant.participantType]
  const label = participant.count > 1 ? `×${participant.count} группа` : participantTypeLabel
  return (
    <div className="campaign-npc-row">
      <div>
        <div className="campaign-npc-name">{participant.displayName}</div>
        <div className="campaign-npc-sub">{label}</div>
      </div>
      <div className="campaign-hp-row">
        <div className="campaign-hp-track"><div className="campaign-hp-fill" style={{ width: `${Math.round(ratio(hp, hpMax) * 100)}%` }} /></div>
        <span>{hp}/{hpMax}</span>
      </div>
      <span className="badge danger-badge">{participantTypeLabel}</span>
    </div>
  )
}

function StoryPointsBlock({ session, isGm, onSessionRun }: {
  session: GameSession | null
  isGm: boolean
  onSessionRun: (a: () => Promise<unknown>) => Promise<void>
}) {
  const player = session?.playerStoryPoints ?? 0
  const gm = session?.gmStoryPoints ?? 0
  const total = Math.max(6, player + gm)
  const set = (patch: { playerStoryPoints?: number; gmStoryPoints?: number }) =>
    session && onSessionRun(() => api.updateSession(session.campaignId, patch))

  return (
    <div className="campaign-dash-block">
      <h4>Сюжетные очки</h4>
      <div className="campaign-story-head">
        <div className="campaign-story-count"><b>{player}</b>игроки</div>
        <div className="campaign-pips">
          {Array.from({ length: total }, (_, i) => (
            <span key={i} className={i < player ? 'campaign-pip player' : i < player + gm ? 'campaign-pip gm' : 'campaign-pip empty'} />
          ))}
        </div>
        <div className="campaign-story-count right"><b>{gm}</b>мастер</div>
      </div>
      {session && isGm ? (
        <div className="campaign-story-actions">
          <button className="small" onClick={() => set({ playerStoryPoints: player + 1 })}>+ Игроки</button>
          <button className="small" onClick={() => set({ gmStoryPoints: gm + 1 })}>+ Мастер</button>
          <button className="small" disabled={player <= 0} onClick={() => set({ playerStoryPoints: player - 1 })}>− Игроки</button>
          <button className="small" disabled={gm <= 0} onClick={() => set({ gmStoryPoints: gm - 1 })}>− Мастер</button>
          <button className="small wide" disabled={player <= 0} onClick={() => set({ playerStoryPoints: player - 1, gmStoryPoints: gm + 1 })}>⇄ Мастеру</button>
        </div>
      ) : (
        <p className="muted">Сюжетные очки появятся после запуска сцены.</p>
      )}
    </div>
  )
}

function InitiativeBlock({ session, isGm, onView, onSessionRun }: {
  session: GameSession | null
  isGm: boolean
  onView: (view: CampaignView) => void
  onSessionRun: (a: () => Promise<unknown>) => Promise<void>
}) {
  const nameOf = (participantId: string | null) =>
    session?.participants.find(p => p.id === participantId)?.displayName ?? '— абстрактный —'
  return (
    <div className="campaign-dash-block">
      <h4>Инициатива</h4>
      {!session || session.slots.length === 0 ? (
        <p className="muted">Слотов инициативы пока нет.</p>
      ) : (
        <div className="campaign-init-list">
          {session.slots.slice(0, 5).map((slot, i) => (
            <div key={slot.id} className={i === session.currentTurnIndex ? 'campaign-init-slot current' : 'campaign-init-slot'}>
              <span className="campaign-init-num">{i + 1}</span>
              <span className="campaign-init-name">{nameOf(slot.assignedParticipantId)}</span>
              <span className={`badge slot-${slot.slotType}`}>{slot.slotType === 'npc' ? 'НПС' : SLOT_TYPE_LABELS[slot.slotType]}</span>
            </div>
          ))}
        </div>
      )}
      {session && isGm
        ? <button className="small campaign-init-next" onClick={() => onSessionRun(() => api.nextTurn(session.campaignId))}>→ Следующий ход</button>
        : <button className="small campaign-init-next" onClick={() => onView('table')}>Игровой стол</button>}
    </div>
  )
}

function ratio(value: number, max: number): number {
  if (!Number.isFinite(value) || !Number.isFinite(max) || max <= 0) return 0
  return Math.max(0, Math.min(1, value / max))
}

function CampaignNotesSection({ campaign, onRun, variant = 'panel' }: {
  campaign: CampaignDetail
  onRun: (a: () => Promise<unknown>) => Promise<void>
  variant?: 'panel' | 'dashboard'
}) {
  const [title, setTitle] = useState('')
  const [body, setBody] = useState('')
  const [isPrivate, setIsPrivate] = useState(true)

  const fmt = (iso: string) => new Date(iso).toLocaleString('ru-RU', { dateStyle: 'short', timeStyle: 'short' })

  return (
    <section className={variant === 'dashboard' ? 'campaign-dash-block campaign-notes-dash' : 'panel'}>
      <h3>Заметки кампании</h3>
      {!campaign.isGm && <p className="hint">Здесь видны только общие заметки мастера.</p>}
      {campaign.isGm && (
        <form className="custom-form" onSubmit={e => {
          e.preventDefault()
          void onRun(async () => { await api.createCampaignNote(campaign.id, { title, body, isPrivate }); setTitle(''); setBody('') })
        }}>
          <label>Заголовок<input value={title} onChange={e => setTitle(e.target.value)} required /></label>
          <label>Текст<textarea value={body} onChange={e => setBody(e.target.value)} rows={3} /></label>
          <label className="checkbox">
            <input type="checkbox" checked={isPrivate} onChange={e => setIsPrivate(e.target.checked)} />
            Приватная (видна только мастеру)
          </label>
          <button className="primary" type="submit">Добавить заметку</button>
        </form>
      )}
      <div className="notes-list">
        {campaign.notes.map(n => (
          <div key={n.id} className="note-card">
            <div className="note-card-head">
              <strong>{n.title}</strong>
              <span className="note-actions">
                {n.isPrivate
                  ? <span className="badge tier">приватная</span>
                  : <span className="badge custom">общая</span>}
                {campaign.isGm && (
                  <button className="danger small"
                    onClick={() => { if (confirm('Удалить заметку?')) void onRun(() => api.deleteCampaignNote(campaign.id, n.id)) }}>
                    Удалить
                  </button>
                )}
              </span>
            </div>
            {n.body && <p className="note-body">{n.body}</p>}
            <div className="muted small-text">обновлено {fmt(n.updatedAt)}</div>
          </div>
        ))}
        {campaign.notes.length === 0 && <p className="muted">Заметок пока нет.</p>}
      </div>
    </section>
  )
}
