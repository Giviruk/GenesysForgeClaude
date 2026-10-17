import '@testing-library/jest-dom/vitest'
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, expect, it, vi } from 'vitest'
import { NpcPortraitEditor } from './NpcPortraitEditor'
import { Portrait } from './Portrait'

const upload = vi.fn()
const remove = vi.fn()
vi.mock('../api/client', () => ({ api: {
  uploadNpcPortrait: (...args: unknown[]) => upload(...args),
  deleteNpcPortrait: (...args: unknown[]) => remove(...args),
} }))

beforeEach(() => { upload.mockReset(); remove.mockReset() })

it('uploads, replaces and removes the preview without submitting the NPC form', async () => {
  upload.mockResolvedValueOnce({ portraitUrl: '/one.png' }).mockResolvedValueOnce({ portraitUrl: '/two.png' })
  remove.mockResolvedValue(undefined)
  const submit = vi.fn(event => event.preventDefault())
  render(<form onSubmit={submit}><NpcPortraitEditor npcId="npc" name="Гоблин" /></form>)
  const input = screen.getByLabelText('Файл портрета NPC') as HTMLInputElement
  const file = new File(['image'], 'portrait.png', { type: 'image/png' })
  fireEvent.change(input, { target: { files: [file] } })
  expect(await screen.findByRole('img', { name: 'Гоблин' })).toHaveAttribute('src', '/one.png')
  expect(input.value).toBe('')
  expect(upload).toHaveBeenCalledWith('npc', file)
  fireEvent.change(input, { target: { files: [file] } })
  await waitFor(() => expect(screen.getByRole('img')).toHaveAttribute('src', '/two.png'))
  fireEvent.click(screen.getByRole('button', { name: 'Удалить портрет' }))
  await waitFor(() => expect(screen.queryByRole('img')).toBeNull())
  expect(remove).toHaveBeenCalledWith('npc')
  expect(submit).not.toHaveBeenCalled()
})

it('rejects oversize and unsupported files, and preserves the preview on API errors', async () => {
  render(<NpcPortraitEditor npcId="npc" name="Гоблин" initialUrl="/old.png" />)
  const input = screen.getByLabelText('Файл портрета NPC')
  const large = new File(['x'], 'large.png', { type: 'image/png' })
  Object.defineProperty(large, 'size', { value: 5 * 1024 * 1024 + 1 })
  fireEvent.change(input, { target: { files: [large] } })
  expect(await screen.findByRole('alert')).toHaveTextContent('Файл больше 5 МБ.')
  fireEvent.change(input, { target: { files: [new File(['<svg/>'], 'file.svg', { type: 'image/svg+xml' })] } })
  expect(await screen.findByRole('alert')).toHaveTextContent('JPEG, PNG и WebP')
  expect(upload).not.toHaveBeenCalled()
  upload.mockRejectedValue(new Error('Нет доступа'))
  fireEvent.change(input, { target: { files: [new File(['x'], 'file.webp', { type: 'image/webp' })] } })
  expect(await screen.findByRole('alert')).toHaveTextContent('Нет доступа')
  expect(screen.getByRole('img')).toHaveAttribute('src', '/old.png')
  remove.mockRejectedValue(new Error('Ошибка удаления'))
  fireEvent.click(screen.getByRole('button', { name: 'Удалить портрет' }))
  expect(await screen.findByRole('alert')).toHaveTextContent('Ошибка удаления')
  expect(screen.getByRole('img')).toHaveAttribute('src', '/old.png')
})

it('blocks concurrent uploads and deletion until the upload completes', async () => {
  let finish!: (value: { portraitUrl: string }) => void
  upload.mockReturnValue(new Promise(resolve => { finish = resolve }))
  render(<NpcPortraitEditor npcId="npc" name="Гоблин" initialUrl="/old.png" />)
  const input = screen.getByLabelText('Файл портрета NPC')
  const file = new File(['x'], 'file.jpg', { type: 'image/jpeg' })
  fireEvent.change(input, { target: { files: [file] } })
  fireEvent.change(input, { target: { files: [file] } })
  expect(upload).toHaveBeenCalledTimes(1)
  expect(screen.getByRole('button', { name: 'Удалить портрет' })).toBeDisabled()
  await act(async () => finish({ portraitUrl: '/new.jpg' }))
  expect(screen.getByRole('img')).toHaveAttribute('src', '/new.jpg')
})

it('retries a new portrait URL after an old image failed', () => {
  const { rerender } = render(<Portrait url="/old.png" name="Гоблин" fallback={<span>Г</span>} />)
  fireEvent.error(screen.getByRole('img'))
  expect(screen.getByText('Г')).toBeInTheDocument()
  rerender(<Portrait url="/new.png" name="Гоблин" fallback={<span>Г</span>} />)
  expect(screen.getByRole('img')).toHaveAttribute('src', '/new.png')
})
