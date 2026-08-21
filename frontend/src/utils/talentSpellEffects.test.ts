import { describe, expect, it } from 'vitest'
import type { SheetTalent } from '../api/types'
import { talentSpellEffects } from './talentSpellEffects'

const talent = (linkCode: string, needsChoice = false) =>
  ({ linkCode, needsChoice } as unknown as SheetTalent)

describe('talentSpellEffects', () => {
  it('maps the elemental talents to their stable spell effect codes', () => {
    expect(talentSpellEffects([
      talent('chill-of-nordros'),
      talent('dominion-of-the-dimora'),
      talent('favor-of-the-fae'),
      talent('flames-of-kellos'),
    ])).toEqual([
      { action: 'Attack', effectCode: 'Ice', mode: 'optionalFree', freeUses: 1 },
      { action: 'Attack', effectCode: 'Impact', mode: 'optionalFree', freeUses: 1 },
      { action: 'Attack', effectCode: 'Manipulative', mode: 'optionalFree', freeUses: 1 },
      { action: 'Attack', effectCode: 'Fire', mode: 'optionalFree', freeUses: 1 },
    ])
  })

  it('keeps the Conjure effect mandatory and ignores incomplete choices', () => {
    expect(talentSpellEffects([
      talent('natural-communion'),
      talent('flames-of-kellos', true),
    ])).toEqual([
      { action: 'Conjure', effectCode: 'Summon Ally', mode: 'mandatoryFree', freeUses: 1 },
    ])
  })
})
