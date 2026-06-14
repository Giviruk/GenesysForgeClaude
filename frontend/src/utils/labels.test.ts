import { describe, expect, it } from 'vitest'
import { magicSkillLabel } from './labels'

describe('magicSkillLabel', () => {
  it('переводит известные магические навыки', () => {
    expect(magicSkillLabel('Arcana')).toContain('Arcana')
    expect(magicSkillLabel('Runes')).toContain('Руны')
  })

  it('возвращает исходный код для неизвестного навыка', () => {
    expect(magicSkillLabel('CustomSkill')).toBe('CustomSkill')
  })
})
