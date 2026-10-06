import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { CharacterSheet, Reference, SkillDef, SkillKind, Spell, TalentCategory, TalentDef } from '../api/types'
import { TalentsTab } from './TalentsTab'

const buyTalentMock = vi.fn()
const npcsMock = vi.fn()
const spellsMock = vi.fn()

vi.mock('../api/client', () => ({
  api: {
    buyTalent: (...args: unknown[]) => buyTalentMock(...args),
    npcs: (...args: unknown[]) => npcsMock(...args),
    spells: (...args: unknown[]) => spellsMock(...args),
    refundTalent: vi.fn(),
  },
}))

const talent = (id: string, nameRu: string, category: TalentCategory): TalentDef => ({
  id,
  name: nameRu,
  nameRu,
  tier: 1,
  isRanked: false,
  category,
  setting: 'any',
  activationEn: '',
  canUseOutOfTurn: false,
  careerSkillNames: [],
  linkCode: '',
  requiresTalentCode: '',
  excludesTalentCodes: [],
  usesPerScope: 0,
  useScope: 'none',
  storyPointCost: 0,
  strainCost: 0,
  trigger: '',
  choiceKind: 'none',
  choiceCountFirstRank: 0,
  choiceCountNextRank: 0,
  choiceDistinctAcrossRanks: false,
  choiceAllowedSkillKinds: [],
  activation: 'Пассивный',
  description: `${nameRu}: описание`,
  safeDescription: `${nameRu}: описание`,
  source: 'Test',
  woundBonus: 0,
  strainBonus: 0,
  soakBonus: 0,
  meleeDefenseBonus: 0,
  rangedDefenseBonus: 0,
  isCustom: false,
  grantsCharacteristic: false,
})

const skill = (name: string, nameRu: string, kind: SkillKind): SkillDef => ({
  id: `skill-${name}`, name, nameRu, characteristic: 'intellect', kind,
  safeDescription: '', source: 'Test', isCustom: false,
})

const spell = (fields: Partial<Spell>): Spell => ({
  id: `${fields.kind}-${fields.parentEffect ?? ''}-${fields.nameEn}`, magicSkill: 'Arcana',
  parentEffect: '', difficulty: '+1', repeatable: false, exclusions: [], allowedSkills: [],
  ...fields,
}) as Spell

const sheet = {
  id: 'char-1',
  system: 'genesysCore',
  availableXp: 100,
  isCreationPhase: true,
  talents: [],
  talentTierCounts: {},
  characteristics: { brawn: 2, agility: 2, intellect: 2, cunning: 2, willpower: 2, presence: 2 },
} as unknown as CharacterSheet

const reference = {
  talents: [
    talent('combat-1', 'Боевой талант', 'combat'),
    talent('social-1', 'Социальный талант', 'social'),
    talent('magic-1', 'Магический талант', 'magic'),
  ],
} as unknown as Reference

