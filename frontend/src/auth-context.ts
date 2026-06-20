import { createContext, useContext } from 'react'

export interface AuthState {
  token: string | null
  /** true, если сессия завершилась из-за истёкшего/невалидного токена (401), а не обычного выхода. */
  sessionExpired: boolean
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string, displayName: string) => Promise<void>
  /** Вход через Google: idToken от Google Identity Services обменивается на сессию. */
  loginWithGoogle: (idToken: string) => Promise<void>
  logout: () => void
}

export const AuthContext = createContext<AuthState | null>(null)

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth вне AuthProvider')
  return ctx
}
