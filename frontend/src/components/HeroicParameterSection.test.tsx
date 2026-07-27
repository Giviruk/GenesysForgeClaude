import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { describe, expect, it, vi, beforeEach } from 'vitest'
import type { CharacterSheet, HeroicConfiguration, Reference } from '../api/types'
import { HeroicParameterSection } from './HeroicTab'

const setConfigMock = vi.fn()
const replaceMock = vi.fn()
vi.mock('../api/client', () => ({
  api: {
    setHeroicConfiguration: (...a: unknown[]) => setConfigMock(...a),
    replaceSignatureWeapon: (...a: unknown[]) => replaceMock(...a),
  },
}))

const reference = {
  skills: [
    { id: 'skill-1', name: 'Melee (Light)', nameRu: 'Ближний бой (лёгкое)' },
    { id: 'skill-2', name: 'Vigilance', nameRu: 'Бдительность' },
  ],
} as unknown as Reference

function sheetWith(config: HeroicConfiguration, overrides: Partial<CharacterSheet> = {}): CharacterSheet {
  return {
    id: 'char-1',
    isCreationPhase: true,
    heroicConfiguration: config,
    heroicConfigurationIncomplete: !config.complete,
    ...overrides,
  } as unknown as CharacterSheet
}

const emptyConfig: HeroicConfiguration = {
  kind: 'none',
  paragonSkillDefId: null,
  paragonSkillName: null,
  paragonSkillMissing: false,
  sixthSenseSubject: null,
  signatureWeapon: null,
  complete: true,
}

const run = (action: () => Promise<unknown>) => action().then(() => {})

describe('HeroicParameterSection (ROT-HA-02)', () => {
  beforeEach(() => {
    setConfigMock.mockReset().mockResolvedValue(undefined)
    replaceMock.mockReset().mockResolvedValue(undefined)
  })

  it('способность без параметра не показывает секцию', () => {
    const { container } = render(
      <HeroicParameterSection sheet={sheetWith(emptyConfig)} reference={reference} run={run} />)

    expect(container.firstChild).toBeNull()
  })

  it('Paragon отправляет выбранный навык', async () => {
    render(<HeroicParameterSection
      sheet={sheetWith({ ...emptyConfig, kind: 'paragonSkill', complete: false })}
      reference={reference} run={run} />)

    fireEvent.change(screen.getByRole('combobox'), { target: { value: 'skill-2' } })
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }))

    await waitFor(() => expect(setConfigMock)
      .toHaveBeenCalledWith('char-1', { paragonSkillDefId: 'skill-2' }))
  })

  it('скрытый навык Paragon показывает требование починки', () => {
    render(<HeroicParameterSection
      sheet={sheetWith({
        ...emptyConfig, kind: 'paragonSkill', paragonSkillDefId: 'gone',
        paragonSkillName: 'Свой навык', paragonSkillMissing: true,
      })}
      reference={reference} run={run} />)

    expect(screen.getByText(/требуется исправление/)).toBeTruthy()
  })

  it('Sixth Sense отправляет обрезанную категорию', async () => {
    render(<HeroicParameterSection
      sheet={sheetWith({ ...emptyConfig, kind: 'sixthSenseSubject', complete: false })}
      reference={reference} run={run} />)

    fireEvent.change(screen.getByRole('textbox'), { target: { value: '  духи предков  ' } })
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }))

    await waitFor(() => expect(setConfigMock)
      .toHaveBeenCalledWith('char-1', { sixthSenseSubject: 'духи предков' }))
  })

  it('именное оружие отправляет профиль и подтверждённые признаки формы', async () => {
    render(<HeroicParameterSection
      sheet={sheetWith({ ...emptyConfig, kind: 'signatureWeapon', complete: false })}
      reference={reference} run={run} />)

    fireEvent.click(screen.getByRole('radio', { name: /Двуручный/ }))
    fireEvent.change(screen.getByPlaceholderText('форма оружия'), { target: { value: 'Родовой молот' } })
    fireEvent.click(screen.getByRole('checkbox', { name: /дробящее/ }))
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }))

    await waitFor(() => expect(setConfigMock).toHaveBeenCalledWith('char-1', {
      weaponProfile: 'twoHanded',
      craftsmanship: 'steel',
      narrativeForm: 'Родовой молот',
      formTraits: 'bluntOrCrushing',
    }))
  })

  it('показывает числа профиля, пришедшие с сервера, и позволяет пометить оружие потерянным', async () => {
    render(<HeroicParameterSection
      sheet={sheetWith({
        ...emptyConfig, kind: 'signatureWeapon', complete: true,
        signatureWeapon: {
          profile: 'ranged', craftsmanship: 'elven', narrativeForm: 'Лук предков',
          formTraits: 'ranged, bowOrCrossbow', isLost: false, skillName: 'Ranged',
          damage: '8', crit: 3, rangeBand: 'Long', encumbrance: 2, hardPoints: 2,
          qualities: [{ code: 'superior', nameRu: 'Превосходное', nameEn: 'Superior', rating: null,
            hasRating: false, isActive: false, activationCost: '' }],
        },
      }, { isCreationPhase: false })}
      reference={reference} run={run} />)

    const summary = screen.getByText(/Лук предков/)
    expect(summary.textContent).toContain('Ranged')
    expect(summary.textContent).toContain('Превосходное')

    fireEvent.click(screen.getByRole('button', { name: 'Отметить потерянным' }))
    await waitFor(() => expect(replaceMock).toHaveBeenCalledWith('char-1', { lost: true }))
  })
})
