export interface ChronicleMention {
  start: number
  end: number
  query: string
}

/** Находит незавершённое @-упоминание непосредственно перед курсором. */
export function findChronicleMention(text: string, cursor: number): ChronicleMention | null {
  const beforeCursor = text.slice(0, Math.max(0, cursor))
  const match = /(?:^|[\s(])@([\p{L}\p{N} _.'-]*)$/u.exec(beforeCursor)
  if (!match) return null
  const query = match[1]
  return { start: beforeCursor.length - query.length - 1, end: beforeCursor.length, query }
}

export function replaceChronicleMention(
  text: string,
  mention: ChronicleMention,
  label: string,
  target: string,
): { text: string; cursor: number } {
  const markdown = `[${label}](${target}) `
  const next = text.slice(0, mention.start) + markdown + text.slice(mention.end)
  return { text: next, cursor: mention.start + markdown.length }
}
