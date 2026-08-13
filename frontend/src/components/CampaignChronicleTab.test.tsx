import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { CampaignChronicleChapter, NpcListItem } from '../api/types'
import { CampaignChronicleTab } from './CampaignChronicleTab'

const chapter: CampaignChronicleChapter = {
  id: 'chapter-1', title: 'Пролог', content: '# Пролог\n\n', sortOrder: 0, currentVersion: 1,
  createdAt: '2026-08-13T00:00:00Z', updatedAt: '2026-08-13T00:00:00Z', updatedBy: 'Мастер',
}

const npc = (id: string, name: string): NpcListItem => ({
  id, name, system: 'realmsOfTerrinoth', kind: 'rival', role: 'custom', silhouette: 1,
  soak: 2, woundThreshold: 10, strainThreshold: 10, visibility: 'publicTemplate', campaignId: null,
  isMine: false, isBuiltIn: true, skills: [], tags: [], createdAt: '2026-08-13T00:00:00Z',
})

const campaignChronicleMock = vi.fn()
const npcsMock = vi.fn()
const characterId = '11111111-1111-1111-1111-111111111111'

vi.mock('../api/client', () => ({
  api: {
    campaignChronicle: (...args: unknown[]) => campaignChronicleMock(...args),
    npcs: (...args: unknown[]) => npcsMock(...args),
  },
}))

vi.mock('../router', () => ({ navigate: vi.fn() }))

describe('CampaignChronicleTab — links', () => {
  beforeEach(() => {
    campaignChronicleMock.mockResolvedValue([chapter])
    npcsMock.mockResolvedValue([npc('npc-goblin', 'Гоблин'), npc('npc-baron', 'Барон'), npc('npc-dragon', 'Дракон')])
  })

  function renderTab(onOpenCharacter = vi.fn()) {
    return render(<CampaignChronicleTab campaignId="campaign-1" refreshSignal={0}
      members={[{ characterId, characterName: 'Бард', system: 'realmsOfTerrinoth',
        archetype: 'Человек', career: 'Менестрель', isMine: false }]}
      onOpenCharacter={onOpenCharacter} onError={vi.fn()} />)
  }

  it('filters the NPC selector by name', async () => {
    renderTab()
    const search = await screen.findByRole('searchbox', { name: 'Поиск NPC' })
    await waitFor(() => expect(within(screen.getByRole('combobox', { name: 'NPC для ссылки' }))
      .getByRole('option', { name: 'Гоблин' })).toBeTruthy())

    fireEvent.change(search, { target: { value: 'гоб' } })

    const selector = screen.getByRole('combobox', { name: 'NPC для ссылки' })
    expect(within(selector).getByRole('option', { name: 'Гоблин' })).toBeTruthy()
    expect(within(selector).queryByRole('option', { name: 'Дракон' })).toBeNull()
  })

  it('inserts a character link through @ and Enter', async () => {
    renderTab()
    const editor = await screen.findByRole('textbox', { name: 'Markdown-текст главы' }) as HTMLTextAreaElement
    const text = 'Встретили @бар'
    fireEvent.change(editor, { target: { value: text, selectionStart: text.length } })

    const suggestions = await screen.findByRole('listbox', { name: 'Упоминания' })
    expect(within(suggestions).getByRole('option', { name: /Бард/ })).toBeTruthy()
    fireEvent.keyDown(editor, { key: 'Enter' })

    expect(editor.value).toBe(`Встретили [Бард](character:${characterId}) `)
  })

  it('filters @ suggestions and inserts an NPC link with a click', async () => {
    renderTab()
    const editor = await screen.findByRole('textbox', { name: 'Markdown-текст главы' }) as HTMLTextAreaElement
    const text = 'У ворот @гоб'
    fireEvent.change(editor, { target: { value: text, selectionStart: text.length } })

    const suggestions = await screen.findByRole('listbox', { name: 'Упоминания' })
    expect(within(suggestions).queryByRole('option', { name: /Дракон/ })).toBeNull()
    fireEvent.click(within(suggestions).getByRole('option', { name: /Гоблин/ }))

    expect(editor.value).toBe('У ворот [Гоблин](npc:npc-goblin) ')
  })

  it('передаёт ссылку персонажа в экранную навигацию кампании', async () => {
    campaignChronicleMock.mockResolvedValue([{ ...chapter, content: `[Бард](character:${characterId})` }])
    const onOpenCharacter = vi.fn()
    renderTab(onOpenCharacter)

    fireEvent.click(await screen.findByRole('link', { name: 'Бард' }))

    expect(onOpenCharacter).toHaveBeenCalledWith(characterId, 'Бард')
  })
})
