import { t } from '../i18n'

const UNCONFIRMED = 'страница не подтверждена'

/**
 * Prefix of the stored source → publisher page. Core pages follow the Russian edition's pagination,
 * so they link to that edition and show its title; the English edition is paginated differently.
 */
const BOOKS: { prefix: string, url: string, title?: [string, string] }[] = [
  {
    prefix: 'Genesys Core Rulebook (RU translation),',
    url: 'https://hobbyworld.ru/genesys-osnovnaja-kniga-pravil',
    title: ['Genesys. Основная книга правил,', 'Genesys Core Rulebook, Russian edition,'],
  },
  { prefix: 'Genesys Core Rulebook,', url: 'https://www.edge-studio.net/games/genesys-core-rulebook/' },
  { prefix: 'Realms of Terrinoth,', url: 'https://www.edge-studio.net/games/realms-of-terrinoth/' },
]

/** Only publisher links are generated; custom source text never becomes an arbitrary URL. */
export function BookReference({ source }: { source?: string }) {
  if (!source) return null
  const book = BOOKS.find(b => source.startsWith(b.prefix))
  const named = book?.title ? t(...book.title) + source.slice(book.prefix.length) : source
  const label = named.replace(', с. ', t(', с. ', ', p. ')).replace(UNCONFIRMED, t(UNCONFIRMED, 'page unconfirmed'))
  return (
    <span className="book-reference hint small-text">
      {book ? <a href={book.url} target="_blank" rel="noopener noreferrer">{label}</a> : label}
    </span>
  )
}
