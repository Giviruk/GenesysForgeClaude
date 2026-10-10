import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type {
  CharacterSheet, CraftingPreview, CraftingProject, CraftingSpend, Reference,
} from '../api/types'
import { CraftingTab } from './CraftingTab'

const craftingMock = vi.fn()
const previewMock = vi.fn()
const startMock = vi.fn()
const resolveMock = vi.fn()
const cancelMock = vi.fn()
vi.mock('../api/client', () => ({
  api: {
    crafting: (...a: unknown[]) => craftingMock(...a),
    craftingPreview: (...a: unknown[]) => previewMock(...a),
    startCrafting: (...a: unknown[]) => startMock(...a),
    resolveCrafting: (...a: unknown[]) => resolveMock(...a),
    cancelCrafting: (...a: unknown[]) => cancelMock(...a),
  },
}))

const superior: CraftingSpend = {
  code: 'craft-superior', rowCode: 'craft-4', table: 'item',
  nameRu: 'Превосходное', nameEn: 'Superior',
  description: 'Результат получает качество «Превосходное».',
  descriptionEn: 'The result gains the Superior quality.',
  advantageCost: 0, threatCost: 0, triumphCost: 1, despairCost: 0,
  isNegative: false, repeatable: false, requiresGmConfirmation: false, requiresParameter: false,
  effect: 'addQuality', weaponOnly: false, sortOrder: 8,
}

const faster: CraftingSpend = {
  code: 'craft-time-minus', rowCode: 'craft-1', table: 'item',
  nameRu: 'Сократить время на день', nameEn: 'Cut a day off the work',
  description: 'Работа идёт быстрее на один день.',
  descriptionEn: 'The work takes one day less.',
  advantageCost: 1, threatCost: 0, triumphCost: 1, despairCost: 0,
  isNegative: false, repeatable: true, requiresGmConfirmation: false, requiresParameter: false,
  effect: 'time', weaponOnly: false, sortOrder: 1,
}

/** Трата, которую приложение не исполняет: она должна быть видна как «только описание». */
const boost: CraftingSpend = {
  code: 'craft-boost-next', rowCode: 'craft-1', table: 'item',
  nameRu: 'Бонусный куб к следующей проверке', nameEn: 'A boost die on the next check',
  description: 'Следующая проверка тем же навыком получает один бонусный куб.',
  descriptionEn: 'The next check with the same skill gains one boost die.',
  advantageCost: 1, threatCost: 0, triumphCost: 1, despairCost: 0,
  isNegative: false, repeatable: false, requiresGmConfirmation: false, requiresParameter: false,
  effect: 'descriptive', weaponOnly: false, sortOrder: 2,
}

const preview: CraftingPreview = {
  kind: 'item', targetName: 'Axe', targetPrice: 150, targetRarity: 1,
  craftsmanship: 'steel', material: 'oak',
  skillName: 'Mechanics', baseDifficulty: 1, difficulty: 1, baseTime: 2, time: 2,
  timeUnit: 'days', listedCost: 75, costPercent: 100, costOverride: null, cost: 75,
  isWeapon: true, spends: [faster, boost, superior],
}

const draft: CraftingProject = {
  id: 'proj-1', kind: 'item', status: 'draft', itemDefId: 'def-axe', baseCharacterItemId: null,
  targetName: 'Axe', targetPrice: 150, targetRarity: 1, skillName: 'Mechanics',
  craftsmanship: 'steel', material: 'oak',
  baseDifficulty: 1, difficulty: 1, difficultyReason: '', baseTime: 2, time: 2,
  timeUnit: 'days', timeReason: '', listedCost: 75, costPercent: 100, costOverride: null,
  costOverrideReason: '', cost: 75, requirements: 'кузница', intent: '', roughSurvival: false,
  netSuccesses: 0, advantages: 0, threats: 0, triumphs: 0, despairs: 0,
  createdCharacterItemId: null, outcome: '', spends: [],
  createdAt: '2026-08-03T00:00:00Z', resolvedAt: null,
}

