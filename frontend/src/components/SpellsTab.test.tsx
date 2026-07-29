import { render, screen, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { Spell } from '../api/types'
import { parseDifficulty } from '../utils/labels'
import { api } from '../api/client'
import { SpellsTab } from './SpellsTab'

vi.mock('../api/client', () => ({ api: { spells: vi.fn() } }))

const spell = (over: Partial<Spell>): Spell => {
  const base: Spell = {
    id: Math.random().toString(36), magicSkill: '', kind: 'effect', parentEffect: '',
    nameRu: '', nameEn: '', difficulty: '', description: '', safeDescription: '',
    source: 'Test', isCustom: false, restrictedSkill: '', repeatable: false,
    allowedSkills: [], difficultyIncrease: 0, exclusions: [],
    resolution: 'onSuccess', isOptional: false, ...over,
  }
  return { ...base, difficultyIncrease: over.difficultyIncrease ?? parseDifficulty(base.difficulty) }
}

// Проклятье умеют Магия и Вера, Лечение — только Вера, Маска — опциональная книга.
const spells: Spell[] = [
  spell({ magicSkill: 'Arcana', nameRu: 'Проклятье', nameEn: 'Curse', difficulty: '2 (Average)', allowedSkills: ['Arcana', 'Divine'] }),
  spell({ magicSkill: 'Divine', nameRu: 'Проклятье', nameEn: 'Curse', difficulty: '2 (Average)', allowedSkills: ['Arcana', 'Divine'] }),
  spell({ magicSkill: 'Divine', nameRu: 'Лечение', nameEn: 'Heal', difficulty: '1 (Easy)', allowedSkills: ['Divine'] }),
  spell({ magicSkill: 'Arcana', nameRu: 'Маска', nameEn: 'Mask', difficulty: '1 (Easy)', allowedSkills: ['Arcana'], isOptional: true }),
  spell({
    kind: 'additionalEffect', parentEffect: 'Curse', nameRu: 'Рок', nameEn: 'Doom', difficulty: '+2',
    allowedSkills: ['Arcana'], restrictedSkill: 'Arcana', safeDescription: 'Меняет грань кости.',
  }),
  spell({
    kind: 'additionalEffect', parentEffect: 'Curse', nameRu: 'Отчаяние', nameEn: 'Despair', difficulty: '+1',
    allowedSkills: ['Divine'], restrictedSkill: 'Divine', exclusions: ['Additional Target'],
    safeDescription: 'Снижает пороги.',
  }),
  spell({
    kind: 'additionalEffect', parentEffect: 'Curse', nameRu: 'Дополнительная цель', nameEn: 'Additional Target',
    difficulty: '+1', allowedSkills: ['Arcana', 'Divine'], exclusions: ['Despair'], safeDescription: 'Добавляет цель.',
  }),
]

/**
 * ROT-MAG-01: справочник обязан объяснять и недоступность тоже — иначе непонятно, потерялось
 * действие или направление им не владеет.
 */
describe('SpellsTab — матрица доступности', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(api.spells).mockResolvedValue(spells)
  })

  it('показывает и доступные действия, и недоступные прочерком', async () => {
    render(<SpellsTab system="realmsOfTerrinoth" onError={() => {}} />)
    await screen.findByText(/Доступность действий по направлениям/)

    const heal = screen.getByRole('row', { name: /Лечение/ })
    // Вера лечит, Магия — нет.
    expect(within(heal).getByLabelText(/Божественная \(Divine\): доступно/)).toBeTruthy()
    expect(within(heal).getByLabelText(/Тайная \(Arcana\): недоступно/)).toBeTruthy()

    const curse = screen.getByRole('row', { name: /Проклятье/ })
    expect(within(curse).getByLabelText(/Тайная \(Arcana\): доступно/)).toBeTruthy()
  })

  it('помечает опциональный контент Expanded Player’s Guide', async () => {
    render(<SpellsTab system="realmsOfTerrinoth" onError={() => {}} />)
    await screen.findByText(/Доступность действий по направлениям/)

    const mask = screen.getByRole('row', { name: /Маска/ })
    expect(within(mask).getByText(/EPG/)).toBeTruthy()
  })

  it('у дополнительного эффекта показывает, кому он доступен и с чем не сочетается', async () => {
    render(<SpellsTab system="realmsOfTerrinoth" onError={() => {}} />)
    await screen.findByText(/Доступность действий по направлениям/)

    const doom = screen.getByRole('row', { name: /Рок/ })
    expect(within(doom).getByText('Тайная (Arcana)')).toBeTruthy()

    // Имя строки начинается с названия эффекта: «Отчаяние» упоминается и в соседней строке.
    const despair = screen.getByRole('row', { name: /^Отчаяние/ })
    expect(within(despair).getByText(/не сочетается с/)).toBeTruthy()
    expect(within(despair).getByText(/Дополнительная цель/)).toBeTruthy()
  })
})
