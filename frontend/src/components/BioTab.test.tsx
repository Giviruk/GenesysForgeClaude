import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { describe, expect, it, vi, beforeEach } from 'vitest'
import type { CharacterSheet } from '../api/types'
import { BioTab } from './BioTab'
import { api } from '../api/client'

const updateMock = vi.fn()
vi.mock('../api/client', () => ({
  api: { notes: vi.fn().mockResolvedValue([]), updateCharacter: (...a: unknown[]) => updateMock(...a) },
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
    vi.mocked(api.notes).mockClear()
    updateMock.mockReset()
    updateMock.mockResolvedValue(undefined)
  })

  it('tracks completion, words and unsaved/saved changes', async () => {
    render(<BioTab sheet={sheet} onError={() => {}} refresh={async () => {}} />)
    expect(screen.getByText('Заполнено 1 из 5')).toBeTruthy()
    for (const label of ['Страх', 'Сильная сторона', 'Слабость']) {
      fireEvent.change(screen.getByLabelText(label), { target: { value: 'Черта' } })
    }
    fireEvent.change(screen.getByLabelText('Предыстория'), { target: { value: 'Один  два\nтри' } })
    expect(screen.getByText('Образ заполнен')).toBeTruthy()
    expect(screen.getByText('3 слов')).toBeTruthy()
    expect(screen.getByRole('status').textContent).toContain('несохранённые')
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }))
    await waitFor(() => expect(screen.getByRole('status').textContent).toBe('Сохранено'))
    expect(screen.getByRole('button', { name: 'Сохранить' }).hasAttribute('disabled')).toBe(true)
  })

  it('opens the existing portrait uploader callback', () => {
    const upload = vi.fn()
    render(<BioTab sheet={sheet} onPortraitClick={upload} />)
    fireEvent.click(screen.getByRole('button', { name: 'портрет' }))
    expect(upload).toHaveBeenCalledOnce()
  })

  it('renders the campaign bio without note requests or save controls', () => {
    render(<BioTab sheet={sheet} readOnly />)
    expect(screen.getByLabelText('Стремление')).toHaveProperty('readOnly', true)
    expect(screen.getByLabelText('Предыстория')).toHaveProperty('readOnly', true)
    expect(screen.queryByRole('button')).toBeNull()
    expect(screen.queryByText('Заметки')).toBeNull()
    expect(api.notes).not.toHaveBeenCalled()
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