const sheet = {
  id: 'char-1', system: 'realmsOfTerrinoth',
  items: [
    { id: 'item-axe', itemDefId: 'def-axe', name: 'Axe', nameRu: 'Топор' },
    { id: 'item-rune', itemDefId: 'def-rune', name: 'Lesser Rune', nameRu: 'Малая руна' },
  ],
  skills: [
    { skillDefId: 'skill-arcana', name: 'Arcana', nameRu: 'Аркана', kind: 'magic', ranks: 1 },
    { skillDefId: 'skill-runes', name: 'Runes', nameRu: 'Руны', kind: 'magic', ranks: 1 },
    { skillDefId: 'skill-mechanics', name: 'Mechanics', nameRu: 'Механика', kind: 'general' },
  ],
} as unknown as CharacterSheet
const reference = {
  items: [
    { id: 'def-axe', code: 'axe', name: 'Axe', nameRu: 'Топор', kind: 'weapon',
      price: 150, rarity: 1, implement: null },
    { id: 'def-staff', code: 'magic-staff', name: 'Magic Staff', nameRu: 'Магический посох',
      kind: 'gear', price: 400, rarity: 6, implement: { code: 'magic-staff' } },
    { id: 'def-potion', code: 'stamina-elixir', name: 'Stamina Elixir',
      nameRu: 'Эликсир выносливости', price: 50, rarity: 3, shopCategory: 'consumable' },
    { id: 'def-meal', code: 'meal-tavern', name: 'Meal (Tavern)',
      nameRu: 'Еда в таверне', price: 2, rarity: 0, shopCategory: 'service' },
    { id: 'def-rune', code: 'lesser-rune', name: 'Lesser Rune', nameRu: 'Малая руна',
      price: null, rarity: null, shopCategory: 'magicImplement', shard: { code: 'lesser-rune' } },
  ],
} as unknown as Reference

/** Кнопка оплаты триумфом в строке таблицы: строки платятся разными символами. */
const triumphChip = (rowName: string): HTMLButtonElement => {
  const row = screen.getByText(rowName).closest('.crafting-row')!
  return [...row.querySelectorAll('button')]
    .find(b => b.textContent?.includes('★')) as HTMLButtonElement
}

const renderTab = () => render(
  <CraftingTab sheet={sheet} reference={reference} onError={() => {}}
    refresh={() => Promise.resolve()} />)

