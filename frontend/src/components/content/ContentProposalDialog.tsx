import { PROPOSAL_LABELS } from './contentLabels'
import { useEffect, useState } from 'react'
import { api } from '../../api/client'
import type { CampaignDetail, GameSystem, LibraryProposal, ContentEntryRef } from '../../api/types'
import { t } from '../../i18n'
import { DialogFrame } from './ContentUi'


export function ContentProposalDialog({ system, name, proposal, proposals, onClose, refresh }: {
  system: GameSystem; name: string; proposal: { packId?: string } & Partial<ContentEntryRef>
  proposals: LibraryProposal[]; onClose: () => void; refresh: () => Promise<void>
}) {
  const [campaigns, setCampaigns] = useState<CampaignDetail[] | null>(null)
  const [selected, setSelected] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const kind = proposal.packId ? 'pack' : 'item'
  const targetId = proposal.packId ?? proposal.entryId!
  const statuses = proposals.filter(x => x.kind === kind && x.targetId === targetId)
  useEffect(() => {
    let cancelled = false
    api.campaigns().then(async list => {
      const details = await Promise.all(list.filter(x => !x.isGm).map(x => api.campaign(x.id)))
      if (!cancelled) setCampaigns(details.filter(x => !x.closedSystems?.includes(system)))
    }).catch(err => { if (!cancelled) setError(err instanceof Error ? err.message : t('Ошибка', 'Error')) })
    return () => { cancelled = true }
  }, [system])
  async function run(action: () => Promise<void>) {
    setBusy(true); setError(null)
    try { await action(); await refresh() }
    catch (err) { setError(err instanceof Error ? err.message : t('Ошибка', 'Error')) }
    finally { setBusy(false) }
  }
  return <DialogFrame title={t('Предложить мастеру', 'Propose to GM')} onClose={onClose} busy={busy} footer={<>
    <button disabled={busy} onClick={onClose}>{t('Закрыть', 'Close')}</button>
    <button className="primary" disabled={busy || !selected || statuses.some(x => x.campaignId === selected && x.status !== 'declined')}
      onClick={() => void run(() => api.proposeCampaignContent(selected, proposal))}>{t('Предложить', 'Propose')}</button>
  </>}>
    <p>{name}</p><p className="hint">{t('Мастер подключит оригинальный контент после одобрения. Изменения автора будут доступны кампании.', 'The GM connects the original content after approval. Author edits remain available to the campaign.')}</p>
    {error && <p className="error" role="alert">{error}</p>}
    {!campaigns && <p>{t('Загрузка…', 'Loading…')}</p>}
    {campaigns?.length === 0 && <p className="rd-empty">{t('Нет кампаний с открытой системой, где вы участвуете как игрок.', 'No campaigns with this system open where you are a player.')}</p>}
    {!!campaigns?.length && <label>{t('Кампания', 'Campaign')}<select value={selected} onChange={e => setSelected(e.target.value)} disabled={busy}>
      <option value="">{t('Выберите кампанию', 'Choose a campaign')}</option>
      {campaigns.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}
    </select></label>}
    {statuses.map(x => <div key={x.campaignId} className="rd-content-row"><span>{x.campaignName} · <b>{PROPOSAL_LABELS[x.status]}</b></span>
      {x.status === 'pending' && <button disabled={busy} onClick={() => void run(() => api.withdrawCampaignContent(x.campaignId, kind, targetId))}>{t('Отозвать', 'Withdraw')}</button>}
    </div>)}
  </DialogFrame>
}
