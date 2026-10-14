import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { NpcDetail, NpcListItem } from '../api/types'
import { NpcsPage } from './NpcsPage'

const builtInItem: NpcListItem = {
  id: 'b1', name: 'Гигант', system: 'realmsOfTerrinoth', kind: 'nemesis', role: 'brute',
  silhouette: 3,
  soak: 8, woundThreshold: 33, strainThreshold: 29, visibility: 'publicTemplate', campaignId: null,
  isMine: false, isBuiltIn: true, skills: [], tags: [], createdAt: '',
}
const mineItem: NpcListItem = {
  id: 'm1', name: 'Мой гоблин', system: 'realmsOfTerrinoth', kind: 'rival', role: 'skirmisher',
  silhouette: 1,
  soak: 3, woundThreshold: 12, strainThreshold: null, visibility: 'private', campaignId: null,
  isMine: true, isBuiltIn: false, skills: [], tags: [], createdAt: '',
}

const builtInDetail: NpcDetail = {
  id: 'b1', name: 'Гигант', system: 'realmsOfTerrinoth', kind: 'nemesis', role: 'brute',
  description: '', source: 'Realms of Terrinoth', brawn: 6, agility: 2, intellect: 1, cunning: 2, willpower: 2, presence: 2,
  woundThreshold: 33, strainThreshold: 29, soak: 8, meleeDefense: 0, rangedDefense: 0, silhouette: 3, tactics: '',
  visibility: 'publicTemplate', campaignId: null, isMine: false, isBuiltIn: true,
  skills: [], abilities: [], attacks: [], talents: [], equipment: [], tags: [], warnings: [],
  createdAt: '', updatedAt: '',
}

const npcsMock = vi.fn().mockResolvedValue([builtInItem, mineItem])
const npcMock = vi.fn().mockResolvedValue(builtInDetail)
const duplicateMock = vi.fn().mockResolvedValue({ ...builtInDetail, id: 'c1', isMine: true, isBuiltIn: false, visibility: 'private' })
const referenceMock = vi.fn()
const updateMock = vi.fn()
vi.mock('../api/client', () => ({
  api: {
    npcs: (...a: unknown[]) => npcsMock(...a),
    npc: (...a: unknown[]) => npcMock(...a),
    reference: (...a: unknown[]) => referenceMock(...a),
    updateNpc: (...a: unknown[]) => updateMock(...a),
    duplicateNpc: (...a: unknown[]) => duplicateMock(...a),
  },
}))

