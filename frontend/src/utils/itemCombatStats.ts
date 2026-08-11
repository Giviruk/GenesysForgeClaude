import type { ItemDef } from '../api/types'

/**
 * Боевые значения каталожного оружия. Структурный профиль — источник истины; строковые поля
 * остаются запасным вариантом для старого и пользовательского контента.
 */
export function catalogWeaponStats(item: ItemDef): { damage: string; crit: string } {
  const profiles = item.attackProfiles ?? []
  const profile = profiles.find(value => value.isDefault) ?? profiles[0]
  if (profile) {
    return {
      damage: `${profile.damageKind === 'brawnPlus' ? '+' : ''}${profile.damageValue}`,
      crit: String(profile.crit),
    }
  }
  return {
    damage: item.damage?.trim() || '—',
    crit: item.crit?.trim() || '—',
  }
}
