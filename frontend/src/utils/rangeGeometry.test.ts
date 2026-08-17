import { describe, expect, it } from 'vitest'
import {
  estimateRangeBetween, nearestFreeRangeAngle, RANGE_ZONE_RADII_PERCENT, rangeCellFromBoardPoint, rangeZoneFromRadius,
  snapRangeAngle,
} from './rangeGeometry'
import type { RangeZone } from './uiPreferences'

describe('range tracker geometry', () => {
  it('snaps angles to one of twelve stable cells', () => {
    expect(snapRangeAngle(14)).toBe(15)
    expect(snapRangeAngle(31)).toBe(45)
    expect(snapRangeAngle(-31)).toBe(315)
  })

  it('places drops inside the five range bands', () => {
    expect(rangeZoneFromRadius(4)).toBe('engaged')
    expect(rangeZoneFromRadius(12)).toBe('short')
    expect(rangeZoneFromRadius(24)).toBe('medium')
    expect(rangeZoneFromRadius(35)).toBe('long')
    expect(rangeZoneFromRadius(47)).toBe('extreme')
  })

  it('converts pointer coordinates to the intended ring cell', () => {
    const board = { left: 100, top: 50, width: 400, height: 400 }
    expect(rangeCellFromBoardPoint(300, 50 + 200 - 30, board)).toEqual({
      zone: 'engaged', angle: 285, leftPercent: 50, topPercent: 42.5,
    })
    expect(rangeCellFromBoardPoint(300 + 50, 250, board)).toEqual({
      zone: 'short', angle: 15, leftPercent: 62.5, topPercent: 50,
    })
    expect(rangeCellFromBoardPoint(300 - 190, 250, board)).toEqual({
      zone: 'extreme', angle: 195, leftPercent: 2.5, topPercent: 50,
    })
  })

  it.each(Object.entries(RANGE_ZONE_RADII_PERCENT) as [RangeZone, number][])(
    'maps the center of the %s ring back to that same ring',
    (zone, radiusPercent) => {
      const board = { left: 100, top: 50, width: 400, height: 400 }
      const point = rangeCellFromBoardPoint(
        board.left + board.width / 2 + board.width * radiusPercent / 100,
        board.top + board.height / 2,
        board,
      )
      expect(point).toMatchObject({ zone, angle: 15 })
    },
  )

  it('allows several participants in the same sector', () => {
    expect(nearestFreeRangeAngle(3, 'medium', [
      { zone: 'medium', angle: 15 }, { zone: 'medium', angle: 45 },
    ])).toBe(15)
  })

  it('estimates distance from maneuver cost and sectors', () => {
    expect(estimateRangeBetween(
      { zone: 'short', angle: 0 }, { zone: 'medium', angle: 0 },
    ).zone).toBe('short')
    expect(estimateRangeBetween(
      { zone: 'medium', angle: 0 }, { zone: 'medium', angle: 180 },
    ).zone).toBe('medium')
    expect(estimateRangeBetween(
      { zone: 'long', angle: 60 }, { zone: 'long', angle: 60 },
    ).zone).toBe('engaged')
  })

  it.each(['engaged', 'short', 'medium', 'long', 'extreme'] as const)(
    'uses the same angular separation in the %s band',
    zone => {
      expect(estimateRangeBetween(
        { zone, angle: 15 }, { zone, angle: 45 },
      )).toMatchObject({ zone: 'short' })
      expect(estimateRangeBetween(
        { zone, angle: 15 }, { zone, angle: 105 },
      )).toMatchObject({ zone: 'short' })
      expect(estimateRangeBetween(
        { zone, angle: 15 }, { zone, angle: 195 },
      )).toMatchObject({ zone: 'medium', bandUnits: 2 })
    },
  )

  it('combines radial maneuver cost and sector separation', () => {
    expect(estimateRangeBetween(
      { zone: 'short', angle: 15 }, { zone: 'medium', angle: 105 },
    ).zone).toBe('medium')
    expect(estimateRangeBetween(
      { zone: 'engaged', angle: 15 }, { zone: 'short', angle: 195 },
    ).zone).toBe('short')
    expect(estimateRangeBetween(
      { zone: 'short', angle: 15 }, { zone: 'long', angle: 15 },
    )).toMatchObject({ zone: 'medium', bandUnits: 3 })
  })

  it('maps two and three maneuver units to medium range', () => {
    expect(estimateRangeBetween(
      { zone: 'medium', angle: 15 }, { zone: 'long', angle: 15 },
    )).toMatchObject({ zone: 'medium', bandUnits: 2 })
    expect(estimateRangeBetween(
      { zone: 'short', angle: 15 }, { zone: 'long', angle: 15 },
    )).toMatchObject({ zone: 'medium', bandUnits: 3 })
  })

  it('never estimates more than medium between short and medium rings', () => {
    const sectorAngles = Array.from({ length: 12 }, (_, index) => index * 30 + 15)

    for (const shortAngle of sectorAngles) {
      for (const mediumAngle of sectorAngles) {
        const forward = estimateRangeBetween(
          { zone: 'short', angle: shortAngle },
          { zone: 'medium', angle: mediumAngle },
        )
        const backward = estimateRangeBetween(
          { zone: 'medium', angle: mediumAngle },
          { zone: 'short', angle: shortAngle },
        )
        for (const estimate of [forward, backward]) {
          expect(estimate.bandUnits).toBeLessThanOrEqual(3)
          expect(['short', 'medium']).toContain(estimate.zone)
        }
      }
    }
  })

  it('charges two maneuvers for medium-long and long-extreme', () => {
    expect(estimateRangeBetween(
      { zone: 'medium', angle: 15 }, { zone: 'long', angle: 15 },
    )).toMatchObject({ zone: 'medium', bandUnits: 2 })
    expect(estimateRangeBetween(
      { zone: 'long', angle: 15 }, { zone: 'extreme', angle: 15 },
    )).toMatchObject({ zone: 'medium', bandUnits: 2 })
  })

  it('counts medium to extreme as long inside the same sector', () => {
    expect(estimateRangeBetween(
      { zone: 'medium', angle: 15 }, { zone: 'extreme', angle: 15 },
    )).toMatchObject({ zone: 'long', bandUnits: 4 })
    expect(estimateRangeBetween(
      { zone: 'extreme', angle: 15 }, { zone: 'medium', angle: 15 },
    )).toMatchObject({ zone: 'long', bandUnits: 4 })

    // Одного соседнего сектора недостаточно, чтобы повысить дальнюю дистанцию:
    // предельная начинается с шести суммарных манёвров.
    expect(estimateRangeBetween(
      { zone: 'medium', angle: 15 }, { zone: 'extreme', angle: 45 },
    )).toMatchObject({ zone: 'long', bandUnits: 5 })
    expect(estimateRangeBetween(
      { zone: 'medium', angle: 15 }, { zone: 'extreme', angle: 195 },
    )).toMatchObject({ zone: 'extreme', bandUnits: 6 })
  })

  it('counts short to extreme as long inside the same sector', () => {
    expect(estimateRangeBetween(
      { zone: 'short', angle: 15 }, { zone: 'extreme', angle: 15 },
    )).toMatchObject({ zone: 'long', bandUnits: 5 })
    expect(estimateRangeBetween(
      { zone: 'extreme', angle: 15 }, { zone: 'short', angle: 15 },
    )).toMatchObject({ zone: 'long', bandUnits: 5 })

    expect(estimateRangeBetween(
      { zone: 'short', angle: 15 }, { zone: 'extreme', angle: 45 },
    )).toMatchObject({ zone: 'extreme', bandUnits: 6 })
  })

  it('treats every engaged point as the same origin relative to outer rings', () => {
    const target = { zone: 'long', angle: 105 } as const
    expect(estimateRangeBetween({ zone: 'engaged', angle: 15 }, target))
      .toEqual(estimateRangeBetween({ zone: 'engaged', angle: 195 }, target))
  })

  it('counts engaged participants as engaged only in the same sector', () => {
    expect(estimateRangeBetween(
      { zone: 'engaged', angle: 15 }, { zone: 'engaged', angle: 15 },
    ).zone).toBe('engaged')
    expect(estimateRangeBetween(
      { zone: 'engaged', angle: 15 }, { zone: 'engaged', angle: 45 },
    ).zone).toBe('short')
  })
})
