import { useEffect, useId, useRef, useState } from 'react'
import { parseProperties, type ParsedProperty } from '../data/itemQualities'

/**
 * Рендерит строку свойств предмета («Точное 1, Оборонительное 2») как набор тегов.
 * У каждого свойства с известным описанием появляется тултип:
 *  - при наведении — временно;
 *  - при нажатии — закрепляется и держится открытым, пока пользователь не нажмёт
 *    в любом другом месте экрана (или на сам тег ещё раз).
 */
export function PropertyTags({ properties, className }: { properties: string | null | undefined; className?: string }) {
  const parsed = parseProperties(properties)
  if (parsed.length === 0) return null
  return (
    <span className={`prop-tags${className ? ` ${className}` : ''}`}>
      {parsed.map((p, i) => <PropertyTag key={`${p.raw}-${i}`} property={p} />)}
    </span>
  )
}

function PropertyTag({ property }: { property: ParsedProperty }) {
  const { raw, quality } = property
  const [hovered, setHovered] = useState(false)
  const [pinned, setPinned] = useState(false)
  const wrapRef = useRef<HTMLSpanElement>(null)
  const tipId = useId()

  // Пока тултип закреплён, нажатие вне тега закрывает его.
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

  // Свойство без известного описания — просто текст, без интерактивности.
  if (!quality) return <span className="prop-tag prop-tag-plain">{raw}</span>

  const open = hovered || pinned

  return (
    <span
      ref={wrapRef}
      className={`prop-tag${pinned ? ' pinned' : ''}`}
      tabIndex={0}
      role="button"
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
      {raw}
      {open && (
        <span id={tipId} role="tooltip" className="prop-tooltip" onClick={e => e.stopPropagation()}>
          <span className="prop-tooltip-title">
            {quality.nameRu}
            {quality.nameRu !== quality.nameEn && <span className="prop-tooltip-en"> · {quality.nameEn}</span>}
            {quality.rated && <span className="prop-tooltip-en"> · рейтинг</span>}
          </span>
          <span className="prop-tooltip-body">{quality.description}</span>
        </span>
      )}
    </span>
  )
}
