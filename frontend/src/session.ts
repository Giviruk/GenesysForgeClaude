/**
 * Сохранение «куда вернуться» при автоматическом разлогине (истёкшая сессия, 401).
 * Хранится в sessionStorage, чтобы переживать перезагрузку экрана авторизации,
 * но не утекать между вкладками/днями. Не зависит от роутера: оперирует чистым
 * путём (`pathname`), поэтому работает и до, и после внедрения клиентского роутинга.
 */

const RETURN_TO_KEY = 'genesysforge.returnTo'

/** Пути, на которые не имеет смысла «возвращаться» (экран авторизации/корень). */
function isMeaningful(path: string): boolean {
  return path !== '' && path !== '/' && path !== '/login' && path !== '/register'
}

/** Запомнить текущий (или указанный) путь как точку возврата после повторного входа. */
export function captureReturnTo(path: string = window.location.pathname): void {
  if (isMeaningful(path)) {
    try { sessionStorage.setItem(RETURN_TO_KEY, path) } catch { /* приватный режим без storage */ }
  }
}

/** Посмотреть сохранённую точку возврата, не очищая её (для подсказки на экране входа). */
export function peekReturnTo(): string | null {
  try {
    const value = sessionStorage.getItem(RETURN_TO_KEY)
    return isMeaningful(value ?? '') ? value : null
  } catch {
    return null
  }
}

/** Прочитать и очистить сохранённую точку возврата (если есть). */
export function consumeReturnTo(): string | null {
  try {
    const value = sessionStorage.getItem(RETURN_TO_KEY)
    if (value) sessionStorage.removeItem(RETURN_TO_KEY)
    return isMeaningful(value ?? '') ? value : null
  } catch {
    return null
  }
}

/** Забыть точку возврата (обычный выход не должен возвращать на прежний экран). */
export function clearReturnTo(): void {
  try { sessionStorage.removeItem(RETURN_TO_KEY) } catch { /* нет storage — нечего чистить */ }
}

/**
 * После успешного входа вернуть пользователя на сохранённый путь.
 * Меняет адрес через History API и шлёт popstate, чтобы клиентский роутер
 * (если он подключён) перерисовался; без роутера просто корректирует адресную строку.
 */
export function restoreReturnTo(): void {
  const to = consumeReturnTo()
  if (to && to !== window.location.pathname) {
    window.history.replaceState(null, '', to)
    window.dispatchEvent(new PopStateEvent('popstate'))
  }
}
