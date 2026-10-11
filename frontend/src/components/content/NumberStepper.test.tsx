import { useState } from 'react'
import '@testing-library/jest-dom/vitest'
import { fireEvent, render, screen } from '@testing-library/react'
import { expect, it } from 'vitest'
import { NumberStepper } from './NumberStepper'

function Stepper({ bounded }: { bounded: boolean }) {
  const [value, setValue] = useState(6)
  return <NumberStepper label="Мощь" value={value} onChange={setValue} min={1} max={bounded ? 6 : undefined} />
}

it('enforces characteristic bounds for typed values and button actions', () => {
  render(<Stepper bounded />)
  const input = screen.getByRole('spinbutton', { name: 'Мощь' })
  expect(screen.getByRole('button', { name: 'Увеличить: Мощь' })).toBeDisabled()
  fireEvent.change(input, { target: { value: '9' } })
  expect(input).toHaveValue(6)
  fireEvent.change(input, { target: { value: '0' } })
  expect(input).toHaveValue(1)
  expect(screen.getByRole('button', { name: 'Уменьшить: Мощь' })).toBeDisabled()
  fireEvent.click(screen.getByRole('button', { name: 'Увеличить: Мощь' }))
  expect(input).toHaveValue(2)
})

it('keeps existing unbounded steppers free to increase above six', () => {
  render(<Stepper bounded={false} />)
  fireEvent.click(screen.getByRole('button', { name: 'Увеличить: Мощь' }))
  expect(screen.getByRole('spinbutton', { name: 'Мощь' })).toHaveValue(7)
})
