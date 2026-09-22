import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { useAuthStore } from '../../features/auth/authStore'
import type { AuthResponse } from '../../features/auth/types'

/**
 * Cliente HTTP central de CalisApp.
 * - baseURL: '/api' en dev (proxy de Vite a http://localhost:5260) o
 *   VITE_API_URL en producción (ej. https://api.tudominio.com/api).
 * - Adjunta el access token JWT en cada request.
 * - Ante un 401 intenta renovar con el refresh token (una sola vez por request,
 *   con cola para requests concurrentes) y reintenta.
 */
const API_BASE = import.meta.env.VITE_API_URL ?? '/api'

export const apiClient = axios.create({
  baseURL: API_BASE,
  headers: { 'Content-Type': 'application/json' },
  timeout: 15000,
})

apiClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// Cola para evitar múltiples llamadas simultáneas a /auth/refresh
let refreshPromise: Promise<string> | null = null

async function refreshAccessToken(): Promise<string> {
  const { refreshToken, clear } = useAuthStore.getState()
  if (!refreshToken) {
    clear()
    throw new Error('No hay refresh token')
  }
  try {
    const { data } = await axios.post<AuthResponse>(`${API_BASE}/auth/refresh`, { refreshToken }, { timeout: 15000 })
    if (useAuthStore.getState().refreshToken !== refreshToken) throw new Error('La sesión cambió durante la renovación.')
    useAuthStore.getState().setSession(data)
    return data.accessToken
  } catch (error) {
    if (useAuthStore.getState().refreshToken === refreshToken && axios.isAxiosError(error) && error.response?.status === 401) clear()
    throw error
  }
}

/** Renovación única compartida por el arranque y los reintentos HTTP (incluido React StrictMode). */
export function refreshSession(): Promise<string> {
  refreshPromise ??= refreshAccessToken().finally(() => { refreshPromise = null })
  return refreshPromise
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as (InternalAxiosRequestConfig & { _retried?: boolean }) | undefined
    const isAuthEndpoint = original?.url?.includes('/auth/')

    if (error.response?.status === 401 && original && !original._retried && !isAuthEndpoint) {
      original._retried = true
      try {
        const newToken = await refreshSession()
        original.headers.Authorization = `Bearer ${newToken}`
        return apiClient(original)
      } catch {
        // sesión expirada: el guard de rutas redirige a /login
      }
    }
    return Promise.reject(error)
  },
)

/** Mensaje de error legible desde la API ({ message, code }) o genérico. */
export function apiErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { message?: string } | undefined
    if (data?.message) return data.message
    if (error.code === 'ERR_NETWORK') return 'Sin conexión con el servidor.'
    if (error.code === 'ECONNABORTED') return 'El servidor tardó demasiado en responder. Intenta de nuevo.'
  }
  return 'Ocurrió un error inesperado.'
}
