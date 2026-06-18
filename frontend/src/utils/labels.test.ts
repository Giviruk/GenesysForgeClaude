import { describe, expect, it } from 'vitest'
import { difficultyLabel, magicSkillLabel, parseDifficulty } from './labels'

describe('magicSkillLabel', () => {
  it('переводит известные магические навыки', () => {
    expect(magicSkillLabel('Arcana')).toContain('Arcana')
    expect(magicSkillLabel('Runes')).toContain('Руны')
  })

  it('возвращает исходный код для неизвестного навыка', () => {
    expect(magicSkillLabel('CustomSkill')).toBe('CustomSkill')
  })
})

describe('parseDifficulty', () => {
  it('извлекает базовую сложность из строки эффекта', () => {
    expect(parseDifficulty('1 (Easy)')).toBe(1)
    expect(parseDifficulty('2 (Average)')).toBe(2)
    expect(parseDifficulty('3 (Hard)')).toBe(3)
  })

  it('извлекает приращение из доп. эффекта', () => {
    expect(parseDifficulty('+1')).toBe(1)
    expect(parseDifficulty('+2')).toBe(2)
  })

  it('возвращает 0 для пустой/нечисловой строки', () => {
    expect(parseDifficulty('')).toBe(0)
    expect(parseDifficulty('—')).toBe(0)
  })
})

describe('difficultyLabel', () => {
  it('подписывает уровни сложности', () => {
    expect(difficultyLabel(0)).toBe('Простая')
    expect(difficultyLabel(2)).toBe('Средняя')
    expect(difficultyLabel(5)).toBe('Грозная')
  })

  it('ограничивает диапазон 0..5', () => {
    expect(difficultyLabel(7)).toBe('Грозная')
    expect(difficultyLabel(-1)).toBe('Простая')
  })
})
