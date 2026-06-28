import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import type { NpcDetail, NpcListItem } from '../api/types'
import { NpcsPage } from './NpcsPage'

const builtInItem: NpcListItem = {
  id: 'b1', name: 'Гигант', system: 'realmsOfTerrinoth', kind: 'nemesis', role: 'brute',
  soak: 8, woundThreshold: 33, strainThreshold: 29, visibility: 'publicTemplate', campaignId: null,
  isMine: false, isBuiltIn: true, skills: [], tags: [], createdAt: '',
}
const mineItem: NpcListItem = {
  id: 'm1', name: 'Мой гоблин', system: 'realmsOfTerrinoth', kind: 'rival', role: 'skirmisher',
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
vi.mock('../api/client', () => ({
  api: {
    npcs: (...a: unknown[]) => npcsMock(...a),
    npc: (...a: unknown[]) => npcMock(...a),
    reference: () => Promise.reject(new Error('no ref')),
    duplicateNpc: (...a: unknown[]) => duplicateMock(...a),
  },
}))

describe('NpcsPage — встроенный бестиарий', () => {
  it('помечает встроенных существ и фильтрует по источнику', async () => {
    render(<NpcsPage openId={null} onOpen={() => {}} onBack={() => {}} />)

    await waitFor(() => expect(screen.getByText('Гигант')).toBeTruthy())
    expect(screen.getByText('Мой гоблин')).toBeTruthy()
    expect(screen.getByText('встроенный')).toBeTruthy() // флаг на встроенном

    // Фильтр «Встроенные» скрывает мои NPC.
    fireEvent.change(screen.getByDisplayValue('Все источники'), { target: { value: 'builtin' } })
    expect(screen.getByText('Гигант')).toBeTruthy()
    expect(screen.queryByText('Мой гоблин')).toBeNull()

    // Фильтр «Мои» скрывает встроенных.
    fireEvent.change(screen.getByDisplayValue('Встроенные'), { target: { value: 'mine' } })
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
