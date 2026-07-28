import { render, screen, waitFor, fireEvent, within } from '@testing-library/react'
import { describe, expect, it, vi, beforeEach } from 'vitest'
import type { CharacterSheet, ItemImplement, Spell } from '../api/types'
import { MagicTab } from './MagicTab'
import { implementPrice, implementRarity } from '../utils/implements'

const spellsMock = vi.fn()
const configureMock = vi.fn()
vi.mock('../api/client', () => ({
  api: {
    spells: (...a: unknown[]) => spellsMock(...a),
    setImplementConfiguration: (...a: unknown[]) => configureMock(...a),
  },
}))

const spell = (over: Partial<Spell>): Spell => ({
  id: Math.random().toString(36), magicSkill: '', kind: 'effect', parentEffect: '',
  nameRu: '', nameEn: '', difficulty: '', description: '', safeDescription: '',
  source: '', isCustom: false, restrictedSkill: '', ...over,
})

const SPELLS: Spell[] = [
  spell({ id: 'attack', magicSkill: 'Arcana', kind: 'effect', nameRu: 'Атака', nameEn: 'Attack', difficulty: '1 (Easy)' }),
  spell({ id: 'barrier', magicSkill: 'Divine', kind: 'effect', nameRu: 'Барьер', nameEn: 'Barrier', difficulty: '1 (Easy)' }),
  spell({ id: 'range', kind: 'additionalEffect', parentEffect: 'Attack', nameRu: 'Дистанционный', nameEn: 'Range', difficulty: '+1' }),
  spell({ id: 'close', kind: 'additionalEffect', parentEffect: 'Attack', nameRu: 'Ближний бой', nameEn: 'Close Combat', difficulty: '+1' }),
  spell({ id: 'sanctuary', kind: 'additionalEffect', parentEffect: 'Barrier', nameRu: 'Святилище', nameEn: 'Sanctuary', difficulty: '+2', restrictedSkill: 'Divine' }),
]

const implement = (over: Partial<ItemImplement>): ItemImplement => ({
  code: 'magic-staff', material: 'oak', attackDamageBonus: 4, boostDice: 0,
  requiredMagicSkill: '', discount: 'firstNamedEffect', discountEffects: ['Range'],
  choiceCount: 0, choiceMaxIncreaseSum: null, choiceExactIncrease: null,
  chosenEffects: [], pending: false, ...over,
})

const sheetWith = (impl: ItemImplement | null, over: Record<string, unknown> = {}) => ({
  id: 'char-1', system: 'realmsOfTerrinoth',
  skills: [{ name: 'Arcana', kind: 'magic', pool: { ability: 1, proficiency: 1 } }],
  items: impl
    ? [{
      id: 'item-1', nameRu: 'Магический посох', name: 'Magic Staff', state: 'equipped',
      isUsable: true, implement: impl, ...over,
    }]
    : [],
} as unknown as CharacterSheet)

describe('Магические инструменты в сборщике (ROT-MAG-IMP-01)', () => {
  beforeEach(() => {
    spellsMock.mockReset(); spellsMock.mockResolvedValue(SPELLS)
    configureMock.mockReset(); configureMock.mockResolvedValue(undefined)
  })

  const difficulty = () => document.querySelector('.difficulty-badge')?.textContent ?? ''

  it('посох делает первую Дистанцию бесплатной и объясняет скидку', async () => {
    render(<MagicTab sheet={sheetWith(implement({}))} onError={() => {}} />)
    await screen.findByText(/Сборка магического действия/)

    fireEvent.change(screen.getByLabelText(/Инструмент/), { target: { value: 'item-1' } })
    fireEvent.click(screen.getByRole('button', { name: /Дистанционный/ }))

    // Базовая 1 + Дистанция 1 = 2, но посох снимает первую Дистанцию.
    await waitFor(() => expect(difficulty()).toContain('1'))
    expect(document.body.textContent).toContain('Range −1')
  })

  it('без инструмента сложность считается полной суммой', async () => {
    render(<MagicTab sheet={sheetWith(implement({}))} onError={() => {}} />)
    await screen.findByText(/Сборка магического действия/)

    fireEvent.click(screen.getByRole('button', { name: /Дистанционный/ }))

    await waitFor(() => expect(difficulty()).toContain('2'))
  })

  it('инструмент чужого направления не работает и говорит об этом', async () => {
    const verseOnly = implement({
      code: 'musical-instrument', requiredMagicSkill: 'Verse', discount: 'namedEffects',
      discountEffects: ['Range'], attackDamageBonus: 0,
    })
    render(<MagicTab sheet={sheetWith(verseOnly)} onError={() => {}} />)
    await screen.findByText(/Сборка магического действия/)

    fireEvent.change(screen.getByLabelText(/Инструмент/), { target: { value: 'item-1' } })
    fireEvent.click(screen.getByRole('button', { name: /Дистанционный/ }))

    await waitFor(() => expect(difficulty()).toContain('2'))
    expect(document.body.textContent).toContain('Работает только с направлением')
  })

  it('ненастроенный фолиант не даёт скидки и просит выбор ведущего', async () => {
    const tome = implement({
      code: 'magic-tome', discount: 'chosenEffects', discountEffects: [], attackDamageBonus: 0,
      choiceCount: 2, choiceMaxIncreaseSum: 3, chosenEffects: [], pending: true,
    })
    render(<MagicTab sheet={sheetWith(tome)} onError={() => {}}
      refresh={() => Promise.resolve()} />)
    await screen.findByText(/Сборка магического действия/)

    fireEvent.change(screen.getByLabelText(/Инструмент/), { target: { value: 'item-1' } })
    // Чипов «Дистанционный» на экране двое, и это разные вещи: выбор эффекта для текущего
    // заклинания и выбор эффекта, который ведущий закрепляет за фолиантом навсегда.
    const inEffects = () => within(document.querySelector('.effect-chips') as HTMLElement)
    const inConfig = () => within(document.querySelector('.implement-config') as HTMLElement)

    fireEvent.click(inEffects().getByRole('button', { name: /Дистанционный/ }))
    await waitFor(() => expect(difficulty()).toContain('2'))
    expect(document.body.textContent).toContain('Не настроен')

    // Выбор ведущего уходит на сервер — он же проверяет бюджет надбавок.
    fireEvent.click(inConfig().getByRole('button', { name: /Дистанционный/ }))
    fireEvent.click(screen.getByRole('button', { name: 'Настроить' }))
    await waitFor(() => expect(configureMock).toHaveBeenCalledWith('char-1', 'item-1', ['Range']))
  })

  it('инструмент не в руках сборщику не предлагается', async () => {
    render(<MagicTab sheet={sheetWith(implement({}), { state: 'backpack' })} onError={() => {}} />)
    await screen.findByText(/Сборка магического действия/)

    expect(screen.queryByLabelText(/Инструмент/)).toBeNull()
  })

  it('цена и редкость материала считаются той же формулой, что на сервере', () => {
    // Полтора по официальной errata — не «вдвое дешевле».
    expect(implementPrice(400, 'bone')).toBe(600)
    expect(implementPrice(400, 'willow')).toBe(800)
    expect(implementPrice(400, 'oak')).toBe(400)
    expect(implementRarity(6, 'bone')).toBe(8)
    expect(implementRarity(9, 'willow')).toBe(10)
  })
})
