import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { describe, expect, it, vi, beforeEach } from 'vitest'
import type { CharacterSheet, ItemDef, ItemRepair, Reference, SheetItem } from '../api/types'
import { InventoryTab } from './InventoryTab'
import { discountedCost } from '../utils/repair'

const setDamageStateMock = vi.fn()
const repairItemMock = vi.fn()
vi.mock('../api/client', () => ({
  api: {
    addItem: vi.fn(),
    updateItem: vi.fn(),
    removeItem: vi.fn(),
    sellItem: vi.fn(),
    setItemThrown: vi.fn(),
    setItemDamageState: (...a: unknown[]) => setDamageStateMock(...a),
    repairItem: (...a: unknown[]) => repairItemMock(...a),
  },
}))

const openRollerMock = vi.fn()
vi.mock('../dice-roller-store', () => ({ useDiceRoller: () => ({ openRoller: openRollerMock }) }))

const swordDef = {
  id: 'def-sword', name: 'Sword', nameRu: 'Меч', kind: 'weapon', encumbrance: 1, soakBonus: 0,
  meleeDefense: 0, rangedDefense: 0, encumbranceThresholdBonus: 0, description: '', safeDescription: '',
  source: '', price: 500, rarity: 4, skillName: 'Melee', damage: '+2', crit: '2', rangeBand: 'engaged',
  properties: '', isCustom: false, qualities: [], hardPoints: 3, checkModifiers: [], attackProfiles: [],
} as unknown as ItemDef

const repair = (over: Partial<ItemRepair> = {}): ItemRepair => ({
  state: 'undamaged', canRepair: false, difficulty: null, hoursMin: 0, hoursMax: 0,
  materialPercent: 0, materialCost: 0, skillName: 'Mechanics', affordable: true, ...over,
})

/** Профиль атаки с уже посчитанными сервером поправками пула. */
const profile = (setback: number, difficulty: number) => ({
  code: 'default', nameRu: '', nameEn: '', isDefault: true, skillName: 'Melee',
  damageKind: 'brawnPlus', damageValue: 2, crit: 2, range: 'engaged',
  cannotAttackEngaged: false, fixedDifficulty: null, qualities: [], baseDamage: 5,
  poolModifiers: {
    boost: 0, setback, difficultyIncrease: difficulty, automaticAdvantage: 0, automaticThreat: 0,
    sources: setback > 0
      ? [{ nameEn: 'Minor damage', nameRu: 'Незначительное повреждение', boost: 0, setback, difficulty: 0, advantage: 0, threat: 0 }]
      : difficulty > 0
        ? [{ nameEn: 'Moderate damage', nameRu: 'Умеренное повреждение', boost: 0, setback: 0, difficulty, advantage: 0, threat: 0 }]
        : [],
  },
})

const sword = (over: Partial<SheetItem> = {}): SheetItem => ({
  id: 'item-1', itemDefId: 'def-sword', name: 'Sword', nameRu: 'Меч', kind: 'weapon',
  state: 'equipped', quantity: 1, encumbrance: 1, soakBonus: 0, meleeDefense: 0, rangedDefense: 0,
  encumbranceThresholdBonus: 0, load: 1, description: '', price: 500, skillName: 'Melee',
  damage: '+2', crit: '2', rangeBand: 'engaged', properties: '', isActiveArmor: false,
  hardPoints: 3, checkModifiers: [], attackProfiles: [profile(0, 0)], isThrown: false,
  craftsmanship: 'steel', rarity: 4, reinforced: false, adjustments: [], attachments: [],
  usedHardPoints: 0, overCapacity: false, attachmentNotes: [], formTraits: 'oneHanded',
  canEquip: true,
  canBeDamaged: true,
  damageState: 'undamaged', isUsable: true, repair: repair(),
  ...over,
} as unknown as SheetItem)

const sheetWith = (item: SheetItem, money = 10000) => ({
  id: 'char-1', money, startingPurchaseBudget: 0, isCreationPhase: false, items: [item],
  skills: [{
    id: 'skill-melee', name: 'Melee', nameRu: 'Ближний бой', kind: 'combat',
    characteristic: 'brawn', ranks: 2, isCareer: true, pool: { ability: 1, proficiency: 2 },
    setbackDice: 0, boostDice: 0, modifierSources: [],
  }],
  characteristics: { brawn: 3 },
  derived: {
    soak: 3, meleeDefense: 0, rangedDefense: 0, encumbranceThreshold: 8, encumbranceLoad: 1,
    encumbered: false,
  },
} as unknown as CharacterSheet)

