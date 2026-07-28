/** Скидка самостоятельного ремонта: 10 % за каждое чистое преимущество (GEN-EQP-DMG-01). */
export const DISCOUNT_PERCENT_PER_ADVANTAGE = 10

/**
 * Стоимость материалов со скидкой за чистые преимущества. Источник истины — сервер: он считает
 * по этой же формуле и списывает сам. Здесь она повторена, чтобы на кнопке стояла ровно та сумма,
 * которую спишут, — как и остальная арифметика витрины.
 *
 * Округление вверх на каждом шаге: сервер сначала округляет долю цены, потом применяет скидку
 * к уже округлённой сумме, поэтому оба считают одинаково.
 */
export function discountedCost(baseCost: number, netAdvantages: number): number {
  const discount = Math.min(100, Math.max(0, netAdvantages) * DISCOUNT_PERCENT_PER_ADVANTAGE)
  return Math.ceil(baseCost * (100 - discount) / 100)
}
