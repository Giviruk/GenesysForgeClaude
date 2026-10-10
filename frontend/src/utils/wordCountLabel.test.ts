import { describe, expect, it } from 'vitest'
import { wordCountLabel } from './wordCountLabel'

describe('word count labels', () => {
  it.each([[0, 'слов'], [1, 'слово'], [2, 'слова'], [3, 'слова'], [5, 'слов'], [11, 'слов'], [21, 'слово'], [22, 'слова'], [25, 'слов']])('inflects %i Russian words', (count, suffix) => {
    expect(wordCountLabel(count as number, 'ru')).toBe(`${count} ${suffix}`)
  })
  it.each([[0, 'words'], [1, 'word'], [2, 'words'], [21, 'words']])('inflects %i English words', (count, suffix) => {
    expect(wordCountLabel(count as number, 'en')).toBe(`${count} ${suffix}`)
  })
})
