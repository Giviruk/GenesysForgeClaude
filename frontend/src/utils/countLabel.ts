import { lang, type Lang } from '../i18n'

export type CountForms = readonly [one: string, few: string, many: string]
const plurals = { ru: new Intl.PluralRules('ru'), en: new Intl.PluralRules('en') }

export function countLabel(count: number, forms: CountForms, language: Lang = lang): string {
  const category = plurals[language].select(Math.abs(count))
  const word = forms[category === 'one' ? 0 : category === 'few' ? 1 : 2]
  return `${count} ${word}`
}
