import type { Quality } from '../api/types'
import { findQuality, ITEM_QUALITIES, type ItemQuality } from '../data/itemQualities'
import { localizedDescription } from '../utils/labels'
import { t } from '../i18n'
import { InfoTip } from './InfoTip'

type QualityLike = Pick<Quality, 'nameRu' | 'nameEn' | 'description' | 'safeDescription' | 'descriptionEn' | 'hasRating'>

interface PropertyDescriptor {
  nameRu: string
  nameEn: string
  description: string
  hasRating: boolean
}

interface PropertyMatch {
  start: number
  end: number
  quality: PropertyDescriptor
}

/** Рендерит текст и превращает известные качества внутри него в inline-тултипы. */
export function PropertyText({ text, qualities = [] }: { text: string; qualities?: QualityLike[] }) {
  if (!text) return null

  const matches = findMatches(text, descriptors(qualities))
  if (matches.length === 0) return <>{text}</>

  const parts: React.ReactNode[] = []
  let cursor = 0
  for (const match of matches) {
    if (match.start > cursor) parts.push(text.slice(cursor, match.start))
    const displayName = t(match.quality.nameRu, match.quality.nameEn)
    const alternateName = t(match.quality.nameEn, match.quality.nameRu)
    parts.push(
      <InfoTip key={`${match.start}-${match.end}`} label={text.slice(match.start, match.end)}
        title={displayName === alternateName ? displayName : `${displayName} · ${alternateName}`}
        className="spell-property">
        {match.quality.description}
        {match.quality.hasRating && t(' Рейтинг свойства указывается в эффекте.', ' The quality rating is shown in the effect.')}
      </InfoTip>,
    )
    cursor = match.end
  }
  if (cursor < text.length) parts.push(text.slice(cursor))
  return <>{parts}</>
}

function descriptors(qualities: QualityLike[]): PropertyDescriptor[] {
  const result: PropertyDescriptor[] = []
  const seen = new Set<string>()
  const add = (quality: PropertyDescriptor) => {
    const key = quality.nameEn.trim().toLowerCase()
    if (!key || seen.has(key)) return
    seen.add(key)
    result.push(quality)
  }

  // Серверный справочник первым: он включает пользовательские качества и актуальные переводы.
  for (const quality of qualities) {
    const fallback = findQuality(quality.nameEn || quality.nameRu)
    const serverNameRu = quality.nameRu?.trim() ?? ''
    const serverNameEn = quality.nameEn?.trim() ?? ''
    // Старые ссылки иногда сохраняют английское имя в nameRu. Для встроенных качеств
    // в этом случае берём каноничный перевод, но оставляем серверное описание.
    const normalizedRu = serverNameRu.toLocaleLowerCase()
    const normalizedEn = serverNameEn.toLocaleLowerCase()
    const isEnglishFallback = fallback && (!normalizedRu
      || normalizedRu === normalizedEn
      || normalizedRu === fallback.nameEn.toLocaleLowerCase())
    const nameRu = isEnglishFallback
      ? fallback.nameRu
      : serverNameRu || fallback?.nameRu || serverNameEn
    const nameEn = serverNameEn || fallback?.nameEn || nameRu
    add({
      nameRu,
      nameEn,
      description: localizedDescription({
        description: quality.description || quality.safeDescription || fallback?.description,
        descriptionEn: quality.descriptionEn || fallback?.description,
      }),
      hasRating: fallback?.rated ?? quality.hasRating,
    })
  }
  // В справочнике магии и в кампаниях без Reference остаётся встроенный fallback.
  for (const quality of ITEM_QUALITIES) add(itemQualityDescriptor(quality))
  return result.sort((a, b) => Math.max(b.nameRu.length, b.nameEn.length) - Math.max(a.nameRu.length, a.nameEn.length))
}

function itemQualityDescriptor(quality: ItemQuality): PropertyDescriptor {
  return { nameRu: quality.nameRu, nameEn: quality.nameEn, description: quality.description, hasRating: quality.rated }
}

function findMatches(text: string, candidates: PropertyDescriptor[]): PropertyMatch[] {
  const lower = text.toLocaleLowerCase()
  const found: PropertyMatch[] = []
  for (const quality of candidates) {
    for (const name of new Set([quality.nameRu, quality.nameEn])) {
      const term = name.trim()
      if (!term) continue
      const needle = term.toLocaleLowerCase()
      let from = 0
      while (from < lower.length) {
        const start = lower.indexOf(needle, from)
        if (start < 0) break
        const end = start + needle.length
        if (!isWordCharacter(lower[start - 1]) && !isWordCharacter(lower[end])) {
          found.push({ start, end, quality })
        }
        from = end
      }
    }
  }

  found.sort((a, b) => a.start - b.start || (b.end - b.start) - (a.end - a.start))
  const accepted: PropertyMatch[] = []
  let end = -1
  for (const match of found) {
    if (match.start < end) continue
    accepted.push(match)
    end = match.end
  }
  return accepted
}

function isWordCharacter(value: string | undefined): boolean {
  return value != null && /[\p{L}\p{N}]/u.test(value)
}
