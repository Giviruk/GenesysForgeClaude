import type { GameSystem } from '../api/types'
import { lang, t } from '../i18n'
import { localizedName, SYSTEM_LABELS } from './labels'

type NamedDefinition = { name: string; nameRu?: string }

export function localizedReferenceTerm(name: string, definitions: readonly NamedDefinition[] = []): string {
  const definition = definitions.find(value => value.name.toLocaleLowerCase() === name.toLocaleLowerCase())
  return definition ? localizedName(definition) : name
}

/** Audit text stays intact; only complete quoted catalogue names are translated. */
export function localizedAuditSummary(summary: string, definitions: readonly NamedDefinition[]): string {
  if (lang !== 'ru') return summary
  return summary.replace(/«([^»]+)»|"([^"\n]+)"/g, (quoted, ru: string | undefined, en: string | undefined) => {
    const name = ru ?? en ?? ''
    const translated = localizedReferenceTerm(name, definitions)
    return translated === name ? quoted : `«${translated}»`
  })
}

export function localizeHeroicMeta(value: string): string {
  if (/incidental/i.test(value) && /свободн/i.test(value)) return t('Свободное действие', 'Incidental')
  return value.replace(/incidental|duration|frequency/gi, token => ({
    incidental: t('Свободное', 'Incidental'), duration: t('Длительность', 'Duration'), frequency: t('Частота', 'Frequency'),
  })[token.toLowerCase()] ?? token)
}

export function displayPackName(name: string): string {
  return name.replace(/\b(RealmsOfTerrinoth|GenesysCore)\b/g, token => SYSTEM_LABELS[token === 'RealmsOfTerrinoth' ? 'realmsOfTerrinoth' : 'genesysCore'])
}

export function preferredLibrarySystem(entries: readonly { system: GameSystem }[], packs: readonly { system: GameSystem }[], saved: GameSystem | null): GameSystem {
  if (saved) return saved
  return (['genesysCore', 'realmsOfTerrinoth'] as const).find(system => entries.some(entry => entry.system === system) || packs.some(pack => pack.system === system)) ?? 'genesysCore'
}

export function criticalSeverityDifficulty(group: string): number {
  const normalized = group.toLocaleLowerCase()
  if (/daunting|сверхслож|очень слож/.test(normalized)) return 4
  if (/hard|слож/.test(normalized)) return 3
  if (/average|средн/.test(normalized)) return 2
  if (/easy|л[её]гк/.test(normalized)) return 1
  return 0
}
