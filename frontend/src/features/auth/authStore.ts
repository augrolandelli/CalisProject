import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { AuthResponse, UserDto } from './types'

interface AuthState {
  user: UserDto | null
  /** Solo en memoria — nunca se persiste (spec §10). */
  accessToken: string | null
  /** Persistido para poder renovar la sesión al recargar la PWA. */
  refreshToken: string | null
  setSession: (session: AuthResponse) => void
  setAccessToken: (token: string) => void
  clear: () => void
}

/**
 * Store de autenticación.
 * - accessToken en memoria (se renueva via refresh token al recargar).
 * - refreshToken + user persistidos en localStorage.
 * TODO (endurecimiento futuro): migrar refresh token a cookie HttpOnly (spec §10).
 */
export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      accessToken: null,
      refreshToken: null,
      setSession: (session) =>
        set({
          user: session.user,
          accessToken: session.accessToken,
          refreshToken: session.refreshToken,
        }),
      setAccessToken: (token) => set({ accessToken: token }),
      clear: () => set({ user: null, accessToken: null, refreshToken: null }),
    }),
    {
      name: 'calisapp-auth',
      partialize: (state) => ({ user: state.user, refreshToken: state.refreshToken }),
    },
  ),
)
