import { render, screen } from '@testing-library/react'
import { expect, it } from 'vitest'
import { DicePoolView } from './DicePoolView'
import { GenesysSymbol } from './GenesysSymbol'

it('keeps the pool source tooltip available over every die', () => {
  const { container } = render(<DicePoolView pool={{ ability: 1, proficiency: 2 }} setback={1}
    boost={1} difficulty={1} challenge={1} setbackTitle="+1 кость штрафа — Броня" />)
  expect(container.querySelector('.dice-pool')?.getAttribute('title')).toContain('+1 кость штрафа — Броня')
  for (const glyph of screen.getAllByRole('img')) expect(glyph.getAttribute('title')).toBeNull()
  expect(screen.getByRole('img', { name: 'Кость штрафа' })).toBeTruthy()
})

it('decorative glyphs do not override their control tooltip or accessible name', () => {
  const { container } = render(<button title="Источник модификатора" aria-label="Добавить штраф">
    <GenesysSymbol kind="setback" decorative />
  </button>)
  expect(container.querySelector('.genesys-symbol')?.getAttribute('title')).toBeNull()
  expect(screen.getByRole('button', { name: 'Добавить штраф' })).toBeTruthy()
})
