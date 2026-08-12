export const SHEET_TABS = [
  'sheet', 'talents', 'heroic', 'inventory', 'attachments', 'transport', 'crafting', 'magic',
  'bio', 'history', 'notes', 'custom',
] as const

export type CharacterSheetTab = typeof SHEET_TABS[number]

export const RANGE_ZONES = ['engaged', 'short', 'medium', 'long', 'extreme'] as const
export type RangeZone = typeof RANGE_ZONES[number]

export interface StoredRangeTrackerState {
  zones: Record<string, RangeZone>
  log: string[]
}

const sheetTabKey = (characterId: string) => `genesysforge.sheet-tab.${characterId}`
const rangeTrackerKey = (campaignId: string, sessionId: string) =>
  `genesysforge.game-table.range.${campaignId}.${sessionId}`

const isSheetTab = (value: unknown): value is CharacterSheetTab =>
  typeof value === 'string' && (SHEET_TABS as readonly string[]).includes(value)

const isRangeZone = (value: unknown): value is RangeZone =>
  typeof value === 'string' && (RANGE_ZONES as readonly string[]).includes(value)

export function readSheetTab(characterId: string): CharacterSheetTab {
  try {
    const value = localStorage.getItem(sheetTabKey(characterId))
    return isSheetTab(value) ? value : 'sheet'
  } catch {
    return 'sheet'
  }
}

export function writeSheetTab(characterId: string, tab: CharacterSheetTab): void {
  try { localStorage.setItem(sheetTabKey(characterId), tab) } catch { /* storage unavailable */ }
}

export function readRangeTrackerState(
  campaignId: string,
  sessionId: string,
): StoredRangeTrackerState {
  try {
    const parsed: unknown = JSON.parse(localStorage.getItem(rangeTrackerKey(campaignId, sessionId)) ?? 'null')
    if (!parsed || typeof parsed !== 'object') return { zones: {}, log: [] }
    const candidate = parsed as { zones?: unknown; log?: unknown }
    const zones: Record<string, RangeZone> = {}
    if (candidate.zones && typeof candidate.zones === 'object') {
      for (const [participantId, zone] of Object.entries(candidate.zones)) {
        if (isRangeZone(zone)) zones[participantId] = zone
      }
    }
    const log = Array.isArray(candidate.log)
      ? candidate.log.filter((entry): entry is string => typeof entry === 'string').slice(0, 20)
      : []
    return { zones, log }
  } catch {
    return { zones: {}, log: [] }
  }
}

export function writeRangeTrackerState(
  campaignId: string,
  sessionId: string,
  state: StoredRangeTrackerState,
): void {
  try { localStorage.setItem(rangeTrackerKey(campaignId, sessionId), JSON.stringify(state)) } catch { /* storage unavailable */ }
}
