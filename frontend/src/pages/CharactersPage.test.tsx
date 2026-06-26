import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import type { Archetype, Reference } from '../api/types'
import { CreateCharacterForm } from './CharactersPage'

function archetype(over: Partial<Archetype>): Archetype {
  return {
    id: 'a', name: 'A', nameRu: 'А', brawn: 2, agility: 2, intellect: 2, cunning: 2, willpower: 2, presence: 2,
    woundBase: 10, strainBase: 10, startingXp: 100, description: '', safeDescription: '', source: '',
    abilities: [], startingSkills: [], ...over,
  }
}

const reference: Reference = {
  archetypes: [
    archetype({
      id: 'arch-choice', name: 'Average Human', nameRu: 'Обыватель',
      abilities: [{ code: 'c.ability.1', nameRu: 'Готов ко всему', nameEn: '',
        safeDescription: 'Готов ко всему: перемещает очко сюжета.', automationKind: 'manual' }],
      startingSkills: [{ skillName: '', nameRu: '', freeRanks: 1, isChoice: true, choiceGroup: 'any-noncareer', choiceCount: 2 }],
    }),
    archetype({
      id: 'arch-fixed', name: 'Laborer', nameRu: 'Трудяга',
      startingSkills: [{ skillName: 'Athletics', nameRu: 'Атлетика', freeRanks: 1, isChoice: false, choiceGroup: '', choiceCount: 0 }],
    }),
  ],
  careers: [
    { id: 'career-soldier', name: 'Soldier', nameRu: 'Солдат', description: 'desc',
      safeDescription: '', source: '', careerSkillNames: ['Athletics', 'Cool'],
      startingMoneyFixed: 0, startingMoneyDice: '', startingGear: [], rules: [] },
    { id: 'career-warrior', name: 'Warrior', nameRu: 'Воин', description: 'боец',
      safeDescription: '', source: '', careerSkillNames: ['Athletics'],
      startingMoneyFixed: 0, startingMoneyDice: '1d100',
      startingGear: [
        { itemCode: 'leather', itemNameRu: 'кожаная броня', quantity: 1, isChoice: false, choiceGroup: '', choiceOption: 0 },
        { itemCode: 'sword', itemNameRu: 'меч', quantity: 1, isChoice: true, choiceGroup: 'slot-1', choiceOption: 0 },
        { itemCode: 'shield', itemNameRu: 'щит', quantity: 1, isChoice: true, choiceGroup: 'slot-1', choiceOption: 0 },
        { itemCode: 'halberd', itemNameRu: 'алебарда', quantity: 1, isChoice: true, choiceGroup: 'slot-1', choiceOption: 1 },
      ],
      rules: [{ code: 'r1', kind: 'advisory', description: 'Замена Melee на Melee (Light).' }] },
  ],
  skills: [
    { id: 's1', name: 'Athletics', nameRu: 'Атлетика', characteristic: 'brawn', kind: 'general', safeDescription: '', source: '', isCustom: false },
    { id: 's2', name: 'Cool', nameRu: 'Хладнокровие', characteristic: 'presence', kind: 'general', safeDescription: '', source: '', isCustom: false },
    { id: 's3', name: 'Stealth', nameRu: 'Скрытность', characteristic: 'agility', kind: 'general', safeDescription: '', source: '', isCustom: false },
    { id: 's4', name: 'Coordination', nameRu: 'Координация', characteristic: 'agility', kind: 'general', safeDescription: '', source: '', isCustom: false },
  ],
  talents: [], items: [], heroicAbilities: [], qualities: [],
}

const createCharacterMock = vi.fn().mockResolvedValue({ id: 'new-id' })
vi.mock('../api/client', () => ({
  api: {
    reference: () => Promise.resolve(reference),
    createCharacter: (...args: unknown[]) => createCharacterMock(...args),
  },
}))

