import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { api } from '../api/client'
import { NotesTab } from './NotesTab'

vi.mock('../api/client', () => ({ api: {
  notes: vi.fn(), createNote: vi.fn(), updateNote: vi.fn(), deleteNote: vi.fn(),
} }))
const note = { id: 'n1', title: 'Зацепка', body: 'Поговорить с кузнецом.',
  createdAt: '2026-10-10T12:00:00Z', updatedAt: '2026-10-10T12:00:00Z' }

describe('Private notes', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(api.notes).mockResolvedValue([note])
    vi.mocked(api.createNote).mockResolvedValue({ ...note, id: 'n2' })
    vi.mocked(api.updateNote).mockResolvedValue(note)
    vi.mocked(api.deleteNote).mockResolvedValue(undefined)
  })
  it('opens an empty form and creates a body-only note', async () => {
    render(<NotesTab characterId="c1" onError={() => {}} />)
    await screen.findByText('Зацепка')
    fireEvent.click(screen.getByRole('button', { name: 'Новая' }))
    expect(screen.getByRole('button', { name: 'Добавить' }).hasAttribute('disabled')).toBe(true)
    fireEvent.change(screen.getByLabelText('Текст'), { target: { value: 'Новая зацепка' } })
    fireEvent.click(screen.getByRole('button', { name: 'Добавить' }))
    await waitFor(() => expect(api.createNote).toHaveBeenCalledWith('c1', 'Без заголовка', 'Новая зацепка'))
    expect(screen.queryByLabelText('Текст')).toBeNull()
  })
  it('edits in place and confirms deletion in the note card', async () => {
    render(<NotesTab characterId="c1" onError={() => {}} />)
    await screen.findByText('Зацепка')
    fireEvent.click(screen.getByRole('button', { name: 'Изменить' }))
    fireEvent.change(screen.getByLabelText('Заголовок'), { target: { value: 'План' } })
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }))
    await waitFor(() => expect(api.updateNote).toHaveBeenCalledWith('c1', 'n1', 'План', note.body))
    await screen.findByRole('button', { name: 'Удалить' })
    fireEvent.click(screen.getByRole('button', { name: 'Удалить' }))
    expect(api.deleteNote).not.toHaveBeenCalled()
    fireEvent.click(within(document.querySelector('.note-delete-confirm')!).getByRole('button', { name: 'Отмена' }))
    expect(screen.queryByText('Удалить заметку?')).toBeNull()
    fireEvent.click(screen.getByRole('button', { name: 'Удалить' }))
    vi.mocked(api.notes).mockResolvedValue([])
    fireEvent.click(within(document.querySelector('.note-delete-confirm')!).getByRole('button', { name: 'Удалить' }))
    await waitFor(() => expect(api.deleteNote).toHaveBeenCalledWith('c1', 'n1'))
    expect(await screen.findByText('Заметок пока нет')).toBeTruthy()
  })
})
