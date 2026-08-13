export interface MarkdownHeading {
  level: number
  text: string
  id: string
}

const slugify = (text: string, index: number) => {
  const slug = text.toLocaleLowerCase().replace(/[^\p{L}\p{N}]+/gu, '-').replace(/^-|-$/g, '')
  return slug || `section-${index + 1}`
}

export function markdownHeadings(markdown: string): MarkdownHeading[] {
  return markdown.split(/\r?\n/).flatMap((line, index) => {
    const match = /^(#{1,6})\s+(.+?)\s*#*$/.exec(line)
    if (!match) return []
    const text = match[2].replace(/[*_~`]/g, '').trim()
    return [{ level: match[1].length, text, id: slugify(text, index) }]
  })
}