const reference = { items: [swordDef], qualities: [] } as unknown as Reference

const renderTab = (sheet: CharacterSheet) => render(
  <InventoryTab sheet={sheet} reference={reference} onError={() => {}}
    refresh={() => Promise.resolve()} />)

describe('Инвентарь: состояние предмета и ремонт (GEN-EQP-DMG-01)', () => {
  beforeEach(() => {
    setDamageStateMock.mockReset()
    setDamageStateMock.mockResolvedValue(undefined)
    repairItemMock.mockReset()
    repairItemMock.mockResolvedValue(undefined)
    openRollerMock.mockReset()
  })

  it('меняет состояние отдельной кнопкой, не трогая «используется»', async () => {
    renderTab(sheetWith(sword()))

    fireEvent.click(screen.getByRole('button', { name: 'Умеренное' }))

    await waitFor(() => expect(setDamageStateMock).toHaveBeenCalledWith('char-1', 'item-1', 'moderate'))
    // Переключатель места остался своим: «в рюкзаке» и «сломано» — разные вещи.
    expect(screen.getByRole('button', { name: 'В рюкзаке' })).toBeTruthy()
  })

  it('чинит по кнопке и показывает стоимость материалов', async () => {
    const item = sword({
      canEquip: true,
  canBeDamaged: true,
  damageState: 'moderate',
      repair: repair({
        state: 'moderate', canRepair: true, difficulty: 2, hoursMin: 2, hoursMax: 4,
        materialPercent: 50, materialCost: 250,
      }),
    })
    renderTab(sheetWith(item))

    const button = screen.getByRole('button', { name: /Починить/ })
    expect(button.textContent).toContain('250')

    fireEvent.click(button)
    await waitFor(() => expect(repairItemMock).toHaveBeenCalledWith(
      'char-1', 'item-1', { netAdvantages: 0 }))
  })

  it('снимает 10 % за каждое чистое преимущество — той же формулой, что и сервер', async () => {
    const item = sword({
      canEquip: true,
  canBeDamaged: true,
  damageState: 'minor',
      repair: repair({
        state: 'minor', canRepair: true, difficulty: 1, hoursMin: 1, hoursMax: 2,
        materialPercent: 25, materialCost: 126,
      }),
    })
    renderTab(sheetWith(item))

    fireEvent.change(screen.getByLabelText(/преим/), { target: { value: '2' } })

    expect(discountedCost(126, 2)).toBe(101)
    expect(screen.getByRole('button', { name: /Починить/ }).textContent).toContain('101')

    fireEvent.click(screen.getByRole('button', { name: /Починить/ }))
    await waitFor(() => expect(repairItemMock).toHaveBeenCalledWith(
      'char-1', 'item-1', { netAdvantages: 2 }))
  })

  it('просит цену ведущего и причину, когда обычной цены нет', async () => {
    const item = sword({
      canEquip: true,
  canBeDamaged: true,
  damageState: 'minor',
      repair: repair({
        state: 'minor', canRepair: true, difficulty: 1, hoursMin: 1, hoursMax: 2,
        materialPercent: 25, materialCost: null,
      }),
    })
    renderTab(sheetWith(item))

    // Без цены и причины чинить нечем: сервер откажет, и кнопка это повторяет.
    const button = () => screen.getByRole('button', { name: /Починить/ }) as HTMLButtonElement
    expect(button().disabled).toBe(true)

    fireEvent.change(screen.getByLabelText(/материалы/), { target: { value: '300' } })
    expect(button().disabled).toBe(true)
    expect(button().title).toContain('нужна причина')

    fireEvent.change(screen.getByLabelText(/причина/), { target: { value: 'цена ведущего' } })
    fireEvent.click(button())

    await waitFor(() => expect(repairItemMock).toHaveBeenCalledWith(
      'char-1', 'item-1', { costOverride: 300, overrideReason: 'цена ведущего' }))
  })

  it('не предлагает ремонт уничтоженного', () => {
    renderTab(sheetWith(sword({
      canEquip: true,
  canBeDamaged: true,
  damageState: 'destroyed', isUsable: false,
      repair: repair({ state: 'destroyed' }),
    })))

    expect(screen.queryByRole('button', { name: /Починить/ })).toBeNull()
    expect(screen.getByText('Обычный ремонт недоступен')).toBeTruthy()
  })

  it('гасит кнопку ремонта, когда на материалы не хватает монет', () => {
    const item = sword({
      canEquip: true,
  canBeDamaged: true,
  damageState: 'major',
      repair: repair({
        state: 'major', canRepair: true, difficulty: 3, hoursMin: 3, hoursMax: 6,
        materialPercent: 100, materialCost: 500, affordable: false,
      }),
    })
    renderTab(sheetWith(item, 100))

    const button = screen.getByRole('button', { name: /Починить/ }) as HTMLButtonElement
    expect(button.disabled).toBe(true)
    expect(button.title).toContain('Недостаточно монет')
  })

  it('держит памятку по ремонту в тултипе: сложность, время и материалы', () => {
    const item = sword({
      canEquip: true,
  canBeDamaged: true,
  damageState: 'moderate',
      repair: repair({
        state: 'moderate', canRepair: true, difficulty: 2, hoursMin: 2, hoursMax: 4,
        materialPercent: 50, materialCost: 250,
      }),
    })
    renderTab(sheetWith(item))

    // Пока тултип закрыт, правил на карточке нет — они не занимают место.
    expect(screen.queryByRole('tooltip')).toBeNull()

    fireEvent.click(screen.getByRole('button', { name: 'Памятка: ремонт' }))

    const memo = screen.getByRole('tooltip').textContent ?? ''
    expect(memo).toContain('Механика')
    expect(memo).toContain('Средняя')
    expect(memo).toContain('2–4 ч')
    expect(memo).toContain('50%')
    expect(memo).toContain('10 % за каждое чистое преимущество')
    expect(memo).toContain('бросок не делает')
  })

  it('показывает помеху состояния в пуле атаки', () => {
    const { container } = renderTab(sheetWith(sword({
      canEquip: true,
  canBeDamaged: true,
  damageState: 'minor',
      attackProfiles: [profile(1, 0)] as unknown as SheetItem['attackProfiles'],
      repair: repair({
        state: 'minor', canRepair: true, difficulty: 1, hoursMin: 1, hoursMax: 2,
        materialPercent: 25, materialCost: 125,
      }),
    })))

    const pool = container.querySelector('.weapon-pool')!
    // Помеха приехала с сервера вместе с профилем и подписана источником.
    expect(pool.getAttribute('title')).toContain('Незначительное повреждение')
    expect(pool.textContent).toContain('■')
  })

  it('не даёт атаковать сломанным оружием', () => {
    renderTab(sheetWith(sword({
      canEquip: true,
  canBeDamaged: true,
  damageState: 'major', isUsable: false,
      repair: repair({
        state: 'major', canRepair: true, difficulty: 3, hoursMin: 3, hoursMax: 6,
        materialPercent: 100, materialCost: 500,
      }),
    })))

    expect(screen.queryByRole('button', { name: /Атаковать/ })).toBeNull()
    expect(screen.getByText('сломано — атаковать нельзя')).toBeTruthy()
    expect(screen.getByText(/не даёт ни поглощения, ни защиты/)).toBeTruthy()
  })

  it('объясняет обнулённые числа в разборе поправок', () => {
    const { container } = renderTab(sheetWith(sword({
      kind: 'armor', damageState: 'major', isUsable: false, soakBonus: 0, attackProfiles: [],
      adjustments: [{ field: 'soak', base: 2, effective: 0, stage: 'damageState', source: 'Major' }],
      repair: repair({
        state: 'major', canRepair: true, difficulty: 3, hoursMin: 3, hoursMax: 6,
        materialPercent: 100, materialCost: 500,
      }),
    } as unknown as Partial<SheetItem>)))

    const card = container.querySelector('.inv-card')!.textContent ?? ''
    expect(card).toContain('Поглощение 2 → 0')
    expect(card).toContain('(Серьёзное)')
  })
})
