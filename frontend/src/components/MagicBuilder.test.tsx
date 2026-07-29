import { fireEvent, render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { Spell } from '../api/types'
import { parseDifficulty } from '../utils/labels'
import { api } from '../api/client'
import { MagicBuilder } from './MagicBuilder'

vi.mock('../api/client', () => ({
  api: {
    spells: vi.fn(),
  },
}))

const spell = (over: Partial<Spell>): Spell => {
  const base: Spell = {
    id: 'spell',
    magicSkill: '',
    kind: 'effect',
    parentEffect: '',
    nameRu: '',
    nameEn: '',
    difficulty: '',
    restrictedSkill: '', repeatable: false,
    description: '',
    safeDescription: '',
    source: 'Test',
    isCustom: false,
    allowedSkills: [], difficultyIncrease: 0, exclusions: [],
    resolution: 'onSuccess', isOptional: false,
    ...over,
  }
  // Число сложности приходит с сервера полем; в фикстуре оно выводится из печатной строки,
  // чтобы тесты не задавали одно и то же дважды.
  return { ...base, difficultyIncrease: over.difficultyIncrease ?? parseDifficulty(base.difficulty) }
}

// База 2 + доп. эффекты (+1, +2, +2): потолок 5 достигается парой «+1 и +2».
const spells: Spell[] = [
  spell({ id: 'base', kind: 'effect', magicSkill: 'Runes', nameRu: 'Атака', nameEn: 'Attack', difficulty: '2 (Average)' }),
  spell({ id: 'a1', kind: 'additionalEffect', parentEffect: 'Attack', nameRu: 'Дальность', nameEn: 'Range', difficulty: '+1', safeDescription: 'Увеличивает дальность.' }),
  spell({ id: 'a2', kind: 'additionalEffect', parentEffect: 'Attack', nameRu: 'Огонь', nameEn: 'Fire', difficulty: '+2', safeDescription: 'Добавляет свойство Огонь.' }),
  spell({ id: 'a3', kind: 'additionalEffect', parentEffect: 'Attack', nameRu: 'Лёд', nameEn: 'Ice', difficulty: '+2', safeDescription: 'Добавляет свойство Лёд.' }),
]

describe('MagicBuilder — потолок сложности 5', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(api.spells).mockResolvedValue(spells)
  })

  it('считает итоговую сложность и блокирует эффекты сверх потолка', async () => {
    render(<MagicBuilder system="realmsOfTerrinoth" onError={() => {}} />)

    // База 2 загрузилась.
    expect(await screen.findByText(/Сложность: 2/)).toBeTruthy()

    // +1 и +2 → итог 5 (потолок).
    fireEvent.click(screen.getByRole('button', { name: /Дальность \+1/ }))
    fireEvent.click(screen.getByRole('button', { name: /Огонь \+2/ }))
    expect(screen.getByText(/Сложность: 5/)).toBeTruthy()
    expect(screen.getByText(/потолок 5 достигнут/)).toBeTruthy()

    // Оставшийся «+2» превысил бы потолок — chip недоступен и клики игнорируются.
    const ice = screen.getByRole('button', { name: /Лёд \+2/ }) as HTMLButtonElement
    expect(ice.disabled).toBe(true)
    expect(ice.title).toContain('превысит потолок 5')
    fireEvent.click(ice)
    expect(screen.getByText(/Сложность: 5/)).toBeTruthy()
  })

  it('после снятия эффекта заблокированный chip снова доступен', async () => {
    render(<MagicBuilder system="realmsOfTerrinoth" onError={() => {}} />)
    await screen.findByText(/Сложность: 2/)

    fireEvent.click(screen.getByRole('button', { name: /Дальность \+1/ }))
    fireEvent.click(screen.getByRole('button', { name: /Огонь \+2/ }))
    expect((screen.getByRole('button', { name: /Лёд \+2/ }) as HTMLButtonElement).disabled).toBe(true)

    // Снимаем «Огонь» через chip — итог 3, «Лёд +2» снова доступен.
    fireEvent.click(screen.getByRole('button', { name: /Огонь \+2/ }))
    expect(screen.getByText(/Сложность: 3/)).toBeTruthy()
    expect((screen.getByRole('button', { name: /Лёд \+2/ }) as HTMLButtonElement).disabled).toBe(false)
  })
})

