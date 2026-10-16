import { useLayoutEffect, useRef } from 'react'

const GUTTER = 12
const GAP = 6

/** Horizontal band a tooltip may occupy: the viewport minus a gutter. */
export interface TooltipBounds { left: number; right: number }

export function tooltipBounds(viewportWidth: number): TooltipBounds {
  return { left: GUTTER, right: Math.max(GUTTER, viewportWidth - GUTTER) }
}

/** Shift (px) that moves a tooltip starting at `naturalLeft` with `width` inside `bounds`. */
export function tooltipShift(naturalLeft: number, width: number, bounds: TooltipBounds): number {
  const target = Math.max(bounds.left, Math.min(naturalLeft, bounds.right - width))
  return target - naturalLeft
}

/** Top edge: under the anchor, or above it when it does not fit below but fits above. */
export function tooltipTop(anchor: { top: number; bottom: number }, height: number, viewportHeight: number): number {
  const below = anchor.bottom + GAP
  const above = anchor.top - GAP - height
  return below + height > viewportHeight - GUTTER && above >= GUTTER ? above : below
}

/**
 * Keep an anchored tooltip next to its tag and inside the viewport. The tooltip is fixed, not
 * absolute: scrolling wrappers such as `.table-wrap` clip absolute children vertically too, so a
 * quality tooltip in an NPC attack table used to vanish inside the table (GEN-V5-17).
 */
export function useTooltipPosition(open: boolean) {
  const ref = useRef<HTMLSpanElement>(null)

  useLayoutEffect(() => {
    const tip = ref.current
    const anchor = tip?.parentElement
    if (!open || !tip || !anchor) return
    let frame = 0

    function position() {
      if (!tip || !anchor) return
      const bounds = tooltipBounds(document.documentElement.clientWidth)
      tip.style.position = 'fixed'
      tip.style.maxWidth = `min(22rem, ${bounds.right - bounds.left}px)`
      tip.style.left = '0px'
      tip.style.top = '0px'
      // A transformed ancestor moves the origin of fixed positioning — measure it, do not assume (0, 0).
      const origin = tip.getBoundingClientRect()
      const rect = anchor.getBoundingClientRect()
      const left = rect.left + tooltipShift(rect.left, origin.width, bounds)
      tip.style.left = `${left - origin.left}px`
      tip.style.top = `${tooltipTop(rect, origin.height, document.documentElement.clientHeight) - origin.top}px`
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
