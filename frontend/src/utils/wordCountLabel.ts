import type { Lang } from '../i18n'

const russianPlural = new Intl.PluralRules('ru')

export function wordCountLabel(count: number, language: Lang): string {
  if (language === 'en') return `${count} ${count === 1 ? 'word' : 'words'}`
  const plural = russianPlural.select(count)
  return `${count} ${plural === 'one' ? 'слово' : plural === 'few' ? 'слова' : 'слов'}`
}
