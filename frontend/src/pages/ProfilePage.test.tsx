import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { describe, expect, it, vi, beforeEach } from 'vitest'
import type { Account } from '../api/types'
import { ProfilePage } from './ProfilePage'

const account: Account = {
  id: 'u1', email: 'gm@test.local', displayName: 'Мастер', avatarUrl: null, createdAt: '',
}

const accountMock = vi.fn()
const updateMock = vi.fn()
const changePasswordMock = vi.fn()
const uploadAvatarMock = vi.fn()
vi.mock('../api/client', () => ({
  api: {
    account: () => accountMock(),
    updateAccount: (...a: unknown[]) => updateMock(...a),
    changePassword: (...a: unknown[]) => changePasswordMock(...a),
    uploadAvatar: (...a: unknown[]) => uploadAvatarMock(...a),
  },
}))

describe('ProfilePage (U-21)', () => {
  beforeEach(() => {
    accountMock.mockResolvedValue(account)
    updateMock.mockResolvedValue({ ...account, displayName: 'Новый' })
    changePasswordMock.mockResolvedValue(undefined)
    uploadAvatarMock.mockClear()
    uploadAvatarMock.mockResolvedValue({ ...account, avatarUrl: 'https://storage.test/avatars/u1/x.png' })
  })

  it('показывает профиль и сохраняет имя/аватар', async () => {
    render(<ProfilePage onBack={() => {}} />)
    await waitFor(() => expect(screen.getByText('gm@test.local')).toBeTruthy())
    expect((screen.getByLabelText(/Отображаемое имя/) as HTMLInputElement).value).toBe('Мастер')

    fireEvent.change(screen.getByLabelText(/Отображаемое имя/), { target: { value: 'Новый' } })
    fireEvent.change(screen.getByLabelText(/URL аватара/), { target: { value: 'https://cdn/a.png' } })
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }))

    await waitFor(() => expect(updateMock).toHaveBeenCalledWith({ displayName: 'Новый', avatarUrl: 'https://cdn/a.png' }))
    await waitFor(() => expect(screen.getByText('Сохранено.')).toBeTruthy())
  })

  it('валидирует совпадение паролей до запроса', async () => {
    render(<ProfilePage onBack={() => {}} />)
    await waitFor(() => expect(screen.getByText('gm@test.local')).toBeTruthy())

    fireEvent.change(screen.getByLabelText(/Текущий пароль/), { target: { value: 'old123' } })
    fireEvent.change(screen.getByLabelText(/^Новый пароль/), { target: { value: 'newpass1' } })
    fireEvent.change(screen.getByLabelText(/Повтор нового пароля/), { target: { value: 'mismatch' } })
    fireEvent.click(screen.getByRole('button', { name: 'Изменить пароль' }))

    expect(screen.getByText('Пароли не совпадают.')).toBeTruthy()
    expect(changePasswordMock).not.toHaveBeenCalled()
  })

  it('меняет пароль при корректном вводе', async () => {
    render(<ProfilePage onBack={() => {}} />)
    await waitFor(() => expect(screen.getByText('gm@test.local')).toBeTruthy())

    fireEvent.change(screen.getByLabelText(/Текущий пароль/), { target: { value: 'old123' } })
    fireEvent.change(screen.getByLabelText(/^Новый пароль/), { target: { value: 'newpass1' } })
    fireEvent.change(screen.getByLabelText(/Повтор нового пароля/), { target: { value: 'newpass1' } })
    fireEvent.click(screen.getByRole('button', { name: 'Изменить пароль' }))

    await waitFor(() => expect(changePasswordMock).toHaveBeenCalledWith('old123', 'newpass1'))
    await waitFor(() => expect(screen.getByText('Пароль изменён.')).toBeTruthy())
  })

  it('загружает файл аватара и показывает новый URL', async () => {
    render(<ProfilePage onBack={() => {}} />)
    await waitFor(() => expect(screen.getByText('gm@test.local')).toBeTruthy())

    const file = new File([new Uint8Array([0x89, 0x50, 0x4e, 0x47])], 'a.png', { type: 'image/png' })
    fireEvent.change(screen.getByTestId('avatar-file'), { target: { files: [file] } })

    await waitFor(() => expect(uploadAvatarMock).toHaveBeenCalledWith(file))
    await waitFor(() => expect((screen.getByLabelText(/URL аватара/) as HTMLInputElement).value)
      .toBe('https://storage.test/avatars/u1/x.png'))
    expect(screen.getByText('Сохранено.')).toBeTruthy()
  })

  it('отклоняет файл больше 5 МБ без запроса', async () => {
    render(<ProfilePage onBack={() => {}} />)
    await waitFor(() => expect(screen.getByText('gm@test.local')).toBeTruthy())

    const big = new File(['x'], 'big.png', { type: 'image/png' })
    Object.defineProperty(big, 'size', { value: 5 * 1024 * 1024 + 1 }) // jsdom не считает size из ArrayBuffer
    fireEvent.change(screen.getByTestId('avatar-file'), { target: { files: [big] } })

    await waitFor(() => expect(screen.getByText('Файл больше 5 МБ.')).toBeTruthy())
    expect(uploadAvatarMock).not.toHaveBeenCalled()
  })
})
