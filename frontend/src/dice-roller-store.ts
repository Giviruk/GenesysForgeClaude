import { createContext, useContext } from 'react'
import type { RollPool } from './utils/diceRoller'
import type { CombatQuality } from './utils/combat'
import type { RollLogRequest } from './components/DiceRoller'

export type DiceRollerRequest =
  | {
      kind: 'roll'
      title?: string
      label?: string
      initialPool?: Partial<RollPool>
      onLog?: (req: RollLogRequest) => void
      canSecret?: boolean
    }
  | {
      kind: 'combat'
      title: string
      skillLabel: string | null
      basePool: Partial<RollPool>
      damage: string
      brawn: number
      crit: string
      rangeBand: string
      qualities: CombatQuality[]
      onLog?: (req: RollLogRequest) => void
      canSecret?: boolean
    }

export interface DiceRollerContextValue {
  openRoller: (request?: DiceRollerRequest) => void
  closeRoller: () => void
}

export const DiceRollerContext = createContext<DiceRollerContextValue | null>(null)

const fallback: DiceRollerContextValue = {
  openRoller: () => {},
  closeRoller: () => {},
}

export function useDiceRoller() {
  return useContext(DiceRollerContext) ?? fallback
}
