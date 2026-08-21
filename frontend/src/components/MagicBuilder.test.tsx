import { fireEvent, render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { SheetTalent, Spell } from '../api/types'
import { parseDifficulty } from '../utils/labels'
import { api } from '../api/client'
import { MagicBuilder } from './MagicBuilder'

const { openRollerMock } = vi.hoisted(() => ({ openRollerMock: vi.fn() }))

vi.mock('../api/client', () => ({
  api: {
    spells: vi.fn(),
  },
}))

vi.mock('../dice-roller-store', () => ({
  useDiceRoller: () => ({ openRoller: openRollerMock }),
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
    usesKnowledgeRating: false, ratedQualities: [],
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

describe('MagicBuilder — бесплатные эффекты талантов', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('«Пламя Келлоса» делает выбранный Огонь бесплатным', async () => {
    vi.mocked(api.spells).mockResolvedValue(spells)
    const flames = { linkCode: 'flames-of-kellos', needsChoice: false } as unknown as SheetTalent
    render(<MagicBuilder system="realmsOfTerrinoth" talents={[flames]} onError={() => {}} />)
    await screen.findByText(/Сложность: 2/)

    const fire = screen.getByRole('button', { name: /Огонь/ })
    expect(fire.className).toContain('free')
    fireEvent.click(fire)

    // Базовая Атака остаётся со сложностью 2: +2 Огня снят талантом.
    expect(screen.getByText(/Сложность: 2/)).toBeTruthy()
    expect(fire.getAttribute('title')).toContain('Талант делает этот эффект бесплатным')
  })

  it('«Природное единение» добавляет обязательный бесплатный Призыв союзника', async () => {
    const conjureSpells = [
      spell({ id: 'conjure', kind: 'effect', magicSkill: 'Arcana', nameRu: 'Призыв', nameEn: 'Conjure', difficulty: '1 (Easy)' }),
      spell({ id: 'ally', kind: 'additionalEffect', parentEffect: 'Conjure', nameRu: 'Призыв союзника', nameEn: 'Summon Ally', difficulty: '+1' }),
    ]
    vi.mocked(api.spells).mockResolvedValue(conjureSpells)
    const communion = { linkCode: 'natural-communion', needsChoice: false } as unknown as SheetTalent
    render(<MagicBuilder system="realmsOfTerrinoth" talents={[communion]} onError={() => {}} />)

    await screen.findByText(/Сложность: 1/)
    expect(document.querySelector('.effect-summary')?.textContent).toContain('Призыв союзника')
    expect(document.querySelector('.effect-summary')?.textContent).toContain('обязательно')
    expect(screen.getByText(/Сложность: 1/)).toBeTruthy()
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

/**
 * ROT-MAG-10. Рейтинг свойств равен рангам Знания, и там, где правило даёт выбор навыка,
 * выбирает игрок — сборщик показывает получившееся число, а не отсылку «равен рангу Знания».
 */
describe('MagicBuilder — рейтинг по Знанию', () => {
  const rated: Spell[] = [
    spell({ id: 'attack', magicSkill: 'Arcana', nameRu: 'Атака', nameEn: 'Attack', difficulty: '1 (Easy)', allowedSkills: ['Arcana'] }),
    spell({
      id: 'fire', kind: 'additionalEffect', parentEffect: 'Attack', nameRu: 'Огненный', nameEn: 'Fire',
      difficulty: '+1', allowedSkills: ['Arcana'], usesKnowledgeRating: true,
      ratedQualities: [{ code: 'Burn', nameRu: 'Жжение', nameEn: 'Burn' }],
    }),
    spell({
      id: 'poison', kind: 'additionalEffect', parentEffect: 'Attack', nameRu: 'Ядовитый', nameEn: 'Poisonous',
      difficulty: '+2', allowedSkills: ['Arcana'], usesKnowledgeRating: true,
    }),
    spell({
      id: 'range', kind: 'additionalEffect', parentEffect: 'Attack', nameRu: 'Дистанционный', nameEn: 'Range',
      difficulty: '+1', allowedSkills: ['Arcana'],
    }),
  ]

  const lore = { skill: 'Knowledge (Lore)', skillRu: 'Знание (предания)', ranks: 2, reason: 'default' as const }
  const forbidden = { skill: 'Knowledge (Forbidden)', skillRu: 'Знание (запретное)', ranks: 4, reason: 'darkInsight' as const }

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(api.spells).mockResolvedValue(rated)
  })

  it('показывает рейтинг числом: свойству — своё имя, числовому эффекту — «по Знанию»', async () => {
    render(<MagicBuilder system="realmsOfTerrinoth" knowledgeRating={{ options: [lore] }} onError={() => {}} />)
    await screen.findByText(/Сложность: 1/)

    expect(screen.getByRole('button', { name: /Огненный.*Жжение 2/ })).toBeTruthy()
    expect(screen.getByRole('button', { name: /Ядовитый.*по Знанию 2/ })).toBeTruthy()
    // Эффект, не зависящий от Знания, рейтинга не получает.
    expect(screen.getByRole('button', { name: /Дистанционный/ }).textContent).not.toContain('Знанию')
  })

  it('без права выбора навык не предлагается, а просто назван', async () => {
    render(<MagicBuilder system="realmsOfTerrinoth" knowledgeRating={{ options: [lore] }} onError={() => {}} />)
    await screen.findByText(/Сложность: 1/)

    expect(screen.queryByLabelText(/Рейтинг по навыку/)).toBeNull()
    expect(screen.getByText(/Рейтинг свойств: Знание \(предания\) 2/)).toBeTruthy()
  })

  it('когда правило даёт выбор, игрок выбирает навык и числа пересчитываются', async () => {
    render(<MagicBuilder system="realmsOfTerrinoth" knowledgeRating={{ options: [lore, forbidden] }} onError={() => {}} />)
    await screen.findByText(/Сложность: 1/)

    // По умолчанию — навык из правил системы, а не самый выгодный.
    expect(screen.getByRole('button', { name: /Огненный.*Жжение 2/ })).toBeTruthy()

    fireEvent.change(screen.getByLabelText(/Рейтинг по навыку/), { target: { value: 'Knowledge (Forbidden)' } })
    expect(screen.getByRole('button', { name: /Огненный.*Жжение 4/ })).toBeTruthy()
    expect(screen.getByRole('button', { name: /Ядовитый.*по Знанию 4/ })).toBeTruthy()
  })

  it('без листа персонажа сборщик работает и рейтинг не выдумывает', async () => {
    render(<MagicBuilder system="realmsOfTerrinoth" onError={() => {}} />)
    await screen.findByText(/Сложность: 1/)

    expect(screen.queryByText(/Рейтинг свойств/)).toBeNull()
    expect(screen.getByRole('button', { name: /Огненный/ }).textContent).not.toContain('Жжение')
  })
})

describe('MagicBuilder — дайсроллер', () => {
  const rollerSpells: Spell[] = [
    spell({
      id: 'attack', magicSkill: 'Arcana', nameRu: 'Атака', nameEn: 'Attack',
      difficulty: '2 (Average)', allowedSkills: ['Arcana'],
    }),
    spell({
      id: 'range', kind: 'additionalEffect', parentEffect: 'Attack',
      nameRu: 'Дистанционный', nameEn: 'Range', difficulty: '+1',
      allowedSkills: ['Arcana'],
    }),
    spell({
      id: 'empowered', kind: 'additionalEffect', parentEffect: 'Attack',
      nameRu: 'Усиленный', nameEn: 'Empowered', difficulty: '+2',
      allowedSkills: ['Arcana'],
    }),
    spell({
      id: 'holy', kind: 'additionalEffect', parentEffect: 'Attack',
      nameRu: 'Святой/нечестивый', nameEn: 'Holy/Unholy', difficulty: '+1',
      allowedSkills: ['Arcana'],
    }),
  ]

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(api.spells).mockResolvedValue(rollerSpells)
  })

  it('открывает общий роллер с пулом навыка, итоговой сложностью и модификаторами', async () => {
    render(<MagicBuilder system="realmsOfTerrinoth" onError={() => {}}
      characterSkills={[{
        name: 'Arcana',
        characteristic: 'intellect',
        characteristicValue: 3,
        pool: { ability: 1, proficiency: 2 },
        ranks: 2,
        isCareer: true,
        setbackDice: 1,
        boostDice: 2,
      }]}
      implements={[{
        itemId: 'scepter',
        name: 'Скипетр',
        implement: {
          code: 'scepter',
          attackDamageBonus: 0,
          boostDice: 1,
          requiredMagicSkill: '',
          discount: 'none',
          discountEffects: [],
          choiceCount: 0,
          choiceMaxIncreaseSum: null,
          choiceExactIncrease: null,
          material: 'oak',
          chosenEffects: [],
          pending: false,
          damageSetbackDice: 1,
          damageDifficultyIncrease: 0,
        },
      }]} />)
    await screen.findByText(/Сложность: 2/)

    fireEvent.change(screen.getByLabelText('Инструмент'), { target: { value: 'scepter' } })
    fireEvent.click(screen.getByRole('button', { name: /Дистанционный \+1/ }))
    fireEvent.click(screen.getByRole('button', { name: '🎲 Бросить' }))

    expect(openRollerMock).toHaveBeenCalledWith({
      kind: 'magic',
      title: 'Магическая проверка',
      label: 'Тайная (Arcana) · Атака',
      skillLabel: 'Тайная (Arcana) (3)',
      basePool: {
        ability: 1,
        proficiency: 2,
        difficulty: 3,
        boost: 3,
        setback: 2,
      },
      damage: {
        base: 3,
        characteristic: 3,
        characteristicMultiplier: 1,
        implementBonus: 0,
        successMultiplier: 1,
      },
      advantageSpends: [],
    })
  })

  it('не предлагает бросок для Runes без выбранного runebound shard', async () => {
    vi.mocked(api.spells).mockResolvedValue(spells)
    render(<MagicBuilder system="realmsOfTerrinoth" onError={() => {}} characterSkills={[{
      name: 'Runes',
      characteristic: 'intellect',
      characteristicValue: 3,
      pool: { ability: 2, proficiency: 1 },
      ranks: 1,
      isCareer: true,
      setbackDice: 0,
      boostDice: 0,
    }]} />)

    await screen.findByText(/Сборка недействительна/)
    expect(screen.queryByRole('button', { name: '🎲 Бросить' })).toBeNull()
  })

  it('передаёт удвоенную характеристику, бонус инструмента и условный урон Holy/Unholy', async () => {
    render(<MagicBuilder system="realmsOfTerrinoth" onError={() => {}}
      characterSkills={[{
        name: 'Arcana',
        characteristic: 'intellect',
        characteristicValue: 3,
        pool: { ability: 2, proficiency: 1 },
        ranks: 1,
        isCareer: true,
        setbackDice: 0,
        boostDice: 0,
      }]}
      implements={[{
        itemId: 'staff',
        name: 'Посох',
        implement: {
          code: 'staff',
          attackDamageBonus: 4,
          boostDice: 0,
          requiredMagicSkill: '',
          discount: 'none',
          discountEffects: [],
          choiceCount: 0,
          choiceMaxIncreaseSum: null,
          choiceExactIncrease: null,
          material: 'oak',
          chosenEffects: [],
          pending: false,
          damageSetbackDice: 0,
          damageDifficultyIncrease: 0,
        },
      }]} />)
    await screen.findByText(/Сложность: 2/)

    fireEvent.change(screen.getByLabelText('Инструмент'), { target: { value: 'staff' } })
    fireEvent.click(screen.getByRole('button', { name: /Усиленный \+2/ }))
    fireEvent.click(screen.getByRole('button', { name: /Святой\/нечестивый \+1/ }))
    fireEvent.click(screen.getByRole('button', { name: '🎲 Бросить' }))

    expect(openRollerMock).toHaveBeenLastCalledWith(expect.objectContaining({
      kind: 'magic',
      damage: {
        base: 10,
        characteristic: 3,
        characteristicMultiplier: 2,
        implementBonus: 4,
        successMultiplier: 1,
        conditionalSuccessMultiplier: 2,
        conditionalLabelRu: 'Если цель — враг веры или божества',
        conditionalLabelEn: 'If the target is an enemy of the faith or deity',
      },
    }))
  })
})
