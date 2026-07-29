import { render, screen, waitFor, fireEvent, within } from '@testing-library/react'
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
  implement: null, shard: null, purchasable: true, sellable: true,
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
  attachments: [
    { id: 'att-1', attachmentDefId: 'def-plating', name: 'Deflective Plating',
      nameRu: 'Отклоняющие пластины', hardPointCost: 1, isEnchantment: false, price: 450,
      rarity: 4, hostCharacterItemId: 'item-1', note: '', effects: [] },
  ],
  usedHardPoints: 1,
  overCapacity: false,
  attachmentNotes: ['Шипы: правило решает ведущий.'],
  damageState: 'undamaged',
  isUsable: true,
  repair: {
    state: 'undamaged', canRepair: false, difficulty: null, hoursMin: 0, hoursMax: 0,
    materialPercent: 0, materialCost: 0, skillName: 'Mechanics', affordable: true,
  },
} as unknown as SheetItem

const sheet = {
  id: 'char-1', money: 10000, startingPurchaseBudget: 0, isCreationPhase: false,
  items: [ironPlate], skills: [],
  derived: {
    soak: 4, meleeDefense: 0, rangedDefense: 0, encumbranceThreshold: 10, encumbranceLoad: 5,
    encumbered: false,
  },
} as unknown as CharacterSheet

/** Магический инструмент — по виду записи снаряжение, но своя корзина витрины (ROT-MAG-IMP-01). */
const staffDef = {
  ...plateDef, id: 'def-staff', name: 'Magic Staff', nameRu: 'Магический посох', kind: 'gear',
  price: 400, rarity: 6, hardPoints: null,
  implement: {
    code: 'magic-staff', attackDamageBonus: 4, boostDice: 0, requiredMagicSkill: '',
    discount: 'firstNamedEffect', discountEffects: ['Range'], choiceCount: 0,
    choiceMaxIncreaseSum: null, choiceExactIncrease: null,
  },
} as unknown as ItemDef

/** Обычное снаряжение: оно должно остаться в своей корзине и не уехать к инструментам. */
const ropeDef = {
  ...plateDef, id: 'def-rope', name: 'Rope', nameRu: 'Верёвка', kind: 'gear', price: 10, rarity: 0,
} as unknown as ItemDef

const shardDef = {
  ...ropeDef, id: 'def-shard', name: 'Arcane Bolt Rune', nameRu: 'Руна магического заряда',
  price: null, rarity: null, purchasable: false, sellable: false,
  shard: {
    code: 'arcane-bolt-rune', requiredMagicSkill: 'Runes', minimumSkillRank: 1,
    attackDamageBonus: 4, castingStrainReduction: 0, difficultyReductions: [],
    spellEffects: [], activationCost: 'maneuver', activationFrequency: 'turn',
    activationAttack: null, needsConfiguration: false,
  },
} as unknown as ItemDef

const reference = { items: [plateDef] } as unknown as Reference
const shopReference = { items: [plateDef, staffDef, ropeDef, shardDef] } as unknown as Reference

