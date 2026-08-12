import { describe, expect, it } from 'vitest'
import {
  estimateRangeBetween, nearestFreeRangeAngle, rangeCellFromBoardPoint, rangeZoneFromRadius,
  snapRangeAngle,
} from './rangeGeometry'

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
    expect(rangeCellFromBoardPoint(300, 50 + 200 - 30, board)).toEqual({ zone: 'short', angle: 285 })
    expect(rangeCellFromBoardPoint(300 + 50, 250, board)).toEqual({ zone: 'medium', angle: 15 })
    expect(rangeCellFromBoardPoint(300 - 190, 250, board)).toEqual({ zone: 'extreme', angle: 195 })
  })

  it('uses the nearest free cell in the selected band', () => {
    expect(nearestFreeRangeAngle(3, 'medium', [
      { zone: 'medium', angle: 15 }, { zone: 'medium', angle: 45 },
    ])).toBe(345)
  })

  it('estimates pair distance from both radii and angles', () => {
    expect(estimateRangeBetween(
      { zone: 'short', angle: 0 }, { zone: 'medium', angle: 0 },
    ).zone).toBe('short')
    expect(estimateRangeBetween(
      { zone: 'medium', angle: 0 }, { zone: 'medium', angle: 180 },
    ).zone).toBe('extreme')
    expect(estimateRangeBetween(
      { zone: 'long', angle: 60 }, { zone: 'long', angle: 60 },
    ).zone).toBe('engaged')
  })

  it('uses the visible ring boundaries without a one-step offset', () => {
    expect(estimateRangeBetween(
      { zone: 'short', angle: 0 }, { zone: 'medium', angle: 90 },
    )).toMatchObject({ zone: 'medium' })
    expect(estimateRangeBetween(
      { zone: 'engaged', angle: 0 }, { zone: 'short', angle: 180 },
    )).toMatchObject({ zone: 'medium' })
    expect(estimateRangeBetween(
      { zone: 'short', angle: 0 }, { zone: 'long', angle: 0 },
    )).toMatchObject({ zone: 'medium' })
  })
})