describe('CreateCharacterForm — стартовые навыки вида (U-12)', () => {
  it('показывает фиксированные стартовые навыки выбранного вида', async () => {
    render(<CreateCharacterForm onCancel={() => {}} onCreated={() => {}} />)
    await waitFor(() => expect(screen.getByRole('option', { name: 'Трудяга' })).toBeTruthy())

    const [archetypeSelect] = screen.getAllByRole('combobox')
    fireEvent.change(archetypeSelect, { target: { value: 'arch-fixed' } })

    expect(screen.getByText(/Стартовые навыки: Атлетика/)).toBeTruthy()
    // у фиксированного вида пикера выбора нет
    expect(screen.queryByText(/выберите 2 разных/)).toBeNull()
  })

  it('требует выбрать N некарьерных навыков и передаёт их при создании', async () => {
    createCharacterMock.mockClear()
    render(<CreateCharacterForm onCancel={() => {}} onCreated={() => {}} />)
    await waitFor(() => expect(screen.getByRole('option', { name: 'Обыватель' })).toBeTruthy())

    fireEvent.change(screen.getByLabelText('Имя персонажа'), { target: { value: 'Герой' } })
    const [archetypeSelect, careerSelect] = screen.getAllByRole('combobox')
    fireEvent.change(archetypeSelect, { target: { value: 'arch-choice' } })
    fireEvent.change(careerSelect, { target: { value: 'career-soldier' } })

    // способность вида показана
    expect(screen.getByText('Готов ко всему')).toBeTruthy()
    // пикер требует 2 навыка, кнопка «Создать» заблокирована
    expect(screen.getByText(/выберите 2 разных некарьерных/)).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Создать' })).toHaveProperty('disabled', true)

    // карьерные навыки (Атлетика/Хладнокровие) не предлагаются как некарьерный выбор
    fireEvent.click(screen.getByRole('button', { name: 'Скрытность' }))
    fireEvent.click(screen.getByRole('button', { name: 'Координация' }))

    const submit = screen.getByRole('button', { name: 'Создать' })
    expect(submit).toHaveProperty('disabled', false)
    fireEvent.click(submit)

    await waitFor(() => expect(createCharacterMock).toHaveBeenCalled())
    const call = createCharacterMock.mock.calls[0]
    expect(call[5]).toEqual([{ choiceGroup: 'any-noncareer', skillNames: ['Stealth', 'Coordination'] }])
  })
})

describe('CreateCharacterForm — стартовое снаряжение карьеры (U-13)', () => {
  it('показывает деньги/снаряжение, гейтит выбор и передаёт его при создании', async () => {
    createCharacterMock.mockClear()
    render(<CreateCharacterForm onCancel={() => {}} onCreated={() => {}} />)
    await waitFor(() => expect(screen.getByRole('option', { name: 'Воин' })).toBeTruthy())

    fireEvent.change(screen.getByLabelText('Имя персонажа'), { target: { value: 'Герой' } })
    const [archetypeSelect, careerSelect] = screen.getAllByRole('combobox')
    fireEvent.change(archetypeSelect, { target: { value: 'arch-fixed' } }) // без выборов навыков
    fireEvent.change(careerSelect, { target: { value: 'career-warrior' } })

    // деньги, фиксированное снаряжение и заметка карьеры
    expect(screen.getByText(/1d100 серебра/)).toBeTruthy()
    expect(screen.getByText(/Снаряжение: кожаная броня/)).toBeTruthy()
    expect(screen.getByText(/Замена Melee/)).toBeTruthy()

    // вариант снаряжения не выбран → «Создать» заблокирована
    expect(screen.getByRole('button', { name: 'Создать' })).toHaveProperty('disabled', true)

    fireEvent.click(screen.getByRole('button', { name: 'алебарда' }))
    const submit = screen.getByRole('button', { name: 'Создать' })
    expect(submit).toHaveProperty('disabled', false)
    fireEvent.click(submit)

    await waitFor(() => expect(createCharacterMock).toHaveBeenCalled())
    expect(createCharacterMock.mock.calls[0][6]).toEqual([{ choiceGroup: 'slot-1', optionIndex: 1 }])
  })
})