describe('Ремесло (ROT-CRAFT-01, ROT-ALCH-02, ROT-CRAFT-MAGIC-01)', () => {
  beforeEach(() => {
    craftingMock.mockReset().mockResolvedValue([])
    previewMock.mockReset().mockResolvedValue(preview)
    startMock.mockReset().mockResolvedValue({ id: 'proj-1' })
    resolveMock.mockReset().mockResolvedValue(draft)
    cancelMock.mockReset().mockResolvedValue(undefined)
  })

  it('показывает, что ресурсы — только описание и ничего не списывается', async () => {
    renderTab()
    expect(await screen.findByText(/не списывает и наличия не проверяет/)).toBeTruthy()
  })

  it('шлёт выбранную долю стоимости в предпросмотр', async () => {
    renderTab()
    fireEvent.click(await screen.findByRole('button', { name: /Топор/ }))
    await waitFor(() => expect(previewMock).toHaveBeenCalled())

    fireEvent.click(screen.getByText('150%'))
    await waitFor(() => expect(previewMock).toHaveBeenLastCalledWith(
      'char-1', expect.objectContaining({ costPercent: 150, costOverride: null })))
  })

  it('передаёт материал обычного и магического снаряжения в расчёт стоимости', async () => {
    renderTab()
    fireEvent.click(await screen.findByRole('button', { name: /Топор/ }))
    fireEvent.click(screen.getByRole('button', { name: /Железо/ }))
    await waitFor(() => expect(previewMock).toHaveBeenLastCalledWith(
      'char-1', expect.objectContaining({ craftsmanship: 'iron', material: 'oak' })))

    fireEvent.click(screen.getByRole('button', { name: /Магический посох/ }))
    fireEvent.click(screen.getByRole('button', { name: 'Ива' }))
    await waitFor(() => expect(previewMock).toHaveBeenLastCalledWith(
      'char-1', expect.objectContaining({ craftsmanship: 'steel', material: 'willow' })))
  })

  it('переключает каталог между обычными предметами и зельями', async () => {
    renderTab()
    expect(await screen.findByRole('button', { name: /Топор/ })).toBeTruthy()
    expect(screen.queryByRole('button', { name: /Эликсир выносливости/ })).toBeNull()
    fireEvent.click(screen.getByRole('button', { name: /Топор/ }))
    fireEvent.click(screen.getByRole('tab', { name: /Варка зелья/ }))
    expect(screen.queryByRole('button', { name: /Топор/ })).toBeNull()
    fireEvent.click(screen.getByRole('button', { name: /Эликсир выносливости/ }))
    await waitFor(() => expect(previewMock).toHaveBeenLastCalledWith(
      'char-1', expect.objectContaining({ itemDefId: 'def-potion', kind: 'potion' })))
  })

  it('отделяет услуги от предметов, доступных для ремесла', async () => {
    renderTab()
    expect((await screen.findByRole('button', { name: /Топор/ })).hasAttribute('disabled')).toBe(false)
    expect(screen.getByRole('button', { name: /Еда в таверне/ }).hasAttribute('disabled')).toBe(true)
    expect(screen.getByRole('button', { name: /Еда в таверне/ }).title).toBe('Услуги не создаются ремеслом')
  })

  /** Своя цена отменяет долю и требует причины — то же правило, что при покупке. */
  it('своя стоимость требует причины и блокирует старт без неё', async () => {
    renderTab()
    fireEvent.click(await screen.findByRole('button', { name: /Топор/ }))
    fireEvent.click(screen.getByText(/Поправки ведущего/))
    fireEvent.change(screen.getByLabelText(/Своя стоимость/), { target: { value: '10' } })

    const start = screen.getByText('Начать проект') as HTMLButtonElement
    expect(start.disabled).toBe(true)

    fireEvent.change(screen.getAllByLabelText(/^Причина/)[0], { target: { value: 'свои материалы' } })
    await waitFor(() => expect((screen.getByText('Начать проект') as HTMLButtonElement).disabled)
      .toBe(false))

    fireEvent.click(screen.getByText('Начать проект'))
    await waitFor(() => expect(startMock).toHaveBeenCalledWith(
      'char-1', expect.objectContaining({ costOverride: 10, costOverrideReason: 'свои материалы' })))
  })

  it('траты, которые приложение не исполняет, помечены описанием', async () => {
    craftingMock.mockResolvedValue([draft])
    renderTab()
    expect(await screen.findByText(/только описание/)).toBeTruthy()
  })

  /** Выбор траты интерактивен: символы тратятся, а выбранное видно списком до разрешения. */
  it('позволяет выбрать трату только в пределах выпавших символов', async () => {
    craftingMock.mockResolvedValue([draft])
    renderTab()
    await screen.findByText('Разрешить проект')

    await screen.findByText('Превосходное')
    expect(triumphChip('Превосходное').disabled).toBe(true) // триумфов ноль

    fireEvent.change(screen.getByRole('spinbutton', { name: 'Триумфы' }), { target: { value: '1' } })
    await waitFor(() => expect(triumphChip('Превосходное').disabled).toBe(false))
  })

  it('шлёт выбранные траты вместе с символами броска', async () => {
    craftingMock.mockResolvedValue([draft])
    renderTab()
    await screen.findByText('Разрешить проект')

    fireEvent.change(screen.getByRole('spinbutton', { name: 'Нетто-успехов' }), { target: { value: '2' } })
    fireEvent.change(screen.getByRole('spinbutton', { name: 'Триумфы' }), { target: { value: '1' } })
    fireEvent.click(triumphChip('Превосходное'))
    fireEvent.click(screen.getByText('Разрешить проект'))

    await waitFor(() => expect(resolveMock).toHaveBeenCalled())
    const [charId, projectId, body] = resolveMock.mock.lastCall as [string, string, {
      netSuccesses: number; triumphs: number; spends: Array<{ code: string; paidWith: string }>
    }]
    expect([charId, projectId]).toEqual(['char-1', 'proj-1'])
    expect(body.netSuccesses).toBe(2)
    expect(body.triumphs).toBe(1)
    expect(body.spends).toEqual([
      { code: 'craft-superior', count: 1, paidWith: 'triumph', parameter: '' }])
  })

  it('провал объявляется до разрешения, а не после', async () => {
    craftingMock.mockResolvedValue([draft])
    renderTab()
    expect(await screen.findByText(/Провал: предмет не создаётся/)).toBeTruthy()

    fireEvent.change(screen.getByRole('spinbutton', { name: 'Нетто-успехов' }), { target: { value: '1' } })
    expect(await screen.findByText(/Успех: предмет будет создан/)).toBeTruthy()
  })

  it('зачарование требует согласованной способности', async () => {
    renderTab()
    fireEvent.click(await screen.findByRole('tab', { name: /Зачарование/ }))
    expect(await screen.findByText(/уже превосходной основы/)).toBeTruthy()
    expect((screen.getByText('Начать проект') as HTMLButtonElement).disabled).toBe(true)
  })

  it('не предлагает руну основой зачарования', async () => {
    renderTab()
    fireEvent.click(await screen.findByRole('tab', { name: /Зачарование/ }))

    expect(screen.getByRole('button', { name: /Топор/ })).toBeTruthy()
    expect(screen.queryByRole('button', { name: /Малая руна/ })).toBeNull()
  })

  it('передаёт выбранный магический навык для зачарования', async () => {
    renderTab()
    fireEvent.click(await screen.findByRole('tab', { name: /Зачарование/ }))
    fireEvent.click(screen.getByRole('button', { name: /Топор/ }))
    fireEvent.change(screen.getByLabelText(/Согласованная способность/), { target: { value: 'огненный клинок' } })
    fireEvent.click(screen.getByRole('button', { name: /Руны/ }))

    await waitFor(() => expect(previewMock).toHaveBeenLastCalledWith(
      'char-1', expect.objectContaining({
        itemDefId: 'def-axe', baseCharacterItemId: 'item-axe', kind: 'enchantment', skillName: 'Runes',
      })))
  })

  it('starts enchantment with an untrained magic skill', async () => {
    const untrained = { ...sheet, skills: sheet.skills.map(sk => ({ ...sk, ranks: 0 })) }
    render(<CraftingTab sheet={untrained} reference={reference} onError={() => {}} refresh={async () => {}} />)
    fireEvent.click(screen.getByRole('tab', { name: /Зачарование/ }))
    fireEvent.click(screen.getByRole('button', { name: /Топор/ }))
    fireEvent.change(screen.getByLabelText(/Согласованная способность/), { target: { value: 'Согласованный тестовый эффект' } })
    expect(screen.getByRole('button', { name: /Аркана · 0/ })).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Начать проект' })).toHaveProperty('disabled', false)
    fireEvent.click(screen.getByRole('button', { name: 'Начать проект' }))
    await waitFor(() => expect(startMock).toHaveBeenCalledWith('char-1', expect.objectContaining({ kind: 'enchantment', skillName: 'Arcana' })))
  })

  it('keeps symbols above selected spends until the spend is removed', async () => {
    craftingMock.mockResolvedValue([draft])
    previewMock.mockResolvedValue({ ...preview, spends: [{ ...faster, advantageCost: 3 }] })
    renderTab()
    await screen.findByText('Сократить время на день')
    const advantages = screen.getByRole('spinbutton', { name: 'Преимущества' })
    fireEvent.change(advantages, { target: { value: '3' } })
    fireEvent.click(screen.getByText('▲ 3'))
    const decrease = screen.getByRole('button', { name: 'Уменьшить: Преимущества' })
    expect(decrease).toHaveProperty('disabled', true)
    fireEvent.click(decrease)
    fireEvent.change(advantages, { target: { value: '1' } })
    expect(advantages).toHaveProperty('value', '3')
    expect(screen.getByText(/Осталось:/).textContent).toContain('▲ 0')
    fireEvent.click(screen.getByRole('button', { name: 'Разрешить проект' }))
    await waitFor(() => expect(resolveMock).toHaveBeenCalledWith('char-1', 'proj-1', expect.objectContaining({ advantages: 3 })))
    await waitFor(() => expect(screen.getByRole('button', { name: 'Разрешить проект' })).toHaveProperty('disabled', false))
    fireEvent.click(screen.getByRole('button', { name: /Убрать трату: Сократить/ }))
    fireEvent.click(decrease)
    expect(advantages).toHaveProperty('value', '2')
  })

  it('keeps the search visible when inventory shrinks below the threshold', async () => {
    const bases = Array.from({ length: 13 }, (_, n) => ({ ...sheet.items[0], id: `base-${n}`, nameRu: n === 0 ? 'Меч' : `Основа ${n}` }))
    const props = { reference, onError: () => {}, refresh: async () => {} }
    const { rerender } = render(<CraftingTab {...props} sheet={{ ...sheet, items: bases }} />)
    fireEvent.click(screen.getByRole('tab', { name: /Зачарование/ }))
    fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'Меч' } })
    rerender(<CraftingTab {...props} sheet={{ ...sheet, items: bases.slice(0, 12) }} />)
    expect(screen.getByRole('searchbox')).toHaveProperty('value', 'Меч')
    expect(screen.queryByRole('button', { name: /Основа 1/ })).toBeNull()
    fireEvent.change(screen.getByRole('searchbox'), { target: { value: '' } })
    expect(await screen.findByText('Основа 1', { selector: 'strong' })).toBeTruthy()
  })

  it('cancels a project only after explicit confirmation', async () => {
    craftingMock.mockResolvedValue([draft])
    renderTab()
    fireEvent.click(await screen.findByRole('button', { name: 'Отменить проект' }))
    expect(cancelMock).not.toHaveBeenCalled()
    expect(screen.getByText(/восстановить его нельзя/)).toBeTruthy()
    fireEvent.click(screen.getByRole('button', { name: 'Продолжить работу' }))
    expect(screen.queryByRole('button', { name: 'Да, отменить проект' })).toBeNull()
    fireEvent.click(screen.getByRole('button', { name: 'Отменить проект' }))
    fireEvent.click(screen.getByRole('button', { name: 'Да, отменить проект' }))
    await waitFor(() => expect(cancelMock).toHaveBeenCalledExactlyOnceWith('char-1', 'proj-1'))
  })

})
