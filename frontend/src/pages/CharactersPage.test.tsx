import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import type { Archetype, ArchetypeAbility, Reference } from '../api/types'
import { CreateCharacterForm } from './CharactersPage'

function ability(over: Partial<ArchetypeAbility>): ArchetypeAbility {
  return {
    code: 'a', nameRu: '', nameEn: '', safeDescription: '', automationKind: 'manual',
    ruleKind: 'manual', ruleValue: 0, ruleParameters: '', usesPerScope: 0, useScope: 'none',
    storyPointCost: 0, choiceOptions: null, ...over,
  }
}

function archetype(over: Partial<Archetype>): Archetype {
  return {
    id: 'a', name: 'A', nameRu: 'А', brawn: 2, agility: 2, intellect: 2, cunning: 2, willpower: 2, presence: 2,
    woundBase: 10, strainBase: 10, startingXp: 100, description: '', safeDescription: '', source: '',
    isCustom: false, abilities: [], startingSkills: [], silhouette: 1, ...over,
  }
}

const reference: Reference = {
  archetypes: [
    archetype({
      id: 'arch-choice', name: 'Average Human', nameRu: 'Обыватель',
      abilities: [ability({ code: 'c.ability.1', nameRu: 'Готов ко всему',
        safeDescription: 'Готов ко всему: перемещает очко сюжета.' })],
      startingSkills: [{ skillName: '', nameRu: '', freeRanks: 1, isChoice: true, choiceGroup: 'any-noncareer', choiceCount: 2, grantsCareerSkill: false }],
    }),
    archetype({
      id: 'arch-fixed', name: 'Laborer', nameRu: 'Трудяга',
      startingSkills: [{ skillName: 'Athletics', nameRu: 'Атлетика', freeRanks: 1, isChoice: false, choiceGroup: '', choiceCount: 0, grantsCareerSkill: false }],
    }),
    // Аналог Deep Elf: вид даёт 2 бесплатных ранга и делает навык карьерным (ROT-CRE-01).
    archetype({
      id: 'arch-grantor', name: 'Deep Elf', nameRu: 'Тёмный эльф',
      startingSkills: [
        { skillName: 'Stealth', nameRu: 'Скрытность', freeRanks: 2, isChoice: false, choiceGroup: '', choiceCount: 0, grantsCareerSkill: true },
        { skillName: 'Coordination', nameRu: 'Координация', freeRanks: 1, isChoice: false, choiceGroup: '', choiceCount: 0, grantsCareerSkill: false },
      ],
    }),
    // Аналог Catfolk/Half-Catfolk: вид требует обязательного выбора одной из двух способностей.
    archetype({
      id: 'arch-catfolk', name: 'Catfolk', nameRu: 'Котолюд',
      abilities: [
        ability({ code: 'cat.claws', nameRu: 'Когти', ruleKind: 'naturalWeapon' }),
        ability({ code: 'cat.fleet', nameRu: 'Быстрые лапы', ruleKind: 'freeSecondMoveManeuver' }),
      ],
    }),
    archetype({
      id: 'arch-half-catfolk', name: 'Half-Catfolk', nameRu: 'Полукотолюд',
      abilities: [ability({
        code: 'half.choice', nameRu: 'Кошачья кровь', ruleKind: 'chooseOneAbility',
        choiceOptions: ['cat.claws', 'cat.fleet'],
      })],
    }),
    // Аналог Highborn Elf: один бесплатный ранг, второй можно добрать карьерным выбором.
    archetype({
      id: 'arch-grantor-1', name: 'Highborn Elf', nameRu: 'Высокий эльф',
      startingSkills: [
        { skillName: 'Stealth', nameRu: 'Скрытность', freeRanks: 1, isChoice: false, choiceGroup: '', choiceCount: 0, grantsCareerSkill: true },
      ],
    }),
  ],
  careers: [
    { id: 'career-soldier', name: 'Soldier', nameRu: 'Солдат', description: 'desc',
      safeDescription: '', source: '', isCustom: false, careerSkillNames: ['Athletics', 'Cool'],
      startingMoneyFixed: 0, startingMoneyDice: '', startingGear: [], rules: [] },
    { id: 'career-warrior', name: 'Warrior', nameRu: 'Воин', description: 'боец',
      safeDescription: '', source: '', isCustom: false, careerSkillNames: ['Athletics'],
      startingMoneyFixed: 0, startingMoneyDice: '1d100',
      startingGear: [
        { itemCode: 'leather', itemNameRu: 'кожаная броня', quantity: 1, isChoice: false, choiceGroup: '', choiceOption: 0 },
        { itemCode: 'sword', itemNameRu: 'меч', quantity: 1, isChoice: true, choiceGroup: 'slot-1', choiceOption: 0 },
        { itemCode: 'shield', itemNameRu: 'щит', quantity: 1, isChoice: true, choiceGroup: 'slot-1', choiceOption: 0 },
        { itemCode: 'halberd', itemNameRu: 'алебарда', quantity: 1, isChoice: true, choiceGroup: 'slot-1', choiceOption: 1 },
      ],
      rules: [{ code: 'r1', kind: 'advisory', description: 'Замена Melee на Melee (Light).' }] },
    { id: 'career-generalist', name: 'Generalist', nameRu: 'Универсал', description: 'универсал',
      safeDescription: '', source: '', isCustom: false,
      careerSkillNames: ['Athletics', 'Cool', 'Stealth', 'Coordination'],
      startingMoneyFixed: 0, startingMoneyDice: '', startingGear: [], rules: [] },
  ],
  skills: [
    { id: 's1', name: 'Athletics', nameRu: 'Атлетика', characteristic: 'brawn', kind: 'general', safeDescription: '', source: '', isCustom: false },
    { id: 's2', name: 'Cool', nameRu: 'Хладнокровие', characteristic: 'presence', kind: 'general', safeDescription: '', source: '', isCustom: false },
    { id: 's3', name: 'Stealth', nameRu: 'Скрытность', characteristic: 'agility', kind: 'general', safeDescription: '', source: '', isCustom: false },
    { id: 's4', name: 'Coordination', nameRu: 'Координация', characteristic: 'agility', kind: 'general', safeDescription: '', source: '', isCustom: false },
  ],
  talents: [], items: [], heroicAbilities: [], heroicSecondaryEffects: [], qualities: [], attachments: [],
  mounts: [],
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

    // карьерные навыки (Атлетика/Хладнокровие) не предлагаются как некарьерный выбор;
    // чипы показывают RU/ENG подпись
    fireEvent.click(screen.getByRole('button', { name: 'Скрытность / Stealth' }))
    fireEvent.click(screen.getByRole('button', { name: 'Координация / Coordination' }))

    const submit = screen.getByRole('button', { name: 'Создать' })
    expect(submit).toHaveProperty('disabled', false)
    fireEvent.click(submit)

    await waitFor(() => expect(createCharacterMock).toHaveBeenCalled())
    const call = createCharacterMock.mock.calls[0]
    expect(call[5]).toEqual([{ choiceGroup: 'any-noncareer', skillNames: ['Stealth', 'Coordination'] }])
  })
})

