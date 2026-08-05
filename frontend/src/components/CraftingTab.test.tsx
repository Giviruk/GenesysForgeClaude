import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
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
  skillName: 'Mechanics', baseDifficulty: 1, difficulty: 1, baseTime: 2, time: 2,
  timeUnit: 'days', listedCost: 75, costPercent: 100, costOverride: null, cost: 75,
  isWeapon: true, spends: [faster, boost, superior],
}

const draft: CraftingProject = {
  id: 'proj-1', kind: 'item', status: 'draft', itemDefId: 'def-axe', baseCharacterItemId: null,
  targetName: 'Axe', targetPrice: 150, targetRarity: 1, skillName: 'Mechanics',
  baseDifficulty: 1, difficulty: 1, difficultyReason: '', baseTime: 2, time: 2,
  timeUnit: 'days', timeReason: '', listedCost: 75, costPercent: 100, costOverride: null,
  costOverrideReason: '', cost: 75, requirements: 'кузница', intent: '', roughSurvival: false,
  netSuccesses: 0, advantages: 0, threats: 0, triumphs: 0, despairs: 0,
  createdCharacterItemId: null, outcome: '', spends: [],
  createdAt: '2026-08-03T00:00:00Z', resolvedAt: null,
}

const sheet = {
  id: 'char-1',
  items: [
    { id: 'item-axe', itemDefId: 'def-axe', name: 'Axe', nameRu: 'Топор' },
    { id: 'item-rune', itemDefId: 'def-rune', name: 'Lesser Rune', nameRu: 'Малая руна' },
  ],
  skills: [
    { skillDefId: 'skill-arcana', name: 'Arcana', nameRu: 'Аркана', kind: 'magic' },
    { skillDefId: 'skill-runes', name: 'Runes', nameRu: 'Руны', kind: 'magic' },
    { skillDefId: 'skill-mechanics', name: 'Mechanics', nameRu: 'Механика', kind: 'general' },
  ],
} as unknown as CharacterSheet
const reference = {
  items: [
    { id: 'def-axe', code: 'axe', name: 'Axe', nameRu: 'Топор', price: 150, rarity: 1 },
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
    fireEvent.change(await screen.findByLabelText(/Что делаем/), { target: { value: 'def-axe' } })
    await waitFor(() => expect(previewMock).toHaveBeenCalled())

    fireEvent.click(screen.getByText('150%'))
    await waitFor(() => expect(previewMock).toHaveBeenLastCalledWith(
      'char-1', expect.objectContaining({ costPercent: 150, costOverride: null })))
  })

  it('переключает каталог между обычными предметами и зельями', async () => {
    renderTab()
    const select = await screen.findByLabelText(/Что делаем/)
    expect(within(select).getByRole('option', { name: 'Топор' })).toBeTruthy()
    expect(within(select).queryByRole('option', { name: 'Эликсир выносливости' })).toBeNull()

    fireEvent.change(select, { target: { value: 'def-axe' } })
    fireEvent.click(screen.getByRole('tab', { name: 'Варка зелья' }))

    const potionSelect = screen.getByLabelText(/Что делаем/) as HTMLSelectElement
    expect(potionSelect.value).toBe('')
    expect(within(potionSelect).queryByRole('option', { name: 'Топор' })).toBeNull()
    expect(within(potionSelect).getByRole('option', { name: 'Эликсир выносливости' })).toBeTruthy()

    fireEvent.change(potionSelect, { target: { value: 'def-potion' } })
    await waitFor(() => expect(previewMock).toHaveBeenLastCalledWith(
      'char-1', expect.objectContaining({ itemDefId: 'def-potion', kind: 'potion' })))
  })

  it('отделяет услуги от предметов, доступных для ремесла', async () => {
    renderTab()
    const select = await screen.findByLabelText(/Что делаем/)
    const axe = within(select).getByRole('option', { name: 'Топор' }) as HTMLOptionElement
    const meal = within(select).getByRole('option', { name: 'Еда в таверне' }) as HTMLOptionElement

    expect(axe.disabled).toBe(false)
    expect(meal.disabled).toBe(true)
    expect(meal.parentElement?.getAttribute('label')).toBe('Нельзя создать ремеслом')
  })

  /** Своя цена отменяет долю и требует причины — то же правило, что при покупке. */
  it('своя стоимость требует причины и блокирует старт без неё', async () => {
    renderTab()
    fireEvent.change(await screen.findByLabelText(/Что делаем/), { target: { value: 'def-axe' } })
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

    fireEvent.change(screen.getByLabelText(/Триумфы/), { target: { value: '1' } })
    await waitFor(() => expect(triumphChip('Превосходное').disabled).toBe(false))
  })

  it('шлёт выбранные траты вместе с символами броска', async () => {
    craftingMock.mockResolvedValue([draft])
    renderTab()
    await screen.findByText('Разрешить проект')

    fireEvent.change(screen.getByLabelText(/Нетто-успехов/), { target: { value: '2' } })
    fireEvent.change(screen.getByLabelText(/Триумфы/), { target: { value: '1' } })
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

    fireEvent.change(screen.getByLabelText(/Нетто-успехов/), { target: { value: '1' } })
    expect(await screen.findByText(/Успех: предмет будет создан/)).toBeTruthy()
  })

  it('зачарование требует согласованной способности', async () => {
    renderTab()
    fireEvent.click(await screen.findByRole('tab', { name: 'Зачарование' }))
    expect(await screen.findByText(/уже превосходной основы/)).toBeTruthy()
    expect((screen.getByText('Начать проект') as HTMLButtonElement).disabled).toBe(true)
  })

  it('не предлагает руну основой зачарования', async () => {
    renderTab()
    fireEvent.click(await screen.findByRole('tab', { name: 'Зачарование' }))

    const select = screen.getByLabelText(/Основа из инвентаря/)
    expect(within(select).getByRole('option', { name: 'Топор' })).toBeTruthy()
    expect(within(select).queryByRole('option', { name: 'Малая руна' })).toBeNull()
  })

  it('передаёт выбранный магический навык для зачарования', async () => {
    renderTab()
    fireEvent.click(await screen.findByRole('tab', { name: 'Зачарование' }))
    fireEvent.change(screen.getByLabelText(/Основа из инвентаря/), { target: { value: 'item-axe' } })
    fireEvent.change(screen.getByLabelText(/Согласованная способность/), { target: { value: 'огненный клинок' } })
    fireEvent.change(screen.getByLabelText(/Навык зачарования/), { target: { value: 'Runes' } })

    await waitFor(() => expect(previewMock).toHaveBeenLastCalledWith(
      'char-1', expect.objectContaining({
        itemDefId: 'def-axe', baseCharacterItemId: 'item-axe', kind: 'enchantment', skillName: 'Runes',
      })))
  })
})
