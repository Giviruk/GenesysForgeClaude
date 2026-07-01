import type { ReactNode } from 'react'
import { Footer } from '../components/Footer'
import { navigate } from '../router'
import guideMarkdown from '../content/user-guide.md?raw'

type GuideBlock =
  | { kind: 'heading'; level: 1 | 2 | 3; text: string; id: string }
  | { kind: 'paragraph'; text: string }
  | { kind: 'list'; items: string[] }
type GuideHeading = Extract<GuideBlock, { kind: 'heading' }>

const slugify = (text: string) =>
  text
    .toLowerCase()
    .replace(/[^\p{L}\p{N}]+/gu, '-')
    .replace(/^-|-$/g, '')

function parseMarkdown(markdown: string): GuideBlock[] {
  const blocks: GuideBlock[] = []
  const lines = markdown.replace(/\r\n/g, '\n').split('\n')
  let i = 0

  while (i < lines.length) {
    const line = lines[i].trim()
    if (!line) { i += 1; continue }

    const heading = /^(#{1,3})\s+(.+)$/.exec(line)
    if (heading) {
      const level = heading[1].length as 1 | 2 | 3
      const text = heading[2].trim()
      blocks.push({ kind: 'heading', level, text, id: slugify(text) })
      i += 1
      continue
    }

    if (line.startsWith('- ')) {
      const items: string[] = []
      while (i < lines.length && lines[i].trim().startsWith('- ')) {
        items.push(lines[i].trim().slice(2).trim())
        i += 1
      }
      blocks.push({ kind: 'list', items })
      continue
    }

    const paragraph: string[] = []
    while (i < lines.length) {
      const current = lines[i].trim()
      if (!current || current.startsWith('#') || current.startsWith('- ')) break
      paragraph.push(current)
      i += 1
    }
    blocks.push({ kind: 'paragraph', text: paragraph.join(' ') })
  }

  return blocks
}

function inlineMarkdown(text: string): ReactNode[] {
  const nodes: ReactNode[] = []
  const linkPattern = /\[([^\]]+)]\(([^)]+)\)/g
  let lastIndex = 0
  let match: RegExpExecArray | null

  while ((match = linkPattern.exec(text))) {
    if (match.index > lastIndex) nodes.push(text.slice(lastIndex, match.index))
    nodes.push(
      <a key={`${match[2]}-${match.index}`} href={match[2]} target="_blank" rel="noreferrer">
        {match[1]}
      </a>,
    )
    lastIndex = match.index + match[0].length
  }
  if (lastIndex < text.length) nodes.push(text.slice(lastIndex))
  return nodes
}

const GUIDE_BLOCKS = parseMarkdown(guideMarkdown)
  .filter(block => !(block.kind === 'heading' && block.level === 1))
const GUIDE_SECTIONS = GUIDE_BLOCKS.filter((b): b is GuideHeading => b.kind === 'heading' && b.level === 2)

/** User-facing guide rendered from docs/user-guide Markdown without pulling extra markdown dependencies. */
export function HelpPage({ loggedIn, showFooter = true }: { loggedIn: boolean; showFooter?: boolean }) {
  return (
    <div className="page help-page">
      <div className="panel">
        <div className="page-head">
          <div>
            <h2>Справка</h2>
            <p className="muted">Короткий пользовательский маршрут по основным возможностям GenesysForge.</p>
          </div>
          <button className="small" onClick={() => navigate(loggedIn ? '/characters' : '/login')}>
            ← Назад
          </button>
        </div>

        <div className="help-layout">
          <aside className="help-toc" aria-label="Разделы справки">
            <h3>Разделы</h3>
            {GUIDE_SECTIONS.map(section => (
              <a key={section.id} href={`#${section.id}`}>{section.text}</a>
            ))}
          </aside>

          <article className="help-content">
            {GUIDE_BLOCKS.map((block, index) => {
              if (block.kind === 'heading') {
                const HeadingTag = `h${Math.min(block.level + 1, 4)}` as 'h2' | 'h3' | 'h4'
                return <HeadingTag key={`${block.id}-${index}`} id={block.id}>{block.text}</HeadingTag>
              }
              if (block.kind === 'list') {
                return (
                  <ul key={`list-${index}`}>
                    {block.items.map((item, itemIndex) => (
                      <li key={`${item}-${itemIndex}`}>{inlineMarkdown(item)}</li>
                    ))}
                  </ul>
                )
              }
              return <p key={`p-${index}`}>{inlineMarkdown(block.text)}</p>
            })}
          </article>
        </div>
      </div>
      {showFooter && <Footer />}
    </div>
  )
}
