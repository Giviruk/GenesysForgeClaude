import { useLayoutEffect, useRef } from 'react'

const GUTTER = 12
const CLIPPING_OVERFLOW = /^(auto|scroll|hidden|clip)$/

/** Horizontal band a tooltip may occupy: the viewport minus a gutter, narrowed by clipping panels. */
export interface TooltipBounds { left: number; right: number }

/** Ancestors that clip horizontal overflow; collected once per opening, not on every scroll. */
export function clippingAncestors(tip: HTMLElement): HTMLElement[] {
  const result: HTMLElement[] = []
  for (let parent = tip.parentElement; parent; parent = parent.parentElement) {
    if (CLIPPING_OVERFLOW.test(getComputedStyle(parent).overflowX)) result.push(parent)
  }
  return result
}

export function tooltipBounds(viewportWidth: number, ancestors: HTMLElement[]): TooltipBounds {
  let left = GUTTER
  let right = viewportWidth - GUTTER
  for (const parent of ancestors) {
    const rect = parent.getBoundingClientRect()
    left = Math.max(left, rect.left + parent.clientLeft)
    right = Math.min(right, rect.left + parent.clientLeft + parent.clientWidth)
  }
  return { left, right: Math.max(left, right) }
}

/** Shift (px) that moves a tooltip starting at `naturalLeft` with `width` inside `bounds`. */
export function tooltipShift(naturalLeft: number, width: number, bounds: TooltipBounds): number {
  const target = Math.max(bounds.left, Math.min(naturalLeft, bounds.right - width))
  return target - naturalLeft
}

/** Keep an anchored tooltip inside the viewport and any scrolling panel. */
export function useTooltipPosition(open: boolean) {
  const ref = useRef<HTMLSpanElement>(null)

  useLayoutEffect(() => {
    const tip = ref.current
    if (!open || !tip) return
    const ancestors = clippingAncestors(tip)
    let frame = 0

    function position() {
      if (!tip) return
      const bounds = tooltipBounds(document.documentElement.clientWidth, ancestors)
      tip.style.maxWidth = `min(22rem, ${bounds.right - bounds.left}px)`
      tip.style.left = '0px'
      const rect = tip.getBoundingClientRect()
      tip.style.left = `${tooltipShift(rect.left, rect.width, bounds)}px`
    }
    // Scroll and resize fire in bursts; one layout pass per frame is enough.
    function schedule() {
      if (frame) return
      frame = requestAnimationFrame(() => { frame = 0; position() })
    }

    position()
    const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(schedule)
    observer?.observe(tip)
    window.addEventListener('resize', schedule)
    window.addEventListener('scroll', schedule, true)
    return () => {
      if (frame) cancelAnimationFrame(frame)
      observer?.disconnect()
      window.removeEventListener('resize', schedule)
      window.removeEventListener('scroll', schedule, true)
    }
  }, [open])

  return ref
}
