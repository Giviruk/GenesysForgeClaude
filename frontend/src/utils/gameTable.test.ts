import { describe, expect, it } from 'vitest'
import type { GameParticipant } from '../api/types'
import { effectiveParticipantCount, minionGroupState, participantNameWithCount, participantRollPool } from './gameTable'

const group = (woundsCurrent: number, patch: Partial<GameParticipant> = {}): GameParticipant => ({
  id: 'group', characterId: null, npcId: 'npc', displayName: 'Миньоны',
  participantType: 'minionGroup', initiativeSlotType: 'npc', count: 3,
  woundsCurrent, woundsThreshold: 12, strainCurrent: 0, strainThreshold: null,
  soak: 2, meleeDefense: 0, rangedDefense: 0, boostDice: 0, setbackDice: 0, criticalInjuries: 0,
  isActive: true, isDefeated: false, isHiddenFromPlayers: false, notes: '', order: 0,
  ...patch,
})

describe('participantRollPool — модификаторы участника', () => {
  it('добавляет бусты и сетбеки участника к уже собранному пулу', () => {
    expect(participantRollPool(
      { ability: 2, boost: 1, setback: 2 },
      { boostDice: 2, setbackDice: 1 },
    )).toEqual({ ability: 2, boost: 3, setback: 3 })
  })
})

describe('minionGroupState — потери группы от ран', () => {
  it.each([[4, 3], [5, 2], [8, 2], [9, 1], [12, 1], [13, 0]])(
    'при %i ранах оставляет %i миньонов для T=4, N=3',
    (wounds, remaining) => expect(minionGroupState(group(wounds))?.remainingCount).toBe(remaining),
  )

  it('считает помеченную поверженной группу полностью выбывшей', () => {
    expect(minionGroupState(group(0, { isDefeated: true }))?.remainingCount).toBe(0)
  })

  it('не угадывает индивидуальный порог неоднозначного legacy snapshot', () => {
    expect(minionGroupState(group(5, { woundsThreshold: 10 }))).toBeNull()
  })

  it('не меняет количество обычного NPC', () => {
    expect(effectiveParticipantCount(group(9, { participantType: 'npc', count: 1 }))).toBe(1)
  })

  it('заменяет сохранённый suffix исходного размера актуальным счётчиком', () => {
    expect(participantNameWithCount(group(9, { displayName: 'Гоблин ×3' }))).toBe('Гоблин ×1/3')
  })
})
