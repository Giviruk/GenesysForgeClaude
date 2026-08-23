import type { ReactNode } from 'react'
import { t } from '../i18n'

type RuleTokenKind = 'boost' | 'setback' | 'success' | 'failure' | 'advantage' | 'threat' | 'triumph' | 'despair' | 'difficulty'

interface RuleTokenDefinition {
  pattern: string
  kind: RuleTokenKind
  display?: string
  /** Some legacy OCR fallbacks are only valid in their original case. */
  caseSensitive?: boolean
}

interface RuleMatch extends RuleTokenDefinition {
  start: number
  end: number
}

const TOKEN_LABELS: Record<RuleTokenKind, string> = t({
  boost: 'Бонусная кость',
  setback: 'Кость помехи',
  success: 'Успех',
  failure: 'Провал',
  advantage: 'Преимущество',
  threat: 'Угроза',
  triumph: 'Триумф',
  despair: 'Отчаяние',
  difficulty: 'Кость сложности',
}, {
  boost: 'Boost die',
  setback: 'Setback die',
  success: 'Success',
  failure: 'Failure',
  advantage: 'Advantage',
  threat: 'Threat',
  triumph: 'Triumph',
  despair: 'Despair',
  difficulty: 'Difficulty die',
})

// Long phrases must be considered before their shorter parts (e.g. «бонусные кости» before «кость»).
const WORD_TOKENS: RuleTokenDefinition[] = [
  ...['бонусные кости', 'бонусную кость', 'бонусная кость', 'бонусный кубик', 'бонусные кубики',
    'бонус-кубик', 'бонус-кубики', 'синие кубики', 'синих кубика', 'синий кубик',
    'синий куб', 'синих куба', 'boost dice', 'boost die', 'blue dice', 'blue die']
    .map(pattern => ({ pattern, kind: 'boost' as const })),
  ...['кости помехи', 'кость помехи', 'чёрные кубики', 'чёрных кубика', 'чёрный кубик',
    'черные кубики', 'черных кубика', 'черный кубик', 'setback dice', 'setback die',
    'black dice', 'black die']
    .map(pattern => ({ pattern, kind: 'setback' as const })),
  ...['успехов', 'успеха', 'успехом', 'успех', 'successes', 'success']
    .map(pattern => ({ pattern, kind: 'success' as const })),
  ...['провалов', 'провала', 'провалом', 'провал', 'failures', 'failure']
    .map(pattern => ({ pattern, kind: 'failure' as const })),
  ...['преимуществом', 'преимуществ', 'преимущества', 'преимущество', 'advantages', 'advantage']
    .map(pattern => ({ pattern, kind: 'advantage' as const })),
  ...['угрозами', 'угрозу', 'угрозы', 'угроз', 'угроза', 'threats', 'threat']
    .map(pattern => ({ pattern, kind: 'threat' as const })),
  ...['триумфом', 'триумфа', 'триумфы', 'триумф', 'triumphs', 'triumph']
    .map(pattern => ({ pattern, kind: 'triumph' as const })),
  ...['отчаянием', 'отчаяния', 'отчаяние', 'despairs', 'despair']
    .map(pattern => ({ pattern, kind: 'despair' as const })),
]

// These are the symbols used by the dice roller. The asterisk/at-sign/Cyrillic A entries
// are kept as fallbacks for older custom descriptions; built-in catalog data uses the glyphs.
const SYMBOL_TOKENS: RuleTokenDefinition[] = [
  { pattern: '✶', kind: 'success' }, { pattern: '*', kind: 'success', display: '✶' },
  { pattern: '✸', kind: 'failure' },
  { pattern: '▲', kind: 'advantage' }, { pattern: 'А', kind: 'advantage', display: '▲', caseSensitive: true },
  { pattern: '▼', kind: 'threat' },
  { pattern: '★', kind: 'triumph' }, { pattern: '@', kind: 'triumph', display: '★' },
  { pattern: '☠', kind: 'despair' },
  { pattern: '◻', kind: 'boost' }, { pattern: '□', kind: 'boost' },
  { pattern: '■', kind: 'setback' },
  { pattern: '◆', kind: 'difficulty' }, { pattern: '♦', kind: 'difficulty' },
]

const ALL_TOKENS = [...SYMBOL_TOKENS, ...WORD_TOKENS]

/** Рендерит правила талантов с единым цветом костей и символов результата. */
export function RuleText({ text }: { text: string }) {
  if (!text) return null

  const matches = findMatches(text)
  if (matches.length === 0) return <>{text}</>

  const parts: ReactNode[] = []
  let cursor = 0
  for (const match of matches) {
    if (match.start > cursor) parts.push(text.slice(cursor, match.start))
    const display = match.display ?? text.slice(match.start, match.end)
    parts.push(
      <span key={`${match.start}-${match.end}-${match.kind}`} className={`rule-token rule-${match.kind}`}
        title={TOKEN_LABELS[match.kind]} aria-label={TOKEN_LABELS[match.kind]}>
        {display}
      </span>,
    )
    cursor = match.end
  }
  if (cursor < text.length) parts.push(text.slice(cursor))
  return <>{parts}</>
}

function findMatches(text: string): RuleMatch[] {
  const lower = text.toLocaleLowerCase()
  const found: RuleMatch[] = []
  for (const token of ALL_TOKENS) {
    const haystack = token.caseSensitive ? text : lower
    const needle = token.caseSensitive ? token.pattern : token.pattern.toLocaleLowerCase()
    let from = 0
    while (from < haystack.length) {
      const start = haystack.indexOf(needle, from)
      if (start < 0) break
      const end = start + needle.length
      // Symbols may touch punctuation; words must be standalone so «успех» does not match «успешный».
      const isSymbol = needle.length === 1 && !isWordCharacter(needle)
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
