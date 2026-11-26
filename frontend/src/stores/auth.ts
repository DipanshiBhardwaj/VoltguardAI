import { defineStore } from 'pinia'
import { ref } from 'vue'
import { api, setToken, getToken } from '@/api/client'
import type { CurrentUser } from '@/api/types'

// Auth store — holds the current user in memory and the JWT in localStorage.
export const useAuthStore = defineStore('auth', () => {
  const user = ref<CurrentUser | null>(null)
  const isReady = ref(false) // false until the first session probe finishes

  async function login(email: string, password: string) {
    const { data } = await api.post<CurrentUser>('/api/v1/auth/login', { email, password })
    setToken(data.token)
    user.value = data
  }

  async function register(email: string, password: string, fullName: string) {
    const { data } = await api.post<CurrentUser>('/api/v1/auth/register', { email, password, fullName })
    setToken(data.token)
    user.value = data
  }

  function logout() {
    setToken(null)
    user.value = null
  }

  // Called once on startup: if a token is stored, confirm it is still valid.
  async function probeSession() {
    if (!getToken()) {
      isReady.value = true
      return
    }
    try {
      const { data } = await api.get<CurrentUser>('/api/v1/auth/me')
      user.value = data
    } catch {
      logout()
    } finally {
      isReady.value = true
    }
  }

  return { user, isReady, login, register, logout, probeSession }
})
