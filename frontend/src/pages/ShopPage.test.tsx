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

const sword = {
  ...rope,
  id: 'item-sword',
  code: 'rot.item.sword',
  name: 'Sword',
  nameRu: 'Меч',
  kind: 'weapon',
  shopCategory: 'weaponLight',
  price: 100,
  rarity: 1,
  properties: 'Высококритичное 1, Оборонительное 1',
  description: 'Надёжный клинок.',
  safeDescription: 'Надёжный клинок.',
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

const staff = {
  ...rope,
  id: 'item-staff',
  code: 'rot.item.magic-staff',
  name: 'Magic Staff',
  nameRu: 'Магический посох',
  price: 400,
  rarity: 6,
  implement: { code: 'magic-staff' },
} as unknown as ItemDef

const reference = {
  items: [rope, sword, staff, service],
  attachments: [],
} as unknown as Reference

const sheet = {
  id: 'char-1',
  money: 5000,
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

  it('показывает и отправляет цену материала обычного и магического снаряжения', async () => {
    render(<ShopPage />)

    fireEvent.click(await screen.findByRole('button', { name: /Меч/ }))
    let dialog = screen.getByRole('dialog')
    await waitFor(() => expect(
      (within(dialog).getByRole('button', { name: 'Купить' }) as HTMLButtonElement).disabled,
    ).toBe(false))
    fireEvent.change(within(dialog).getByLabelText(/Материал \/ качество изготовления/),
      { target: { value: 'iron' } })
    expect(within(dialog).getAllByText(/50 монеты/)).toHaveLength(2)
    fireEvent.click(within(dialog).getByRole('button', { name: 'Купить' }))
    await waitFor(() => expect(addItemMock).toHaveBeenLastCalledWith(
      'char-1', 'item-sword', 1, 'carried', { free: false, craftsmanship: 'iron' },
    ))

    fireEvent.click(within(dialog).getByRole('button', { name: 'Закрыть' }))
    fireEvent.click(await screen.findByRole('button', { name: /Магический посох/ }))
    dialog = screen.getByRole('dialog')
    await waitFor(() => expect(
      (within(dialog).getByRole('button', { name: 'Купить' }) as HTMLButtonElement).disabled,
    ).toBe(false))
    fireEvent.change(within(dialog).getAllByRole('combobox')[1], { target: { value: 'willow' } })
    expect(within(dialog).getAllByText(/800 монеты/)).toHaveLength(2)
    fireEvent.click(within(dialog).getByRole('button', { name: 'Купить' }))
    await waitFor(() => expect(addItemMock).toHaveBeenLastCalledWith(
      'char-1', 'item-staff', 1, 'carried', { free: false, material: 'willow' },
    ))
  })

  it('показывает у оружия теги свойств с теми же тултипами, что и магазин инвентаря', async () => {
    render(<ShopPage />)

    const swordButton = await screen.findByRole('button', { name: /Меч/ })
    const viciousTag = screen.getByText('Высококритичное 1')
    fireEvent.click(viciousTag)

    expect(screen.queryByRole('dialog')).toBeNull()
    const tooltip = screen.getByRole('tooltip')
    expect(tooltip.textContent).toContain('Высококритичное')
    expect(tooltip.textContent).toContain('критической травмы')

    fireEvent.click(swordButton)
    const dialog = screen.getByRole('dialog')
    expect(within(dialog).getByText('Высококритичное 1')).toBeTruthy()
  })

  it('ищет оружие по названию свойства', async () => {
    render(<ShopPage />)

    const search = await screen.findByPlaceholderText(/Поиск по названию, описанию и свойствам/)
    fireEvent.change(search, { target: { value: 'Оборонительное' } })

    expect(await screen.findByRole('button', { name: /Меч/ })).toBeTruthy()
    expect(screen.queryByRole('button', { name: /Верёвка/ })).toBeNull()
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
