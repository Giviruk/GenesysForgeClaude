import { createElement, Fragment, type MouseEvent, type ReactNode } from 'react'

export interface MarkdownEntityLink {
  kind: 'character' | 'npc'
  id: string
}

interface Props {
  markdown: string
  onEntityLink?: (link: MarkdownEntityLink) => void
}

const slugify = (text: string, index: number) => {
  const slug = text.toLocaleLowerCase().replace(/[^\p{L}\p{N}]+/gu, '-').replace(/^-|-$/g, '')
  return slug || `section-${index + 1}`
}

function entityLink(href: string): MarkdownEntityLink | null {
  const match = /^(character|npc):([0-9a-f-]{36})$/i.exec(href.trim())
  return match ? { kind: match[1].toLowerCase() as MarkdownEntityLink['kind'], id: match[2] } : null
}

function safeHref(href: string): string | null {
  const value = href.trim()
  if (/^(https?:|mailto:)/i.test(value) || value.startsWith('#')) return value
  return null
}

function Inline({ text, onEntityLink }: { text: string; onEntityLink?: Props['onEntityLink'] }) {
  const pattern = /(!?\[[^\]]*\]\([^)]+\)|`[^`]+`|\*\*[^*]+\*\*|__[^_]+__|~~[^~]+~~|\*[^*]+\*|_[^_]+_)/g
  const nodes: ReactNode[] = []
  let cursor = 0
  for (const match of text.matchAll(pattern)) {
    const index = match.index ?? 0
    if (index > cursor) nodes.push(text.slice(cursor, index))
    const token = match[0]
    const link = /^(!?)\[([^\]]*)\]\(([^)]+)\)$/.exec(token)
    if (link) {
      const [, image, label, rawHref] = link
      const entity = entityLink(rawHref)
      const href = safeHref(rawHref)
      if (image && href) nodes.push(<img key={index} src={href} alt={label} loading="lazy" />)
      else if (!image && entity) {
        const click = (event: MouseEvent<HTMLAnchorElement>) => {
          event.preventDefault()
          onEntityLink?.(entity)
        }
        nodes.push(<a key={index} href={`#${entity.kind}-${entity.id}`} onClick={click}>{label}</a>)
      } else if (!image && href) {
        nodes.push(<a key={index} href={href} target={href.startsWith('#') ? undefined : '_blank'}
          rel={href.startsWith('#') ? undefined : 'noreferrer'}>{label}</a>)
      } else nodes.push(label)
    } else if (token.startsWith('`')) nodes.push(<code key={index}>{token.slice(1, -1)}</code>)
    else if (token.startsWith('**') || token.startsWith('__')) nodes.push(<strong key={index}>{token.slice(2, -2)}</strong>)
    else if (token.startsWith('~~')) nodes.push(<del key={index}>{token.slice(2, -2)}</del>)
    else nodes.push(<em key={index}>{token.slice(1, -1)}</em>)
    cursor = index + token.length
  }
  if (cursor < text.length) nodes.push(text.slice(cursor))
  return <>{nodes}</>
}

function splitCells(line: string) {
  return line.trim().replace(/^\||\|$/g, '').split('|').map(cell => cell.trim())
}

export function MarkdownContent({ markdown, onEntityLink }: Props) {
  const lines = markdown.replace(/\r\n/g, '\n').split('\n')
  const blocks: ReactNode[] = []
  let i = 0
  let headingIndex = 0

  while (i < lines.length) {
    const line = lines[i]
    if (!line.trim()) { i++; continue }

    const fence = /^```([\w-]*)\s*$/.exec(line)
    if (fence) {
      const code: string[] = []
      i++
      while (i < lines.length && !/^```/.test(lines[i])) code.push(lines[i++])
      if (i < lines.length) i++
      blocks.push(<pre key={`code-${i}`}><code data-language={fence[1] || undefined}>{code.join('\n')}</code></pre>)
      continue
    }

    const heading = /^(#{1,6})\s+(.+?)\s*#*$/.exec(line)
    if (heading) {
      const level = heading[1].length
      const text = heading[2]
      const id = slugify(text.replace(/[*_~`]/g, '').trim(), headingIndex++)
      blocks.push(createElement(`h${level}`, { key: `h-${i}`, id },
        <Inline text={text} onEntityLink={onEntityLink} />))
      i++
      continue
    }

    if (/^\s*(([-*_])\s*){3,}$/.test(line)) {
      blocks.push(<hr key={`hr-${i}`} />); i++; continue
    }

    if (i + 1 < lines.length && line.includes('|') && /^\s*\|?\s*:?-{3,}/.test(lines[i + 1])) {
      const headers = splitCells(line)
      i += 2
      const rows: string[][] = []
      while (i < lines.length && lines[i].includes('|') && lines[i].trim()) rows.push(splitCells(lines[i++]))
      blocks.push(<div className="markdown-table-wrap" key={`table-${i}`}><table><thead><tr>
        {headers.map((cell, n) => <th key={n}><Inline text={cell} onEntityLink={onEntityLink} /></th>)}
      </tr></thead><tbody>{rows.map((row, r) => <tr key={r}>{row.map((cell, n) =>
        <td key={n}><Inline text={cell} onEntityLink={onEntityLink} /></td>)}</tr>)}</tbody></table></div>)
      continue
    }

    const list = /^\s*(?:([-+*])|(\d+)\.)\s+(.+)$/.exec(line)
    if (list) {
      const ordered = Boolean(list[2])
      const items: string[] = []
      while (i < lines.length) {
        const item = /^\s*(?:([-+*])|(\d+)\.)\s+(.+)$/.exec(lines[i])
        if (!item || Boolean(item[2]) !== ordered) break
        items.push(item[3]); i++
      }
      const Tag = ordered ? 'ol' : 'ul'
      blocks.push(<Tag key={`list-${i}`}>{items.map((item, n) =>
        <li key={n}><Inline text={item} onEntityLink={onEntityLink} /></li>)}</Tag>)
      continue
    }

    if (/^>\s?/.test(line)) {
      const quote: string[] = []
      while (i < lines.length && /^>\s?/.test(lines[i])) quote.push(lines[i++].replace(/^>\s?/, ''))
      blocks.push(<blockquote key={`quote-${i}`}><MarkdownContent markdown={quote.join('\n')} onEntityLink={onEntityLink} /></blockquote>)
      continue
    }

    const paragraph: string[] = [line]
    i++
    while (i < lines.length && lines[i].trim() &&
      !/^(#{1,6})\s|^```|^>\s?|^\s*(?:[-+*]|\d+\.)\s+/.test(lines[i])) paragraph.push(lines[i++])
    blocks.push(<p key={`p-${i}`}>{paragraph.map((part, n) => <Fragment key={n}>
      {n > 0 && <br />}<Inline text={part} onEntityLink={onEntityLink} />
    </Fragment>)}</p>)
  }

  return <div className="markdown-content">{blocks}</div>
}
