import { describe, expect, it } from 'vitest'
import { heroicMetaParts, referenceNameIndex, criticalSeverityDifficulty, displayPackName, localizedAuditSummary, localizedReferenceTerm, localizeHeroicMeta, preferredLibrarySystem } from './designPresentation'
import { readLibrarySystem, writeLibrarySystem } from './uiPreferences'

describe('design presentation', () => {
  it('localizes whole quoted catalogue names and preserves user prose and unknown names', () => {
    const names = referenceNameIndex([{ name: 'Axe', nameRu: 'Топор' }, { name: 'Superior Weapon Customization', nameRu: 'Мастерская доработка' }])
    expect(localizedAuditSummary('Улучшение «Superior Weapon Customization» установлено на «Axe»', names)).toBe('Улучшение «Мастерская доработка» установлено на «Топор»')
    expect(localizedAuditSummary('Мой Axe и «Axe Knight»', names)).toBe('Мой Axe и «Axe Knight»')
    expect(localizedReferenceTerm('Athletics', [{ name: 'Athletics', nameRu: 'Атлетика' }])).toBe('Атлетика')
    expect(localizedReferenceTerm('Custom skill')).toBe('Custom skill')
  })
  it('replaces technical pack system names for display only', () => {
    expect(displayPackName('Тени (RealmsOfTerrinoth)')).toBe('Тени (Realms of Terrinoth)')
    expect(displayPackName('Мои заметки')).toBe('Мои заметки')
    expect(localizeHeroicMeta('incidental; улучшение Duration или Frequency')).toBe('Свободное; улучшение Длительность или Частота')
  })
  it('looks up complete names case-insensitively while preserving unknown prose', () => {
    const names = referenceNameIndex([{ name: 'Beast of Burden', nameRu: 'Вьючный зверь' }])
    expect(localizedAuditSummary('Куплен «BEAST OF BURDEN», без оплаты', names)).toBe('Куплен «Вьючный зверь», без оплаты')
    expect(localizedAuditSummary('Мой Beast of Burden и «неизвестный»', names)).toBe('Мой Beast of Burden и «неизвестный»')
  })
  it('preserves default duration and omits explanations removed by localization', () => {
    expect(heroicMetaParts('По умолчанию до конца следующего хода')).toEqual({ value: 'По умолчанию до конца следующего хода', explanation: '' })
    expect(heroicMetaParts('incidental; свободное действие')).toEqual({ value: 'Свободное действие', explanation: '' })
    expect(heroicMetaParts('Два хода； дополнительное пояснение')).toEqual({ value: 'Два хода', explanation: 'дополнительное пояснение' })
  })
  it('opens a populated system on first visit but respects an explicit saved empty system', () => {
    expect(preferredLibrarySystem([{ system: 'realmsOfTerrinoth' }], [], null)).toBe('realmsOfTerrinoth')
    expect(preferredLibrarySystem([], [{ system: 'realmsOfTerrinoth' }], null)).toBe('realmsOfTerrinoth')
    expect(preferredLibrarySystem([{ system: 'realmsOfTerrinoth' }], [], 'genesysCore')).toBe('genesysCore')
    expect(preferredLibrarySystem([], [], null)).toBe('genesysCore')
  })
  it('stores valid library selections and ignores invalid stored values', () => {
    localStorage.removeItem('genesysforge.library-system')
    expect(readLibrarySystem()).toBeNull()
    writeLibrarySystem('realmsOfTerrinoth')
    expect(readLibrarySystem()).toBe('realmsOfTerrinoth')
    localStorage.setItem('genesysforge.library-system', 'other')
    expect(readLibrarySystem()).toBeNull()
    localStorage.removeItem('genesysforge.library-system')
  })
  it.each([['Лёгкая', 1], ['Средняя', 2], ['Сложная', 3], ['Очень сложная', 4], ['Daunting', 4], ['Авторское описание', 0]])('renders %s severity with %i difficulty symbols', (group, expected) => {
    expect(criticalSeverityDifficulty(group)).toBe(expected)
  })
})
