import { expect, it } from 'vitest'
import { countLabel } from './countLabel'
import { genesysCountLabel } from '../data/genesysSymbols'

it.each([[0, 'костей'], [1, 'кость'], [2, 'кости'], [5, 'костей'], [11, 'костей'], [21, 'кость'], [22, 'кости']])(
  'formats Russian die counts: %s', (count, word) => {
    expect(genesysCountLabel(Number(count), 'setback')).toBe(`${count} ${word} штрафа`)
  },
)
it('uses English singular/plural and Russian negative counts correctly', () => {
  expect(countLabel(1, ['threat', 'threats', 'threats'], 'en')).toBe('1 threat')
  expect(countLabel(2, ['threat', 'threats', 'threats'], 'en')).toBe('2 threats')
  expect(genesysCountLabel(-2, 'threat')).toBe('-2 осложнения')
  expect(genesysCountLabel(5, 'despair')).toBe('5 крахов')
})
