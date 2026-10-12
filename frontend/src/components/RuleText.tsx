import type { ReactNode } from 'react'
import { GenesysSymbol } from './GenesysSymbol'
import { GENESYS_SYMBOL_LABELS, type GenesysSymbolKind } from '../data/genesysSymbols'

type RuleTokenKind = GenesysSymbolKind

interface RuleTokenDefinition {
  pattern: string
  kind: RuleTokenKind
  /** Some legacy OCR fallbacks are only valid in their original case. */
  caseSensitive?: boolean
  symbol?: boolean
  standalone?: boolean
  legacyOcr?: boolean
}

interface RuleMatch extends RuleTokenDefinition {
  start: number
  end: number
}

const TOKEN_LABELS = GENESYS_SYMBOL_LABELS

// Long phrases must be considered before their shorter parts (e.g. «бонусные кости» before «кость»).
const WORD_TOKENS: RuleTokenDefinition[] = [
  ...['кости бонуса', 'кость бонуса', 'бонусные кости', 'бонусную кость', 'бонусная кость', 'бонусный кубик', 'бонусные кубики',
    'бонус-кубик', 'бонус-кубики', 'синие кубики', 'синих кубика', 'синий кубик',
    'синий куб', 'синих куба', 'boost dice', 'boost die', 'blue dice', 'blue die']
    .map(pattern => ({ pattern, kind: 'boost' as const })),
  ...['кости штрафа', 'кость штрафа', 'кости помехи', 'кость помехи', 'чёрные кубики', 'чёрных кубика', 'чёрный кубик',
    'черные кубики', 'черных кубика', 'черный кубик', 'setback dice', 'setback die',
    'black dice', 'black die']
    .map(pattern => ({ pattern, kind: 'setback' as const })),
  ...['успехов', 'успеха', 'успехом', 'успех', 'successes', 'success']
    .map(pattern => ({ pattern, kind: 'success' as const })),
  ...['провалов', 'провала', 'провалом', 'провал', 'failures', 'failure']
    .map(pattern => ({ pattern, kind: 'failure' as const })),
  ...['преимуществом', 'преимуществ', 'преимущества', 'преимущество', 'advantages', 'advantage']
    .map(pattern => ({ pattern, kind: 'advantage' as const })),
  ...['осложнениями', 'осложнением', 'осложнений', 'осложнения', 'осложнение',
    'угрозами', 'угрозу', 'угрозы', 'угроз', 'угроза', 'threats', 'threat']
    .map(pattern => ({ pattern, kind: 'threat' as const })),
  ...['триумфом', 'триумфа', 'триумфы', 'триумф', 'triumphs', 'triumph']
    .map(pattern => ({ pattern, kind: 'triumph' as const })),
  ...['крахом', 'краха', 'крахи', 'крах', 'отчаянием', 'отчаяния', 'отчаяние', 'despairs', 'despair']
    .map(pattern => ({ pattern, kind: 'despair' as const })),
]

// These are the symbols used by the dice roller. The asterisk/at-sign/Cyrillic A entries
// require explicit opt-in for known OCR text; free prose must retain these characters.
const SYMBOL_TOKENS: RuleTokenDefinition[] = [
  { pattern: '✶', kind: 'success' }, { pattern: '*', kind: 'success', standalone: true, legacyOcr: true },
  { pattern: '✸', kind: 'failure' },
  { pattern: '▲', kind: 'advantage' }, { pattern: 'А', kind: 'advantage', caseSensitive: true, legacyOcr: true },
  { pattern: '▼', kind: 'threat' },
  { pattern: '★', kind: 'triumph' }, { pattern: '@', kind: 'triumph', standalone: true, legacyOcr: true },
  { pattern: '☠', kind: 'despair' },
  { pattern: '◻', kind: 'boost' }, { pattern: '□', kind: 'boost' },
  { pattern: '■', kind: 'setback' },
  { pattern: '◆', kind: 'difficulty' }, { pattern: '♦', kind: 'difficulty' },
  { pattern: '⬣', kind: 'proficiency' },
  // Publisher PDFs also use supplementary private-use code points for results.
  { pattern: '\u{F32B1}', kind: 'threat' }, { pattern: '\u{F32B2}', kind: 'despair' },
  { pattern: '\u{F32B3}', kind: 'success' }, { pattern: '\u{F32B4}', kind: 'failure' },
  { pattern: '\u{F32B5}', kind: 'advantage' }, { pattern: '\u{F32B6}', kind: 'triumph' },
]

const ALL_TOKENS = [...SYMBOL_TOKENS.map(token => ({ ...token, symbol: true })), ...WORD_TOKENS]

/** Рендерит правила талантов с единым цветом костей и символов результата. */
export function RuleText({ text, symbolsOnly = false, wordsAsSymbols = false, legacyOcr = false }: {
  text: string; symbolsOnly?: boolean; wordsAsSymbols?: boolean; legacyOcr?: boolean
}) {
  if (!text) return null

  const matches = findMatches(text, symbolsOnly, legacyOcr)
  if (matches.length === 0) return <>{text}</>

  const parts: ReactNode[] = []
  let cursor = 0
  for (const match of matches) {
    if (match.start > cursor) parts.push(text.slice(cursor, match.start))
    const original = text.slice(match.start, match.end)
    parts.push(match.symbol || wordsAsSymbols ? (
      <GenesysSymbol key={`${match.start}-${match.end}-${match.kind}`} kind={match.kind}
        className={`rule-token rule-${match.kind}`} />
    ) : (
      <span key={`${match.start}-${match.end}-${match.kind}`} className={`rule-token rule-${match.kind}`}
        title={TOKEN_LABELS[match.kind]} aria-label={TOKEN_LABELS[match.kind]}>
        {original}
      </span>
    ))
    cursor = match.end
  }
  if (cursor < text.length) parts.push(text.slice(cursor))
  return <>{parts}</>
}

function findMatches(text: string, symbolsOnly: boolean, legacyOcr: boolean): RuleMatch[] {
  const lower = text.toLocaleLowerCase()
  const found: RuleMatch[] = []
  for (const token of ALL_TOKENS) {
    if (symbolsOnly && !token.symbol) continue
    if (token.legacyOcr && !legacyOcr) continue
    const haystack = token.caseSensitive ? text : lower
    const needle = token.caseSensitive ? token.pattern : token.pattern.toLocaleLowerCase()
    let from = 0
    while (from < haystack.length) {
      const start = haystack.indexOf(needle, from)
      if (start < 0) break
      const end = start + needle.length
      // Symbols may touch punctuation; words must be standalone so «успех» does not match «успешный».
      const isSymbol = token.symbol && !token.standalone && !isWordCharacter(needle)
      if (isSymbol || (!isWordCharacter(lower[start - 1]) && !isWordCharacter(lower[end]))) {
        found.push({ ...token, start, end })
      }
      from = end
    }
  }

  found.sort((a, b) => a.start - b.start || (b.end - b.start) - (a.end - a.start))
  const accepted: RuleMatch[] = []
  let end = -1
  for (const match of found) {
    if (match.start < end) continue
    accepted.push(match)
    end = match.end
  }
  return accepted
}

function isWordCharacter(value: string | undefined): boolean {
  return value != null && /[\p{L}\p{N}]/u.test(value)
}
