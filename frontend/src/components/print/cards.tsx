import type {
  Characteristic, EncounterDetail, NpcDetail, SheetItem, SheetTalent,
} from '../../api/types'
import {
  CHARACTERISTIC_LABELS, ENCOUNTER_TYPE_LABELS, ITEM_KIND_LABELS, NPC_KIND_LABELS, NPC_ROLE_LABELS,
  PARTICIPANT_TYPE_LABELS, SLOT_TYPE_LABELS, SYSTEM_LABELS, THREAT_LEVEL_LABELS, difficultyLabel,
  magicSkillLabel,
} from '../../utils/labels'
import type { PrintVersion } from './PrintPreview'

const CHARS: Characteristic[] = ['brawn', 'agility', 'intellect', 'cunning', 'willpower', 'presence']

/** Карточка NPC (§3.1). Версия игрока скрывает stats/заметки/способности. */
export function AdversaryCard({ npc, version }: { npc: NpcDetail; version: PrintVersion }) {
  const gm = version === 'gm'
  return (
    <article className="print-card">
      <header className="pcard-head">
        <h3>{npc.name}</h3>
        <span className="pcard-sub">{SYSTEM_LABELS[npc.system]} · {NPC_KIND_LABELS[npc.kind]} · {NPC_ROLE_LABELS[npc.role]}</span>
      </header>
      {npc.description && <p>{npc.description}</p>}
      {gm ? (
        <>
          <div className="pcard-chars">
            {CHARS.map(c => <span key={c}><b>{npc[c]}</b> {CHARACTERISTIC_LABELS[c]}</span>)}
          </div>
          <div className="pcard-line">
            <span><b>Soak</b> {npc.soak}</span>
            <span><b>Раны</b> {npc.woundThreshold}</span>
            {npc.strainThreshold != null && <span><b>Стрейн</b> {npc.strainThreshold}</span>}
            <span><b>Бл.защ</b> {npc.meleeDefense}</span>
            <span><b>Дал.защ</b> {npc.rangedDefense}</span>
          </div>
          {npc.skills.length > 0 && <p><b>Навыки:</b> {npc.skills.map(s => `${s.name} ${s.ranks}`).join(' · ')}</p>}
          {npc.abilities.length > 0 && (
            <div>
              <b>Способности:</b>
              <ul>{npc.abilities.map((a, i) => <li key={i}>{a.name}{a.description && ` — ${a.description}`}</li>)}</ul>
            </div>
          )}
          {npc.talents.length > 0 && <p><b>Таланты:</b> {npc.talents.join(' · ')}</p>}
          {npc.equipment.length > 0 && <p><b>Снаряжение:</b> {npc.equipment.join(' · ')}</p>}
          {npc.tags.length > 0 && <p className="pcard-tags">{npc.tags.join(' · ')}</p>}
        </>
      ) : (
        <p className="pcard-muted">Подробные характеристики скрыты мастером.</p>
      )}
      {npc.source && <footer className="pcard-src">Источник: {npc.source}</footer>}
    </article>
  )
}

/** Лист энкаунтера (§3.2). Версия игрока — только публичная часть. */
export function EncounterSheet({ enc, version }: { enc: EncounterDetail; version: PrintVersion }) {
  const gm = version === 'gm'
  const participants = enc.participants.filter(p => gm || !p.startsHidden)
  return (
    <article className="print-card wide">
      <header className="pcard-head">
        <h3>{enc.name}</h3>
        <span className="pcard-sub">{ENCOUNTER_TYPE_LABELS[enc.type]} · {THREAT_LEVEL_LABELS[enc.threatLevel]}</span>
      </header>
      {gm && enc.gmDescription && <p><b>Для мастера:</b> {enc.gmDescription}</p>}
      {enc.playerDescription && <p><b>Для игроков:</b> {enc.playerDescription}</p>}
      {enc.location && <p><b>Локация:</b> {enc.location}{enc.environment && ` · ${enc.environment}`}</p>}
      {enc.playerGoals && <p><b>Цели игроков:</b> {enc.playerGoals}</p>}
      {gm && enc.npcGoals && <p><b>Цели NPC:</b> {enc.npcGoals}</p>}
      {gm && enc.complications && <p><b>Осложнения:</b> {enc.complications}</p>}
      {enc.rewards && <p><b>Награды:</b> {enc.rewards}</p>}
      {participants.length > 0 && (
        <div>
          <b>Участники:</b>
          <ul>
            {participants.map(p => (
              <li key={p.id}>
                {p.displayName}{p.quantity > 1 ? ` ×${p.quantity}` : ''} — {PARTICIPANT_TYPE_LABELS[p.participantType]}
                {gm && ` · ${SLOT_TYPE_LABELS[p.initiativeSide]}`}
                {gm && p.startsHidden && ' · скрыт'}
                {gm && '   ⟶ Раны ___ / Стрейн ___'}
              </li>
            ))}
          </ul>
        </div>
      )}
      {enc.tags.length > 0 && <p className="pcard-tags">{enc.tags.join(' · ')}</p>}
    </article>
  )
}

