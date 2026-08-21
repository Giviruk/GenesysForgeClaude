import { useEffect, useId, useRef, useState } from 'react'
import type { Quality } from '../api/types'
import { parseProperties, qualityName, type ItemQuality, type ParsedProperty } from '../data/itemQualities'
import { t } from '../i18n'
import { localizedDescription } from '../utils/labels'

type QualityDefinition = Pick<Quality,
  'code' | 'nameRu' | 'nameEn' | 'description' | 'safeDescription' | 'descriptionEn' | 'hasRating'>

/**
 * Рендерит строку свойств предмета («Точное 1, Оборонительное 2») как набор тегов.
 * У каждого свойства с известным описанием появляется тултип:
 *  - при наведении — временно;
 *  - при нажатии — закрепляется и держится открытым, пока пользователь не нажмёт
 *    в любом другом месте экрана (или на сам тег ещё раз).
 */
export function PropertyTags({ properties, className, qualityDefinitions = [] }: {
  properties: string | null | undefined
  className?: string
  /** Дополнительные серверные качества, включая пользовательские записи приватной кампании. */
  qualityDefinitions?: QualityDefinition[]
}) {
  const parsed = parseProperties(properties)
  if (parsed.length === 0) return null
  return (
    <span className={`prop-tags${className ? ` ${className}` : ''}`}>
      {parsed.map((p, i) => <PropertyTag key={`${p.raw}-${i}`} property={p} qualityDefinitions={qualityDefinitions} />)}
    </span>
  )
}

function PropertyTag({ property, qualityDefinitions }: {
  property: ParsedProperty
  qualityDefinitions: QualityDefinition[]
}) {
  const { raw, rating } = property
  const serverDefinition = qualityDefinitions.find(q => [q.code, q.nameRu, q.nameEn]
    .filter(Boolean)
    .some(name => normalizeQualityToken(name) === normalizeQualityToken(raw)))
  const quality: ResolvedQuality | null = property.quality
    ? { local: property.quality }
    : serverDefinition ? { server: serverDefinition } : null
  const display = quality
    ? quality.local ? qualityName(quality.local) : localizedServerName(quality.server)
    : raw
  const description = quality
    ? quality.local?.description ?? (quality.server ? localizedDescription(quality.server) : '')
    : ''
  const hasRating = quality
    ? quality.local?.rated ?? quality.server?.hasRating ?? false
    : false
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
  const label = `${display}${rating != null ? ` ${rating}` : ''}`
  const title = quality.local ? qualityName(quality.local) : display
  const englishName = quality.local ? quality.local.nameEn : quality.server.nameEn

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
      {label}
      {open && (
        <span id={tipId} role="tooltip" className="prop-tooltip" onClick={e => e.stopPropagation()}>
          <span className="prop-tooltip-title">
            {title}
            {title !== englishName && <span className="prop-tooltip-en"> · {t(englishName, title)}</span>}
            {hasRating && <span className="prop-tooltip-en"> · {t('рейтинг', 'rated')}</span>}
          </span>
          {description && <span className="prop-tooltip-body">{description}</span>}
        </span>
      )}
    </span>
  )
}

type ResolvedQuality =
  | { local: ItemQuality; server?: never }
  | { local?: never; server: QualityDefinition }

function normalizeQualityToken(value: string): string {
  return value.toLocaleLowerCase().replace(/ё/g, 'е').replace(/\s+\d+\s*$/, '').trim()
}

function localizedServerName(definition: QualityDefinition): string {
  const ru = definition.nameRu?.trim() ?? ''
  const en = definition.nameEn?.trim() ?? ''
  const normalizedRu = ru.toLocaleLowerCase()
  const normalizedEn = en.toLocaleLowerCase()
  // PrivateFull data from older seeds may repeat the English name in nameRu. The
  // built-in parser handles those known qualities before this server-only fallback.
  return ru && normalizedRu !== normalizedEn ? t(ru, en) : t(ru || en, en || ru)
}
