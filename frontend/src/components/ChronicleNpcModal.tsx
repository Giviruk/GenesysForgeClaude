import { useEffect, useState } from 'react'
import { api } from '../api/client'
import type { Characteristic, NpcDetail } from '../api/types'
import { t } from '../i18n'
import { CHARACTERISTIC_LABELS, NPC_KIND_LABELS, NPC_ROLE_LABELS, SYSTEM_LABELS } from '../utils/labels'

interface Props {
  npcId: string
  onClose: () => void
}

/** Read-only карточка NPC по ссылке из хроники, не покидающая страницу кампании. */
export function ChronicleNpcModal({ npcId, onClose }: Props) {
  const [npc, setNpc] = useState<NpcDetail | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    api.npc(npcId).then(result => { if (!cancelled) setNpc(result) })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof Error ? err.message : t('Не удалось открыть NPC', 'Could not open NPC'))
      })
    return () => { cancelled = true }
  }, [npcId])

  useEffect(() => {
    const closeOnEscape = (event: KeyboardEvent) => { if (event.key === 'Escape') onClose() }
    document.addEventListener('keydown', closeOnEscape)
    return () => document.removeEventListener('keydown', closeOnEscape)
  }, [onClose])

  const characteristics: [Characteristic, number][] = npc ? [
    ['brawn', npc.brawn], ['agility', npc.agility], ['intellect', npc.intellect],
    ['cunning', npc.cunning], ['willpower', npc.willpower], ['presence', npc.presence],
  ] : []

  return <div className="modal-backdrop" role="presentation" onClick={onClose}>
    <div className="modal wide chronicle-npc-modal" role="dialog" aria-modal="true"
      aria-label={npc ? t(`Карточка NPC: ${npc.name}`, `NPC card: ${npc.name}`) : t('Карточка NPC', 'NPC card')}
      onClick={event => event.stopPropagation()}>
      <div className="modal-header">
        <div>
          <h3>{npc?.name ?? t('NPC', 'NPC')}</h3>
          {npc && <div className="muted small-text">
            {SYSTEM_LABELS[npc.system]} · {NPC_KIND_LABELS[npc.kind]} · {NPC_ROLE_LABELS[npc.role]}
          </div>}
        </div>
        <button type="button" onClick={onClose}>{t('Закрыть', 'Close')}</button>
      </div>
      {error && <div className="error">{error}</div>}
      {!npc && !error && <p className="muted">{t('Загрузка…', 'Loading…')}</p>}
      {npc && <div className="npc-card">
        {npc.description && <p className="npc-desc">{npc.description}</p>}
        <div className="npc-char-row">
          {characteristics.map(([key, value]) => <div className="npc-char" key={key}>
            <span className="npc-char-val">{value}</span>
            <span className="npc-char-label">{CHARACTERISTIC_LABELS[key]}</span>
          </div>)}
        </div>
        <div className="npc-derived">
          <span><b>{t('Поглощение', 'Soak')}</b> {npc.soak}</span>
          <span><b>{t('Раны', 'Wounds')}</b> {npc.woundThreshold}</span>
          {npc.strainThreshold != null && <span><b>{t('Усталость', 'Strain')}</b> {npc.strainThreshold}</span>}
          <span><b>{t('Бл. защита', 'Melee def.')}</b> {npc.meleeDefense}</span>
          <span><b>{t('Дал. защита', 'Ranged def.')}</b> {npc.rangedDefense}</span>
          <span><b>{t('Силуэт', 'Silhouette')}</b> {npc.silhouette}</span>
        </div>
        {npc.skills.length > 0 && <section className="npc-section">
          <h4>{t('Навыки', 'Skills')}</h4>
          <div className="chips">{npc.skills.map(skill =>
            <span className="chip" key={skill.name}>{skill.name}{npc.kind !== 'minion' ? ` ${skill.ranks}` : ''}</span>)}</div>
        </section>}
        {npc.attacks.length > 0 && <section className="npc-section">
          <h4>{t('Атаки', 'Attacks')}</h4>
          <ul className="npc-weapon-list">{npc.attacks.map((attack, index) => <li className="npc-weapon" key={`${attack.name}-${index}`}>
            <strong>{attack.name}</strong>
            <div className="npc-weapon-stats">
              {attack.skillName && <span>{attack.skillName}</span>}
              {attack.damage && <span>{t('Урон', 'Damage')} <b>{attack.damage}</b></span>}
              {attack.critical && <span>{t('Крит', 'Crit')} <b>{attack.critical}</b></span>}
              {attack.rangeBand && <span>{attack.rangeBand}</span>}
            </div>
            {attack.notes && <div className="muted small-text">{attack.notes}</div>}
          </li>)}</ul>
        </section>}
        {npc.abilities.length > 0 && <section className="npc-section"><h4>{t('Способности', 'Abilities')}</h4>
          <ul>{npc.abilities.map((ability, index) => <li key={index}><b>{ability.name}.</b> {ability.description}</li>)}</ul></section>}
        {npc.talents.length > 0 && <section className="npc-section"><h4>{t('Таланты', 'Talents')}</h4>
          <div className="chips">{npc.talents.map(talent => <span className="chip" key={talent}>{talent}</span>)}</div></section>}
        {npc.equipment.length > 0 && <section className="npc-section"><h4>{t('Снаряжение', 'Gear')}</h4>
          <div className="chips">{npc.equipment.map(item => <span className="chip" key={item}>{item}</span>)}</div></section>}
        {npc.tactics && <section className="npc-section"><h4>{t('Тактика', 'Tactics')}</h4><p>{npc.tactics}</p></section>}
        {npc.source && <div className="muted small-text npc-source">{t('Источник:', 'Source:')} {npc.source}</div>}
      </div>}
    </div>
  </div>
}
