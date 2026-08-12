import type { GameParticipant, GameSession, UpdateParticipantRequest } from '../api/types'
import type { RollPool } from './diceRoller'

export interface MinionGroupState {
  initialCount: number
  remainingCount: number
  defeatedCount: number
  perMemberWoundThreshold: number
}

/** Локальная проекция безопасной числовой команды до подтверждения сервера. */
export function applyParticipantPatch(
  session: GameSession, participantId: string, patch: UpdateParticipantRequest,
): GameSession {
  return {
    ...session,
    participants: session.participants.map(participant => {
      if (participant.id !== participantId) return participant
      const present = Object.fromEntries(
        Object.entries(patch).filter(([, value]) => value !== null && value !== undefined),
      ) as Partial<GameParticipant>
      const next: GameParticipant = { ...participant, ...present }
      if (next.participantType === 'minionGroup' && next.perMemberWoundThreshold && patch.woundsCurrent != null) {
        const defeated = Math.min(next.count,
          Math.floor(Math.max(0, next.woundsCurrent - 1) / next.perMemberWoundThreshold))
        next.remainingCount = next.isDefeated ? 0 : next.count - defeated
      }
      return next
    }),
  }
}

/**
 * Производное состояние группы миньонов в текущей модели Game Table.
 * Общий порог хранится как N × T. Миньон выбывает после превышения T:
 * при T=4 первая потеря происходит на пятой ране.
 */
export function minionGroupState(participant: GameParticipant): MinionGroupState | null {
  if (participant.participantType !== 'minionGroup' || participant.count < 1) return null
  if (participant.remainingCount != null && participant.perMemberWoundThreshold != null) {
    const remainingCount = Math.max(0, Math.min(participant.count, participant.remainingCount))
    return {
      initialCount: participant.count,
      remainingCount,
      defeatedCount: participant.count - remainingCount,
      perMemberWoundThreshold: participant.perMemberWoundThreshold,
    }
  }
  if (participant.woundsThreshold < 1 || participant.woundsThreshold % participant.count !== 0) return null

  const perMemberWoundThreshold = participant.woundsThreshold / participant.count
  const defeatedByWounds = Math.min(
    participant.count,
    Math.floor(Math.max(0, participant.woundsCurrent - 1) / perMemberWoundThreshold),
  )
  const defeatedCount = participant.isDefeated ? participant.count : defeatedByWounds

  return {
    initialCount: participant.count,
    remainingCount: participant.count - defeatedCount,
    defeatedCount,
    perMemberWoundThreshold,
  }
}

export function effectiveParticipantCount(participant: GameParticipant): number {
  return minionGroupState(participant)?.remainingCount ?? participant.count
}

/** Добавляет назначенные участнику модификаторы сцены к базовому пулу броска. */
export function participantRollPool(
  basePool: Partial<RollPool>,
  participant: Pick<GameParticipant, 'boostDice' | 'setbackDice'>,
): Partial<RollPool> {
  return {
    ...basePool,
    boost: Math.max(0, basePool.boost ?? 0) + Math.max(0, participant.boostDice),
    setback: Math.max(0, basePool.setback ?? 0) + Math.max(0, participant.setbackDice),
  }
}

/** Имя участника с актуальным счётчиком, без дублирования старого suffix `×N`. */
export function participantNameWithCount(participant: GameParticipant): string {
  const group = minionGroupState(participant)
  if (group) {
    const oldSuffix = ` ×${group.initialCount}`
    const baseName = participant.displayName.endsWith(oldSuffix)
      ? participant.displayName.slice(0, -oldSuffix.length)
      : participant.displayName
    return `${baseName} ×${group.remainingCount}/${group.initialCount}`
  }
  return participant.count > 1 ? `${participant.displayName} ×${participant.count}` : participant.displayName
}
