import type { GameParticipant } from '../api/types'

export interface MinionGroupState {
  initialCount: number
  remainingCount: number
  defeatedCount: number
  perMemberWoundThreshold: number
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
