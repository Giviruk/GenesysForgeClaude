import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { CharacterSheet } from '../api/types'
import { ReadOnlyBioTab, ReadOnlyInventoryTab, ReadOnlyTalentsTab } from './CampaignMemberReadOnlyTabs'

const sheet = {
  money: 42,
  derived: { encumbranceLoad: 2, encumbranceThreshold: 7 },
  talents: [{
    talentDefId: 't1', name: 'Toughened', nameRu: 'Закалённый', tier: 1, isRanked: true, ranks: 2,
    activation: 'Пассивно', description: 'Повышает стойкость.', choices: [], needsChoice: false,
  }],
  items: [{
    id: 'i1', name: 'Sword', nameRu: 'Меч', kind: 'weapon', state: 'equipped', quantity: 1,
    damageState: 'undamaged', isUsable: true, attackProfiles: [], damage: '+3', crit: '2', rangeBand: 'Вплотную',
    properties: '', attachments: [], description: 'Надёжный клинок.', descriptionEn: '', safeDescription: '', load: 1,
  }],
  desire: 'Защитить город', fear: null, strength: null, flaw: null, background: 'Бывший стражник.',
} as unknown as CharacterSheet

describe('campaign member read-only tabs', () => {
  it('shows talents, inventory and bio without mutation controls', () => {
    const talents = render(<ReadOnlyTalentsTab sheet={sheet} />)
    expect(screen.getByText('Закалённый')).toBeTruthy()
    expect(screen.queryByRole('button', { name: /Купить/ })).toBeNull()
    talents.unmount()

    const inventory = render(<ReadOnlyInventoryTab sheet={sheet} />)
    expect(screen.getByText('Меч')).toBeTruthy()
    expect(screen.queryByRole('button', { name: /Продать|Убрать/ })).toBeNull()
    inventory.unmount()

    render(<ReadOnlyBioTab sheet={sheet} />)
    expect(screen.getByText('Защитить город')).toBeTruthy()
    expect(screen.getByText('Бывший стражник.')).toBeTruthy()
  })
})
