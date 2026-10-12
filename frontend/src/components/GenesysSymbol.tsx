import { GENESYS_GLYPHS, GENESYS_SYMBOL_LABELS, type GenesysSymbolKind } from '../data/genesysSymbols'

export function GenesysSymbol({ kind, className = '', decorative = false }: {
  kind: GenesysSymbolKind
  className?: string
  /** Use when the enclosing control already names this symbol. */
  decorative?: boolean
}) {
  const die = ['ability', 'proficiency', 'difficulty', 'challenge', 'boost', 'setback'].includes(kind)
  return (
    <span className={`genesys-symbol genesys-${kind}${die ? ' genesys-die' : ''}${className ? ` ${className}` : ''}`}
      role={decorative ? undefined : 'img'} aria-hidden={decorative || undefined}
      aria-label={decorative ? undefined : GENESYS_SYMBOL_LABELS[kind]}
      title={GENESYS_SYMBOL_LABELS[kind]}>
      {GENESYS_GLYPHS[kind]}
    </span>
  )
}
