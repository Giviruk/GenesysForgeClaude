import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type { CampaignDetail, CampaignListItem, CharacterListItem } from '../api/types'
import { SYSTEM_LABELS } from '../utils/labels'
import { GameTableTab } from '../components/GameTableTab'
import { EncountersTab } from '../components/EncountersTab'
import { HandbookTab } from '../components/HandbookTab'
import { useCampaignHub, type CampaignHubStatus } from '../useCampaignHub'

interface Props {
  openId: string | null
  onOpen: (id: string) => void
  onBack: () => void
}

export function CampaignsPage({ openId, onOpen, onBack }: Props) {
  const [campaigns, setCampaigns] = useState<CampaignListItem[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  const reload = useCallback(
    () => api.campaigns().then(setCampaigns).catch((e: unknown) =>
      setError(e instanceof Error ? e.message : 'Ошибка загрузки')),
    [])

  // Перезагружаем список при показе (в т.ч. при возврате из карточки кампании).
  useEffect(() => { if (!openId) void reload() }, [openId, reload])

  if (openId) return <CampaignDetailView campaignId={openId} onBack={onBack} />

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

function CampaignDetailView({ campaignId, onBack }: { campaignId: string; onBack: () => void }) {
  const [c, setC] = useState<CampaignDetail | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [tab, setTab] = useState<'overview' | 'handbook' | 'encounters' | 'table'>('overview')
  // Счётчик realtime-инвалидаций: меняется на событиях хаба, вкладки перечитывают данные.
  const [liveSignal, setLiveSignal] = useState(0)
  const [hubStatus, setHubStatus] = useState<CampaignHubStatus>('connecting')

  const reload = useCallback(
    () => api.campaign(campaignId).then(setC).catch((e: unknown) =>
      setError(e instanceof Error ? e.message : 'Ошибка загрузки')),
    [campaignId])
  useEffect(() => { void reload() }, [reload])

  // Подписка на события кампании на время открытой карточки.
  useCampaignHub(campaignId, {
    onGameTableChanged: () => setLiveSignal(v => v + 1),
    onCampaignChanged: () => { setLiveSignal(v => v + 1); void reload() },
    onStatus: setHubStatus,
  })

  async function run(action: () => Promise<unknown>) {
    try { await action(); await reload() }
    catch (err) { setError(err instanceof Error ? err.message : 'Ошибка') }
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
        <button className={tab === 'overview' ? 'tab active' : 'tab'} onClick={() => setTab('overview')}>Обзор</button>
        <button className={tab === 'handbook' ? 'tab active' : 'tab'} onClick={() => setTab('handbook')}>Handbook</button>
        <button className={tab === 'encounters' ? 'tab active' : 'tab'} onClick={() => setTab('encounters')}>Энкаунтеры</button>
        <button className={tab === 'table' ? 'tab active' : 'tab'} onClick={() => setTab('table')}>Game Table</button>
      </div>

      {tab === 'table' ? (
        <GameTableTab campaignId={c.id} isGm={c.isGm} members={c.members} refreshSignal={liveSignal} />
      ) : tab === 'handbook' ? (
        <HandbookTab campaignId={c.id} isGm={c.isGm} />
      ) : tab === 'encounters' ? (
        <EncountersTab campaignId={c.id} isGm={c.isGm} members={c.members} onSentToTable={() => setTab('table')} />
      ) : (
        <>
          {c.description && <p className="muted">{c.description}</p>}

          {c.isGm && c.joinCode && (
            <div className="panel">
              <strong>Код присоединения:</strong> <code className="join-code">{c.joinCode}</code>
              <span className="hint"> — передайте игрокам, чтобы они добавили своих персонажей.</span>
            </div>
          )}

          <section className="panel">
            <h3>Персонажи кампании ({c.members.length})</h3>
            {c.members.length === 0 && <p className="muted">Пока никто не присоединился.</p>}
            <table className="skills">
              <tbody>
                {c.members.map(m => (
                  <tr key={m.characterId}>
                    <td><strong>{m.characterName}</strong>{m.isMine && <span className="badge custom">мой</span>}</td>
                    <td className="muted">{SYSTEM_LABELS[m.system]} · {m.archetype} · {m.career}</td>
                    <td className="right">
                      {(c.isGm || m.isMine) && (
                        <button className="danger small"
                          onClick={() => run(() => api.removeCampaignCharacter(c.id, m.characterId))}>Убрать</button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </section>

          <CampaignNotesSection campaign={c} onRun={run} />
        </>
      )}
    </div>
  )
}

function CampaignNotesSection({ campaign, onRun }: { campaign: CampaignDetail; onRun: (a: () => Promise<unknown>) => Promise<void> }) {
  const [title, setTitle] = useState('')
  const [body, setBody] = useState('')
  const [isPrivate, setIsPrivate] = useState(true)

  const fmt = (iso: string) => new Date(iso).toLocaleString('ru-RU', { dateStyle: 'short', timeStyle: 'short' })

  return (
    <section className="panel">
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