describe('TalentsTab', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    buyTalentMock.mockResolvedValue(undefined)
    npcsMock.mockResolvedValue([{
      id: 'npc-falcon', name: 'Серый сокол', system: 'genesysCore', silhouette: 0,
      tags: ['животное'],
    }, {
      id: 'npc-guard', name: 'Городской стражник', system: 'genesysCore', silhouette: 0,
      tags: ['человек'],
    }])
  })

  it('фильтрует доступные таланты по категории', () => {
    render(<TalentsTab sheet={sheet} reference={reference} onError={() => {}} refresh={() => Promise.resolve()} />)

    expect(screen.getByText('Боевой талант')).toBeTruthy()
    expect(screen.getByText('Социальный талант')).toBeTruthy()

    fireEvent.click(screen.getByRole('button', { name: /Социальные/ }))

    expect(screen.queryByText('Боевой талант')).toBeNull()
    expect(screen.getByText('Социальный талант')).toBeTruthy()
    expect(screen.queryByText('Магический талант')).toBeNull()
  })

  it('предлагает выбрать животное-спутника и передаёт выбор при покупке', async () => {
    const companion = {
      ...talent('animal-companion', 'Животное-спутник', 'general'),
      tier: 3,
      isRanked: true,
      choiceKind: 'animalCompanion' as const,
      choiceCountFirstRank: 1,
      choiceCountNextRank: 1,
    }
    const companionSheet = {
      ...sheet,
      talentTierCounts: { '1': 3, '2': 2 },
    } as unknown as CharacterSheet
    const refresh = vi.fn().mockResolvedValue(undefined)

    render(<TalentsTab sheet={companionSheet}
      reference={{ talents: [companion] } as unknown as Reference}
      onError={() => {}} refresh={refresh} />)

    fireEvent.click(screen.getByRole('button', { name: /Купить/ }))
    expect(buyTalentMock).not.toHaveBeenCalled()
    expect(screen.getByRole('heading', { name: /выбор спутника/ })).toBeTruthy()
    await screen.findByRole('option', { name: /Серый сокол/ })
    expect(screen.queryByRole('option', { name: /Городской стражник/ })).toBeNull()
    fireEvent.click(screen.getByRole('button', { name: 'Подтвердить и купить' }))

    await waitFor(() => expect(buyTalentMock).toHaveBeenCalledWith(
      'char-1', 'animal-companion', undefined, ['npc-falcon']))
    expect(refresh).toHaveBeenCalledOnce()
  })

  it('показывает сохранённого спутника у купленного таланта', () => {
    const companion = {
      ...talent('animal-companion', 'Животное-спутник', 'general'),
      tier: 3, isRanked: true, choiceKind: 'animalCompanion' as const,
      choiceCountFirstRank: 1, choiceCountNextRank: 1,
    }
    const companionSheet = {
      ...sheet,
      talents: [{
        talentDefId: companion.id, name: companion.name, nameRu: companion.nameRu,
        tier: 3, isRanked: true, ranks: 1, activation: 'Пассивный', description: '',
        woundBonus: 0, strainBonus: 0, soakBonus: 0, meleeDefenseBonus: 0,
        rangedDefenseBonus: 0, grantsCharacteristic: false, grantedCharacteristics: [],
        choices: [{ rankIndex: 0, kind: 'animalCompanion', value: 'npc-falcon',
          displayName: 'Серый сокол' }], needsChoice: false, activationEn: 'Passive',
        canUseOutOfTurn: false,
      }],
      talentTierCounts: { '1': 3, '2': 2, '3': 1 },
    } as unknown as CharacterSheet

    render(<TalentsTab sheet={companionSheet}
      reference={{ talents: [companion] } as unknown as Reference}
      onError={() => {}} refresh={() => Promise.resolve()} />)

    expect(screen.getAllByText(/Выбор:.*Серый сокол/).length).toBeGreaterThan(0)
  })

  it('просит выбрать две характеристики для «Героической воли» и передаёт их при покупке', async () => {
    const heroicWill = {
      ...talent('heroic-will', 'Героическая воля', 'general'),
      choiceKind: 'characteristic' as const, choiceCountFirstRank: 2, choiceDistinctAcrossRanks: true,
    }
    render(<TalentsTab sheet={sheet}
      reference={{ talents: [heroicWill], skills: [] } as unknown as Reference}
      onError={() => {}} refresh={() => Promise.resolve()} />)

    fireEvent.click(screen.getByRole('button', { name: /Купить/ }))
    expect(buyTalentMock).not.toHaveBeenCalled()
    expect(screen.getByRole('heading', { name: /выбор характеристик/ })).toBeTruthy()

    const confirm = screen.getByRole('button', { name: 'Подтвердить и купить' }) as HTMLButtonElement
    fireEvent.click(screen.getByRole('button', { name: 'Воля' }))
    expect(confirm.disabled).toBe(true)
    fireEvent.click(screen.getByRole('button', { name: 'Мощь' }))
    // Больше двух выбрать нельзя.
    expect((screen.getByRole('button', { name: 'Ловкость' }) as HTMLButtonElement).disabled).toBe(true)
    fireEvent.click(confirm)

    await waitFor(() => expect(buyTalentMock).toHaveBeenCalledWith(
      'char-1', 'heroic-will', undefined, ['willpower', 'brawn']))
  })

  it('для следующего ранга «Квалификации» требует два новых не боевых и не магических навыка', async () => {
    const knack = {
      ...talent('knack', 'Квалификация', 'general'),
      isRanked: true, choiceKind: 'skill' as const, choiceCountFirstRank: 1, choiceCountNextRank: 2,
      choiceDistinctAcrossRanks: true, choiceAllowedSkillKinds: ['general', 'knowledge', 'social'] as SkillKind[],
    }
    const skills = [
      skill('Athletics', 'Атлетика', 'general'), skill('Charm', 'Обаяние', 'social'),
      skill('Lore', 'Предания', 'knowledge'), skill('Melee', 'Ближний бой', 'combat'),
      skill('Arcana', 'Тайная магия', 'magic'),
    ]
    const knackSheet = {
      ...sheet,
      talents: [{
        talentDefId: 'knack', name: 'Квалификация', nameRu: 'Квалификация', tier: 1, isRanked: true,
        ranks: 1, activation: 'Пассивный', description: '', woundBonus: 0, strainBonus: 0, soakBonus: 0,
        meleeDefenseBonus: 0, rangedDefenseBonus: 0, grantsCharacteristic: false,
        grantedCharacteristics: [], needsChoice: false, activationEn: 'Passive', canUseOutOfTurn: false,
        choices: [{ rankIndex: 0, kind: 'skill', value: 'Athletics', displayName: 'Атлетика' }],
      }],
      talentTierCounts: { '1': 2 },
    } as unknown as CharacterSheet

    render(<TalentsTab sheet={knackSheet}
      reference={{ talents: [knack], skills } as unknown as Reference}
      onError={() => {}} refresh={() => Promise.resolve()} />)

    fireEvent.click(screen.getByRole('button', { name: /Купить/ }))
    expect(screen.getByRole('heading', { name: /выбор навыков/ })).toBeTruthy()
    expect(screen.getByText(/Уже выбрано этим талантом: Атлетика/)).toBeTruthy()
    expect(screen.queryByRole('button', { name: 'Атлетика' })).toBeNull()
    expect(screen.queryByRole('button', { name: 'Ближний бой' })).toBeNull()
    expect(screen.queryByRole('button', { name: 'Тайная магия' })).toBeNull()

    fireEvent.click(screen.getByRole('button', { name: 'Обаяние' }))
    fireEvent.click(screen.getByRole('button', { name: 'Предания' }))
    fireEvent.click(screen.getByRole('button', { name: 'Подтвердить и купить' }))

    await waitFor(() => expect(buyTalentMock).toHaveBeenCalledWith(
      'char-1', 'knack', undefined, ['Charm', 'Lore']))
  })

  it('собирает конфигурацию «Коронного заклинания» из действия и эффектов', async () => {
    spellsMock.mockResolvedValue([
      spell({ kind: 'effect', nameEn: 'Heal', nameRu: 'Исцеление', difficulty: '1' }),
      spell({ kind: 'effect', nameEn: 'Attack', nameRu: 'Атака', difficulty: '2' }),
      spell({ kind: 'additionalEffect', parentEffect: 'Attack', nameEn: 'Fire', nameRu: 'Огонь' }),
      spell({ kind: 'additionalEffect', parentEffect: 'Attack', nameEn: 'Range', nameRu: 'Дистанция', repeatable: true }),
      spell({ kind: 'additionalEffect', parentEffect: 'Attack', nameEn: 'Deadly', nameRu: 'Смертельный', exclusions: ['Non-Lethal'] }),
      spell({ kind: 'additionalEffect', parentEffect: 'Attack', nameEn: 'Non-Lethal', nameRu: 'Несмертельный' }),
    ])
    const signature = {
      ...talent('signature', 'Коронное заклинание', 'magic'),
      choiceKind: 'spellConfiguration' as const, choiceCountFirstRank: 1,
    }
    render(<TalentsTab sheet={sheet}
      reference={{ talents: [signature], skills: [] } as unknown as Reference}
      onError={() => {}} refresh={() => Promise.resolve()} />)

    fireEvent.click(screen.getByRole('button', { name: /Купить/ }))
    expect(screen.getByRole('heading', { name: /выбор заклинания/ })).toBeTruthy()
    expect(((await screen.findByRole('option', { name: 'Атака' })) as HTMLOptionElement).selected).toBe(true)
    const confirm = screen.getByRole('button', { name: 'Подтвердить и купить' }) as HTMLButtonElement
    expect(confirm.disabled).toBe(true)

    fireEvent.click(screen.getByRole('button', { name: /^Огонь/ }))
    fireEvent.click(screen.getByRole('button', { name: /^Дистанция/ }))
    fireEvent.click(screen.getByRole('button', { name: /^Дистанция/ }))
    fireEvent.click(screen.getByRole('button', { name: /^Смертельный/ }))
    // Взаимоисключение действует в обе стороны: его назвал только «Смертельный».
    expect((screen.getByRole('button', { name: /^Несмертельный/ }) as HTMLButtonElement).disabled).toBe(true)
    expect(screen.getByText('Атака: Огонь, Дистанция ×2, Смертельный')).toBeTruthy()

    fireEvent.click(confirm)
    await waitFor(() => expect(buyTalentMock).toHaveBeenCalledWith(
      'char-1', 'signature', undefined, ['Attack|Deadly|Fire|Range|Range']))
  })
})
