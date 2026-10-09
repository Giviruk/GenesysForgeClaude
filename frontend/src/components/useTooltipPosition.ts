import { useLayoutEffect, useRef } from 'react'

/** Keep an anchored tooltip inside the viewport and any scrolling panel. */
export function useTooltipPosition(open: boolean) {
  const ref = useRef<HTMLSpanElement>(null)

  useLayoutEffect(() => {
    const tip = ref.current
    if (!open || !tip) return

    function position() {
      if (!tip) return
      const gutter = 12
      let left = gutter
      let right = document.documentElement.clientWidth - gutter
      for (let parent = tip.parentElement; parent; parent = parent.parentElement) {
        if (!/^(auto|scroll|hidden|clip)$/.test(getComputedStyle(parent).overflowX)) continue
        const rect = parent.getBoundingClientRect()
        left = Math.max(left, rect.left + parent.clientLeft)
        right = Math.min(right, rect.left + parent.clientLeft + parent.clientWidth)
      }
      tip.style.maxWidth = `min(22rem, ${Math.max(0, right - left)}px)`
      tip.style.left = '0px'
      const rect = tip.getBoundingClientRect()
      const target = Math.max(left, Math.min(rect.left, right - rect.width))
      tip.style.left = `${target - rect.left}px`
    }

    position()
    const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(position)
    observer?.observe(tip)
    window.addEventListener('resize', position)
    window.addEventListener('scroll', position, true)
    return () => {
      observer?.disconnect()
      window.removeEventListener('resize', position)
      window.removeEventListener('scroll', position, true)
    }
  }, [open])

  return ref
}
