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
    [bonuses.woundBonus, 'к порогу ран'],
    [bonuses.strainBonus, 'к порогу стрейна'],
    [bonuses.soakBonus, 'к поглощению'],
    [bonuses.meleeDefenseBonus, 'к защите (ближней)'],
    [bonuses.rangedDefenseBonus, 'к защите (дальней)'],
  ]
  const sign = (v: number) => (v > 0 ? `+${v}` : String(v))
  return fields
    .filter(([perRank]) => perRank !== 0)
    .map(([perRank, label]) =>
      `${sign(perRank * ranks)} ${label}${ranks > 1 ? ` (${ranks} ранга × ${sign(perRank)})` : ''}`)
}