describe('CreateCharacterForm — видовые карьерные навыки (ROT-CRE-01)', () => {
  it('добавляет видовую выдачу в список карьерных навыков и блокирует её при ранге 2', async () => {
    render(<CreateCharacterForm onCancel={() => {}} onCreated={() => {}} />)
    await waitFor(() => expect(screen.getByRole('option', { name: 'Тёмный эльф' })).toBeTruthy())

    const [archetypeSelect, careerSelect] = screen.getAllByRole('combobox')
    fireEvent.change(archetypeSelect, { target: { value: 'arch-grantor' } })
    fireEvent.change(careerSelect, { target: { value: 'career-soldier' } })

    // Stealth не входит в careerSkillNames карьеры, но вид сделал его карьерным.
    const stealth = screen.getByRole('button', { name: /Скрытность/ })
    expect(stealth).toHaveProperty('disabled', true) // вид уже дал ранг 2
    expect(screen.getByText(/уже ранг 2, выбрать нельзя/)).toBeTruthy()

    // Навык вида без grantsCareerSkill карьерным не становится.
    expect(screen.queryByRole('button', { name: /Координация/ })).toBeNull()
  })

  it('позволяет отметить видовой карьерный навык, если у него только 1 ранг', async () => {
    createCharacterMock.mockClear()
    render(<CreateCharacterForm onCancel={() => {}} onCreated={() => {}} />)
    await waitFor(() => expect(screen.getByRole('option', { name: 'Высокий эльф' })).toBeTruthy())

    fireEvent.change(screen.getByLabelText('Имя персонажа'), { target: { value: 'Эльф' } })
    const [archetypeSelect, careerSelect] = screen.getAllByRole('combobox')
    fireEvent.change(archetypeSelect, { target: { value: 'arch-grantor-1' } })
    fireEvent.change(careerSelect, { target: { value: 'career-soldier' } })

    const stealth = screen.getByRole('button', { name: /Скрытность/ })
    expect(stealth).toHaveProperty('disabled', false)
    fireEvent.click(stealth)
    fireEvent.click(screen.getByRole('button', { name: 'Создать' }))

    await waitFor(() => expect(createCharacterMock).toHaveBeenCalled())
    expect(createCharacterMock.mock.calls[0][4]).toEqual(['Stealth'])
  })
})

