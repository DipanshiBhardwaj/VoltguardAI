import axios from 'axios'

// Shared Axios instance. The JWT is kept in localStorage and attached to every
// request; Vite's dev proxy forwards `/api` to the .NET backend, so no baseURL
// is needed here.
const TOKEN_KEY = 'voltaguard.token'

export const api = axios.create({
  headers: {
    'Content-Type': 'application/json',
  },
})

export function setToken(token: string | null) {
  if (token) {
    localStorage.setItem(TOKEN_KEY, token)
  } else {
    localStorage.removeItem(TOKEN_KEY)
  }
}

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY)
}

api.interceptors.request.use((config) => {
  const token = getToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})
