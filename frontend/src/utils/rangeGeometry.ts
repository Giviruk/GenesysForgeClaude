import type { RangeZone } from './uiPreferences'

export const RANGE_CELL_COUNT = 12
export const RANGE_CELL_ANGLE = 360 / RANGE_CELL_COUNT
const RANGE_CELL_OFFSET = RANGE_CELL_ANGLE / 2

/** Центры кольцевых диапазонов в процентах от радиуса доски. */
export const RANGE_ZONE_RADII_PERCENT: Record<RangeZone, number> = {
  engaged: 4.9,
  short: 14.7,
  medium: 24.5,
  long: 34.3,
  extreme: 44.1,
}

const ZONE_BOUNDARIES_PERCENT = [9.8, 19.6, 29.4, 39.2]
const ZONES: RangeZone[] = ['engaged', 'short', 'medium', 'long', 'extreme']

export interface RangeCellPosition {
  zone: RangeZone
  angle: number
}

export interface EstimatedRange {
  zone: RangeZone
  /** Геометрическое расстояние в ширинах одного диапазона. */
  bandUnits: number
}

export interface RangeBoardPoint {
  zone: RangeZone
  angle: number
}

export function normalizeAngle(angle: number): number {
  return ((angle % 360) + 360) % 360
}

export function snapRangeAngle(angle: number): number {
  return normalizeAngle(
    Math.round((normalizeAngle(angle) - RANGE_CELL_OFFSET) / RANGE_CELL_ANGLE) * RANGE_CELL_ANGLE
      + RANGE_CELL_OFFSET,
  )
}

export function rangeZoneFromRadius(radiusPercent: number): RangeZone {
  const index = ZONE_BOUNDARIES_PERCENT.findIndex(boundary => radiusPercent < boundary)
  return ZONES[index < 0 ? ZONES.length - 1 : index]
}

/** Переводит координату указателя относительно доски в ближайшую кольцевую ячейку. */
export function rangeCellFromBoardPoint(
  clientX: number,
  clientY: number,
  board: { left: number; top: number; width: number; height: number },
): RangeBoardPoint {
  const x = clientX - board.left - board.width / 2
  const y = clientY - board.top - board.height / 2
  const boardRadius = Math.min(board.width, board.height) / 2
  const radiusPercent = boardRadius > 0 ? Math.hypot(x, y) / boardRadius * 100 : 0
  return {
    zone: rangeZoneFromRadius(radiusPercent),
    angle: snapRangeAngle(Math.atan2(y, x) * 180 / Math.PI),
  }
}

/**
 * Выбирает ближайший свободный угловой сектор в нужном кольце. При заполненном кольце
 * возвращает запрошенный сектор: это лучше, чем самовольно переносить участника в другой диапазон.
 */
export function nearestFreeRangeAngle(
  desiredAngle: number,
  zone: RangeZone,
  occupied: RangeCellPosition[],
): number {
  const desired = snapRangeAngle(desiredAngle)
  const used = new Set(occupied
    .filter(position => position.zone === zone)
    .map(position => snapRangeAngle(position.angle)))
  for (let distance = 0; distance <= RANGE_CELL_COUNT / 2; distance += 1) {
    const candidates = distance === 0 ? [desired] : [desired + distance * RANGE_CELL_ANGLE, desired - distance * RANGE_CELL_ANGLE]
    const available = candidates.map(snapRangeAngle).find(candidate => !used.has(candidate))
    if (available !== undefined) return available
  }
  return desired
}

function point(zone: RangeZone, angle: number): { x: number; y: number } {
  // Делим проценты на ширину кольца: соседние радиальные диапазоны отстоят на одну единицу.
  const radius = RANGE_ZONE_RADII_PERCENT[zone] / 9.8
  const radians = normalizeAngle(angle) * Math.PI / 180
  return { x: Math.cos(radians) * radius, y: Math.sin(radians) * radius }
}

export function estimateRangeBetween(from: RangeCellPosition, to: RangeCellPosition): EstimatedRange {
  const a = point(from.zone, from.angle)
  const b = point(to.zone, to.angle)
  const bandUnits = Math.hypot(a.x - b.x, a.y - b.y)
  // Те же границы, что у колец доски: одна ширина кольца = «Вплотную», две = «Ближняя» и т. д.
  // Пограничное значение относится к следующему диапазону; допуск убирает ошибки float
  // (например, разность центров 3.5 − 1.5 должна быть ровно 2).
  const epsilon = 1e-9
  const zone: RangeZone = bandUnits < 1 - epsilon ? 'engaged'
    : bandUnits < 2 - epsilon ? 'short'
      : bandUnits < 3 - epsilon ? 'medium'
        : bandUnits < 4 - epsilon ? 'long'
          : 'extreme'
  return { zone, bandUnits }
}
