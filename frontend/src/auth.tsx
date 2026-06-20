import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api, setUnauthorizedHandler, tokenStorage } from './api/client'
import { AuthContext } from './auth-context'
import { captureReturnTo, clearReturnTo, restoreReturnTo } from './session'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(tokenStorage.get())
  const [sessionExpired, setSessionExpired] = useState(false)

  // Протухший/невалидный токен (401 от API) → сбрасываем сессию, запоминаем открытый
  // экран и показываем сообщение на логине, чтобы после входа вернуться на то же место.
  useEffect(() => {
    setUnauthorizedHandler(() => {
      captureReturnTo()
      setToken(null)
      setSessionExpired(true)
    })
    return () => setUnauthorizedHandler(null)
  }, [])

  // На загрузке без access-токена пробуем восстановить сессию по refresh-cookie.
  useEffect(() => {
    if (tokenStorage.get()) return
    let cancelled = false
    api.refresh()
      .then(auth => { if (!cancelled) { tokenStorage.set(auth.token); setToken(auth.token) } })
      .catch(() => { /* нет валидного refresh-токена — остаёмся на экране входа */ })
    return () => { cancelled = true }
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    const auth = await api.login(email, password)
    tokenStorage.set(auth.token)
    setSessionExpired(false)
    setToken(auth.token)
    restoreReturnTo() // вернуть на экран, с которого выкинуло по истечении сессии
  }, [])

  const register = useCallback(async (email: string, password: string, displayName: string) => {
    const auth = await api.register(email, password, displayName)
    tokenStorage.set(auth.token)
    setSessionExpired(false)
    setToken(auth.token)
    clearReturnTo() // новая регистрация начинает с чистого листа
  }, [])

  const loginWithGoogle = useCallback(async (idToken: string) => {
    const auth = await api.googleSignIn(idToken)
    tokenStorage.set(auth.token)
    setSessionExpired(false)
    setToken(auth.token)
  }, [])

  const logout = useCallback(() => {
    void api.logout() // отзыв семейства refresh-токенов на сервере + очистка cookie
    tokenStorage.clear()
    clearReturnTo()
    setSessionExpired(false) // обычный выход — не показываем «сессия истекла»
    setToken(null)
  }, [])

  const value = useMemo(
    () => ({ token, sessionExpired, login, register, loginWithGoogle, logout }),
    [token, sessionExpired, login, register, loginWithGoogle, logout])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
