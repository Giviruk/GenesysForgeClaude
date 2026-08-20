import { t } from '../i18n'

interface ErrorLike {
  status?: unknown
  reasonCode?: unknown
  message?: unknown
}

function readError(error: unknown): { status?: number; reasonCode?: string; message?: string } {
  const value: ErrorLike = typeof error === 'object' && error !== null ? error as ErrorLike : {}
  const message = error instanceof Error
    ? error.message
    : typeof value.message === 'string' ? value.message : undefined
  return {
    status: typeof value.status === 'number' ? value.status : undefined,
    reasonCode: typeof value.reasonCode === 'string' ? value.reasonCode : undefined,
    message,
  }
}

/**
 * Converts a failed complete-creation request into an instruction the player can act on.
 * The API reason code is preferred; message matching keeps the UI useful with older servers
 * that returned only `message`.
 */
export function formatCharacterCreationCompletionError(error: unknown): string {
  const { status, reasonCode, message } = readError(error)
  const code = reasonCode ?? (
    message && /личное название и происхождение/i.test(message)
        ? 'heroic.identity.incomplete'
        : message && /параметр героической способности/i.test(message)
          ? 'heroic.parameter.incomplete'
          : message && /героическ.*способност/i.test(message) && /выберите|сначала/i.test(message)
            ? 'heroic.ability.required'
          : undefined
  )

  switch (code) {
    case 'heroic.ability.required':
      return t(
        'Не удалось завершить создание: выберите героическую способность во вкладке «Героика».',
        'Character creation cannot be completed yet: choose a heroic ability in the “Heroic” tab.',
      )
    case 'heroic.identity.incomplete':
      return t(
        'Не удалось завершить создание: заполните личное название и происхождение героической способности во вкладке «Героика».',
        'Character creation cannot be completed yet: fill in the heroic ability’s personal name and origin in the “Heroic” tab.',
      )
    case 'heroic.parameter.incomplete':
      return t(
        'Не удалось завершить создание: выберите параметр героической способности во вкладке «Героика».',
        'Character creation cannot be completed yet: choose the heroic ability parameter in the “Heroic” tab.',
      )
    case 'heroic.weapon.upgrade_incomplete':
      return t(
        'Не удалось завершить создание: завершите выбор улучшения именного оружия во вкладке «Героика».',
        'Character creation cannot be completed yet: finish choosing the signature weapon upgrade in the “Heroic” tab.',
      )
    default:
      if (status === 401) {
        return t(
          'Сессия истекла. Войдите снова и повторите попытку завершения создания.',
          'Your session has expired. Sign in again and retry completing character creation.',
        )
      }
      if (status === 404) {
        return t(
          'Персонаж не найден. Обновите список персонажей и повторите попытку.',
          'The character was not found. Refresh the character list and try again.',
        )
      }
      if (status === 409) {
        return t(
          'Данные персонажа изменились. Обновите лист и повторите попытку.',
          'The character changed elsewhere. Refresh the sheet and try again.',
        )
      }
      return t(
        'Не удалось завершить создание персонажа. Проверьте обязательные поля во вкладке «Героика» и повторите попытку.',
        'Character creation could not be completed. Check the required fields in the “Heroic” tab and try again.',
      )
  }
}
