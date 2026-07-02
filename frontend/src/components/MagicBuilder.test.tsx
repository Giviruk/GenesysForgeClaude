import { fireEvent, render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { Spell } from '../api/types'
import { api } from '../api/client'
import { MagicBuilder } from './MagicBuilder'

vi.mock('../api/client', () => ({
  api: {
    spells: vi.fn(),
  },
}))

const spell = (over: Partial<Spell>): Spell => ({
  id: 'spell',
  magicSkill: '',
  kind: 'effect',
  parentEffect: '',
  nameRu: '',
  nameEn: '',
  difficulty: '',
  description: '',
  safeDescription: '',
  source: 'Test',
  isCustom: false,
  ...over,
})

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
