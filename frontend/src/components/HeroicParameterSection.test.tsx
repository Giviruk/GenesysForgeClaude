import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { describe, expect, it, vi, beforeEach } from 'vitest'
import type { CharacterSheet, HeroicConfiguration, Quality, Reference, SignatureWeapon } from '../api/types'
import { HeroicParameterSection } from './HeroicTab'

const setConfigMock = vi.fn()
const replaceMock = vi.fn()
const upgradesMock = vi.fn()
vi.mock('../api/client', () => ({
  api: {
    setHeroicConfiguration: (...a: unknown[]) => setConfigMock(...a),
    replaceSignatureWeapon: (...a: unknown[]) => replaceMock(...a),
    setSignatureWeaponUpgrades: (...a: unknown[]) => upgradesMock(...a),
  },
}))

const reference = {
  skills: [
    { id: 'skill-1', name: 'Melee (Light)', nameRu: 'Ближний бой (лёгкое)' },
    { id: 'skill-2', name: 'Vigilance', nameRu: 'Бдительность' },
  ],
  // Улучшения нужны выбору базового улучшения именного оружия (ROT-HA-02): совместимость
  // считается по признакам формы, поэтому в наборе есть и подходящее, и заведомо чужое.
  attachments: [
    {
      id: 'att-thunder', code: 'rot.attachment.runic-thunder', name: 'Runic Thunder',
      nameRu: 'Рунический гром', hostKind: 'weapon',
      requiredTraits: 'none', requiredAnyTraits: 'none', forbiddenTraits: 'none',
      description: 'Добавляет свойство «Ошеломление» 1.', descriptionEn: 'Adds the Disorient 1 quality.',
    },
    {
      id: 'att-missile', code: 'rot.attachment.explosive-missile', name: 'Explosive Missile',
      nameRu: 'Взрывной снаряд', hostKind: 'weapon',
      requiredTraits: 'ranged', requiredAnyTraits: 'none', forbiddenTraits: 'none',
      description: 'Добавляет свойство «Взрыв» 5.', descriptionEn: 'Adds the Blast 5 quality.',
    },
  ],
  qualities: [{
    id: 'quality-superior', code: 'superior', nameEn: 'Superior', nameRu: 'Превосходное',
    kind: 'itemQuality', isActive: true, hasRating: false, activationCost: '', category: 'combat',
    description: 'Оружие получает дополнительный эффект превосходного изготовления.',
    safeDescription: 'Дополнительный эффект изготовления.', descriptionEn: '', source: 'Test',
  } as Quality, {
    id: 'quality-vicious', code: 'vicious', nameEn: 'Vicious', nameRu: 'Vicious',
    kind: 'itemQuality', isActive: false, hasRating: true, activationCost: '', category: 'combat',
    description: 'Добавляет бонус к броску критической травмы.',
    safeDescription: 'Бонус к броску критической травмы.', descriptionEn: '', source: 'Test',
  } as Quality],
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

const weaponFixture: SignatureWeapon = {
  profile: 'ranged', craftsmanship: 'elven', narrativeForm: 'Лук предков',
  formTraits: 'ranged, bowOrCrossbow', isLost: false, skillName: 'Ranged',
  damage: '8', crit: 3, rangeBand: 'Long', encumbrance: 2, hardPoints: 2,
  qualities: [
    { code: 'superior', nameRu: 'Превосходное', nameEn: 'Superior', rating: null,
      hasRating: false, isActive: false, activationCost: '' },
    { code: 'vicious', nameRu: 'Vicious', nameEn: 'Vicious', rating: 3,
      hasRating: true, isActive: false, activationCost: '' },
  ],
  baseAttachment: {
    defId: 'att-thunder', code: 'rot.attachment.runic-thunder', name: 'Runic Thunder',
    nameRu: 'Рунический гром', description: '', effects: [],
  },
  improvement: 'none',
  supremeAttachment: null,
  craftsmanshipOutOfRules: false,
}

const run = (action: () => Promise<unknown>) => action().then(() => {})

describe('HeroicParameterSection (ROT-HA-02)', () => {
  beforeEach(() => {
    setConfigMock.mockReset().mockResolvedValue(undefined)
    replaceMock.mockReset().mockResolvedValue(undefined)
    upgradesMock.mockReset().mockResolvedValue(undefined)
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

    // Ближней форме предлагают только подходящее улучшение: дальнобойного в списке нет.
    const attachmentPicker = screen.getByLabelText('Базовое улучшение')
    expect(screen.queryByRole('option', { name: 'Взрывной снаряд' })).toBeNull()
    fireEvent.change(attachmentPicker, { target: { value: 'att-thunder' } })

    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }))

    await waitFor(() => expect(setConfigMock).toHaveBeenCalledWith('char-1', {
      weaponProfile: 'twoHanded',
      craftsmanship: 'steel',
      narrativeForm: 'Родовой молот',
      formTraits: 'bluntOrCrushing',
      baseAttachmentDefId: 'att-thunder',
    }))
  })

  it('качество изготовления предлагается только то, что даёт способность', () => {
    render(<HeroicParameterSection
      sheet={sheetWith({ ...emptyConfig, kind: 'signatureWeapon', complete: false })}
      reference={reference} run={run} />)

    // Железа книга именному оружию не даёт, древняя работа приходит улучшением (ROT-HA-05).
    expect(screen.queryByRole('option', { name: 'Железо' })).toBeNull()
    expect(screen.queryByRole('option', { name: 'Древняя работа' })).toBeNull()
    expect(screen.getByRole('option', { name: 'Гномья работа' })).toBeTruthy()
  })

  it('показывает свойства выбранного качества изготовления', () => {
    render(<HeroicParameterSection
      sheet={sheetWith({ ...emptyConfig, kind: 'signatureWeapon', complete: false })}
      reference={reference} run={run} />)

    expect(screen.getByText(/Без изменений: базовые характеристики профиля/)).toBeTruthy()

    fireEvent.change(screen.getAllByRole('combobox')[0], { target: { value: 'dwarven' } })
    expect(screen.getByText(/Урон \+1, вес \+1, редкость \+2/)).toBeTruthy()
  })

  it('показывает описание выбранного базового улучшения', () => {
    render(<HeroicParameterSection
      sheet={sheetWith({ ...emptyConfig, kind: 'signatureWeapon', complete: false })}
      reference={reference} run={run} />)

    fireEvent.change(screen.getByLabelText('Базовое улучшение'), { target: { value: 'att-thunder' } })

    expect(screen.getByText(/Добавляет свойство/)).toBeTruthy()
  })

  it('после покупки Improved просит выбрать Укреплённое или древнюю работу', async () => {
    render(<HeroicParameterSection
      sheet={sheetWith({
        ...emptyConfig, kind: 'signatureWeapon', complete: true,
        signatureWeapon: { ...weaponFixture, improvement: 'none' },
      }, { isCreationPhase: false, heroicUpgradeRank: 1 })}
      reference={reference} run={run} />)

    fireEvent.change(screen.getByLabelText('Улучшение Improved'), { target: { value: 'ancient' } })
    fireEvent.click(screen.getByRole('button', { name: 'Выбрать навсегда' }))

    await waitFor(() => expect(upgradesMock).toHaveBeenCalledWith('char-1', { improvement: 'ancient' }))
  })

  it('без базового улучшения именное оружие не сохраняется', () => {
    render(<HeroicParameterSection
      sheet={sheetWith({ ...emptyConfig, kind: 'signatureWeapon', complete: false })}
      reference={reference} run={run} />)

    fireEvent.change(screen.getByPlaceholderText('форма оружия'), { target: { value: 'Фамильный меч' } })

    expect(screen.getByRole('button', { name: 'Сохранить' })).toHaveProperty('disabled', true)
  })

  it('показывает числа профиля, пришедшие с сервера, и позволяет пометить оружие потерянным', async () => {
    render(<HeroicParameterSection
      sheet={sheetWith({
        ...emptyConfig, kind: 'signatureWeapon', complete: true,
        signatureWeapon: weaponFixture,
      }, { isCreationPhase: false })}
      reference={reference} run={run} />)

    const summary = screen.getByText(/Лук предков/)
    expect(summary.textContent).toContain('Ranged')
    expect(summary.textContent).toContain('Превосходное')
    expect(summary.textContent).toContain('Высококритичное 3')
    expect(summary.textContent).not.toContain('Vicious')

    const quality = screen.getByRole('button', { name: /Превосходное/ })
    fireEvent.mouseEnter(quality)
    expect(screen.getByRole('tooltip').textContent).toContain('дополнительный эффект')

    fireEvent.mouseLeave(quality)
    const vicious = screen.getByRole('button', { name: /Высококритичное/ })
    fireEvent.mouseEnter(vicious)
    expect(screen.getByRole('tooltip').textContent).toContain('критической травмы')

    fireEvent.click(screen.getByRole('button', { name: 'Отметить потерянным' }))
    await waitFor(() => expect(replaceMock).toHaveBeenCalledWith('char-1', { lost: true }))
  })
})
