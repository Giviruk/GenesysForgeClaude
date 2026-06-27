import type { Characteristic, EncounterDetail, NpcDetail } from '../../api/types'
import {
  CHARACTERISTIC_LABELS, ENCOUNTER_TYPE_LABELS, NPC_KIND_LABELS, NPC_ROLE_LABELS,
  PARTICIPANT_TYPE_LABELS, THREAT_LEVEL_LABELS, difficultyLabel, magicSkillLabel,
} from '../../utils/labels'
import type { PrintVersion } from './PrintPreview'
import type { MagicCardData } from './cards'

const CHARS: Characteristic[] = ['brawn', 'agility', 'intellect', 'cunning', 'willpower', 'presence']

/** Markdown-экспорт карточек (§6.1). Официальный контент — только safe summary + источник (§6.2). */
export function adversaryMarkdown(npc: NpcDetail, version: PrintVersion): string {
  const lines = [`# ${npc.name}`, `${NPC_KIND_LABELS[npc.kind]} · ${NPC_ROLE_LABELS[npc.role]}`]
  if (npc.description) lines.push('', npc.description)
  if (version === 'gm') {
    lines.push('', `**Характеристики:** ${CHARS.map(c => `${CHARACTERISTIC_LABELS[c]} ${npc[c]}`).join(', ')}`)
    lines.push(`**Soak** ${npc.soak} · **Раны** ${npc.woundThreshold}${npc.strainThreshold != null ? ` · **Стрейн** ${npc.strainThreshold}` : ''} · **Бл.защ** ${npc.meleeDefense} · **Дал.защ** ${npc.rangedDefense}${npc.silhouette !== 1 ? ` · **Силуэт** ${npc.silhouette}` : ''}`)
    if (npc.skills.length) lines.push(`**Навыки:** ${npc.skills.map(s => `${s.name} ${s.ranks}`).join(', ')}`)
    if (npc.attacks.length) {
      lines.push('', '**Атаки:**')
      for (const a of npc.attacks) {
        const stats = [
          a.skillName && `навык ${a.skillName}`, a.damage && `Урон ${a.damage}`,
          a.critical && `Крит ${a.critical}`, a.rangeBand,
          ...a.qualities.map(q => (q.nameRu || q.qualityCode) + (q.rating != null ? ` ${q.rating}` : '')),
        ].filter(Boolean)
        lines.push(`- ${a.name}${stats.length ? ` (${stats.join(', ')})` : ''}`)
      }
    }
    if (npc.talents.length) lines.push(`**Таланты:** ${npc.talents.join(', ')}`)
    if (npc.equipment.length) lines.push(`**Снаряжение:** ${npc.equipment.join(', ')}`)
    if (npc.tactics) lines.push(`**Тактика:** ${npc.tactics}`)
  }
  if (npc.source) lines.push('', `_Источник: ${npc.source}_`)
  return lines.join('\n')
}

export function encounterMarkdown(enc: EncounterDetail, version: PrintVersion): string {
  const gm = version === 'gm'
  const lines = [`# ${enc.name}`, `${ENCOUNTER_TYPE_LABELS[enc.type]} · ${THREAT_LEVEL_LABELS[enc.threatLevel]}`]
  if (gm && enc.gmDescription) lines.push('', `**Для мастера:** ${enc.gmDescription}`)
  if (enc.playerDescription) lines.push('', `**Для игроков:** ${enc.playerDescription}`)
  if (enc.playerGoals) lines.push(`**Цели игроков:** ${enc.playerGoals}`)
  if (gm && enc.npcGoals) lines.push(`**Цели NPC:** ${enc.npcGoals}`)
  if (gm && enc.complications) lines.push(`**Осложнения:** ${enc.complications}`)
  if (enc.rewards) lines.push(`**Награды:** ${enc.rewards}`)
  const ps = enc.participants.filter(p => gm || !p.startsHidden)
  if (ps.length) {
    lines.push('', '**Участники:**')
    for (const p of ps) lines.push(`- ${p.displayName}${p.quantity > 1 ? ` ×${p.quantity}` : ''} (${PARTICIPANT_TYPE_LABELS[p.participantType]})`)
  }
  return lines.join('\n')
}

export function magicMarkdown(d: MagicCardData): string {
  const lines = [
    `# ${d.baseEffectRu} (${d.baseEffectEn})`,
    `${magicSkillLabel(d.skill)} · Сложность ${d.totalDifficulty} (${difficultyLabel(d.totalDifficulty)})`,
  ]
  if (d.effects.length) {
    lines.push('', '**Доп. эффекты:**')
    for (const e of d.effects) lines.push(`- ${e.ru} (${e.en}) ${e.difficulty} — ${e.summary}`)
  }
  if (d.sources.length) lines.push('', `_Источники: ${d.sources.join('; ')}_`)
  return lines.join('\n')
}