/**
 * ROT-MAG-01. Доступность приходит с сервера полем allowedSkills: чужой эффект не выбирается
 * и объясняет, почему, а несочетаемая пара блокируется, как только выбран её первый эффект.
 */
describe('MagicBuilder — доступность эффектов направлению', () => {
  // Проклятье умеют Магия и Вера; «Рок» — только Магия, «Отчаяние» не сочетается с «Доп. целью».
  const curse: Spell[] = [
    spell({ id: 'curse-arcana', magicSkill: 'Arcana', nameRu: 'Проклятье', nameEn: 'Curse', difficulty: '2 (Average)', allowedSkills: ['Arcana', 'Divine'] }),
    spell({ id: 'curse-divine', magicSkill: 'Divine', nameRu: 'Проклятье', nameEn: 'Curse', difficulty: '2 (Average)', allowedSkills: ['Arcana', 'Divine'] }),
    spell({ id: 'doom', kind: 'additionalEffect', parentEffect: 'Curse', nameRu: 'Рок', nameEn: 'Doom', difficulty: '+2', allowedSkills: ['Arcana'], restrictedSkill: 'Arcana' }),
    spell({ id: 'target', kind: 'additionalEffect', parentEffect: 'Curse', nameRu: 'Дополнительная цель', nameEn: 'Additional Target', difficulty: '+1', allowedSkills: ['Arcana', 'Divine'], exclusions: ['Despair'] }),
    spell({ id: 'despair', kind: 'additionalEffect', parentEffect: 'Curse', nameRu: 'Отчаяние', nameEn: 'Despair', difficulty: '+1', allowedSkills: ['Divine'], restrictedSkill: 'Divine', exclusions: ['Additional Target'] }),
  ]

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(api.spells).mockResolvedValue(curse)
  })

  it('чужой направлению эффект заблокирован и называет, кому он доступен', async () => {
    render(<MagicBuilder system="realmsOfTerrinoth" onError={() => {}} />)
    await screen.findByText(/Сложность: 2/)

    // Первое направление — Магия: «Рок» доступен, «Отчаяние» (только Вера) — нет.
    const doom = screen.getByRole('button', { name: /Рок/ }) as HTMLButtonElement
    expect(doom.disabled).toBe(false)
    const despair = screen.getByRole('button', { name: /Отчаяние/ }) as HTMLButtonElement
    expect(despair.disabled).toBe(true)
    expect(despair.title).toContain('эффект только для Божественная (Divine)')

    fireEvent.click(despair)
    expect(screen.getByText(/Сложность: 2/)).toBeTruthy() // клик по заблокированному ничего не меняет
  })

  it('при смене направления недоступный эффект перестаёт считаться в сложности', async () => {
    render(<MagicBuilder system="realmsOfTerrinoth" onError={() => {}} />)
    await screen.findByText(/Сложность: 2/)

    fireEvent.click(screen.getByRole('button', { name: /Рок/ }))
    expect(screen.getByText(/Сложность: 4/)).toBeTruthy()

    // Жрецу «Рок» недоступен — он и в сложность больше не входит.
    fireEvent.change(screen.getByLabelText(/Направление/), { target: { value: 'Divine' } })
    expect(screen.getByText(/Сложность: 2/)).toBeTruthy()
    expect((screen.getByRole('button', { name: /Рок/ }) as HTMLButtonElement).disabled).toBe(true)
  })

  it('несочетаемый эффект блокируется, пока выбран его антагонист', async () => {
    render(<MagicBuilder system="realmsOfTerrinoth" onError={() => {}} />)
    await screen.findByText(/Сложность: 2/)
    fireEvent.change(screen.getByLabelText(/Направление/), { target: { value: 'Divine' } })

    // По надбавке в имени, чтобы не поймать крестик «Убрать эффект» у выбранного чипа.
    const target = () => screen.getByRole('button', { name: /Дополнительная цель \+1/ }) as HTMLButtonElement
    const despair = () => screen.getByRole('button', { name: /Отчаяние \+1/ }) as HTMLButtonElement
    expect(despair().disabled).toBe(false)

    fireEvent.click(despair())
    expect(target().disabled).toBe(true)
    expect(target().title).toContain('Не сочетается')

    // Снятие «Отчаяния» снова открывает «Дополнительную цель».
    fireEvent.click(despair())
    expect(target().disabled).toBe(false)
  })
})
