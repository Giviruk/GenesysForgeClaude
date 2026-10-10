import type { Spell } from '../api/types'

/** Keep the first definition as fallback, preferring the active school's own data. */
export function magicActions(spells: readonly Spell[], activeSkill: string): Spell[] {
  const actions = new Map<string, Spell>()
  for (const spell of spells) {
    if (spell.kind !== 'effect') continue
    const current = actions.get(spell.nameEn)
    if (!current || (spell.magicSkill === activeSkill && current.magicSkill !== activeSkill)) {
      actions.set(spell.nameEn, spell)
    }
  }
  return [...actions.values()]
}
