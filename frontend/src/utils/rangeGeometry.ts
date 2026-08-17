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
  /** Непрерывная позиция указателя на доске; не защёлкивается в центр ячейки. */
  leftPercent: number
  topPercent: number
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
  // CSS и RANGE_ZONE_RADII_PERCENT задают позиции в процентах полной ширины доски. Раньше здесь
  // использовался процент от радиуса, поэтому значение было вдвое больше и после drop жетон
  // прыгал в центр ячейки примерно в два раза дальше от курсора.
  const boardDiameter = Math.min(board.width, board.height)
  const radiusPercent = boardDiameter > 0 ? Math.hypot(x, y) / boardDiameter * 100 : 0
  return {
    zone: rangeZoneFromRadius(radiusPercent),
    angle: snapRangeAngle(Math.atan2(y, x) * 180 / Math.PI),
    leftPercent: board.width > 0 ? (clientX - board.left) / board.width * 100 : 50,
    topPercent: board.height > 0 ? (clientY - board.top) / board.height * 100 : 50,
  }
}

/** Сектор — область сцены: несколько участников могут занимать его одновременно. */
export function nearestFreeRangeAngle(
  desiredAngle: number,
  zone: RangeZone,
  occupied: RangeCellPosition[],
): number {
  void zone
  void occupied
  return snapRangeAngle(desiredAngle)
}

export function estimateRangeBetween(from: RangeCellPosition, to: RangeCellPosition): EstimatedRange {
  const fromIndex = ZONES.indexOf(from.zone)
  const toIndex = ZONES.indexOf(to.zone)
  const low = Math.min(fromIndex, toIndex)
  const high = Math.max(fromIndex, toIndex)
  // Стоимость границ в манёврах: два внешних перехода требуют по два манёвра.
  const boundaryCosts = [1, 1, 2, 2]
  let bandUnits = boundaryCosts.slice(low, high).reduce((sum, cost) => sum + cost, 0)

  // Центральная зона не имеет направления относительно внешних колец. Между двумя
  // центральными точками сектор важен: вплотную они только в одном секторе.
  if ((from.zone !== 'engaged' && to.zone !== 'engaged') || from.zone === to.zone) {
    const fromSector = Math.round((snapRangeAngle(from.angle) - RANGE_CELL_OFFSET) / RANGE_CELL_ANGLE)
    const toSector = Math.round((snapRangeAngle(to.angle) - RANGE_CELL_OFFSET) / RANGE_CELL_ANGLE)
    const sectorDelta = Math.abs(fromSector - toSector)
    const shortestSectorDelta = Math.min(sectorDelta, RANGE_CELL_COUNT - sectorDelta)
    bandUnits += Math.ceil(shortestSectorDelta / 3)
  }

  const zone: RangeZone = bandUnits === 0 ? 'engaged'
    : bandUnits === 1 ? 'short'
      : bandUnits <= 3 ? 'medium'
        : bandUnits <= 5 ? 'long'
          : 'extreme'
  return { zone, bandUnits }
}
