import type { DieKind, DieSymbol } from '../utils/diceRoller'
import { t } from '../i18n'
import { CORE_RU } from './coreTerminology'
import { countLabel, type CountForms } from '../utils/countLabel'

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

const COUNT_FORMS: Record<GenesysSymbolKind, CountForms> = t({
  ability: ['кость способности', 'кости способности', 'костей способности'],
  proficiency: ['кость мастерства', 'кости мастерства', 'костей мастерства'],
  difficulty: ['кость сложности', 'кости сложности', 'костей сложности'],
  challenge: ['кость вызова', 'кости вызова', 'костей вызова'],
  boost: ['кость бонуса', 'кости бонуса', 'костей бонуса'],
  setback: ['кость штрафа', 'кости штрафа', 'костей штрафа'],
  success: ['успех', 'успеха', 'успехов'], failure: ['провал', 'провала', 'провалов'],
  advantage: ['преимущество', 'преимущества', 'преимуществ'], threat: ['осложнение', 'осложнения', 'осложнений'],
  triumph: ['триумф', 'триумфа', 'триумфов'], despair: ['крах', 'краха', 'крахов'],
}, {
  ability: ['ability die', 'ability dice', 'ability dice'],
  proficiency: ['proficiency die', 'proficiency dice', 'proficiency dice'],
  difficulty: ['difficulty die', 'difficulty dice', 'difficulty dice'],
  challenge: ['challenge die', 'challenge dice', 'challenge dice'],
  boost: ['boost die', 'boost dice', 'boost dice'], setback: ['setback die', 'setback dice', 'setback dice'],
  success: ['success', 'successes', 'successes'], failure: ['failure', 'failures', 'failures'],
  advantage: ['advantage', 'advantages', 'advantages'], threat: ['threat', 'threats', 'threats'],
  triumph: ['triumph', 'triumphs', 'triumphs'], despair: ['despair', 'despairs', 'despairs'],
})

export const genesysCountLabel = (count: number, kind: GenesysSymbolKind) => countLabel(count, COUNT_FORMS[kind])
