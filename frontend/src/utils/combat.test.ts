import { describe, expect, it } from 'vitest'
import type { Quality } from '../api/types'
import { combatTotal, expandDamage, qualitiesFromProperties, resolveQualityCosts } from './combat'

describe('expandDamage — раскрытие урона оружия', () => {
  it('«+N» в ближнем бою = Мощь + N', () => {
    expect(expandDamage('+2', 3)).toEqual({ base: 5, text: '5 (Мощь +2)' })
  })
  it('абсолютное число — как есть', () => {
    expect(expandDamage('7', 3)).toEqual({ base: 7, text: '7' })
  })
  it('пусто/текст — base null', () => {
    expect(expandDamage('', 3)).toEqual({ base: null, text: '—' })
    expect(expandDamage('особый', 3).base).toBeNull()
  })
})

describe('combatTotal — урон + нетто-успехи', () => {
  it('каждый успех = +1 урон', () => {
    expect(combatTotal(5, 3)).toBe(8)
    expect(combatTotal(7, 0)).toBe(7)
  })
  it('null base → null', () => {
    expect(combatTotal(null, 4)).toBeNull()
  })
  it('отрицательные успехи (промах) не уменьшают базу', () => {
    expect(combatTotal(5, -2)).toBe(5)
  })
})

const reference = {
  qualities: [
    { code: 'pierce', nameRu: 'Пробивание', nameEn: 'Pierce', activationCost: 'пассивно', hasRating: true } as Quality,
    { code: 'stun', nameRu: 'Оглушение', nameEn: 'Stun', activationCost: '2 преимущества', hasRating: false } as Quality,
  ],
}

describe('resolveQualityCosts — цена активации по справочнику', () => {
  it('сопоставляет по коду и подставляет рейтинг в подпись', () => {
    const r = resolveQualityCosts([{ code: 'pierce', label: 'Пробивание', rating: 2 }], reference)
    expect(r[0]).toEqual({ label: 'Пробивание 2', activationCost: 'пассивно' })
  })
  it('фолбэк по имени, если нет кода', () => {
    const r = resolveQualityCosts([{ label: 'Оглушение', rating: null }], reference)
    expect(r[0].activationCost).toBe('2 преимущества')
  })
  it('неизвестное качество — без цены', () => {
    expect(resolveQualityCosts([{ label: 'Самопал', rating: null }], reference)[0].activationCost).toBe('')
  })
})

describe('qualitiesFromProperties — разбор строки свойств', () => {
  it('парсит имя+рейтинг и резолвит цену', () => {
    const r = qualitiesFromProperties('Пробивание 3, Оглушение', reference)
    expect(r).toEqual([
      { label: 'Пробивание 3', activationCost: 'пассивно' },
      { label: 'Оглушение', activationCost: '2 преимущества' },
    ])
  })
  it('пустая строка → пусто', () => {
    expect(qualitiesFromProperties('', reference)).toEqual([])
  })
})
