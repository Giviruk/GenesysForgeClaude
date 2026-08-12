import { describe, expect, it } from 'vitest'
import {
  estimateRangeBetween, nearestFreeRangeAngle, rangeZoneFromRadius, snapRangeAngle,
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
})
