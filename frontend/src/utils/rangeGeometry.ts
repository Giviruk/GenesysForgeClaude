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

export function estimateRangeBetween(from: RangeCellPosition, to: RangeCellPosition): EstimatedRange {
  // Кольца — игровые категории, а не физические окружности разного масштаба. Поэтому радиальная
  // часть равна числу диапазонов между кольцами, а одинаковый угловой разнос должен весить
  // одинаково и на ближнем, и на предельном кольце. Хорда единичной окружности даёт углу вес
  // от 0 (один сектор) до 2 (противоположные стороны), не раздувая внешние кольца.
  const radialUnits = Math.abs(ZONES.indexOf(from.zone) - ZONES.indexOf(to.zone))
  const angleDelta = Math.abs(normalizeAngle(from.angle) - normalizeAngle(to.angle))
  const shortestAngle = Math.min(angleDelta, 360 - angleDelta)
  const angularUnits = 2 * Math.sin(shortestAngle * Math.PI / 360)
  const bandUnits = Math.hypot(radialUnits, angularUnits)

  // Границы проходят посередине между целыми шагами дистанций. Так чистое перемещение на одно
  // кольцо остаётся ближней дистанцией, на два — средней, а погрешность float не сдвигает ступень.
  const zone: RangeZone = bandUnits < 0.75 ? 'engaged'
    : bandUnits < 1.5 ? 'short'
      : bandUnits < 2.5 ? 'medium'
        : bandUnits < 3.5 ? 'long'
          : 'extreme'
  return { zone, bandUnits }
}