export interface MagicCardData {
  skill: string
  baseEffectRu: string
  baseEffectEn: string
  baseDifficulty: number
  totalDifficulty: number
  effects: { ru: string; en: string; difficulty: string; summary: string }[]
  description: string
  sources: string[]
  pool?: { ability: number; proficiency: number } | null
}

/** Карточка магического действия (§3.3). */
export function MagicActionCard({ data }: { data: MagicCardData }) {
  return (
    <article className="print-card">
      <header className="pcard-head">
        <h3>{data.baseEffectRu}</h3>
        <span className="pcard-sub">{magicSkillLabel(data.skill)} · {data.baseEffectEn}</span>
      </header>
      <p className="pcard-diff">
        Сложность: <b>{data.totalDifficulty}</b> ({difficultyLabel(data.totalDifficulty)})
        {data.totalDifficulty !== data.baseDifficulty && <> — базовая {data.baseDifficulty}</>}
      </p>
      {data.pool && <p>Пул: {data.pool.proficiency}⬣ + {data.pool.ability}◆</p>}
      {data.description && <p>{data.description}</p>}
      {data.effects.length > 0 && (
        <div>
          <b>Дополнительные эффекты:</b>
          <ul>{data.effects.map((e, i) => <li key={i}>{e.ru} ({e.en}) {e.difficulty} — {e.summary}</li>)}</ul>
        </div>
      )}
      {data.sources.length > 0 && <footer className="pcard-src">Источники: {data.sources.join('; ')}</footer>}
    </article>
  )
}

/** Карточка предмета (§3.4). */
export function ItemCard({ item }: { item: SheetItem }) {
  return (
    <article className="print-card">
      <header className="pcard-head">
        <h3>{item.name}</h3>
        <span className="pcard-sub">{ITEM_KIND_LABELS[item.kind]}</span>
      </header>
      <div className="pcard-line">
        <span><b>Габариты</b> {item.encumbrance}</span>
        {item.price > 0 && <span><b>Цена</b> {item.price}</span>}
      </div>
      {item.kind === 'weapon' && (
        <div className="pcard-line">
          {item.skillName && <span><b>Навык</b> {item.skillName}</span>}
          {item.damage && <span><b>Урон</b> {item.damage}</span>}
          {item.crit && <span><b>Крит</b> {item.crit}</span>}
          {item.rangeBand && <span><b>Дистанция</b> {item.rangeBand}</span>}
        </div>
      )}
      {item.kind === 'armor' && (
        <div className="pcard-line">
          <span><b>Soak</b> +{item.soakBonus}</span>
          <span><b>Бл.защ</b> +{item.meleeDefense}</span>
          <span><b>Дал.защ</b> +{item.rangedDefense}</span>
        </div>
      )}
      {item.properties && <p><b>Свойства:</b> {item.properties}</p>}
      {item.description && <p>{item.description}</p>}
    </article>
  )
}

/** Карточка таланта (§3.5). */
export function TalentCard({ talent }: { talent: SheetTalent }) {
  return (
    <article className="print-card">
      <header className="pcard-head">
        <h3>{talent.name}</h3>
        <span className="pcard-sub">
          Тир {talent.tier} · {talent.isRanked ? `ранговый (ранг ${talent.ranks})` : 'неранговый'}
          {talent.activation && ` · ${talent.activation}`}
        </span>
      </header>
      {talent.description && <p>{talent.description}</p>}
    </article>
  )
}
