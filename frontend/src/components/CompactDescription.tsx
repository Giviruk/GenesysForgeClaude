import { useEffect, useId, useRef, useState, type ReactNode } from 'react'
import { t } from '../i18n'
import { BookReference } from './BookReference'
import { Icon } from './Icon'

/** Keep list descriptions readable without hiding their full text or source. */
export function CompactDescription({ children, source, details }: { children?: ReactNode; source?: string; details?: ReactNode }) {
  const [expanded, setExpanded] = useState(false)
  const [truncated, setTruncated] = useState(false)
  const text = useRef<HTMLDivElement>(null)
  const id = useId()
  useEffect(() => {
    const element = text.current
    if (!element || typeof ResizeObserver === 'undefined') return
    const measure = () => {
      if (!expanded) setTruncated(element.scrollHeight > element.clientHeight + 1)
    }
    measure()
    const observer = new ResizeObserver(measure)
    observer.observe(element)
    return () => observer.disconnect()
  }, [children, expanded])
  if (!children && !source && !details) return null
  return <div className={`compact-description${expanded ? ' expanded' : ''}`}>
    <div ref={text} id={id} className="compact-description-text">{children}</div>
    <div className="compact-description-footer">
      {source && <span className="compact-description-source"><BookReference source={source} /></span>}
      {(truncated || details) && <button type="button" className="link-btn compact-description-toggle" aria-expanded={expanded} aria-controls={details ? `${id} ${id}-details` : id}
        onClick={() => setExpanded(value => !value)}>
        {expanded ? t('Свернуть', 'Collapse') : truncated ? t('Подробнее', 'Details') : t('Характеристики', 'Stats')}<Icon name="chevron-down" className="button-icon" />
      </button>}
    </div>
    {details && <div id={`${id}-details`} className="compact-description-details">{details}</div>}
  </div>
}
