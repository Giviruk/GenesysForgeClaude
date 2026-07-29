import { fireEvent, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { DiceRoller } from './DiceRoller'

vi.mock('../api/client', () => ({
  api: {
    rules: vi.fn().mockResolvedValue({ entries: [
      {
        id: 'recover', kind: 'symbolSpend', code: 'spend-combat_pos_001_recover_strain',
        nameRu: '', nameEn: '', groupRu: 'Бой', groupEn: 'Combat', sortOrder: 1,
        rollRange: '', symbolCost: '1 Advantage или 1 Triumph',
        body: 'Восстановить 1 усталость.', bodyEn: 'Recover 1 strain.',
        notes: '', source: 'Test', sourcePage: '',
      },
      {
        id: 'triumph', kind: 'symbolSpend', code: 'combat-triumph',
        nameRu: '', nameEn: '', groupRu: 'Бой', groupEn: 'Combat', sortOrder: 2,
        rollRange: '', symbolCost: '1 Triumph',
        body: 'Получить решающее преимущество.', bodyEn: 'Gain a decisive advantage.',
        notes: '', source: 'Test', sourcePage: '',
      },
      {
        id: 'despair', kind: 'symbolSpend', code: 'combat-despair',
        nameRu: '', nameEn: '', groupRu: 'Бой', groupEn: 'Combat', sortOrder: 3,
        rollRange: '', symbolCost: '1 Despair',
        body: 'Повредить используемый предмет.', bodyEn: 'Damage the item being used.',
        notes: '', source: 'Test', sourcePage: '',
      },
    ] }),
  },
}))

describe('DiceRoller — расходы преимуществ', () => {
  afterEach(() => vi.restoreAllMocks())

  it('после броска показывает только доступные по результату варианты', async () => {
    // Последняя грань ability содержит 2 преимущества и ни одного успеха.
    vi.spyOn(Math, 'random').mockReturnValue(0.999)
    render(<DiceRoller initialPool={{ ability: 1 }} spendContext="combat"
      advantageSpends={[
        {
          id: 'miss-option', cost: 2, labelRu: 'Разрешено при промахе',
          labelEn: 'Allowed on a miss',
        },
        {
          id: 'hit-option', cost: 2, labelRu: 'Только при попадании',
          labelEn: 'Only on a hit', requiresSuccess: true,
        },
        {
          id: 'too-expensive', cost: 3, labelRu: 'Слишком дорого',
          labelEn: 'Too expensive',
        },
      ]} />)

    fireEvent.click(screen.getByRole('button', { name: /Бросить/ }))

    expect(await screen.findByText('Восстановить 1 усталость.')).toBeTruthy()
    expect(screen.getByText('Разрешено при промахе')).toBeTruthy()
    expect(screen.queryByText('Только при попадании')).toBeNull()
    expect(screen.queryByText('Слишком дорого')).toBeNull()
  })

  it('объясняет, что несколько провалов не создают универсальный дополнительный эффект', () => {
    // Последняя грань difficulty содержит провал и угрозу; две кости дают два провала.
    vi.spyOn(Math, 'random').mockReturnValue(0.999)
    render(<DiceRoller initialPool={{ difficulty: 2 }} />)

    fireEvent.click(screen.getByRole('button', { name: /Бросить/ }))

    expect(screen.getByText(/Универсального дополнительного эффекта за несколько провалов нет/)).toBeTruthy()
  })

  it('показывает отдельные траты триумфа только в подходящем контексте', async () => {
    vi.spyOn(Math, 'random').mockReturnValue(0.999)
    render(<DiceRoller initialPool={{ proficiency: 1 }} spendContext="combat" />)

    fireEvent.click(screen.getByRole('button', { name: /Бросить/ }))

    expect(await screen.findByText('Получить решающее преимущество.')).toBeTruthy()
    expect(screen.getByText('Восстановить 1 усталость.')).toBeTruthy()
  })

  it('показывает последствия краха в боевом контексте', async () => {
    vi.spyOn(Math, 'random').mockReturnValue(0.999)
    render(<DiceRoller initialPool={{ challenge: 1 }} spendContext="combat" />)

    fireEvent.click(screen.getByRole('button', { name: /Бросить/ }))

    expect(await screen.findByText('Повредить используемый предмет.')).toBeTruthy()
  })
})
