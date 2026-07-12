import type { Characteristic, EncounterDetail, NpcDetail } from '../../api/types'
import {
  CHARACTERISTIC_LABELS, ENCOUNTER_TYPE_LABELS, NPC_KIND_LABELS, NPC_ROLE_LABELS,
  PARTICIPANT_TYPE_LABELS, THREAT_LEVEL_LABELS, difficultyLabel, magicSkillLabel,
} from '../../utils/labels'
import type { PrintVersion } from './PrintPreview'
import { t } from '../../i18n'
import type { MagicCardData } from './cards'

const CHARS: Characteristic[] = ['brawn', 'agility', 'intellect', 'cunning', 'willpower', 'presence']

/** Markdown-экспорт карточек (§6.1). Официальный контент — только safe summary + источник (§6.2). */
export function adversaryMarkdown(npc: NpcDetail, version: PrintVersion): string {
  const lines = [`# ${npc.name}`, `${NPC_KIND_LABELS[npc.kind]} · ${NPC_ROLE_LABELS[npc.role]}`]
  if (npc.description) lines.push('', npc.description)
  if (version === 'gm') {
    lines.push('', `${t('**Характеристики:**', '**Characteristics:**')} ${CHARS.map(c => `${CHARACTERISTIC_LABELS[c]} ${npc[c]}`).join(', ')}`)
    lines.push(t(`**Поглощение** ${npc.soak} · **Раны** ${npc.woundThreshold}${npc.strainThreshold != null ? ` · **Усталость** ${npc.strainThreshold}` : ''} · **Бл.защ** ${npc.meleeDefense} · **Дал.защ** ${npc.rangedDefense}${npc.silhouette !== 1 ? ` · **Силуэт** ${npc.silhouette}` : ''}`, `**Soak** ${npc.soak} · **Wounds** ${npc.woundThreshold}${npc.strainThreshold != null ? ` · **Strain** ${npc.strainThreshold}` : ''} · **M.Def** ${npc.meleeDefense} · **R.Def** ${npc.rangedDefense}${npc.silhouette !== 1 ? ` · **Silhouette** ${npc.silhouette}` : ''}`))
    if (npc.skills.length) lines.push(`${t('**Навыки:**', '**Skills:**')} ${npc.skills.map(s => `${s.name} ${s.ranks}`).join(', ')}`)
    if (npc.attacks.length) {
      lines.push('', t('**Атаки:**', '**Attacks:**'))
      for (const a of npc.attacks) {
        const stats = [
          a.skillName && t(`навык ${a.skillName}`, `skill ${a.skillName}`), a.damage && t(`Урон ${a.damage}`, `Damage ${a.damage}`),
          a.critical && t(`Крит ${a.critical}`, `Crit ${a.critical}`), a.rangeBand,
          ...a.qualities.map(q => (q.nameRu || q.qualityCode) + (q.rating != null ? ` ${q.rating}` : '')),
        ].filter(Boolean)
        lines.push(`- ${a.name}${stats.length ? ` (${stats.join(', ')})` : ''}`)
      }
    }
    if (npc.talents.length) lines.push(`${t('**Таланты:**', '**Talents:**')} ${npc.talents.join(', ')}`)
    if (npc.equipment.length) lines.push(`${t('**Снаряжение:**', '**Gear:**')} ${npc.equipment.join(', ')}`)
    if (npc.tactics) lines.push(`${t('**Тактика:**', '**Tactics:**')} ${npc.tactics}`)
  }
  if (npc.source) lines.push('', t(`_Источник: ${npc.source}_`, `_Source: ${npc.source}_`))
  return lines.join('\n')
}

export function encounterMarkdown(enc: EncounterDetail, version: PrintVersion): string {
  const gm = version === 'gm'
  const lines = [`# ${enc.name}`, `${ENCOUNTER_TYPE_LABELS[enc.type]} · ${THREAT_LEVEL_LABELS[enc.threatLevel]}`]
  if (gm && enc.gmDescription) lines.push('', `${t('**Для мастера:**', '**For the GM:**')} ${enc.gmDescription}`)
  if (enc.playerDescription) lines.push('', `${t('**Для игроков:**', '**For players:**')} ${enc.playerDescription}`)
  if (enc.playerGoals) lines.push(`${t('**Цели игроков:**', '**Player goals:**')} ${enc.playerGoals}`)
  if (gm && enc.npcGoals) lines.push(`${t('**Цели NPC:**', '**NPC goals:**')} ${enc.npcGoals}`)
  if (gm && enc.complications) lines.push(`${t('**Осложнения:**', '**Complications:**')} ${enc.complications}`)
  if (enc.rewards) lines.push(`${t('**Награды:**', '**Rewards:**')} ${enc.rewards}`)
  const ps = enc.participants.filter(p => gm || !p.startsHidden)
  if (ps.length) {
    lines.push('', t('**Участники:**', '**Participants:**'))
    for (const p of ps) lines.push(`- ${p.displayName}${p.quantity > 1 ? ` ×${p.quantity}` : ''} (${PARTICIPANT_TYPE_LABELS[p.participantType]})`)
  }
  return lines.join('\n')
}

export function magicMarkdown(d: MagicCardData): string {
  const lines = [
    t(`# ${d.baseEffectRu} (${d.baseEffectEn})`, `# ${d.baseEffectEn} (${d.baseEffectRu})`),
    `${magicSkillLabel(d.skill)} · ${t('Сложность', 'Difficulty')} ${d.totalDifficulty} (${difficultyLabel(d.totalDifficulty)})`,
  ]
  if (d.effects.length) {
    lines.push('', t('**Доп. эффекты:**', '**Additional effects:**'))
    for (const e of d.effects) lines.push(t(`- ${e.ru} (${e.en}) ${e.difficulty} — ${e.summary}`, `- ${e.en} (${e.ru}) ${e.difficulty} — ${e.summary}`))
  }
  if (d.sources.length) lines.push('', t(`_Источники: ${d.sources.join('; ')}_`, `_Sources: ${d.sources.join('; ')}_`))
  return lines.join('\n')
}
