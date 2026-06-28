import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { describe, expect, it, vi, beforeEach } from 'vitest'
import type { CharacterSheet } from '../api/types'
import { BioTab } from './BioTab'

const updateMock = vi.fn()
vi.mock('../api/client', () => ({
  api: { updateCharacter: (...a: unknown[]) => updateMock(...a) },
}))

const sheet = {
  id: 'char-1',
  desire: 'Найти дом',
  fear: null,
  strength: null,
  flaw: null,
  background: null,
} as unknown as CharacterSheet

describe('BioTab (U-22)', () => {
  beforeEach(() => {
    updateMock.mockReset()
    updateMock.mockResolvedValue(undefined)
  })

  it('предзаполняет поля и блокирует сохранение без изменений', () => {
    render(<BioTab sheet={sheet} onError={() => {}} refresh={() => Promise.resolve()} />)
    expect((screen.getByLabelText('Стремление') as HTMLInputElement).value).toBe('Найти дом')
    expect((screen.getByRole('button', { name: 'Сохранить' }) as HTMLButtonElement).disabled).toBe(true)
  })

  it('сохраняет изменённые мотивации и предысторию', async () => {
    const refresh = vi.fn().mockResolvedValue(undefined)
    render(<BioTab sheet={sheet} onError={() => {}} refresh={refresh} />)

    fireEvent.change(screen.getByLabelText('Страх'), { target: { value: 'Высота' } })
    fireEvent.change(screen.getByLabelText('Предыстория'), { target: { value: 'Вырос в горах.' } })
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }))

    await waitFor(() => expect(updateMock).toHaveBeenCalledWith('char-1', {
      desire: 'Найти дом', fear: 'Высота', strength: '', flaw: '', background: 'Вырос в горах.',
    }))
    expect(refresh).toHaveBeenCalled()
  })
})