describe('CreateCharacterForm — обязательный видовой выбор (ROT-SPECIES-01)', () => {
  it('блокирует создание, пока выбор не сделан, и передаёт выбранный код', async () => {
    createCharacterMock.mockClear()
    render(<CreateCharacterForm onCancel={() => {}} onCreated={() => {}} />)
    await waitFor(() => expect(screen.getByRole('option', { name: 'Полукотолюд' })).toBeTruthy())

    fireEvent.change(screen.getByLabelText('Имя персонажа'), { target: { value: 'Полукот' } })
    const [archetypeSelect, careerSelect] = screen.getAllByRole('combobox')
    fireEvent.change(archetypeSelect, { target: { value: 'arch-half-catfolk' } })
    fireEvent.change(careerSelect, { target: { value: 'career-soldier' } })

    expect(screen.getByText(/выберите одну способность/)).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Создать' })).toHaveProperty('disabled', true)

    fireEvent.click(screen.getByRole('button', { name: 'Быстрые лапы' }))
    const submit = screen.getByRole('button', { name: 'Создать' })
    expect(submit).toHaveProperty('disabled', false)
    fireEvent.click(submit)

    await waitFor(() => expect(createCharacterMock).toHaveBeenCalled())
    expect(createCharacterMock.mock.calls[0][9]).toBe('cat.fleet')
  })

  it('у вида без обязательного выбора пикер не показывается', async () => {
    render(<CreateCharacterForm onCancel={() => {}} onCreated={() => {}} />)
    await waitFor(() => expect(screen.getByRole('option', { name: 'Котолюд' })).toBeTruthy())

    const [archetypeSelect] = screen.getAllByRole('combobox')
    fireEvent.change(archetypeSelect, { target: { value: 'arch-catfolk' } })

    expect(screen.queryByText(/выберите одну способность/)).toBeNull()
  })

  it('смена вида сбрасывает сделанный выбор', async () => {
    render(<CreateCharacterForm onCancel={() => {}} onCreated={() => {}} />)
    await waitFor(() => expect(screen.getByRole('option', { name: 'Полукотолюд' })).toBeTruthy())

    const [archetypeSelect] = screen.getAllByRole('combobox')
    fireEvent.change(archetypeSelect, { target: { value: 'arch-half-catfolk' } })
    fireEvent.click(screen.getByRole('button', { name: 'Когти' }))
    fireEvent.change(archetypeSelect, { target: { value: 'arch-catfolk' } })
    fireEvent.change(archetypeSelect, { target: { value: 'arch-half-catfolk' } })

    expect(screen.getByRole('button', { name: 'Когти' }).className).not.toContain('active')
  })
})

