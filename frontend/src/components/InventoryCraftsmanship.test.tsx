import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { describe, expect, it, vi, beforeEach } from 'vitest'
import type { CharacterSheet, ItemDef, Reference, SheetItem } from '../api/types'
import { InventoryTab } from './InventoryTab'

const addItemMock = vi.fn()
vi.mock('../api/client', () => ({
  api: {
    addItem: (...a: unknown[]) => addItemMock(...a),
    updateItem: vi.fn(),
    removeItem: vi.fn(),
    sellItem: vi.fn(),
    setActiveArmor: vi.fn(),
    updateMoney: vi.fn(),
  },
}))

vi.mock('../dice-roller-store', () => ({ useDiceRoller: () => ({ openRoller: vi.fn() }) }))

const plateDef = {
  id: 'def-plate', name: 'Plate', nameRu: 'Латы', kind: 'armor', encumbrance: 6, soakBonus: 2,
  meleeDefense: 0, rangedDefense: 0, encumbranceThresholdBonus: 0, description: '', safeDescription: '',
  source: '', price: 5000, rarity: 6, skillName: '', damage: '', crit: '', rangeBand: '', properties: '',
  isCustom: false, qualities: [], hardPoints: 4, checkModifiers: [], attackProfiles: [],
} as unknown as ItemDef

const ironPlate = {
  id: 'item-1', itemDefId: 'def-plate', name: 'Plate', nameRu: 'Латы', kind: 'armor', state: 'equipped',
  quantity: 1, encumbrance: 8, soakBonus: 2, meleeDefense: 0, rangedDefense: 0,
  encumbranceThresholdBonus: 0, load: 5, description: '', price: 2500, skillName: '', damage: '',
  crit: '', rangeBand: '', properties: '', isActiveArmor: true, hardPoints: 4, checkModifiers: [],
  attackProfiles: [], isThrown: false, craftsmanship: 'iron', rarity: 5, reinforced: false,
  adjustments: [
    { field: 'encumbrance', base: 6, effective: 8, stage: 'craftsmanship', source: 'Iron' },
    { field: 'price', base: 5000, effective: 2500, stage: 'craftsmanship', source: 'Iron' },
  ],
} as unknown as SheetItem

const sheet = {
  id: 'char-1', money: 10000, items: [ironPlate], skills: [],
  derived: {
    soak: 4, meleeDefense: 0, rangedDefense: 0, encumbranceThreshold: 10, encumbranceLoad: 5,
    encumbered: false,
  },
} as unknown as CharacterSheet

const reference = { items: [plateDef] } as unknown as Reference

describe('Инвентарь: качество изготовления (ROT-WPN-02)', () => {
  beforeEach(() => {
    addItemMock.mockReset()
    addItemMock.mockResolvedValue({ id: 'new' })
  })

  it('показывает работу экземпляра и разбор поправок', () => {
    render(<InventoryTab sheet={sheet} reference={reference} onError={() => {}}
      refresh={() => Promise.resolve()} />)

    expect(screen.getByText(/Железо/)).toBeTruthy()
    // Числа на карточке уже эффективные, а разбор объясняет, откуда они.
    expect(screen.getByText(/Вес 6 → 8/)).toBeTruthy()
    expect(screen.getByText(/Цена 5000 → 2500/)).toBeTruthy()
  })

  it('покупает с выбранной работой и показывает её цену', async () => {
    render(<InventoryTab sheet={sheet} reference={reference} onError={() => {}}
      refresh={() => Promise.resolve()} />)

    fireEvent.click(screen.getAllByRole('button', { name: 'Купить' })[0])
    fireEvent.change(screen.getByRole('combobox'), { target: { value: 'dwarven' } })

    // Гномья работа вдвое дороже каталожной цены; итог всё равно пересчитает сервер.
    expect(screen.getByText(/Цена 10000 × 1 =/)).toBeTruthy()

    const buttons = screen.getAllByRole('button', { name: 'Купить' })
    fireEvent.click(buttons[buttons.length - 1])
    await waitFor(() => expect(addItemMock).toHaveBeenCalledWith(
      'char-1', 'def-plate', 1, 'carried', { craftsmanship: 'dwarven' }))
  })
})
