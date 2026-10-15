import { expect, it, vi } from 'vitest'
import { signatureWeaponDamage } from './designPresentation'
const locale = vi.hoisted(() => ({ ru: true }))
vi.mock('../i18n', () => ({ lang: 'ru', t: (ru: unknown, en: unknown) => locale.ru ? ru : en }))

it('formats structured and legacy damage in RU without changing fixed damage', () => {
  locale.ru = true
  expect(signatureWeaponDamage({ damage: 'Brawn + 2', damageKind: 'brawnPlus', damageValue: 2 })).toBe('Сила +2')
  expect(signatureWeaponDamage({ damage: 'Brawn + 2' })).toBe('Сила +2')
  expect(signatureWeaponDamage({ damage: '7', damageKind: 'fixed', damageValue: 7 })).toBe('7')
})
it('formats the same structured value in English', () => {
  locale.ru = false
  expect(signatureWeaponDamage({ damage: 'Brawn + 2', damageKind: 'brawnPlus', damageValue: 2 })).toBe('Brawn +2')
})
