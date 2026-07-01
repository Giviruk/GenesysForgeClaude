import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { HelpPage } from './HelpPage'

describe('HelpPage', () => {
  it('renders user guide sections from markdown', () => {
    render(<HelpPage loggedIn={false} />)

    expect(screen.getByRole('heading', { name: 'Справка' })).toBeTruthy()
    expect(screen.getByRole('heading', { name: 'Быстрый маршрут игрока' })).toBeTruthy()
    expect(screen.getByRole('heading', { name: 'Энкаунтеры и игровой стол' })).toBeTruthy()
    expect(screen.getAllByText(/PublicSafe/i).length).toBeGreaterThan(0)
    expect(screen.getByRole('button', { name: /Назад/i })).toBeTruthy()
  })
})
