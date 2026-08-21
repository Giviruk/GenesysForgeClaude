import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type {
  CharacterMount, CharacterSheet, MountDef, Reference, SheetItem,
} from '../api/types'
import { TransportTab } from './TransportTab'

const buyMountMock = vi.fn()
const sellMountMock = vi.fn()
const updateMountMock = vi.fn()
const removeMountMock = vi.fn()
const moveCargoMock = vi.fn()
vi.mock('../api/client', () => ({
  api: {
    buyMount: (...a: unknown[]) => buyMountMock(...a),
    sellMount: (...a: unknown[]) => sellMountMock(...a),
    updateMount: (...a: unknown[]) => updateMountMock(...a),
    removeMount: (...a: unknown[]) => removeMountMock(...a),
    moveCargo: (...a: unknown[]) => moveCargoMock(...a),
  },
}))

const warMount: MountDef = {
  id: 'def-war', code: 'rot.mount.war-mount', name: 'War Mount', nameRu: 'Боевой скакун',
  transportKind: 'mount', movementMode: 'ground', requiresTraction: false,
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

/** Повозка: та же запись каталога, но транспортное средство без своего хода. */
const wagon: MountDef = {
  ...warMount,
  id: 'def-wagon', code: 'rot.vehicle.wagon', name: 'Wagon', nameRu: 'Повозка',
  transportKind: 'vehicle', movementMode: 'wheeled', requiresTraction: true,
  kind: 'minion',
  characteristics: { brawn: 0, agility: 0, intellect: 0, cunning: 0, willpower: 0, presence: 0 },
  soak: 2, woundThreshold: 10, strainThreshold: 5, silhouette: 3, capacity: 40,
  price: 200, rarity: 2, skills: [], attacks: [],
}

const beast: MountDef = {
  ...warMount,
  id: 'def-beast', code: 'rot.mount.beast-of-burden', name: 'Beast of Burden',
  nameRu: 'Вьючное животное', kind: 'minion', capacity: 18, price: 200, rarity: 1,
  skills: [], attacks: [],
}

const item = (over: Partial<SheetItem> = {}): SheetItem => ({
  id: 'item-1', itemDefId: 'def-bedroll', name: 'Bedroll', nameRu: 'Спальник',
  quantity: 1, encumbrance: 1, carriedByMountId: null, isInstalledOnMount: false,
  isMountGear: false, isBarding: false,
  ...over,
} as SheetItem)

const barding = () =>
  item({ id: 'item-barding', nameRu: 'Попона', isMountGear: true, isBarding: true, encumbrance: 5 })

const owned = (over: Partial<CharacterMount> = {}): CharacterMount => ({
  id: 'mount-1', mountDefId: 'def-war', displayName: 'Уголь', name: 'Уголь',
  definition: warMount, woundsCurrent: 0, carriedLoad: 0, capacity: 13, isActive: false,
  isOverloaded: false, isIncapacitated: false, provenance: 'purchased', notes: '',
  drawnByMountId: null, drawnByName: '', needsTraction: false,
  soak: 4, meleeDefense: 0, rangedDefense: 0, cargo: [],
  requiresGmApprovalForBarding: false,
  ...over,
})

const sheetWith = (mounts: CharacterMount[], money = 5000, items: SheetItem[] = []) => ({
  id: 'char-1', money, isCreationPhase: false, mounts, items,
} as unknown as CharacterSheet)

const reference = {
  mounts: [warMount],
  qualities: [{ code: 'knockdown', nameEn: 'Knockdown', nameRu: 'Опрокидывание' }],
} as unknown as Reference

const renderTab = (sheet: CharacterSheet) => render(
  <TransportTab sheet={sheet} reference={reference} onError={() => {}}
    refresh={() => Promise.resolve()} />)

describe('Транспорт (ROT-MOUNT-ITEM-01, ROT-TRANSPORT-01)', () => {
  beforeEach(() => {
    for (const mock of
      [buyMountMock, sellMountMock, updateMountMock, removeMountMock, moveCargoMock]) {
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

  it('правит раны через сервер', async () => {
    renderTab(sheetWith([owned()]))

    fireEvent.change(screen.getByLabelText('Ранения'), { target: { value: '5' } })
    await waitFor(() =>
      expect(updateMountMock).toHaveBeenCalledWith('char-1', 'mount-1', { woundsCurrent: 5 }))
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

  it('удаляет транспорт без выручки', async () => {
    renderTab(sheetWith([owned()]))

    fireEvent.click(screen.getByRole('button', { name: 'Удалить' }))

    await waitFor(() => expect(removeMountMock).toHaveBeenCalledWith('char-1', 'mount-1'))
  })

  it('грузит позицию инвентаря на транспорт, а не правит число', async () => {
    renderTab(sheetWith([owned()], 5000, [item()]))

    fireEvent.change(screen.getByLabelText('Что погрузить'), { target: { value: 'item-1' } })
    fireEvent.click(screen.getByRole('button', { name: 'Погрузить' }))

    await waitFor(() => expect(moveCargoMock).toHaveBeenCalledWith('char-1', 'item-1',
      { mountId: 'mount-1', quantity: 1, install: false }))
  })

  it('попону предлагает установить, а не сложить грузом', async () => {
    renderTab(sheetWith([owned()], 5000, [barding()]))

    fireEvent.change(screen.getByLabelText('Что погрузить'), { target: { value: 'item-barding' } })
    fireEvent.click(screen.getByRole('button', { name: 'Установить' }))

    await waitFor(() => expect(moveCargoMock).toHaveBeenCalledWith('char-1', 'item-barding',
      { mountId: 'mount-1', quantity: 1, install: true }))
  })

  it('боевому скакуну попона ставится без причины ведущего', () => {
    renderTab(sheetWith([owned()], 5000, [barding()]))

    fireEvent.change(screen.getByLabelText('Что погрузить'), { target: { value: 'item-barding' } })

    expect(screen.queryByLabelText('Причина ведущего')).toBeNull()
    expect(screen.getByRole('button', { name: 'Установить' }).hasAttribute('disabled')).toBe(false)
  })

  it('другому скакуну попона требует причину ведущего и блокирует кнопку', async () => {
    const beastMount = owned({ definition: beast, requiresGmApprovalForBarding: true })
    renderTab(sheetWith([beastMount], 5000, [barding()]))

    fireEvent.change(screen.getByLabelText('Что погрузить'), { target: { value: 'item-barding' } })

    // То же правило, что на сервере: без причины кнопка заблокирована.
    const install = screen.getByRole('button', { name: 'Установить' })
    expect(install.hasAttribute('disabled')).toBe(true)

    fireEvent.change(screen.getByLabelText('Причина ведущего'), { target: { value: 'подогнал кузнец' } })
    fireEvent.click(screen.getByRole('button', { name: 'Установить' }))

    await waitFor(() => expect(moveCargoMock).toHaveBeenCalledWith('char-1', 'item-barding',
      { mountId: 'mount-1', quantity: 1, install: true, installOverrideReason: 'подогнал кузнец' }))
  })

  it('сумкам причина ведущего не нужна даже на не-боевом скакуне', () => {
    const beastMount = owned({ definition: beast, requiresGmApprovalForBarding: true })
    const bags = item({ id: 'item-bags', nameRu: 'Седельные сумки', isMountGear: true })
    renderTab(sheetWith([beastMount], 5000, [bags]))

    fireEvent.change(screen.getByLabelText('Что погрузить'), { target: { value: 'item-bags' } })

    expect(screen.queryByLabelText('Причина ведущего')).toBeNull()
    expect(screen.getByRole('button', { name: 'Установить' }).hasAttribute('disabled')).toBe(false)
  })

  it('снимает груз обратно владельцу', async () => {
    const loaded = owned({ cargo: [item({ carriedByMountId: 'mount-1' })], carriedLoad: 1 })
    renderTab(sheetWith([loaded]))

    fireEvent.click(screen.getByRole('button', { name: 'Снять' }))

    await waitFor(() =>
      expect(moveCargoMock).toHaveBeenCalledWith('char-1', 'item-1', { mountId: null }))
  })

  it('повозка без тяги помечена и запрягается выбором животного', async () => {
    const cart = owned({
      id: 'wagon-1', mountDefId: 'def-wagon', displayName: 'Повозка', name: '',
      definition: wagon, capacity: 40, needsTraction: true, soak: 2,
    })
    const draft = owned({
      id: 'beast-1', mountDefId: 'def-beast', displayName: 'Серко', name: 'Серко',
      definition: beast, capacity: 18,
    })
    renderTab(sheetWith([cart, draft]))

    expect(screen.getByText(/без тяги/)).toBeTruthy()
    fireEvent.change(screen.getByLabelText('Тяга'), { target: { value: 'beast-1' } })

    await waitFor(() => expect(updateMountMock).toHaveBeenCalledWith('char-1', 'wagon-1',
      { drawnByMountId: 'beast-1' }))
  })

  it('у повозки нет характеристик и её порог назван прочностью', () => {
    const cart = owned({
      id: 'wagon-1', mountDefId: 'def-wagon', displayName: 'Повозка', name: '',
      definition: wagon, capacity: 40, needsTraction: true,
    })
    const { container } = renderTab(sheetWith([cart]))

    // Ищем внутри карточки: в витрине ниже стоит скакун, и его характеристики видны законно.
    const card = container.querySelector('.mount-card')!
    expect(card.textContent).not.toContain('Хитрость')
    expect(card.textContent).toContain('Прочность 10')
    expect(card.textContent).toContain('Системы 5')
  })
})
