import { createContext, useContext, useMemo, useState, type ReactNode } from 'react'
import type { AuthResponse, LoginRequest } from '../types/auth'
import { loginRequest } from '../services/authService'

const STORAGE_KEY = 'portfoliohub.auth'

type AuthContextValue = {
  user: AuthResponse | null
  isAuthenticated: boolean
  login: (request: LoginRequest) => Promise<AuthResponse>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

function readStoredAuth(): AuthResponse | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    const value = JSON.parse(raw) as AuthResponse
    if (!value.token || !value.tokenExpiresAt || new Date(value.tokenExpiresAt) <= new Date()) {
      localStorage.removeItem(STORAGE_KEY)
      localStorage.removeItem('token')
      return null
    }
    localStorage.setItem('token', value.token)
    return value
  } catch {
    localStorage.removeItem(STORAGE_KEY)
    localStorage.removeItem('token')
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthResponse | null>(readStoredAuth)

  const value = useMemo<AuthContextValue>(() => ({
    user,
    isAuthenticated: Boolean(user?.token),
    login: async (request) => {
      const result = await loginRequest(request)
      if (!result.token) throw new Error(result.message || '登入失敗，伺服器未回傳 Token')
      localStorage.setItem('token', result.token)
      localStorage.setItem(STORAGE_KEY, JSON.stringify(result))
      setUser(result)
      return result
    },
    logout: () => {
      localStorage.removeItem('token')
      localStorage.removeItem(STORAGE_KEY)
      setUser(null)
    },
  }), [user])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth 必須在 AuthProvider 內使用')
  return context
}
