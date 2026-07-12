import { t } from '../i18n'

export interface TalentBonuses {
  woundBonus: number
  strainBonus: number
  soakBonus: number
  meleeDefenseBonus: number
  rangedDefenseBonus: number
}

/**
 * Человекочитаемая сводка пассивных бонусов таланта с учётом рангов:
 * «+4 к порогу ран (2 ранга × +2)».
 */
export function talentBonusSummary(bonuses: TalentBonuses, ranks: number): string[] {
  const fields: [number, string][] = [
    [bonuses.woundBonus, t('к порогу ран', 'wound threshold')],
    [bonuses.strainBonus, t('к порогу усталости', 'strain threshold')],
    [bonuses.soakBonus, t('к поглощению', 'soak')],
    [bonuses.meleeDefenseBonus, t('к защите (ближней)', 'melee defense')],
    [bonuses.rangedDefenseBonus, t('к защите (дальней)', 'ranged defense')],
  ]
  const sign = (v: number) => (v > 0 ? `+${v}` : String(v))
  return fields
    .filter(([perRank]) => perRank !== 0)
    .map(([perRank, label]) =>
      `${sign(perRank * ranks)} ${label}${ranks > 1 ? t(` (${ranks} ранга × ${sign(perRank)})`, ` (${ranks} ranks × ${sign(perRank)})`) : ''}`)
}
