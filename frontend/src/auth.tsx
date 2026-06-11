import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'
import { api, tokenStorage } from './api/client'

interface AuthState {
  token: string | null
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string, displayName: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthState | null>(null)

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

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth вне AuthProvider')
  return ctx
}
