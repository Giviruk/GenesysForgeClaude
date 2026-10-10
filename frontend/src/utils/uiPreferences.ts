import type { RangeZone } from '../api/types'

export const SHEET_TABS = [
  'sheet', 'talents', 'inventory', 'magic', 'heroic', 'workshop', 'transport', 'bio', 'history',
] as const

export type CharacterSheetTab = typeof SHEET_TABS[number]

export const RANGE_ZONES = ['engaged', 'short', 'medium', 'long', 'extreme'] as const
export type { RangeZone } from '../api/types'

export interface StoredRangeTrackerState {
  zones: Record<string, RangeZone>
  log: string[]
  /** Угол токена на кольце в градусах; позволяет хранить фланг относительно фокуса. */
  angles: Record<string, number>
  /** Участник, от которого сейчас показаны относительные дистанции. */
  focusParticipantId: string | null
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
    if (value === 'attachments' || value === 'crafting') {
      writeWorkshopMode(characterId, value === 'crafting' ? 'craft' : 'upgrades')
      writeSheetTab(characterId, 'workshop')
      return 'workshop'
    }
    if (value === 'notes') {
      writeSheetTab(characterId, 'bio')
      return 'bio'
    }
    return isSheetTab(value) ? value : 'sheet'
  } catch {
    return 'sheet'
  }
}

export function writeSheetTab(characterId: string, tab: CharacterSheetTab): void {
  try { localStorage.setItem(sheetTabKey(characterId), tab) } catch { /* storage unavailable */ }
}

export type WorkshopMode = 'upgrades' | 'craft'
export function readWorkshopMode(characterId: string): WorkshopMode {
  try { return localStorage.getItem(`genesysforge.workshop-mode.${characterId}`) === 'craft' ? 'craft' : 'upgrades' }
  catch { return 'upgrades' }
}
export function writeWorkshopMode(characterId: string, mode: WorkshopMode): void {
  try { localStorage.setItem(`genesysforge.workshop-mode.${characterId}`, mode) } catch { /* storage unavailable */ }
}
export function readHeroicUses(characterId: string): number {
  try {
    const value = Number(localStorage.getItem(`genesysforge.heroic-uses.${characterId}`))
    return Number.isSafeInteger(value) && value >= 0 ? value : 0
  } catch { return 0 }
}
export function writeHeroicUses(characterId: string, used: number): void {
  try { localStorage.setItem(`genesysforge.heroic-uses.${characterId}`, String(Number.isFinite(used) ? Math.max(0, Math.trunc(used)) : 0)) } catch { /* storage unavailable */ }
}

export function readRangeTrackerState(
  campaignId: string,
  sessionId: string,
): StoredRangeTrackerState {
  try {
    const parsed: unknown = JSON.parse(localStorage.getItem(rangeTrackerKey(campaignId, sessionId)) ?? 'null')
    if (!parsed || typeof parsed !== 'object') return { zones: {}, log: [], angles: {}, focusParticipantId: null }
    const candidate = parsed as { zones?: unknown; log?: unknown; angles?: unknown; focusParticipantId?: unknown }
    const zones: Record<string, RangeZone> = {}
    if (candidate.zones && typeof candidate.zones === 'object') {
      for (const [participantId, zone] of Object.entries(candidate.zones)) {
        if (isRangeZone(zone)) zones[participantId] = zone
      }
    }
    const log = Array.isArray(candidate.log)
      ? candidate.log.filter((entry): entry is string => typeof entry === 'string').slice(0, 20)
      : []
    const angles: Record<string, number> = {}
    if (candidate.angles && typeof candidate.angles === 'object') {
      for (const [participantId, angle] of Object.entries(candidate.angles)) {
        if (typeof angle === 'number' && Number.isFinite(angle)) {
          angles[participantId] = ((angle % 360) + 360) % 360
        }
      }
    }
    const focusParticipantId = typeof candidate.focusParticipantId === 'string'
      ? candidate.focusParticipantId
      : null
    return { zones, log, angles, focusParticipantId }
  } catch {
    return { zones: {}, log: [], angles: {}, focusParticipantId: null }
  }
}

export function writeRangeTrackerState(
  campaignId: string,
  sessionId: string,
  state: StoredRangeTrackerState,
): void {
  try { localStorage.setItem(rangeTrackerKey(campaignId, sessionId), JSON.stringify(state)) } catch { /* storage unavailable */ }
}

const skillProgressKey = (characterId: string) => `genesysforge.skillProgress.${characterId}`

export function readSkillProgress(characterId: string, creation: boolean): boolean {
  try {
    const saved = localStorage.getItem(skillProgressKey(characterId))
    return saved === 'true' ? true : saved === 'false' ? false : creation
  } catch { return creation }
}

export function writeSkillProgress(characterId: string, enabled: boolean): void {
  try { localStorage.setItem(skillProgressKey(characterId), String(enabled)) } catch { /* storage unavailable */ }
}
