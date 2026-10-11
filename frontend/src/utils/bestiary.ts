import type { GameSystem, NpcInput, NpcKind, NpcListItem, NpcRole, Reference, TalentDef } from '../api/types'
import { NPC_KINDS, NPC_ROLES } from './labels'

export interface BestiaryFilters {
  search: string
  system: '' | GameSystem
  kinds: NpcKind[]
  role: '' | NpcRole
  source: '' | 'mine' | 'builtin'
  sort: 'createdAt' | 'name'
}
export const EMPTY_FILTERS: BestiaryFilters = { search: '', system: '', kinds: [], role: '', source: '', sort: 'createdAt' }
export function readBestiaryFilters(query: string): BestiaryFilters {
  const p = new URLSearchParams(query)
  const system = p.get('system')
  const role = p.get('role')
  const source = p.get('source')
  return {
    search: p.get('search') ?? '',
    system: system === 'genesysCore' || system === 'realmsOfTerrinoth' ? system : '',
    role: NPC_ROLES.includes(role as NpcRole) ? role as NpcRole : '',
    kinds: NPC_KINDS.filter(k => (p.get('kind') ?? '').split(',').includes(k)),
    source: source === 'mine' || source === 'builtin' ? source : '',
    sort: p.get('sort') === 'name' ? 'name' : 'createdAt',
  }
}
export function bestiaryQuery(filters: BestiaryFilters): string {
  const p = new URLSearchParams()
  if (filters.search) p.set('search', filters.search)
  if (filters.system) p.set('system', filters.system)
  if (filters.kinds.length) p.set('kind', filters.kinds.join(','))
  if (filters.role) p.set('role', filters.role)
  if (filters.source) p.set('source', filters.source)
  if (filters.sort !== 'createdAt') p.set('sort', filters.sort)
  return p.size ? `?${p}` : ''
}
export function filterBestiary(npcs: NpcListItem[], f: BestiaryFilters): NpcListItem[] {
  const search = f.search.trim().toLocaleLowerCase()
  return npcs.filter(n => (!search || n.name.toLocaleLowerCase().includes(search))
    && (!f.system || n.system === f.system) && (!f.role || n.role === f.role)
    && (!f.kinds.length || f.kinds.includes(n.kind))
    && (!f.source || (f.source === 'mine' ? n.isMine : n.isBuiltIn)))
    .sort((a, b) => f.sort === 'name' ? a.name.localeCompare(b.name) : b.createdAt.localeCompare(a.createdAt))
}
/** Suggested soak for the editor only. Never changes persisted NPCs on opening. */
export function suggestedNpcSoak(form: Pick<NpcInput, 'brawn' | 'equipment'>, reference: Reference | null): number {
  const gear = (reference?.items ?? []).filter(i => form.equipment.some(n => n === i.name || n === i.nameRu))
  // NPC gear has no active-armor selector. Suggest one suit (best soak), plus non-armor bonuses.
  const armor = Math.max(0, ...gear.filter(i => i.kind === 'armor').map(i => i.soakBonus))
  return form.brawn + armor + gear.filter(i => i.kind !== 'armor').reduce((total, item) => total + item.soakBonus, 0)
}
/** NPC strings can contain ranks and either reference language. */
export function findNpcTalent(label: string, talents: TalentDef[]): TalentDef | undefined {
  const normalized = label.trim().replace(/\s+\d+$/, '').toLocaleLowerCase()
  return talents.find(t => [t.name, t.nameRu].some(n => n.trim().toLocaleLowerCase() === normalized))
}
