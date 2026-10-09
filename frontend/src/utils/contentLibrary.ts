import { CHARACTERISTIC_LABELS, SKILL_KIND_LABELS, ITEM_KIND_LABELS } from './labels'
import { t } from '../i18n'

/** Picker keys include the type: definition IDs are opaque, and book keys may repeat across categories. */
export const contentKey = (type: string, id: string) => `${type}:${id}`
export interface PickerRow { key: string; group: string; groupLabel: string; name: string; meta?: string; editedAt?: string | null }
export function filterPickerRows(rows: PickerRow[], group: string, search: string, sort: 'type' | 'name' | 'recent'): PickerRow[] {
  const q = search.trim().toLocaleLowerCase()
  return rows.filter(x => (group === 'all' || x.group === group) && (!q || `${x.name} ${x.meta ?? ''}`.toLocaleLowerCase().includes(q)))
    .sort((a, b) => (sort === 'type' ? a.groupLabel.localeCompare(b.groupLabel) : sort === 'recent' ? (b.editedAt ?? '').localeCompare(a.editedAt ?? '') : 0)
      || a.name.localeCompare(b.name) || a.key.localeCompare(b.key))
}
export function toggleSelection(previous: string[], keys: string[], selected: boolean): string[] {
  const next = new Set(previous)
  for (const key of keys) { if (selected) next.add(key); else next.delete(key) }
  return [...next]
}
export function vitalState(current: number, threshold: number): 'normal' | 'near' | 'threshold' | 'over' {
  return current > threshold ? 'over' : current === threshold ? 'threshold'
    : threshold - current <= Math.ceil(threshold * .25) ? 'near' : 'normal'
}
export function purchasedPool(characteristic: number, rank: number) {
  return { ability: Math.max(characteristic, rank) - Math.min(characteristic, rank), proficiency: Math.min(characteristic, rank) }
}

/** Metadata is structural; localize enum tokens without changing user-entered text. */
export function contentMeta(value: string): string {
  const tokens: Record<string,string> = { ...CHARACTERISTIC_LABELS, ...SKILL_KIND_LABELS, ...ITEM_KIND_LABELS, ranked:t('ранговый','ranked'), passive:t('Пассивный','Passive') }
  return value.split(' · ').map(token => tokens[token.toLowerCase()] ?? token).join(' · ')
}
