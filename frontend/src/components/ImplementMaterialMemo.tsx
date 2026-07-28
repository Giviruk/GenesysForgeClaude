import type { ImplementMaterial } from '../api/types'
import {
  CURRENCY_LABEL, IMPLEMENT_MATERIAL_LABELS, IMPLEMENT_MATERIAL_TRIGGERS,
} from '../utils/labels'
import { implementPrice, implementRarity } from '../utils/implements'
import { InfoTip } from './InfoTip'
import { t } from '../i18n'

/**
 * Памятка по материалу магического инструмента (ROT-MAG-MAT-01).
 *
 * Свойство материала — единственная часть правила, которую приложение не считает: срабатывание
 * привязано к результату проверки и к счётчику «раз за проверку», а рантайма столкновения нет.
 * Раз считать его некому, игрок обязан прочитать правило за столом — значит, оно должно быть
 * не только в магазине, но и там, где вещью пользуются.
 */
export function ImplementMaterialMemo({ material, basePrice, baseRarity }: {
  material: ImplementMaterial
  /** Цена записи каталога — чтобы показать, во что материал её превратил. */
  basePrice?: number
  baseRarity?: number
}) {
  const label = IMPLEMENT_MATERIAL_LABELS[material]
  return (
    <InfoTip label="?" title={t(`Материал: ${label}`, `Material: ${label}`)}>
      <ul>
        <li>{IMPLEMENT_MATERIAL_TRIGGERS[material]}</li>
        {material !== 'oak' && basePrice != null && baseRarity != null && (
          <li>
            {t('Цена', 'Price')} {basePrice} → <strong>{implementPrice(basePrice, material)}</strong>
            {' '}{CURRENCY_LABEL}, {t('редкость', 'rarity')} {baseRarity} →{' '}
            <strong>{implementRarity(baseRarity, material)}</strong>.
            {' '}{t('Полуторный множитель — официальная errata, а не печатное «вдвое дешевле».',
              'The ×1.5 multiplier is official errata, not the printed “half price”.')}
          </li>
        )}
        {material !== 'oak' && (
          <li>
            {t('Приложение это срабатывание не считает: отмечайте его за столом. Материал выбран при изготовлении и не меняется.',
              'The app does not resolve this trigger — track it at the table. The material is chosen when the implement is made and never changes.')}
          </li>
        )}
      </ul>
    </InfoTip>
  )
}
