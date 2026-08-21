import { describe, expect, it } from 'vitest'
import { itemTags, qualityProperties, repeatsProperties } from './itemQualities'

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

describe('qualityProperties — структурные качества предмета', () => {
  it('канонизирует английское имя и сохраняет рейтинг', () => {
    expect(qualityProperties([{ nameRu: 'Vicious', nameEn: 'Vicious', rating: 2 }]))
      .toBe('Высококритичное 2')
  })

  it('не добавляет нулевой рейтинг безымянному качеству', () => {
    expect(qualityProperties([{ nameRu: 'Превосходное', nameEn: 'Superior', rating: 0 }]))
      .toBe('Превосходное')
  })
})

/**
 * У части записей каталога в описание попал тот же список качеств, который уже показан тегами с
 * тултипами: «Высококритичное 1» и там, и там. Правило отличает такой пересказ от настоящего
 * описания — иначе вместе с дублями пропали бы и осмысленные тексты.
 */
describe('repeatsProperties — описание всего лишь пересказывает свойства', () => {
  it('дословный повтор списка свойств', () => {
    expect(repeatsProperties('Высококритичное 1', 'Высококритичное 1')).toBe(true)
  })

  it('повтор на другом языке: «Vicious 1» — то же качество, что «Высококритичное 1»', () => {
    expect(repeatsProperties('Vicious 1', 'Высококритичное 1')).toBe(true)
  })

  it('порядок и пробелы не важны', () => {
    expect(repeatsProperties('Точное 1,Оборонительное 2', 'Оборонительное 2, Точное 1')).toBe(true)
  })

  it('настоящее описание остаётся: проза в качества не разбирается', () => {
    expect(repeatsProperties(
      'Магический длинный лук с высоким качеством и увеличенной дальностью.',
      'Точное 1, Громоздкое 3')).toBe(false)
  })

  it('другой рейтинг — уже не повтор: число несёт смысл', () => {
    expect(repeatsProperties('Оборонительное 1', 'Оборонительное 2')).toBe(false)
  })

  it('описание короче списка свойств — не повтор', () => {
    expect(repeatsProperties('Точное 1', 'Точное 1, Оборонительное 2')).toBe(false)
  })

  it('пустые значения повтором не считаются', () => {
    expect(repeatsProperties('', 'Точное 1')).toBe(false)
    expect(repeatsProperties(null, null)).toBe(false)
    expect(repeatsProperties('Точное 1', null)).toBe(false)
  })
})
