import { describe, expect, it } from 'vitest'
import { tooltipBounds, tooltipShift, tooltipTop } from './useTooltipPosition'

describe('useTooltipPosition helpers', () => {
  it('держит подсказку внутри экрана с отступом 12px', () => {
    const bounds = tooltipBounds(320)
    expect(bounds).toEqual({ left: 12, right: 308 })
    // Вылезает справа — сдвигаем влево ровно до границы.
    expect(tooltipShift(200, 150, bounds)).toBe(-42)
    // Вылезает слева — сдвигаем вправо.
    expect(tooltipShift(4, 100, bounds)).toBe(8)
    // Помещается — не двигаем.
    expect(tooltipShift(40, 100, bounds)).toBe(0)
  })

  it('не даёт границам вывернуться на очень узком экране', () => {
    expect(tooltipBounds(20)).toEqual({ left: 12, right: 12 })
  })

  it('ставит подсказку под тегом, а у нижнего края экрана — над ним', () => {
    expect(tooltipTop({ top: 100, bottom: 120 }, 80, 900)).toBe(126)
    expect(tooltipTop({ top: 800, bottom: 820 }, 80, 900)).toBe(714)
    // Не помещается ни снизу, ни сверху — остаётся снизу, страницу можно прокрутить.
    expect(tooltipTop({ top: 40, bottom: 60 }, 880, 900)).toBe(66)
  })
})
