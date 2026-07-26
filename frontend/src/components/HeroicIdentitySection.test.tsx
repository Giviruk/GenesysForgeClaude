import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { describe, expect, it, vi, beforeEach } from 'vitest'
import type { CharacterSheet, HeroicIdentity } from '../api/types'
import { HeroicIdentitySection } from './SheetTab'

const setIdentityMock = vi.fn()
const rollOriginMock = vi.fn()
vi.mock('../api/client', () => ({
  api: {
    setHeroicIdentity: (...a: unknown[]) => setIdentityMock(...a),
    rollHeroicOrigin: (...a: unknown[]) => rollOriginMock(...a),
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
    rollOriginMock.mockReset().mockResolvedValue({
      rolls: [0, 4, 7], originMode: 'doubleStandard', originPrimary: 'patron', originSecondary: 'blessingOrCurse',
    })
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

  it('собственное происхождение уходит текстом, а не категорией таблицы', async () => {
    render(<HeroicIdentitySection sheet={sheetWith(emptyIdentity)} run={run} />)

    fireEvent.change(screen.getByPlaceholderText('Личное название'), { target: { value: 'Дар предков' } })
    fireEvent.click(screen.getByRole('radio', { name: /описать/ }))
    fireEvent.change(screen.getByPlaceholderText('Откуда взялась сила'), { target: { value: 'Клятва мести' } })

    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }))
    await waitFor(() => expect(setIdentityMock).toHaveBeenCalledWith('char-1', {
      customName: 'Дар предков', originMode: 'custom', originNarrative: 'Клятва мести',
    }))
  })

  it('бросок выполняет сервер, а сохранение названия не переписывает происхождение', async () => {
    const { rerender } = render(<HeroicIdentitySection sheet={sheetWith(emptyIdentity)} run={run} />)

    fireEvent.click(screen.getByRole('radio', { name: /бросить/ }))
    fireEvent.click(screen.getByRole('button', { name: /Бросить d10/ }))
    await waitFor(() => expect(rollOriginMock).toHaveBeenCalledWith('char-1'))

    // Сервер вернул специальный результат: лист перезагружается уже с двумя категориями.
    const rolled: HeroicIdentity = {
      ...emptyIdentity,
      originMode: 'doubleStandard',
      originPrimary: 'patron',
      originSecondary: 'blessingOrCurse',
      originRolls: [0, 4, 7],
    }
    rerender(<HeroicIdentitySection sheet={sheetWith(rolled)} run={run} />)

    fireEvent.change(screen.getByPlaceholderText('Личное название'), { target: { value: 'Наследие' } })
    fireEvent.click(screen.getByRole('button', { name: 'Сохранить' }))
    await waitFor(() => expect(setIdentityMock).toHaveBeenCalledWith('char-1', { customName: 'Наследие' }))
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

    const summary = screen.getByText(/Покровительство/)
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
