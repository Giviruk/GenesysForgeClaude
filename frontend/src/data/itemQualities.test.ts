import { describe, expect, it } from 'vitest'
import { itemTags } from './itemQualities'

describe('itemTags — нормализованные теги из свойств', () => {
  it('убирает числовой рейтинг и берёт каноничное имя свойства', () => {
    // «Оборонительное 2» → известное качество → его nameRu без рейтинга
    expect(itemTags('Оборонительное 2')).toEqual(['Оборонительное'])
  })

  it('разбивает по запятым и убирает дубликаты разных рейтингов', () => {
    const tags = itemTags('Оборонительное 1, Оборонительное 2, Точное 1')
    expect(tags).toContain('Оборонительное')
    expect(tags).toContain('Точное')
    // «Оборонительное 1» и «Оборонительное 2» схлопываются в один тег
    expect(tags.filter(t => t === 'Оборонительное')).toHaveLength(1)
  })

  it('неизвестное свойство остаётся тегом без числа', () => {
    expect(itemTags('Хитрое 3')).toEqual(['Хитрое'])
  })

  it('пустые/нулевые свойства дают пустой список', () => {
    expect(itemTags('')).toEqual([])
    expect(itemTags(null)).toEqual([])
    expect(itemTags(undefined)).toEqual([])
  })
})
