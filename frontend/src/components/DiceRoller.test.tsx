import { fireEvent, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { DiceRoller } from './DiceRoller'

vi.mock('../api/client', () => ({
  api: {
    rules: vi.fn().mockResolvedValue({ entries: [{
      id: 'recover', kind: 'symbolSpend', code: 'spend-combat_pos_001_recover_strain',
      nameRu: '', nameEn: '', groupRu: 'Бой', groupEn: 'Combat', sortOrder: 1,
      rollRange: '', symbolCost: '1 Advantage или 1 Triumph',
      body: 'Восстановить 1 усталость.', bodyEn: 'Recover 1 strain.',
      notes: '', source: 'Test', sourcePage: '',
    }] }),
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
})
