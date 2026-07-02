import { describe, expect, it } from 'vitest'
import {
  difficultyLabel, dualName, magicSkillLabel, MAX_SPELL_DIFFICULTY, parseDifficulty, secondaryName,
  spellDifficulty, TALENT_CATEGORY_LABELS, wouldExceedSpellCap,
} from './labels'

describe('RU/ENG отображение (secondaryName / dualName)', () => {
  it('показывает английское название вторичным, когда оно отличается', () => {
    const skill = { name: 'Melee', nameRu: 'Ближний бой' }
    expect(secondaryName(skill)).toBe('Melee')
    expect(dualName(skill)).toBe('Ближний бой / Melee')
  })

  it('не дублирует название, когда русского нет или оно совпадает', () => {
    expect(secondaryName({ name: 'Melee', nameRu: '' })).toBe('')
    expect(dualName({ name: 'Melee', nameRu: '' })).toBe('Melee')
    expect(secondaryName({ name: 'Заклинание', nameRu: 'Заклинание' })).toBe('')
  })

  it('игнорирует регистр и пробелы при сравнении', () => {
    expect(secondaryName({ name: ' melee ', nameRu: 'Melee' })).toBe('')
  })
})

describe('spellDifficulty / wouldExceedSpellCap (потолок 5)', () => {
  it('складывает базовую сложность и доп. эффекты', () => {
    expect(spellDifficulty('2 (Average)', [])).toBe(2)
    expect(spellDifficulty('2 (Average)', ['+1', '+2'])).toBe(5)
  })

  it('блокирует эффект, превышающий потолок 5', () => {
    expect(wouldExceedSpellCap('2 (Average)', ['+1', '+2'], '+1')).toBe(true)
    expect(wouldExceedSpellCap('2 (Average)', ['+1'], '+2')).toBe(false)
    expect(wouldExceedSpellCap('3 (Hard)', [], '+2')).toBe(false)
    expect(wouldExceedSpellCap('3 (Hard)', ['+2'], '+1')).toBe(true)
  })

  it('потолок равен 5', () => {
    expect(MAX_SPELL_DIFFICULTY).toBe(5)
  })
})

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

describe('TALENT_CATEGORY_LABELS', () => {
  it('подписывает категории талантов по-русски', () => {
    expect(TALENT_CATEGORY_LABELS.general).toBe('Общие')
    expect(TALENT_CATEGORY_LABELS.social).toBe('Социальные')
    expect(TALENT_CATEGORY_LABELS.combat).toBe('Боевые')
    expect(TALENT_CATEGORY_LABELS.magic).toBe('Магические')
  })
})
