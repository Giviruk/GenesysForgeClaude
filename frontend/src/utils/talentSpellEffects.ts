import type { SheetTalent } from '../api/types'

/** Бесплатный эффект, который талант добавляет в конфигурацию заклинания. */
export interface TalentSpellEffect {
  action: string
  effectCode: string
  mode: 'mandatoryFree' | 'optionalFree'
  freeUses: number
}

/**
 * Структурные правила талантов, влияющие на сборку магического действия.
 * Русское/английское описание таланта намеренно не разбирается: код эффекта остаётся стабильным,
 * а название и текст берутся из обычного справочника заклинаний.
 */
const RULES_BY_TALENT: Record<string, TalentSpellEffect[]> = {
  'chill-of-nordros': [{ action: 'Attack', effectCode: 'Ice', mode: 'optionalFree', freeUses: 1 }],
  'dominion-of-the-dimora': [{ action: 'Attack', effectCode: 'Impact', mode: 'optionalFree', freeUses: 1 }],
  'favor-of-the-fae': [{ action: 'Attack', effectCode: 'Manipulative', mode: 'optionalFree', freeUses: 1 }],
  'flames-of-kellos': [{ action: 'Attack', effectCode: 'Fire', mode: 'optionalFree', freeUses: 1 }],
  'natural-communion': [{ action: 'Conjure', effectCode: 'Summon Ally', mode: 'mandatoryFree', freeUses: 1 }],
}

/** Возвращает действующие правила талантов; незаполненный обязательный выбор эффекта блокируется. */
export function talentSpellEffects(talents: SheetTalent[] = []): TalentSpellEffect[] {
  return talents
    .filter(talent => !talent.needsChoice)
    .flatMap(talent => RULES_BY_TALENT[talent.linkCode] ?? [])
}