describe('Инвентарь: качество изготовления (ROT-WPN-02)', () => {
  beforeEach(() => {
    addItemMock.mockReset()
    addItemMock.mockResolvedValue({ id: 'new' })
  })

  it('показывает работу экземпляра и разбор поправок', () => {
    const { container } = render(<InventoryTab sheet={sheet} reference={reference} onError={() => {}}
      refresh={() => Promise.resolve()} />)

    // Бейдж работы стоит в заголовке карточки, рядом со слотами улучшений.
    expect(container.querySelector('.inv-card-title')!.textContent).toContain('· Железо')
    // Числа на карточке уже эффективные, а разбор объясняет, откуда они, и называет работу.
    const breakdown = [...container.querySelectorAll('.inv-card .muted')]
      .map(e => e.textContent ?? '').find(text => text.startsWith('железное'))
    expect(breakdown).toContain('Вес 6 → 8')
    expect(breakdown).toContain('Цена 5000 → 2500')
  })

  it('называет установленные улучшения и занятые слоты', () => {
    const { container } = render(<InventoryTab sheet={sheet} reference={reference} onError={() => {}}
      refresh={() => Promise.resolve()} />)

    // Числа на карточке уже с улучшением, поэтому видно и само улучшение, и занятый слот.
    expect(container.querySelector('.inv-card-title')!.textContent).toContain('HP 1/4')
    const card = container.querySelector('.inv-card')!.textContent ?? ''
    expect(card).toContain('Отклоняющие пластины')
    // Правило, которое приложение не исполняет, показывается, а не теряется.
    expect(card).toContain('Шипы: правило решает ведущий.')
  })

  it('гасит «Используется», когда мест уже нет', () => {
    // Латы надеты, вторая броня в рюкзаке: надеть её нельзя — это правило, а не подсказка.
    const secondArmor = {
      ...ironPlate, id: 'item-2', name: 'Chainmail', nameRu: 'Кольчуга', state: 'backpack',
      isActiveArmor: false, craftsmanship: 'steel', adjustments: [], attachments: [],
    } as unknown as SheetItem
    const twoArmors = { ...sheet, items: [ironPlate, secondArmor] } as unknown as CharacterSheet
    render(<InventoryTab sheet={twoArmors} reference={reference} onError={() => {}}
      refresh={() => Promise.resolve()} />)

    const equipButtons = screen.getAllByRole('button', { name: 'Используется' })
    // Первая карточка — уже надетые латы, вторая — кольчуга в рюкзаке.
    expect((equipButtons[1] as HTMLButtonElement).disabled).toBe(true)
    expect((equipButtons[1] as HTMLButtonElement).title).toContain('Уже надета другая броня')
  })

  it('держит магические инструменты в своей корзине витрины', () => {
    render(<InventoryTab sheet={sheet} reference={shopReference} onError={() => {}}
      refresh={() => Promise.resolve()} />)

    const shop = () => document.querySelector('.shop-list')!.textContent ?? ''
    // По умолчанию видно всё.
    expect(shop()).toContain('Магический посох')
    expect(shop()).toContain('Верёвка')

    fireEvent.click(screen.getByRole('button', { name: 'Инструменты магии' }))
    expect(shop()).toContain('Магический посох')
    expect(shop()).not.toContain('Верёвка')
    expect(shop()).not.toContain('Латы')

    // Из снаряжения инструмент уходит: дважды одна запись не показывается.
    fireEvent.click(screen.getByRole('button', { name: 'Снаряжение' }))
    expect(shop()).toContain('Верёвка')
    expect(shop()).not.toContain('Магический посох')
  })

  it('выносит runebound shards в отдельную вкладку без обычной покупки', () => {
    render(<InventoryTab sheet={sheet} reference={shopReference} onError={() => {}}
      refresh={() => Promise.resolve()} />)

    fireEvent.click(screen.getByRole('button', { name: 'Руны' }))
    const shop = document.querySelector('.shop-list')!
    expect(shop.textContent).toContain('Руна магического заряда')
    expect(shop.textContent).toContain('без обычной цены')
    expect(within(shop as HTMLElement).queryByRole('button', { name: 'Купить' })).toBeNull()
    expect(within(shop as HTMLElement).getByRole('button', { name: '+ Добавить' })).toBeTruthy()

    fireEvent.click(screen.getByRole('button', { name: 'Снаряжение' }))
    expect(document.querySelector('.shop-list')!.textContent).not.toContain('Руна магического заряда')
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
      'char-1', 'def-plate', 1, 'carried', { pricePercent: 100, craftsmanship: 'dwarven' }))
  })

  it('выдаёт без оплаты с выбранной работой, не открывая покупку', async () => {
    render(<InventoryTab sheet={sheet} reference={reference} onError={() => {}}
      refresh={() => Promise.resolve()} />)

    // Выбор работы доступен сразу: бесплатная выдача не должна требовать открытия магазина.
    fireEvent.change(screen.getByRole('combobox'), { target: { value: 'elven' } })
    fireEvent.click(screen.getByRole('button', { name: '+ Добавить' }))

    await waitFor(() => expect(addItemMock).toHaveBeenCalledWith(
      'char-1', 'def-plate', 1, 'carried', { free: true, craftsmanship: 'elven' }))
  })

  it('торгуется долей цены с шагом 25 %', async () => {
    render(<InventoryTab sheet={sheet} reference={reference} onError={() => {}}
      refresh={() => Promise.resolve()} />)

    fireEvent.click(screen.getAllByRole('button', { name: 'Купить' })[0])
    fireEvent.click(screen.getByRole('button', { name: '75%' }))
    expect(screen.getByText(/75% · Цена 3750 × 1 =/)).toBeTruthy()

    const buttons = screen.getAllByRole('button', { name: 'Купить' })
    fireEvent.click(buttons[buttons.length - 1])
    await waitFor(() => expect(addItemMock).toHaveBeenCalledWith(
      'char-1', 'def-plate', 1, 'carried', { pricePercent: 75, craftsmanship: 'steel' }))
  })

  it('своя цена отменяет долю и требует причины', async () => {
    render(<InventoryTab sheet={sheet} reference={reference} onError={() => {}}
      refresh={() => Promise.resolve()} />)

    fireEvent.click(screen.getAllByRole('button', { name: 'Купить' })[0])
    // Переключателя режимов нет: способ выбирает само поле цены.
    fireEvent.click(screen.getByRole('button', { name: '75%' }))
    fireEvent.change(screen.getByLabelText('Своя цена/шт'), { target: { value: '300' } })
    expect((screen.getByRole('button', { name: '75%' }) as HTMLButtonElement).disabled).toBe(true)

    const buy = () => screen.getAllByRole('button', { name: 'Купить' }).at(-1)!
    expect((buy() as HTMLButtonElement).disabled).toBe(true)

    fireEvent.change(screen.getByLabelText('Причина'), { target: { value: 'скидка гильдии' } })
    fireEvent.click(buy())
    await waitFor(() => expect(addItemMock).toHaveBeenCalledWith(
      'char-1', 'def-plate', 1, 'carried',
      { priceOverride: 300, overrideReason: 'скидка гильдии', craftsmanship: 'steel' }))
  })
})
