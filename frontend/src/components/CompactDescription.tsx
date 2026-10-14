import { useId, useState, type ReactNode } from 'react'
import { t } from '../i18n'
import { BookReference } from './BookReference'
import { Icon } from './Icon'

/** Keep list descriptions readable without hiding their full text or source. */
export function CompactDescription({ children, source, details }: { children?: ReactNode; source?: string; details?: ReactNode }) {
  const [expanded, setExpanded] = useState(false)
  const id = useId()
  if (!children && !source && !details) return null
  return <div className={`compact-description${expanded ? ' expanded' : ''}`}>
    <div id={id} className="compact-description-text">{children}</div>
    <button type="button" className="link-btn compact-description-toggle" aria-expanded={expanded} aria-controls={id}
      onClick={() => setExpanded(value => !value)}>
      {expanded ? t('Свернуть', 'Collapse') : t('Подробнее', 'Details')}<Icon name="chevron-down" className="button-icon" />
    </button>
    {source && <div className="compact-description-source"><BookReference source={source} /></div>}
    {details && <div className="compact-description-source">{details}</div>}
  </div>
}
