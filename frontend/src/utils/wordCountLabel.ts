import type { Lang } from '../i18n'
import { countLabel } from './countLabel'

export function wordCountLabel(count: number, language: Lang): string {
  return countLabel(count, language === 'en' ? ['word', 'words', 'words'] : ['слово', 'слова', 'слов'], language)
}
