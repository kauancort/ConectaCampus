import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { ApiError, apiRequest, type Student } from './api'

interface AuthContextValue {
  token: string | null
  student: Student | null
  signIn: (token: string, student: Student) => void
  signOut: () => void
  api: <T,>(path: string, init?: RequestInit) => Promise<T>
  refreshProfile: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

function storedStudent(): Student | null {
  try {
    const value = localStorage.getItem('ac-student')
    return value ? JSON.parse(value) as Student : null
  } catch {
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => localStorage.getItem('ac-token'))
  const [student, setStudent] = useState<Student | null>(() => localStorage.getItem('ac-token') ? storedStudent() : null)

  const signOut = useCallback(() => {
    localStorage.removeItem('ac-token')
    localStorage.removeItem('ac-student')
    setToken(null)
    setStudent(null)
  }, [])

  const signIn = useCallback((accessToken: string, profile: Student) => {
    localStorage.setItem('ac-token', accessToken)
    localStorage.setItem('ac-student', JSON.stringify(profile))
    setToken(accessToken)
    setStudent(profile)
  }, [])

  const api = useCallback(async <T,>(path: string, init?: RequestInit) => {
    try {
      return await apiRequest<T>(path, init, token)
    } catch (error) {
      if (error instanceof ApiError && error.status === 401 && token) signOut()
      throw error
    }
  }, [token, signOut])

  const refreshProfile = useCallback(async () => {
    if (!token) return
    const profile = await api<Student>('/api/auth/me')
    setStudent(profile)
    localStorage.setItem('ac-student', JSON.stringify(profile))
  }, [api, token])

  useEffect(() => { if (token) void refreshProfile().catch(() => {}) }, [token, refreshProfile])
  const value = useMemo(() => ({ token, student, signIn, signOut, api, refreshProfile }), [token, student, signIn, signOut, api, refreshProfile])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used inside AuthProvider')
  return context
}
