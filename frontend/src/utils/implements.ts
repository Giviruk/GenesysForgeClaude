import type { ImplementMaterial, ImplementSpec, ItemImplement, Spell } from '../api/types'

/**
 * Материалы магического инструмента (ROT-MAG-MAT-01) в порядке таблицы книги.
 * Дуб идёт первым как значение по умолчанию: он ничего не меняет.
 */
export const IMPLEMENT_MATERIALS: ImplementMaterial[] = ['oak', 'bone', 'hazel', 'willow', 'yew']

/**
 * Множитель цены материала. У кости, орешника и тиса — полтора по официальной errata; печатное
 * «вдвое дешевле» не используется, иначе редкий материал выходил бы дешевле дуба.
 * Источник истины — сервер: он считает по этой же таблице и списывает сам.
 */
export function implementPrice(basePrice: number, material: ImplementMaterial): number {
  const multiplier = material === 'willow' ? 2
    : material === 'oak' ? 1
      : 1.5
  return Math.ceil(basePrice * multiplier)
}

/** Сдвиг редкости материала; итог обрезается диапазоном 0…10. */
export function implementRarity(baseRarity: number, material: ImplementMaterial): number {
  const shift = material === 'bone' || material === 'willow' ? 2
    : material === 'hazel' || material === 'yew' ? 1
      : 0
  return Math.min(10, Math.max(0, baseRarity + shift))
}

/** Скидка инструмента на один эффект — чтобы в сборщике было видно, почему сложность ниже. */
export interface ImplementEffectDiscount {
  /** Код эффекта (английское имя). */
  code: string
  /** На сколько ступеней дешевле стал этот эффект. */
  reduction: number
}

/** Инструмент действует для этой проверки: у него либо нет своего навыка, либо он совпал. */
export const implementWorks = (implement: ImplementSpec, magicSkill: string) =>
  !implement.requiredMagicSkill
  || implement.requiredMagicSkill.toLowerCase() === magicSkill.toLowerCase()

/**
 * Какие из выбранных эффектов инструмент удешевляет (ROT-MAG-IMP-01). Правило повторяет серверное
 * один в один: посох — первую Дистанцию, скипетр — Ближний бой, музыкальный инструмент — Доп. цель,
 * икона — по единице с каждого эффекта, доступного только Вере, фолиант и палочка — свой выбор.
 *
 * Ненастроенный экземпляр не удешевляет ничего: пока ведущий не подтвердил выбор, бесплатного
 * эффекта у фолианта и палочки нет.
 */
export function implementDiscounts(
  implement: ItemImplement | null,
  effects: Spell[],
  magicSkill: string,
): ImplementEffectDiscount[] {
  if (!implement || implement.pending || !implementWorks(implement, magicSkill)) return []

  const discounts: ImplementEffectDiscount[] = []
  let firstUsed = false
  // Скидка даётся один раз на эффект: повторяемую Дистанцию инструмент удешевляет только
  // в первом добавлении, второе и третье стоят полную надбавку.
  const discounted = new Set<string>()
  for (const effect of effects) {
    const increase = effect.difficultyIncrease
    if (increase <= 0) continue
    if (discounted.has(effect.nameEn)) continue
    discounted.add(effect.nameEn)
    const named = implement.discountEffects.includes(effect.nameEn)
    let reduction = 0
    switch (implement.discount) {
      case 'namedEffects':
        if (named) reduction = increase
        break
      case 'firstNamedEffect':
        if (named && !firstUsed) { reduction = increase; firstUsed = true }
        break
      // Икона удешевляет, но не обнуляет: эффект за +2 всё ещё стоит +1.
      case 'restrictedSkillDiscount':
        if (effect.restrictedSkill
          && effect.restrictedSkill.toLowerCase() === implement.requiredMagicSkill.toLowerCase())
          reduction = Math.min(1, increase)
        break
      case 'chosenEffects':
        if (implement.chosenEffects.includes(effect.nameEn)) reduction = increase
        break
      default:
        break
    }
    if (reduction > 0) discounts.push({ code: effect.nameEn, reduction })
  }
  return discounts
}

/**
 * Итоговая сложность с инструментом. Инструмент удешевляет добавки, а не само действие, поэтому
 * ниже базовой сложности итог не опускается.
 */
export function implementDifficulty(
  baseDifficulty: number, raw: number, discounts: ImplementEffectDiscount[],
): number {
  const total = discounts.reduce((sum, d) => sum + d.reduction, 0)
  return Math.max(baseDifficulty, raw - total)
}

/**
 * Итоговая сложность набора эффектов с учётом инструмента — одной функцией, чтобы потолок и
 * показанное число считались одинаково. Раньше потолок сравнивался с сырой суммой надбавок, и
 * эффект, который инструмент делает бесплатным, всё равно упирался в предел.
 */
export function effectiveSpellDifficulty(
  baseDifficulty: number,
  effects: Spell[],
  implement: ItemImplement | null,
  magicSkill: string,
): number {
  const raw = baseDifficulty + effects.reduce((sum, e) => sum + e.difficultyIncrease, 0)
  return implementDifficulty(
    baseDifficulty, raw, implementDiscounts(implement, effects, magicSkill))
}
