import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { CharacterListItem, CharacterSheet, ItemDef, Reference } from '../api/types'
import { ShopPage } from './ShopPage'

const charactersMock = vi.fn()
const referenceMock = vi.fn()
const sheetMock = vi.fn()
const addItemMock = vi.fn()
const buyServiceMock = vi.fn()
const buyAttachmentMock = vi.fn()

vi.mock('../api/client', () => ({
  api: {
    characters: (...args: unknown[]) => charactersMock(...args),
    reference: (...args: unknown[]) => referenceMock(...args),
    sheet: (...args: unknown[]) => sheetMock(...args),
    addItem: (...args: unknown[]) => addItemMock(...args),
    buyService: (...args: unknown[]) => buyServiceMock(...args),
    buyAttachment: (...args: unknown[]) => buyAttachmentMock(...args),
  },
}))

const character = {
  id: 'char-1',
  name: 'Аэлла',
  system: 'realmsOfTerrinoth',
  archetype: 'Human',
  career: 'Warrior',
} as unknown as CharacterListItem

const rope = {
  id: 'item-rope',
  code: 'rot.item.rope',
  name: 'Rope',
  nameRu: 'Верёвка',
  kind: 'gear',
  shopCategory: 'gear',
  price: 10,
  rarity: 0,
  source: 'Realms of Terrinoth',
  description: 'Надёжная верёвка.',
  safeDescription: 'Надёжная верёвка.',
  purchasable: true,
  implement: null,
} as unknown as ItemDef

const service = {
  ...rope,
  id: 'item-service',
  code: 'rot.item.service-bath',
  name: 'Bath',
  nameRu: 'Баня',
  shopCategory: 'service',
  price: 2,
  description: 'Помыться и отдохнуть.',
  safeDescription: 'Помыться и отдохнуть.',
} as unknown as ItemDef

const reference = {
  items: [rope, service],
  attachments: [],
} as unknown as Reference

const sheet = {
  id: 'char-1',
  money: 100,
  startingPurchaseBudget: 0,
  isCreationPhase: false,
  items: [],
} as unknown as CharacterSheet

describe('Общий магазин', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    charactersMock.mockResolvedValue([character])
    referenceMock.mockResolvedValue(reference)
    sheetMock.mockResolvedValue(sheet)
    addItemMock.mockResolvedValue({ id: 'new-item' })
    buyServiceMock.mockResolvedValue(undefined)
    buyAttachmentMock.mockResolvedValue(undefined)
  })

  it('покупает физический предмет в инвентарь выбранного персонажа', async () => {
    render(<ShopPage />)

    fireEvent.click(await screen.findByRole('button', { name: /Верёвка/ }))
    const dialog = screen.getByRole('dialog')
    await waitFor(() => expect(
      (within(dialog).getByRole('button', { name: 'Купить' }) as HTMLButtonElement).disabled,
    ).toBe(false))
    fireEvent.click(within(dialog).getByRole('button', { name: 'Купить' }))

    await waitFor(() => expect(addItemMock).toHaveBeenCalledWith(
      'char-1', 'item-rope', 1, 'carried', { free: false },
    ))
    expect(buyServiceMock).not.toHaveBeenCalled()
  })

  it('оказывает услугу без записи в инвентарь', async () => {
    render(<ShopPage />)

    fireEvent.click(await screen.findByRole('button', { name: /Баня/ }))
    const dialog = screen.getByRole('dialog')
    expect(within(dialog).getByText(/никогда не появляется в инвентаре/)).toBeTruthy()
    await waitFor(() => expect(
      (within(dialog).getByRole('button', { name: 'Купить' }) as HTMLButtonElement).disabled,
    ).toBe(false))
    fireEvent.click(within(dialog).getByRole('button', { name: 'Купить' }))

    await waitFor(() => expect(buyServiceMock).toHaveBeenCalledWith(
      'char-1', 'item-service', 1, false,
    ))
    expect(addItemMock).not.toHaveBeenCalled()
    expect(await within(dialog).findByText(/В инвентарь ничего не добавлено/)).toBeTruthy()
  })

  it('бесплатное оказание услуги также не вызывает добавление предмета', async () => {
    render(<ShopPage />)

    fireEvent.click(await screen.findByRole('button', { name: /Баня/ }))
    const dialog = screen.getByRole('dialog')
    fireEvent.click(within(dialog).getByRole('button', { name: '+ Добавить без оплаты' }))

    await waitFor(() => expect(buyServiceMock).toHaveBeenCalledWith(
      'char-1', 'item-service', 1, true,
    ))
    expect(addItemMock).not.toHaveBeenCalled()
  })
})
