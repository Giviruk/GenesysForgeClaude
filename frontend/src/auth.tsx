import { useCallback, useMemo, useState, type ReactNode } from 'react'
import { api, tokenStorage } from './api/client'
import { AuthContext } from './auth-context'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(tokenStorage.get())

  const login = useCallback(async (email: string, password: string) => {
    const auth = await api.login(email, password)
    tokenStorage.set(auth.token)
    setToken(auth.token)
  }, [])

  const register = useCallback(async (email: string, password: string, displayName: string) => {
    const auth = await api.register(email, password, displayName)
    tokenStorage.set(auth.token)
    setToken(auth.token)
  }, [])

  const logout = useCallback(() => {
    tokenStorage.clear()
    setToken(null)
  }, [])

  const value = useMemo(() => ({ token, login, register, logout }), [token, login, register, logout])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
