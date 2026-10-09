import { afterEach, describe, expect, it } from 'vitest'
import { clippingAncestors, tooltipBounds, tooltipShift } from './useTooltipPosition'

function panel(left: number, width: number, overflowX = 'auto'): HTMLElement {
  const element = document.createElement('div')
  element.style.overflowX = overflowX
  element.getBoundingClientRect = () => ({ left, right: left + width, width } as DOMRect)
  Object.defineProperty(element, 'clientWidth', { value: width })
  Object.defineProperty(element, 'clientLeft', { value: 0 })
  return element
}

describe('useTooltipPosition helpers', () => {
  afterEach(() => { document.body.innerHTML = '' })

  it('держит подсказку внутри экрана с отступом 12px', () => {
    const bounds = tooltipBounds(320, [])
    expect(bounds).toEqual({ left: 12, right: 308 })
    // Вылезает справа — сдвигаем влево ровно до границы.
    expect(tooltipShift(200, 150, bounds)).toBe(-42)
    // Вылезает слева — сдвигаем вправо.
    expect(tooltipShift(4, 100, bounds)).toBe(8)
    // Помещается — не двигаем.
    expect(tooltipShift(40, 100, bounds)).toBe(0)
  })

  it('сужает границы до прокручиваемой панели', () => {
    const bounds = tooltipBounds(1440, [panel(100, 400)])
    expect(bounds).toEqual({ left: 100, right: 500 })
    expect(tooltipShift(450, 120, bounds)).toBe(-70)
  })

  it('не даёт границам вывернуться, если панель начинается правее отступа экрана', () => {
    // left = панель (310), right = экран минус отступ (308) — без зажима ширина ушла бы в минус.
    expect(tooltipBounds(320, [panel(310, 40)])).toEqual({ left: 310, right: 310 })
  })

  it('учитывает только предков, которые обрезают горизонтальное переполнение', () => {
    const scrolling = panel(0, 300, 'auto')
    const visible = panel(0, 300, 'visible')
    const tip = document.createElement('span')
    scrolling.append(visible)
    visible.append(tip)
    document.body.append(scrolling)
    expect(clippingAncestors(tip)).toEqual([scrolling])
  })
})
