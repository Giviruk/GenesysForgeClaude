import { t } from '../i18n'

const BOOKS = [
  ['Genesys Core Rulebook', 'https://www.edge-studio.net/games/genesys-core-rulebook/'],
  ['Realms of Terrinoth', 'https://www.edge-studio.net/games/realms-of-terrinoth/'],
] as const

/** Only publisher links are generated; custom source text never becomes an arbitrary URL. */
export function BookReference({ source }: { source?: string }) {
  if (!source) return null
  const book = BOOKS.find(([name]) => source.startsWith(`${name},`) || source.startsWith(`${name} (RU translation),`))
  const label = source.replace(', с. ', t(', с. ', ', p. '))
    .replace('страница не подтверждена', t('страница не подтверждена', 'page unconfirmed'))
  return (
    <span className="book-reference hint small-text">
      {book ? <a href={book[1]} target="_blank" rel="noopener noreferrer">{label}</a> : label}
    </span>
  )
}
