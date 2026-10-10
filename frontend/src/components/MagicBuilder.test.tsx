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
    fireEvent.click(screen.getByRole('checkbox', { name: /Дальность/ }))
    fireEvent.click(screen.getByRole('checkbox', { name: /Огонь/ }))
    expect(screen.getByText(/Сложность: 5/)).toBeTruthy()
    expect(screen.getByText(/Достигнут потолок сложности 5/)).toBeTruthy()

    // Оставшийся «+2» превысил бы потолок — chip недоступен и клики игнорируются.
    const ice = screen.getByRole('checkbox', { name: /Лёд/ }) as HTMLButtonElement
    expect(ice.disabled).toBe(true)
    expect(ice.closest('.magic-effect-row')?.textContent).toContain('превысит потолок сложности 5')
    fireEvent.click(ice)
    expect(screen.getByText(/Сложность: 5/)).toBeTruthy()
  })

  it('после снятия эффекта заблокированный chip снова доступен', async () => {
    render(<MagicBuilder system="realmsOfTerrinoth" onError={() => {}} />)
    await screen.findByText(/Сложность: 2/)

    fireEvent.click(screen.getByRole('checkbox', { name: /Дальность/ }))
    fireEvent.click(screen.getByRole('checkbox', { name: /Огонь/ }))
    expect((screen.getByRole('checkbox', { name: /Лёд/ }) as HTMLButtonElement).disabled).toBe(true)

    // Снимаем «Огонь» через chip — итог 3, «Лёд +2» снова доступен.
    fireEvent.click(screen.getByRole('checkbox', { name: /Огонь/ }))
    expect(screen.getByText(/Сложность: 3/)).toBeTruthy()
    expect((screen.getByRole('checkbox', { name: /Лёд/ }) as HTMLButtonElement).disabled).toBe(false)
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

    const fire = screen.getByRole('checkbox', { name: /Огонь/ })
    expect(fire.closest('.magic-effect-row')?.textContent).toContain('бесплатно')
    fireEvent.click(fire)

    // Базовая Атака остаётся со сложностью 2: +2 Огня снят талантом.
    expect(screen.getByText(/Сложность: 2/)).toBeTruthy()
    expect(fire.closest('.magic-effect-row')?.textContent).toContain('Талант делает этот эффект бесплатным')
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
    const doom = screen.getByRole('checkbox', { name: /Рок/ }) as HTMLButtonElement
    expect(doom.disabled).toBe(false)
    const despair = screen.getByRole('checkbox', { name: /^Отчаяние/ }) as HTMLButtonElement
    expect(despair.disabled).toBe(true)
    expect(despair.closest('.magic-effect-row')?.textContent).toContain('только для: Божественная (Divine)')

    fireEvent.click(despair)
    expect(screen.getByText(/Сложность: 2/)).toBeTruthy() // клик по заблокированному ничего не меняет
  })

  it('при смене направления недоступный эффект перестаёт считаться в сложности', async () => {
    render(<MagicBuilder system="realmsOfTerrinoth" onError={() => {}} />)
    await screen.findByText(/Сложность: 2/)

    fireEvent.click(screen.getByRole('checkbox', { name: /Рок/ }))
    expect(screen.getByText(/Сложность: 4/)).toBeTruthy()

    // Жрецу «Рок» недоступен — он и в сложность больше не входит.
    fireEvent.click(screen.getByRole('button', { name: /Божественная/ }))
    expect(screen.getByText(/Сложность: 2/)).toBeTruthy()
    expect((screen.getByRole('checkbox', { name: /Рок/ }) as HTMLButtonElement).disabled).toBe(true)
  })

  it('несочетаемый эффект блокируется, пока выбран его антагонист', async () => {
    render(<MagicBuilder system="realmsOfTerrinoth" onError={() => {}} />)
    await screen.findByText(/Сложность: 2/)
    fireEvent.click(screen.getByRole('button', { name: /Божественная/ }))

    // По надбавке в имени, чтобы не поймать крестик «Убрать эффект» у выбранного чипа.
    const target = () => screen.getByRole('checkbox', { name: /^Дополнительная цель/ }) as HTMLButtonElement
    const despair = () => screen.getByRole('checkbox', { name: /^Отчаяние/ }) as HTMLButtonElement
    expect(despair().disabled).toBe(false)

    fireEvent.click(despair())
    expect(target().disabled).toBe(true)
    expect(target().closest('.magic-effect-row')?.textContent).toContain('не сочетается')

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

    expect(screen.getByRole('checkbox', { name: /Огненный/ }).closest('.magic-effect-row')?.textContent).toContain('Жжение 2')
    expect(screen.getByRole('checkbox', { name: /Ядовитый/ }).closest('.magic-effect-row')?.textContent).toContain('по Знанию 2')
    // Эффект, не зависящий от Знания, рейтинга не получает.
    expect(screen.getByRole('checkbox', { name: /Дистанционный/ }).closest('.magic-effect-row')?.textContent).not.toContain('Знанию')
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
    expect(screen.getByRole('checkbox', { name: /Огненный/ }).closest('.magic-effect-row')?.textContent).toContain('Жжение 2')

    fireEvent.change(screen.getByLabelText(/Рейтинг по навыку/), { target: { value: 'Knowledge (Forbidden)' } })
    expect(screen.getByRole('checkbox', { name: /Огненный/ }).closest('.magic-effect-row')?.textContent).toContain('Жжение 4')
    expect(screen.getByRole('checkbox', { name: /Ядовитый/ }).closest('.magic-effect-row')?.textContent).toContain('по Знанию 4')
  })

  it('помещает пояснение о Тёмном прозрении в центр относительно выбора навыка', async () => {
    render(<MagicBuilder system="realmsOfTerrinoth" knowledgeRating={{ options: [lore, forbidden] }} onError={() => {}} />)
    await screen.findByText(/Сложность: 1/)

    const rating = document.querySelector('.magic-rating')
    const note = screen.getByText(/Тёмное прозрение/)
    expect(rating?.className).toContain('has-selector')
    expect(note.className).toContain('magic-rating-note')
  })

  it('без листа персонажа сборщик работает и рейтинг не выдумывает', async () => {
    render(<MagicBuilder system="realmsOfTerrinoth" onError={() => {}} />)
    await screen.findByText(/Сложность: 1/)

    expect(screen.queryByText(/Рейтинг свойств/)).toBeNull()
    expect(screen.getByRole('checkbox', { name: /Огненный/ }).closest('.magic-effect-row')?.textContent).not.toContain('Жжение')
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

    fireEvent.click(screen.getByRole('button', { name: /Скипетр/ }))
    fireEvent.click(screen.getByRole('checkbox', { name: /Дистанционный/ }))
    fireEvent.click(screen.getByRole('button', { name: 'Бросить' }))

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
      difficultyUpgrades: 0,
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

  it('усиление сложности от травмы передаёт роллеру отдельно, а не красной костью поверх сложности', async () => {
    render(<MagicBuilder system="realmsOfTerrinoth" onError={() => {}}
      characterSkills={[{
        name: 'Arcana',
        characteristic: 'intellect',
        characteristicValue: 3,
        pool: { ability: 1, proficiency: 2 },
        ranks: 2,
        isCareer: true,
        setbackDice: 0,
        boostDice: 0,
        difficultyUpgrades: 1,
      }]} />)
    await screen.findByText(/Сложность: 2/)

    fireEvent.click(screen.getByRole('button', { name: 'Бросить' }))

    const request = openRollerMock.mock.calls.at(-1)![0]
    expect(request.basePool).toMatchObject({ difficulty: 2 })
    expect(request.basePool.challenge ?? 0).toBe(0)
    expect(request.difficultyUpgrades).toBe(1)
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
    expect(screen.queryByRole('button', { name: 'Бросить' })).toBeNull()
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

    fireEvent.click(screen.getByRole('button', { name: /Посох/ }))
    fireEvent.click(screen.getByRole('checkbox', { name: /Усиленный/ }))
    fireEvent.click(screen.getByRole('checkbox', { name: /Святой\/нечестивый/ }))
    fireEvent.click(screen.getByRole('button', { name: 'Бросить' }))

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

describe('MagicBuilder — active school action metadata', () => {
  it('shows difficulty, description and optional flag from the selected school', async () => {
    vi.mocked(api.spells).mockResolvedValue([
      spell({ id: 'arcana-action', magicSkill: 'Arcana', nameRu: 'Общее действие', nameEn: 'Shared', difficultyIncrease: 1, description: 'Описание школы Arcana', allowedSkills: ['Arcana', 'Divine'] }),
      spell({ id: 'divine-action', magicSkill: 'Divine', nameRu: 'Общее действие', nameEn: 'Shared', difficultyIncrease: 3, description: 'Описание школы Divine', isOptional: true, allowedSkills: ['Arcana', 'Divine'] }),
    ])
    render(<MagicBuilder system="realmsOfTerrinoth" onError={() => {}} />)
    const action = await screen.findByRole('button', { name: /^Общее действие/ })
    expect(action.textContent).toContain('(1)')
    expect(action.textContent).not.toContain('EPG')
    expect(action.title).toBe('Описание школы Arcana')
    fireEvent.click(screen.getByRole('button', { name: /Божественная/ }))
    expect(action.textContent).toContain('(3)')
    expect(action.textContent).toContain('EPG')
    expect(action.title).toBe('Описание школы Divine')
  })
})
