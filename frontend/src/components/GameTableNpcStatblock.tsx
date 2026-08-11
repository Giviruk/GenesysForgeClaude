import { useEffect, useMemo, useState } from 'react'
import { api } from '../api/client'
import type { Characteristic, GameParticipant, NpcDetail, Reference } from '../api/types'
import { useDiceRoller } from '../dice-roller-store'
import { t } from '../i18n'
import {
  CHARACTERISTIC_LABELS, NPC_KIND_LABELS, NPC_ROLE_LABELS, SYSTEM_LABELS,
} from '../utils/labels'
import { npcAttackViews, npcSkillViews, skillIndex } from '../utils/npcStats'
import { resolveQualityCosts } from '../utils/combat'
import { DicePoolView } from './DicePoolView'
import type { RollLogRequest } from './DiceRoller'

interface Props {
  participant: GameParticipant
  campaignId: string
  isGm: boolean
  onClose: () => void
}

/** Read-only статблок связанного NPC прямо поверх игрового стола. */
export function GameTableNpcStatblock({ participant, campaignId, isGm, onClose }: Props) {
  const [npc, setNpc] = useState<NpcDetail | null>(null)
  const [reference, setReference] = useState<Reference | null>(null)
  const [error, setError] = useState<string | null>(null)
  const { openRoller } = useDiceRoller()

  useEffect(() => {
    let cancelled = false
    if (!participant.npcId) return
    api.npc(participant.npcId)
      .then(detail => {
        if (cancelled) return
        setNpc(detail)
        return api.reference(detail.system).then(r => { if (!cancelled) setReference(r) })
      })
      .catch((e: unknown) => {
        if (!cancelled) setError(e instanceof Error ? e.message : t('Не удалось открыть статблок NPC.', 'Could not open the NPC stat block.'))
      })
    return () => { cancelled = true }
  }, [participant.npcId])

  const index = useMemo(() => skillIndex(reference), [reference])
  const skills = useMemo(
    () => npc ? npcSkillViews(npc, index, participant.count) : [],
    [npc, index, participant.count],
  )
  const attacks = useMemo(
    () => npc ? npcAttackViews(npc, reference, participant.count) : [],
    [npc, reference, participant.count],
  )

  const logAsNpc = (name: string) => (req: RollLogRequest) => {
    void api.createRoll(campaignId, { ...req, actorName: name })
  }

  const chars: [Characteristic, number][] = npc ? [
    ['brawn', npc.brawn], ['agility', npc.agility], ['intellect', npc.intellect],
    ['cunning', npc.cunning], ['willpower', npc.willpower], ['presence', npc.presence],
  ] : []

  return (
    <div className="modal-backdrop gt-npc-backdrop" role="presentation" onClick={onClose}>
      <div className="modal wide gt-npc-statblock" role="dialog" aria-modal="true"
        aria-label={t(`Статблок NPC: ${participant.displayName}`, `NPC stat block: ${participant.displayName}`)}
        onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <div>
            <h3>{participant.displayName}{participant.count > 1 ? ` ×${participant.count}` : ''}</h3>
            {npc?.kind === 'minion' && (
              <div className="muted small-text">
                {t(`Группа: эффективный ранг групповых навыков ${Math.max(0, participant.count - 1)}`,
                  `Group: effective group-skill rank ${Math.max(0, participant.count - 1)}`)}
              </div>
            )}
          </div>
          <button type="button" className="small" onClick={onClose}>{t('Закрыть', 'Close')}</button>
        </div>

        {error && <div className="error">{error}</div>}
        {!npc && !error && <p className="muted">{t('Загрузка статблока…', 'Loading stat block…')}</p>}

        {npc && (
          <div className="npc-card gt-npc-card">
            <div className="npc-card-head">
              <h3>{npc.name}</h3>
              <span className="muted">{SYSTEM_LABELS[npc.system]} · {NPC_KIND_LABELS[npc.kind]} · {NPC_ROLE_LABELS[npc.role]}</span>
            </div>
            {npc.description && <p className="npc-desc">{npc.description}</p>}

            <div className="npc-char-row">
              {chars.map(([characteristic, value]) => (
                <div key={characteristic} className="npc-char">
                  <span className="npc-char-val">{value}</span>
                  <span className="npc-char-label">{CHARACTERISTIC_LABELS[characteristic]}</span>
                </div>
              ))}
            </div>

            <div className="npc-derived">
              <span><b>{t('Поглощение', 'Soak')}</b> {npc.soak}</span>
              <span><b>{t('Раны', 'Wounds')}</b> {participant.woundsCurrent}/{participant.woundsThreshold}</span>
              {participant.strainThreshold != null && <span><b>{t('Усталость', 'Strain')}</b> {participant.strainCurrent}/{participant.strainThreshold}</span>}
              <span><b>{t('Бл. защита', 'Melee def.')}</b> {npc.meleeDefense}</span>
              <span><b>{t('Дал. защита', 'Ranged def.')}</b> {npc.rangedDefense}</span>
              <span><b>{t('Силуэт', 'Silhouette')}</b> {npc.silhouette}</span>
            </div>

            {skills.length > 0 && (
              <section className="npc-section">
                <h4>{npc.kind === 'minion' ? t('Групповые навыки', 'Group skills') : t('Навыки', 'Skills')}</h4>
                <ul className="npc-skill-list gt-npc-roll-list">
                  {skills.map(skill => (
                    <li key={skill.name} className="npc-skill-row">
                      <span className="npc-skill-name">
                        {skill.name} <span className="muted">{skill.ranks}</span>
                        {skill.characteristic && <span className="muted small-text"> · {CHARACTERISTIC_LABELS[skill.characteristic]}</span>}
                      </span>
                      {skill.pool ? <>
                        <DicePoolView pool={skill.pool} />
                        <button type="button" className="small" onClick={() => openRoller({
                          kind: 'roll',
                          title: `${participant.displayName} — ${skill.name}`,
                          label: skill.name,
                          initialPool: skill.pool ?? {},
                          onLog: logAsNpc(participant.displayName),
                          canSecret: isGm,
                        })}>{t('🎲 Бросить', '🎲 Roll')}</button>
                      </> : <span className="muted small-text">{t('пул не определён', 'pool undefined')}</span>}
                    </li>
                  ))}
                </ul>
              </section>
            )}

            {attacks.length > 0 && (
              <section className="npc-section">
                <h4>{t('Атаки', 'Attacks')}</h4>
                <ul className="npc-weapon-list gt-npc-roll-list">
                  {attacks.map((attack, i) => (
                    <li key={`${attack.name}-${i}`} className="npc-weapon">
                      <div className="npc-weapon-head">
                        <strong>{attack.name}</strong>
                        {attack.pool && <DicePoolView pool={attack.pool} />}
                        <button type="button" className="small" disabled={!attack.pool} onClick={() => openRoller({
                          kind: 'combat',
                          title: attack.name,
                          skillLabel: attack.skillLabel,
                          basePool: attack.pool ?? {},
                          damage: npc.attacks[i].damage,
                          brawn: npc.brawn,
                          crit: npc.attacks[i].critical,
                          rangeBand: npc.attacks[i].rangeBand,
                          qualities: resolveQualityCosts(
                            npc.attacks[i].qualities.map(q => ({ code: q.qualityCode, label: q.nameRu || q.qualityCode, rating: q.rating })),
                            reference,
                          ),
                          onLog: logAsNpc(participant.displayName),
                          canSecret: isGm,
                        })}>{t('🎲 Атаковать', '🎲 Attack')}</button>
                      </div>
                      <div className="npc-weapon-stats">
                        <span className="weapon-stat">{t('Урон', 'Damage')} <strong>{attack.damageText}</strong></span>
                        {attack.crit && <span className="weapon-stat">{t('Крит', 'Crit')} <strong>{attack.crit}</strong></span>}
                        {attack.rangeBand && <span className="weapon-stat">{attack.rangeBand}</span>}
                      </div>
                      {attack.qualities.length > 0 && <div className="chips small-text">{attack.qualities.map((q, j) => <span className="chip" key={j}>{q.label}</span>)}</div>}
                      {attack.notes && <div className="muted small-text">{attack.notes}</div>}
                    </li>
                  ))}
                </ul>
              </section>
            )}

            {npc.abilities.length > 0 && <section className="npc-section"><h4>{t('Способности', 'Abilities')}</h4><ul>{npc.abilities.map((a, i) => <li key={i}><b>{a.name}.</b> {a.description}</li>)}</ul></section>}
            {npc.talents.length > 0 && <section className="npc-section"><h4>{t('Таланты', 'Talents')}</h4><div className="chips">{npc.talents.map((talent, i) => <span key={i} className="chip">{talent}</span>)}</div></section>}
            {npc.equipment.length > 0 && <section className="npc-section"><h4>{t('Снаряжение', 'Gear')}</h4><div className="chips">{npc.equipment.map((item, i) => <span key={i} className="chip">{item}</span>)}</div></section>}
            {npc.tactics && <section className="npc-section"><h4>{t('Тактика', 'Tactics')}</h4><p className="npc-desc">{npc.tactics}</p></section>}
            {npc.source && <div className="muted small-text npc-source">{t('Источник:', 'Source:')} {npc.source}</div>}
          </div>
        )}
      </div>
    </div>
  )
}
