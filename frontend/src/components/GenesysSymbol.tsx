import { GENESYS_GLYPHS, GENESYS_SYMBOL_LABELS, type GenesysSymbolKind } from '../data/genesysSymbols'
import { DIE_KINDS, type DieKind } from '../utils/diceRoller'

export function GenesysSymbol({ kind, className = '', decorative = false, title }: {
  kind: GenesysSymbolKind
  className?: string
  /** Use when the enclosing control already names this symbol. */
  decorative?: boolean
  /** Disable when the parent supplies a richer tooltip. */
  title?: string | false
}) {
  const die = DIE_KINDS.includes(kind as DieKind)
  return (
    <span className={`genesys-symbol genesys-${kind}${die ? ' genesys-die' : ''}${className ? ` ${className}` : ''}`}
      role={decorative ? undefined : 'img'} aria-hidden={decorative || undefined}
      aria-label={decorative ? undefined : GENESYS_SYMBOL_LABELS[kind]}
      title={decorative || title === false ? undefined : title ?? GENESYS_SYMBOL_LABELS[kind]}>
      {GENESYS_GLYPHS[kind]}
    </span>
  )
}