describe('NpcsPage — встроенный бестиарий', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    window.history.replaceState(null, '', '/npcs')
    npcMock.mockResolvedValue(builtInDetail)
    referenceMock.mockRejectedValue(new Error('no ref'))
  })
  it.each(['minion', 'rival'] as const)('keeps %s strain disabled and saves no hidden threshold', async kind => {
    const saved = { ...builtInDetail, kind, isMine: true, isBuiltIn: false }
    npcMock.mockResolvedValue(saved)
    updateMock.mockResolvedValue({ ...saved, strainThreshold: null })
    const { container } = render(<NpcsPage openId="b1" edit onOpen={() => {}} onBack={() => {}} />)
    const strain = await screen.findByLabelText('Усталость', { exact: true }) as HTMLInputElement
    expect(strain.disabled).toBe(true)
    expect(strain.value).toBe('')
    expect(strain.placeholder).toBe('—')
    fireEvent.submit(container.querySelector('form')!)
    await waitFor(() => expect(updateMock).toHaveBeenCalledWith('b1', expect.objectContaining({ strainThreshold: null })))
  })
  it('keeps nemesis strain editable and places talents with abilities', async () => {
    const saved = { ...builtInDetail, isMine: true, isBuiltIn: false, talents: ['Тестовый талант'] }
    npcMock.mockResolvedValue(saved)
    referenceMock.mockResolvedValue({ skills: [], items: [], qualities: [], talents: [{ name: 'Test talent', nameRu: 'Тестовый талант' }] })
    const { container } = render(<NpcsPage openId="b1" edit onOpen={() => {}} onBack={() => {}} />)
    const strain = await screen.findByLabelText('Усталость', { exact: true }) as HTMLInputElement
    expect(strain.disabled).toBe(false)
    expect(strain.value).toBe('29')
    expect(container.querySelector('.bt-section-nav button:nth-child(4) svg')).toBeTruthy()
    expect(container.querySelector('.bt-section-nav button:nth-child(5) svg')).toBeNull()
    fireEvent.click(screen.getByRole('button', { name: /^4 · Способности/ }))
    expect(container.querySelector('#npc-section-3 select[aria-label="Таланты"]')).toBeTruthy()
    expect(container.querySelector('#npc-section-4 select[aria-label="Таланты"]')).toBeNull()
  })
  it('edits a legacy skill as a reference skill and saves its stable English name', async () => {
    const saved = { ...builtInDetail, isMine: true, isBuiltIn: false,
      skills: [{ name: 'Рукопашный бой', ranks: 2 }] }
    npcMock.mockResolvedValue(saved)
    updateMock.mockResolvedValue(saved)
    referenceMock.mockResolvedValue({ skills: [{ id: 'brawl', name: 'Brawl', nameRu: 'Грубая сила',
      characteristic: 'brawn', kind: 'combat', isCustom: false }], items: [], talents: [], qualities: [] })
    const { container } = render(<NpcsPage openId="b1" edit onOpen={() => {}} onBack={() => {}} />)
    const select = await screen.findByRole('combobox', { name: 'Навык 1' }) as HTMLSelectElement
    await waitFor(() => expect(select.value).toBe('Brawl'))
    expect(select.selectedOptions[0].textContent).toBe('Грубая сила / Brawl')
    expect(screen.queryByText(/вне справочника/)).toBeNull()
    expect(container.querySelector('.bt-skill-pool .genesys-symbol')).toBeTruthy()
    expect(container.querySelector('.bt-rank-stepper')?.textContent).toContain('Ранг: Грубая сила')
    fireEvent.submit(container.querySelector('form')!)
    await waitFor(() => expect(updateMock).toHaveBeenCalledWith('b1', expect.objectContaining({
      skills: [{ name: 'Brawl', ranks: 2 }],
    })))
  })
  it('помечает встроенных существ и фильтрует по источнику', async () => {
    render(<NpcsPage openId={null} onOpen={() => {}} onBack={() => {}} />)

    await waitFor(() => expect(screen.getByText('Гигант')).toBeTruthy())
    expect(screen.getByText('Мой гоблин')).toBeTruthy()
    expect(screen.getByText('встроенный')).toBeTruthy() // флаг на встроенном

    // Фильтр «Встроенные» скрывает мои NPC.
    fireEvent.click(screen.getByRole('button', { name: /^Встроенные$/ }))
    expect(screen.getByText('Гигант')).toBeTruthy()
    expect(screen.queryByText('Мой гоблин')).toBeNull()

    // Фильтр «Мои» скрывает встроенных.
    fireEvent.click(screen.getByRole('button', { name: /^Мои/ }))
    expect(screen.queryByText('Гигант')).toBeNull()
    expect(screen.getByText('Мой гоблин')).toBeTruthy()
  })

  it('встроенный — read-only: есть «Клонировать», нет «Редактировать/Удалить»', async () => {
    const onOpen = vi.fn()
    render(<NpcsPage openId="b1" onOpen={onOpen} onBack={() => {}} />)

    await waitFor(() => expect(screen.getByText(/встроенный · только чтение/)).toBeTruthy())
    expect(screen.getByRole('button', { name: /Клонировать в мою библиотеку/ })).toBeTruthy()
    expect(screen.queryByRole('button', { name: 'Редактировать' })).toBeNull()
    expect(screen.queryByRole('button', { name: 'Удалить' })).toBeNull()

    // Клонирование уводит на новую копию пользователя.
    fireEvent.click(screen.getByRole('button', { name: /Клонировать в мою библиотеку/ }))
    await waitFor(() => expect(duplicateMock).toHaveBeenCalledWith('b1'))
    await waitFor(() => expect(onOpen).toHaveBeenCalledWith('c1'))
  })
})
