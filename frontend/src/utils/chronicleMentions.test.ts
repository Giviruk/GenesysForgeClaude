import { describe, expect, it } from 'vitest'
import { findChronicleMention, replaceChronicleMention } from './chronicleMentions'

describe('chronicle mentions', () => {
  it('finds a Cyrillic query after @ at the cursor', () => {
    const text = 'Герои встретили @бар'
    expect(findChronicleMention(text, text.length)).toEqual({ start: 16, end: 20, query: 'бар' })
  })

  it('does not treat email or completed markdown as a mention', () => {
    expect(findChronicleMention('hero@example.com', 16)).toBeNull()
    expect(findChronicleMention('[Бард](character:id)', 20)).toBeNull()
  })

  it('replaces only the active query with a portable markdown link', () => {
    const text = 'Встретили @ба у ворот'
    const mention = findChronicleMention(text, 'Встретили @ба'.length)!
    expect(replaceChronicleMention(text, mention, 'Бард', 'character:abc')).toEqual({
      text: 'Встретили [Бард](character:abc)  у ворот',
      cursor: 32,
    })
  })
})
