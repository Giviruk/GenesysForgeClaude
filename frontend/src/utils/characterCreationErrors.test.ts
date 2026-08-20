import { describe, expect, it } from 'vitest'
import { formatCharacterCreationCompletionError } from './characterCreationErrors'

describe('formatCharacterCreationCompletionError', () => {
  it.each([
    ['heroic.ability.required', /выберите героическую способность/],
    ['heroic.identity.incomplete', /личное название и происхождение/],
    ['heroic.parameter.incomplete', /выберите параметр героической способности/],
    ['heroic.weapon.upgrade_incomplete', /улучшения именного оружия/],
  ])('объясняет ошибку %s понятным действием', (reasonCode, expected) => {
    expect(formatCharacterCreationCompletionError({ reasonCode, message: 'техническое сообщение' })).toMatch(expected)
  })

  it('распознаёт старый ответ без reasonCode по сообщению', () => {
    expect(formatCharacterCreationCompletionError(new Error('Выберите параметр героической способности.')))
      .toMatch(/выберите параметр героической способности/)
  })

  it('даёт полезный текст для неизвестной ошибки вместо сырого ответа API', () => {
    const result = formatCharacterCreationCompletionError({ status: 400, message: 'stack trace' })
    expect(result).toMatch(/Не удалось завершить создание персонажа/)
    expect(result).not.toContain('stack trace')
  })
})
