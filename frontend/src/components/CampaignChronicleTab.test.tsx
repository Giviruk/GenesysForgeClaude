import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { CampaignChronicleChapter, NpcDetail, NpcListItem } from '../api/types'
import { CampaignChronicleTab } from './CampaignChronicleTab'

const chapter: CampaignChronicleChapter = {
  id: 'chapter-1', title: 'Пролог', content: '# Пролог\n\n', sortOrder: 0, currentVersion: 1,
  createdAt: '2026-08-13T00:00:00Z', updatedAt: '2026-08-13T00:00:00Z', updatedBy: 'Мастер',
}

const npc = (id: string, name: string, overrides: Partial<NpcListItem> = {}): NpcListItem => ({
  id, name, system: 'realmsOfTerrinoth', kind: 'rival', role: 'custom', silhouette: 1,
  soak: 2, woundThreshold: 10, strainThreshold: 10, visibility: 'publicTemplate', campaignId: null,
  isMine: false, isBuiltIn: true, skills: [], tags: [], createdAt: '2026-08-13T00:00:00Z',
  ...overrides,
})

const campaignChronicleMock = vi.fn()
const npcsMock = vi.fn()
const npcMock = vi.fn()
const characterId = '11111111-1111-1111-1111-111111111111'
const linkedNpcId = '22222222-2222-2222-2222-222222222222'

vi.mock('../api/client', () => ({
  api: {
    campaignChronicle: (...args: unknown[]) => campaignChronicleMock(...args),
    npcs: (...args: unknown[]) => npcsMock(...args),
    npc: (...args: unknown[]) => npcMock(...args),
  },
}))

vi.mock('../router', () => ({ navigate: vi.fn() }))

describe('CampaignChronicleTab — links', () => {
  beforeEach(() => {
    campaignChronicleMock.mockResolvedValue([chapter])
    npcsMock.mockResolvedValue([
      npc('npc-goblin', 'Гоблин'),
      npc('npc-baron', 'Барон'),
      npc('npc-dragon', 'Дракон'),
      npc('npc-custom', 'Хозяйский алхимик', { isBuiltIn: false, isMine: true }),
      npc('npc-campaign', 'Кампанийный проводник', {
        isBuiltIn: false, isMine: true, visibility: 'campaignVisible', campaignId: null,
      }),
    ])
    npcMock.mockResolvedValue({
      id: linkedNpcId, name: 'Гоблин', system: 'realmsOfTerrinoth', kind: 'rival', role: 'custom',
      description: 'Хитрый противник.', source: 'Test', brawn: 2, agility: 3, intellect: 1,
      cunning: 2, willpower: 1, presence: 1, woundThreshold: 8, strainThreshold: 6, soak: 2,
      meleeDefense: 0, rangedDefense: 0, silhouette: 1, tactics: 'Прячется.', visibility: 'publicTemplate',
      campaignId: null, isMine: false, isBuiltIn: false, skills: [{ name: 'Скрытность', ranks: 2 }],
      abilities: [], attacks: [], talents: [], equipment: [], tags: [], warnings: [], createdAt: '', updatedAt: '',
    } satisfies NpcDetail)
  })

  function renderTab(onOpenCharacter = vi.fn()) {
    return render(<CampaignChronicleTab campaignId="campaign-1" refreshSignal={0}
      members={[{ characterId, characterName: 'Бард', system: 'realmsOfTerrinoth',
        archetype: 'Человек', career: 'Менестрель', isMine: false }]}
      onOpenCharacter={onOpenCharacter} onError={vi.fn()} />)
  }

  it('searches inside the NPC selector', async () => {
    renderTab()
    const selector = await screen.findByRole('combobox', { name: 'NPC для ссылки' })
    fireEvent.focus(selector)
    await waitFor(() => expect(within(screen.getByRole('listbox', { name: 'Результаты поиска NPC' }))
      .getByRole('option', { name: 'Гоблин' })).toBeTruthy())

    fireEvent.change(selector, { target: { value: 'гоб' } })

    const results = screen.getByRole('listbox', { name: 'Результаты поиска NPC' })
    expect(within(results).getByRole('option', { name: 'Гоблин' })).toBeTruthy()
    expect(within(results).queryByRole('option', { name: 'Дракон' })).toBeNull()
  })

  it('finds and inserts a public custom NPC returned by the API', async () => {
    renderTab()
    const selector = await screen.findByRole('combobox', { name: 'NPC для ссылки' })
    fireEvent.change(selector, { target: { value: 'алхимик' } })
    fireEvent.click(within(screen.getByRole('listbox', { name: 'Результаты поиска NPC' }))
      .getByRole('option', { name: /Хозяйский алхимик/ }))
    fireEvent.click(within(selector.closest('.chronicle-npc-picker') as HTMLElement).getByRole('button', { name: '＋' }))

    expect((screen.getByRole('textbox', { name: 'Markdown-текст главы' }) as HTMLTextAreaElement).value)
      .toContain('[Хозяйский алхимик](npc:npc-custom)')
  })

  it('requests NPCs in the current campaign context', async () => {
    renderTab()
    const selector = await screen.findByRole('combobox', { name: 'NPC для ссылки' })
    fireEvent.focus(selector)
    const results = await screen.findByRole('listbox', { name: 'Результаты поиска NPC' })

    expect(npcsMock).toHaveBeenCalledWith({ campaignId: 'campaign-1' })
    expect(within(results).getByRole('option', { name: /Кампанийный проводник/ })).toBeTruthy()
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

  it('открывает ссылку NPC в модальной карточке, не меняя страницу', async () => {
    campaignChronicleMock.mockResolvedValue([{ ...chapter, content: `[Гоблин](npc:${linkedNpcId})` }])
    renderTab()

    fireEvent.click(await screen.findByRole('link', { name: 'Гоблин' }))

    const dialog = await screen.findByRole('dialog', { name: 'Карточка NPC: Гоблин' })
    expect(npcMock).toHaveBeenCalledWith(linkedNpcId)
    expect(within(dialog).getByText('Хитрый противник.')).toBeTruthy()
    expect(window.location.pathname).toBe('/')
  })
})
