import type {
  Characteristic, EncounterDetail, NpcDetail, SheetItem, SheetTalent,
} from '../../api/types'
import {
  CHARACTERISTIC_LABELS, ENCOUNTER_TYPE_LABELS, ITEM_KIND_LABELS, NPC_KIND_LABELS, NPC_ROLE_LABELS,
  PARTICIPANT_TYPE_LABELS, SLOT_TYPE_LABELS, SYSTEM_LABELS, THREAT_LEVEL_LABELS, difficultyLabel,
  localizedDescription, localizedName, magicSkillLabel, secondaryName,
} from '../../utils/labels'
import type { PrintVersion } from './PrintPreview'
import { t } from '../../i18n'

const CHARS: Characteristic[] = ['brawn', 'agility', 'intellect', 'cunning', 'willpower', 'presence']

/** Однострочный статблок атаки NPC для печати: имя [навык], Урон/Крит/Дистанция, качества. */
function attackLine(a: NpcDetail['attacks'][number]): string {
  const stats = [
    a.skillName && t(`навык ${a.skillName}`, `skill ${a.skillName}`),
    a.damage && t(`Урон ${a.damage}`, `Damage ${a.damage}`),
    a.critical && t(`Крит ${a.critical}`, `Crit ${a.critical}`),
    a.rangeBand,
    ...a.qualities.map(q => (q.nameRu || q.qualityCode) + (q.rating != null ? ` ${q.rating}` : '')),
  ].filter(Boolean)
  return `${a.name}${stats.length ? ` (${stats.join(', ')})` : ''}`
}

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
            <span><b>{t('Поглощение', 'Soak')}</b> {npc.soak}</span>
            <span><b>{t('Раны', 'Wounds')}</b> {npc.woundThreshold}</span>
            {npc.strainThreshold != null && <span><b>{t('Усталость', 'Strain')}</b> {npc.strainThreshold}</span>}
            <span><b>{t('Бл.защ', 'M.Def')}</b> {npc.meleeDefense}</span>
            <span><b>{t('Дал.защ', 'R.Def')}</b> {npc.rangedDefense}</span>
            {npc.silhouette !== 1 && <span><b>{t('Силуэт', 'Silhouette')}</b> {npc.silhouette}</span>}
          </div>
          {npc.skills.length > 0 && <p><b>{t('Навыки:', 'Skills:')}</b> {npc.skills.map(s => `${s.name} ${s.ranks}`).join(' · ')}</p>}
          {npc.attacks.length > 0 && (
            <div>
              <b>{t('Атаки:', 'Attacks:')}</b>
              <ul>{npc.attacks.map((a, i) => <li key={i}>{attackLine(a)}</li>)}</ul>
            </div>
          )}
          {npc.abilities.length > 0 && (
            <div>
              <b>{t('Способности:', 'Abilities:')}</b>
              <ul>{npc.abilities.map((a, i) => <li key={i}>{a.name}{a.description && ` — ${a.description}`}</li>)}</ul>
            </div>
          )}
          {npc.talents.length > 0 && <p><b>{t('Таланты:', 'Talents:')}</b> {npc.talents.join(' · ')}</p>}
          {npc.equipment.length > 0 && <p><b>{t('Снаряжение:', 'Gear:')}</b> {npc.equipment.join(' · ')}</p>}
          {npc.tactics && <p><b>{t('Тактика:', 'Tactics:')}</b> {npc.tactics}</p>}
          {npc.tags.length > 0 && <p className="pcard-tags">{npc.tags.join(' · ')}</p>}
        </>
      ) : (
        <p className="pcard-muted">{t('Подробные характеристики скрыты мастером.', 'Detailed stats are hidden by the GM.')}</p>
      )}
      {npc.source && <footer className="pcard-src">{t('Источник:', 'Source:')} {npc.source}</footer>}
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
      {gm && enc.gmDescription && <p><b>{t('Для мастера:', 'For the GM:')}</b> {enc.gmDescription}</p>}
      {enc.playerDescription && <p><b>{t('Для игроков:', 'For players:')}</b> {enc.playerDescription}</p>}
      {enc.location && <p><b>{t('Локация:', 'Location:')}</b> {enc.location}{enc.environment && ` · ${enc.environment}`}</p>}
      {enc.playerGoals && <p><b>{t('Цели игроков:', 'Player goals:')}</b> {enc.playerGoals}</p>}
      {gm && enc.npcGoals && <p><b>{t('Цели NPC:', 'NPC goals:')}</b> {enc.npcGoals}</p>}
      {gm && enc.complications && <p><b>{t('Осложнения:', 'Complications:')}</b> {enc.complications}</p>}
      {enc.rewards && <p><b>{t('Награды:', 'Rewards:')}</b> {enc.rewards}</p>}
      {participants.length > 0 && (
        <div>
          <b>{t('Участники:', 'Participants:')}</b>
          <ul>
            {participants.map(p => (
              <li key={p.id}>
                {p.displayName}{p.quantity > 1 ? ` ×${p.quantity}` : ''} — {PARTICIPANT_TYPE_LABELS[p.participantType]}
                {gm && ` · ${SLOT_TYPE_LABELS[p.initiativeSide]}`}
                {gm && p.startsHidden && t(' · скрыт', ' · hidden')}
                {gm && t('   ⟶ Раны ___ / Усталость ___', '   ⟶ Wounds ___ / Strain ___')}
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
        <h3>{t(data.baseEffectRu, data.baseEffectEn)}</h3>
        <span className="pcard-sub">{magicSkillLabel(data.skill)} · {t(data.baseEffectEn, data.baseEffectRu)}</span>
      </header>
      <p className="pcard-diff">
        {t('Сложность:', 'Difficulty:')} <b>{data.totalDifficulty}</b> ({difficultyLabel(data.totalDifficulty)})
        {data.totalDifficulty !== data.baseDifficulty && <> {t('— базовая', '— base')} {data.baseDifficulty}</>}
      </p>
      {data.pool && <p>{t('Пул:', 'Pool:')} {data.pool.proficiency}⬣ + {data.pool.ability}◆</p>}
      {data.description && <p>{data.description}</p>}
      {data.effects.length > 0 && (
        <div>
          <b>{t('Дополнительные эффекты:', 'Additional effects:')}</b>
          <ul>{data.effects.map((e, i) => <li key={i}>{t(e.ru, e.en)} ({t(e.en, e.ru)}) {e.difficulty} — {e.summary}</li>)}</ul>
        </div>
      )}
      {data.sources.length > 0 && <footer className="pcard-src">{t('Источники:', 'Sources:')} {data.sources.join('; ')}</footer>}
    </article>
  )
}

/** Карточка предмета (§3.4). */
export function ItemCard({ item, skillLabel }: { item: SheetItem; skillLabel?: string | null }) {
  return (
    <article className="print-card">
      <header className="pcard-head">
        <h3>{localizedName(item)}</h3>
        <span className="pcard-sub">
          {secondaryName(item) && `${secondaryName(item)} · `}{ITEM_KIND_LABELS[item.kind]}
        </span>
      </header>
      <div className="pcard-line">
        <span><b>{t('Габариты', 'Encumbrance')}</b> {item.encumbrance}</span>
        {item.price > 0 && <span><b>{t('Цена', 'Price')}</b> {item.price}</span>}
      </div>
      {item.kind === 'weapon' && (
        <div className="pcard-line">
          {item.skillName && <span><b>{t('Навык', 'Skill')}</b> {skillLabel || item.skillName}</span>}
          {item.damage && <span><b>{t('Урон', 'Damage')}</b> {item.damage}</span>}
          {item.crit && <span><b>{t('Крит', 'Crit')}</b> {item.crit}</span>}
          {item.rangeBand && <span><b>{t('Дистанция', 'Range')}</b> {item.rangeBand}</span>}
        </div>
      )}
      {item.kind === 'armor' && (
        <div className="pcard-line">
          <span><b>{t('Поглощение', 'Soak')}</b> +{item.soakBonus}</span>
          <span><b>{t('Бл.защ', 'M.Def')}</b> +{item.meleeDefense}</span>
          <span><b>{t('Дал.защ', 'R.Def')}</b> +{item.rangedDefense}</span>
        </div>
      )}
      {item.properties && <p><b>{t('Свойства:', 'Properties:')}</b> {item.properties}</p>}
      {localizedDescription(item) && <p>{localizedDescription(item)}</p>}
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
          {t('Тир', 'Tier')} {talent.tier} · {talent.isRanked ? t(`ранговый (ранг ${talent.ranks})`, `ranked (rank ${talent.ranks})`) : t('неранговый', 'not ranked')}
          {talent.activation && ` · ${talent.activation}`}
        </span>
      </header>
      {localizedDescription(talent) && <p>{localizedDescription(talent)}</p>}
    </article>
  )
}
