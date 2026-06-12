/**
 * Клиентская проверка правила пирамиды талантов (зеркало серверной логики):
 * после покупки таланта тира N талантов каждого более низкого тира должно быть
 * строго больше, чем талантов следующего тира.
 */
export function canPurchaseTier(tierCounts: Record<string, number>, tierToBuy: number): boolean {
  if (tierToBuy < 1 || tierToBuy > 5) return false
  const counts = [0, 0, 0, 0, 0, 0]
  for (let tier = 1; tier <= 5; tier++) counts[tier] = tierCounts[String(tier)] ?? 0
  counts[tierToBuy]++
  for (let tier = 2; tier <= 5; tier++) {
    if (counts[tier] > 0 && counts[tier - 1] < counts[tier] + 1) return false
  }
  return true
}

/**
 * Обратное правило: талант тира N можно вернуть, только если после удаления
 * пирамида остаётся корректной (зеркало серверной логики).
 */
export function canRemoveTier(tierCounts: Record<string, number>, tierToRemove: number): boolean {
  if (tierToRemove < 1 || tierToRemove > 5) return false
  const counts = [0, 0, 0, 0, 0, 0]
  for (let tier = 1; tier <= 5; tier++) counts[tier] = tierCounts[String(tier)] ?? 0
  if (counts[tierToRemove] === 0) return false
  counts[tierToRemove]--
  for (let tier = 2; tier <= 5; tier++) {
    if (counts[tier] > 0 && counts[tier - 1] < counts[tier] + 1) return false
  }
  return true
}
