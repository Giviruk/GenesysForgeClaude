import type { DieKind, DieSymbol } from '../utils/diceRoller'
import { t } from '../i18n'
import { CORE_RU } from './coreTerminology'

export type GenesysSymbolKind = DieKind | DieSymbol

// Character layout supplied by Genesys Glyphs and Dice.
export const GENESYS_GLYPHS: Record<GenesysSymbolKind, string> = {
  ability: 'k', proficiency: 'l', difficulty: 'k', challenge: 'l', boost: 'j', setback: 'j',
  success: 's', failure: 'f', advantage: 'a', threat: 'h', triumph: 't', despair: 'd',
}

export const GENESYS_SYMBOL_LABELS: Record<GenesysSymbolKind, string> = t({
  ...CORE_RU.dice, ...CORE_RU.results,
}, {
  ability: 'Ability die', proficiency: 'Proficiency die',
  difficulty: 'Difficulty die', challenge: 'Challenge die',
  boost: 'Boost die', setback: 'Setback die',
  success: 'Success', failure: 'Failure', advantage: 'Advantage',
  threat: 'Threat', triumph: 'Triumph', despair: 'Despair',
})
