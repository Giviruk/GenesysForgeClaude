import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { CharacterMount, CharacterSheet, MountDef, Reference } from '../api/types'
import { MountsTab } from './MountsTab'

const buyMountMock = vi.fn()
const sellMountMock = vi.fn()
const updateMountMock = vi.fn()
const removeMountMock = vi.fn()
vi.mock('../api/client', () => ({
  api: {
    buyMount: (...a: unknown[]) => buyMountMock(...a),
    sellMount: (...a: unknown[]) => sellMountMock(...a),
    updateMount: (...a: unknown[]) => updateMountMock(...a),
    removeMount: (...a: unknown[]) => removeMountMock(...a),
  },
}))

const warMount: MountDef = {
  id: 'def-war', code: 'rot.mount.war-mount', name: 'War Mount', nameRu: 'Боевой скакун',
  kind: 'rival',
  characteristics: { brawn: 4, agility: 3, intellect: 1, cunning: 2, willpower: 3, presence: 1 },
  soak: 4, woundThreshold: 14, strainThreshold: null, meleeDefense: 0, rangedDefense: 0,
  silhouette: 2, capacity: 13, price: 1500, rarity: 6, includedGear: [],
  requiresRidingCheck: false,
  skills: [{ name: 'Brawl', ranks: 1, isGroupSkill: false }],
  abilities: [],
  attacks: [{
    name: 'Trample', nameRu: 'Топтание', skillName: 'Brawl', damage: 6, critical: 4,
    range: 'engaged', qualityCodes: ['knockdown'],
  }],
  description: 'Выученный для боя скакун.', descriptionEn: 'A mount trained for battle.',
  source: 'Realms of Terrinoth, с. 106',
}

const owned = (over: Partial<CharacterMount> = {}): CharacterMount => ({
  id: 'mount-1', mountDefId: 'def-war', displayName: 'Уголь', name: 'Уголь',
  definition: warMount, woundsCurrent: 0, carriedLoad: 0, capacity: 13, isActive: false,
  isOverloaded: false, isIncapacitated: false, provenance: 'purchased', notes: '',
  ...over,
})

const sheetWith = (mounts: CharacterMount[], money = 5000) => ({
  id: 'char-1', money, startingPurchaseBudget: 0, isCreationPhase: false, mounts,
} as unknown as CharacterSheet)

const reference = {
  mounts: [warMount],
  qualities: [{ code: 'knockdown', nameEn: 'Knockdown', nameRu: 'Опрокидывание' }],
} as unknown as Reference

const renderTab = (sheet: CharacterSheet) => render(
  <MountsTab sheet={sheet} reference={reference} onError={() => {}}
    refresh={() => Promise.resolve()} />)

describe('Скакуны (ROT-MOUNT-ITEM-01)', () => {
  beforeEach(() => {
    for (const mock of [buyMountMock, sellMountMock, updateMountMock, removeMountMock]) {
      mock.mockReset()
      mock.mockResolvedValue(undefined)
    }
  })

  it('выдаёт скакуна без оплаты отдельной кнопкой', async () => {
    renderTab(sheetWith([]))

    fireEvent.click(screen.getByRole('button', { name: '+ Выдать' }))

    await waitFor(() => expect(buyMountMock).toHaveBeenCalledWith('char-1', 'def-war', { free: true }))
  })

  it('покупает по доле цены теми же контролами, что и предметы', async () => {
    renderTab(sheetWith([]))

    fireEvent.click(screen.getByRole('button', { name: 'Купить' }))
    fireEvent.click(screen.getByRole('button', { name: '75%' }))
    fireEvent.click(screen.getByRole('button', { name: 'Купить' }))

    await waitFor(() =>
      expect(buyMountMock).toHaveBeenCalledWith('char-1', 'def-war', { pricePercent: 75 }))
  })

  it('требует причину для своей цены', async () => {
    renderTab(sheetWith([]))
    fireEvent.click(screen.getByRole('button', { name: 'Купить' }))

    fireEvent.change(screen.getByLabelText('Своя цена/шт'), { target: { value: '900' } })
    // Пока причины нет, кнопка покупки заблокирована — то же правило, что на сервере.
    const confirm = screen.getAllByRole('button', { name: 'Купить' }).at(-1)!
    expect(confirm.hasAttribute('disabled')).toBe(true)

    fireEvent.change(screen.getByLabelText('Причина'), { target: { value: 'сделка с конюхом' } })
    fireEvent.click(screen.getAllByRole('button', { name: 'Купить' }).at(-1)!)

    await waitFor(() => expect(buyMountMock).toHaveBeenCalledWith('char-1', 'def-war',
      { priceOverride: 900, overrideReason: 'сделка с конюхом' }))
  })

  it('показывает статблок профиля, а не строку «Снаряжение»', () => {
    renderTab(sheetWith([owned()]))

    expect(screen.getAllByText(/Ранения/).length).toBeGreaterThan(0)
    expect(screen.getAllByText(/урон 6/).length).toBeGreaterThan(0)
    expect(screen.getAllByText(/Опрокидывание/).length).toBeGreaterThan(0)
  })

  it('правит раны и груз через сервер', async () => {
    renderTab(sheetWith([owned()]))

    fireEvent.change(screen.getByLabelText('Ранения'), { target: { value: '5' } })
    await waitFor(() =>
      expect(updateMountMock).toHaveBeenCalledWith('char-1', 'mount-1', { woundsCurrent: 5 }))

    fireEvent.change(screen.getByLabelText('Груз'), { target: { value: '7' } })
    await waitFor(() =>
      expect(updateMountMock).toHaveBeenCalledWith('char-1', 'mount-1', { carriedLoad: 7 }))
  })

  it('помечает перегруз и выведенного из строя', () => {
    renderTab(sheetWith([owned({ carriedLoad: 20, isOverloaded: true, woundsCurrent: 14, isIncapacitated: true })]))

    expect(screen.getByText(/перегружен/)).toBeTruthy()
    expect(screen.getByText(/выведен из строя/)).toBeTruthy()
  })

  it('продаёт по проверке теми же тремя способами, что и предметы', async () => {
    renderTab(sheetWith([owned()]))

    fireEvent.click(screen.getByRole('button', { name: 'Продать' }))
    fireEvent.click(screen.getByLabelText('По проверке'))
    fireEvent.change(screen.getByLabelText('Нетто-успехов'), { target: { value: '2' } })
    fireEvent.click(screen.getAllByRole('button', { name: 'Продать' }).at(-1)!)

    await waitFor(() =>
      expect(sellMountMock).toHaveBeenCalledWith('char-1', 'mount-1', { netSuccesses: 2 }))
  })

  it('удаляет скакуна без выручки', async () => {
    renderTab(sheetWith([owned()]))

    fireEvent.click(screen.getByRole('button', { name: 'Удалить' }))

    await waitFor(() => expect(removeMountMock).toHaveBeenCalledWith('char-1', 'mount-1'))
  })
})
