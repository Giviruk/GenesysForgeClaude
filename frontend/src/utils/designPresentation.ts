import type { GameSystem } from '../api/types'
import { lang, t } from '../i18n'
import { localizedName, SYSTEM_LABELS } from './labels'

type NamedDefinition = { name: string; nameRu?: string }
export type ReferenceNameIndex = ReadonlyMap<string, string>

export function referenceNameIndex(definitions: readonly NamedDefinition[]): ReferenceNameIndex {
  const index = new Map<string, string>()
  for (const definition of definitions) {
    for (const name of [definition.name, definition.nameRu]) {
      if (name && !index.has(name.toLocaleLowerCase())) index.set(name.toLocaleLowerCase(), localizedName(definition))
    }
  }
  return index
}

export function localizedReferenceTerm(name: string, definitions: readonly NamedDefinition[] = []): string {
  const definition = definitions.find(value => value.name.toLocaleLowerCase() === name.toLocaleLowerCase())
  return definition ? localizedName(definition) : name
}

/** Audit text stays intact; only complete quoted catalogue names are translated. */
export function localizedAuditSummary(summary: string, names: ReferenceNameIndex): string {
  if (lang !== 'ru') return summary
  return summary.replace(/«([^»]+)»|"([^"\n]+)"/g, (quoted, ru: string | undefined, en: string | undefined) => {
    const name = ru ?? en ?? ''
    const translated = names.get(name.toLocaleLowerCase()) ?? name
    return translated === name ? quoted : `«${translated}»`
  })
}

export function heroicMetaParts(value: string): { value: string; explanation: string } {
  const [primary, ...explanation] = localizeHeroicMeta(value).split(/[;；]/)
  return { value: primary.trim(), explanation: explanation.join('; ').trim() }
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

/** Structured signature-weapon damage; retain compatibility with older sheet responses. */
export function signatureWeaponDamage(weapon: { damage: string; damageKind?: 'brawnPlus' | 'fixed' | null; damageValue?: number | null }): string {
  if (weapon.damageValue != null && weapon.damageKind)
    return weapon.damageKind === 'brawnPlus' ? `${t('Сила', 'Brawn')} +${weapon.damageValue}` : String(weapon.damageValue)
  return weapon.damage.replace(/^Brawn\s*\+\s*/i, `${t('Сила', 'Brawn')} +`)
}
