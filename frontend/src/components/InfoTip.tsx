import { useEffect, useId, useRef, useState } from 'react'

/**
 * Памятка в тултипе: короткий значок, за которым лежит правило целиком.
 *
 * Поведение то же, что у тегов свойств предмета (`PropertyTags`): при наведении тултип
 * показывается временно, при нажатии закрепляется и держится, пока пользователь не нажмёт
 * в другом месте или не нажмёт Escape. Закреплять важно именно здесь — памятку по ремонту
 * читают, а не пробегают глазами, и она не должна исчезать от движения мыши.
 */
export function InfoTip({ label, title, children, className }: {
  /** Что видно всегда: обычно «?» или короткое слово. */
  label: string
  /** Заголовок памятки. */
  title: string
  children: React.ReactNode
  className?: string
}) {
  const [hovered, setHovered] = useState(false)
  const [pinned, setPinned] = useState(false)
  const wrapRef = useRef<HTMLSpanElement>(null)
  const tipId = useId()

  useEffect(() => {
    if (!pinned) return
    function onPointerDown(e: PointerEvent) {
      if (wrapRef.current && !wrapRef.current.contains(e.target as Node)) setPinned(false)
    }
    function onKey(e: KeyboardEvent) {
      if (e.key === 'Escape') setPinned(false)
    }
    document.addEventListener('pointerdown', onPointerDown)
    document.addEventListener('keydown', onKey)
    return () => {
      document.removeEventListener('pointerdown', onPointerDown)
      document.removeEventListener('keydown', onKey)
    }
  }, [pinned])

  const open = hovered || pinned

  return (
    <span
      ref={wrapRef}
      className={`prop-tag info-tip${pinned ? ' pinned' : ''}${className ? ` ${className}` : ''}`}
      tabIndex={0}
      role="button"
      aria-label={title}
      aria-expanded={open}
      aria-describedby={open ? tipId : undefined}
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}
      onFocus={() => setHovered(true)}
      onBlur={() => setHovered(false)}
      onClick={e => { e.stopPropagation(); setPinned(p => !p) }}
      onKeyDown={e => {
        if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); setPinned(p => !p) }
      }}
    >
      {label}
      {open && (
        <span id={tipId} role="tooltip" className="prop-tooltip" onClick={e => e.stopPropagation()}>
          <span className="prop-tooltip-title">{title}</span>
          <span className="prop-tooltip-body">{children}</span>
        </span>
      )}
    </span>
  )
}
