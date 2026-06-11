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
