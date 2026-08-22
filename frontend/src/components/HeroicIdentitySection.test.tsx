import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { describe, expect, it, vi, beforeEach } from 'vitest'
import type { CharacterSheet, HeroicIdentity } from '../api/types'
import { HeroicIdentitySection } from './HeroicTab'

const setIdentityMock = vi.fn()
vi.mock('../api/client', () => ({
  api: {
    setHeroicIdentity: (...a: unknown[]) => setIdentityMock(...a),
  },
}))

const emptyIdentity: HeroicIdentity = {
  customName: null,
  originMode: null,
  originPrimary: null,
  originSecondary: null,
  originNarrative: null,
  originRolls: [],
  complete: false,
}

function sheetWith(identity: HeroicIdentity, overrides: Partial<CharacterSheet> = {}): CharacterSheet {
  return {
    id: 'char-1',
    isCreationPhase: true,
    heroicIdentity: identity,
    heroicIdentityIncomplete: !identity.complete,
    ...overrides,
  } as unknown as CharacterSheet
}

/** Прокидывает вызов и обновление листа так же, как SheetTab. */
const run = (action: () => Promise<unknown>) => action().then(() => {})

describe('HeroicIdentitySection (ROT-HA-01)', () => {
  beforeEach(() => {
    setIdentityMock.mockReset().mockResolvedValue(undefined)
  })

  it('требует и название, и происхождение до сохранения', async () => {
    render(<HeroicIdentitySection sheet={sheetWith(emptyIdentity)} run={run} />)

    const save = screen.getByRole('button', { name: 'Сохранить' }) as HTMLButtonElement
    expect(save.disabled).toBe(true)

    fireEvent.change(screen.getByPlaceholderText('Личное название'), { target: { value: 'Клинок рассвета' } })
    expect(save.disabled).toBe(true) // происхождение ещё не выбрано

    fireEvent.change(screen.getByRole('combobox'), { target: { value: 'destiny' } })
    expect(save.disabled).toBe(false)

    fireEvent.click(save)
    await waitFor(() => expect(setIdentityMock).toHaveBeenCalledWith('char-1', {
      customName: 'Клинок рассвета', originMode: 'standard', originPrimary: 'destiny',
    }))
  })

  it('оставляет только селект происхождения из таблицы', () => {
    render(<HeroicIdentitySection sheet={sheetWith(emptyIdentity)} run={run} />)

    expect(screen.getAllByRole('combobox')).toHaveLength(1)
    expect(screen.queryByRole('radio')).toBeNull()
    expect(screen.queryByPlaceholderText('Откуда взялась сила')).toBeNull()
    expect(screen.queryByRole('button', { name: /Бросить d10/ })).toBeNull()
    expect(screen.queryByText(/обязательны для завершения создания/)).toBeNull()
  })

  it('показывает обе категории и грани специального броска', () => {
    const complete: HeroicIdentity = {
      customName: 'Наследие',
      originMode: 'doubleStandard',
      originPrimary: 'patron',
      originSecondary: 'blessingOrCurse',
      originNarrative: null,
      originRolls: [0, 4, 7],
      complete: true,
    }
    render(<HeroicIdentitySection sheet={sheetWith(complete)} run={run} />)

    const summary = screen.getByText(/Покровительство/, { selector: '.hint' })
    expect(summary.textContent).toContain('Благословение либо проклятие')
    expect(summary.textContent).toContain('0, 4, 7')
    expect(summary.textContent).toContain('0 — бросить ещё дважды')
  })

  it('после завершения создания форма недоступна', () => {
    const complete: HeroicIdentity = {
      customName: 'Клинок рассвета',
      originMode: 'standard',
      originPrimary: 'destiny',
      originSecondary: null,
      originNarrative: null,
      originRolls: [],
      complete: true,
    }
    render(<HeroicIdentitySection
      sheet={sheetWith(complete, { isCreationPhase: false })} run={run} />)

    expect(screen.queryByRole('button', { name: 'Сохранить' })).toBeNull()
    expect(screen.getByText(/Избранность судьбой/)).toBeTruthy()
  })

  it('старому персонажу без личности разрешает однократное заполнение', () => {
    render(<HeroicIdentitySection
      sheet={sheetWith(emptyIdentity, { isCreationPhase: false })} run={run} />)

    expect(screen.getByText(/станут неизменяемыми/)).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Сохранить' })).toBeTruthy()
  })
})