describe('CreateCharacterForm — стартовое снаряжение карьеры (U-13, ROT-CRE-03)', () => {
  /** Выбирает Трудягу + Воина: у первого нет выборов навыков, у второго есть слот снаряжения. */
  function pickWarrior() {
    fireEvent.change(screen.getByLabelText('Имя персонажа'), { target: { value: 'Герой' } })
    const [archetypeSelect, careerSelect] = screen.getAllByRole('combobox')
    fireEvent.change(archetypeSelect, { target: { value: 'arch-fixed' } })
    fireEvent.change(careerSelect, { target: { value: 'career-warrior' } })
  }

  it('по умолчанию использует стандартные деньги: комплект не выдаётся и выбор не гейтит', async () => {
    createCharacterMock.mockClear()
    render(<CreateCharacterForm onCancel={() => {}} onCreated={() => {}} />)
    await waitFor(() => expect(screen.getByRole('option', { name: 'Воин' })).toBeTruthy())
    pickWarrior()

    expect(screen.getByText(/Бюджет 500 серебра/)).toBeTruthy()
    expect(screen.queryByRole('button', { name: 'алебарда' })).toBeNull() // слоты комплекта скрыты
    expect(screen.getByText(/Замена Melee/)).toBeTruthy()

    const submit = screen.getByRole('button', { name: 'Создать' })
    expect(submit).toHaveProperty('disabled', false) // выбор снаряжения не требуется
    fireEvent.click(submit)

    await waitFor(() => expect(createCharacterMock).toHaveBeenCalled())
    expect(createCharacterMock.mock.calls[0][6]).toEqual([])          // package choices не отправляются
    expect(createCharacterMock.mock.calls[0][8]).toBe('standardMoney')
  })

  it('в режиме комплекта показывает деньги/снаряжение, гейтит выбор и передаёт его', async () => {
    createCharacterMock.mockClear()
    render(<CreateCharacterForm onCancel={() => {}} onCreated={() => {}} />)
    await waitFor(() => expect(screen.getByRole('option', { name: 'Воин' })).toBeTruthy())
    pickWarrior()

    fireEvent.click(screen.getByRole('button', { name: /Карьерный комплект/ }))

    expect(screen.getByText(/1d100 серебра/)).toBeTruthy()
    expect(screen.getByText(/Всегда входит: кожаная броня/)).toBeTruthy()

    // вариант снаряжения не выбран → «Создать» заблокирована
    expect(screen.getByRole('button', { name: 'Создать' })).toHaveProperty('disabled', true)

    fireEvent.click(screen.getByRole('button', { name: 'алебарда' }))
    const submit = screen.getByRole('button', { name: 'Создать' })
    expect(submit).toHaveProperty('disabled', false)
    fireEvent.click(submit)

    await waitFor(() => expect(createCharacterMock).toHaveBeenCalled())
    expect(createCharacterMock.mock.calls[0][6]).toEqual([{ choiceGroup: 'slot-1', optionIndex: 1 }])
    expect(createCharacterMock.mock.calls[0][8]).toBe('careerPackage')
  })

  it('смена карьеры сбрасывает режим и устаревшие выборы', async () => {
    render(<CreateCharacterForm onCancel={() => {}} onCreated={() => {}} />)
    await waitFor(() => expect(screen.getByRole('option', { name: 'Воин' })).toBeTruthy())
    pickWarrior()

    fireEvent.click(screen.getByRole('button', { name: /Карьерный комплект/ }))
    fireEvent.click(screen.getByRole('button', { name: 'алебарда' }))

    const [, careerSelect] = screen.getAllByRole('combobox')
    fireEvent.change(careerSelect, { target: { value: 'career-soldier' } })
    fireEvent.change(careerSelect, { target: { value: 'career-warrior' } })

    expect(screen.getByText(/Бюджет 500 серебра/)).toBeTruthy()      // режим сброшен
    expect(screen.queryByRole('button', { name: 'алебарда' })).toBeNull()
  })

  it('смена карьеры очищает бесплатные навыки и не мешает выбрать четыре навыка новой карьеры', async () => {
    render(<CreateCharacterForm onCancel={() => {}} onCreated={() => {}} />)
    await waitFor(() => expect(screen.getByRole('option', { name: 'Универсал' })).toBeTruthy())

    const [archetypeSelect, careerSelect] = screen.getAllByRole('combobox')
    fireEvent.change(archetypeSelect, { target: { value: 'arch-fixed' } })
    fireEvent.change(careerSelect, { target: { value: 'career-soldier' } })
    fireEvent.click(screen.getByRole('button', { name: /Атлетика/ }))
    fireEvent.click(screen.getByRole('button', { name: /Хладнокровие/ }))

    fireEvent.change(careerSelect, { target: { value: 'career-generalist' } })
    for (const name of ['Атлетика', 'Хладнокровие', 'Скрытность', 'Координация']) {
      expect(screen.getByRole('button', { name: new RegExp(name) }).className).not.toContain('active')
      fireEvent.click(screen.getByRole('button', { name: new RegExp(name) }))
    }

    expect(screen.getByText(/отметьте до 4.*\(4\/4\)/)).toBeTruthy()
    expect(screen.getByRole('button', { name: /Атлетика/ }).className).toContain('active')
  })
})
