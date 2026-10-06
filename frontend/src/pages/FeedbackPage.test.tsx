import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { FeedbackPage } from './FeedbackPage'

const sendFeedback = vi.hoisted(() => vi.fn())
vi.mock('../api/client', () => ({ api: { sendFeedback } }))

afterEach(() => { cleanup(); sendFeedback.mockReset(); window.history.replaceState(null, '', '/') })

describe('FeedbackPage', () => {
  it('sends the message with the page it was opened from and thanks the user', async () => {
    sendFeedback.mockResolvedValue(undefined)
    window.history.replaceState(null, '', '/feedback?from=%2Fshop')
    render(<FeedbackPage loggedIn />)

    fireEvent.change(screen.getByLabelText('Сообщение'), { target: { value: 'Не открывается магазин' } })
    fireEvent.change(screen.getByLabelText(/Почта для ответа/), { target: { value: 'me@example.com' } })
    fireEvent.click(screen.getByRole('button', { name: 'Отправить' }))

    await waitFor(() => expect(screen.getByText('Спасибо! Сообщение отправлено.')).toBeTruthy())
    expect(sendFeedback).toHaveBeenCalledWith({ message: 'Не открывается магазин', email: 'me@example.com', page: '/shop', website: undefined })
  })

  it('shows the server error and keeps the form', async () => {
    sendFeedback.mockRejectedValue(new Error('Похоже, в адресе почты опечатка.'))
    render(<FeedbackPage loggedIn={false} />)

    fireEvent.change(screen.getByLabelText('Сообщение'), { target: { value: 'Длинное сообщение' } })
    fireEvent.click(screen.getByRole('button', { name: 'Отправить' }))

    await waitFor(() => expect(screen.getByText('Похоже, в адресе почты опечатка.')).toBeTruthy())
    expect(screen.getByRole('button', { name: 'Отправить' })).toBeTruthy()
  })
})
