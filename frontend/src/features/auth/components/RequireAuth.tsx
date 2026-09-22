import { useEffect, useState } from 'react'
import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuthStore } from '../authStore'
import { apiErrorMessage, refreshSession } from '../../../shared/api/client'

/**
 * Guard de rutas privadas.
 * - Sin refresh token → /login.
 * - Con refresh token pero sin access token (recarga de la PWA) → renueva primero.
 */
export function RequireAuth() {
  const { accessToken, refreshToken } = useAuthStore()
  const location = useLocation()
  const [bootstrapError, setBootstrapError] = useState<unknown>(null)
  const [attempt, setAttempt] = useState(0)

  useEffect(() => {
    if (accessToken || !refreshToken) return
    let cancelled = false

    void refreshSession().catch(error => { if (!cancelled) setBootstrapError(error) })

    return () => {
      cancelled = true
    }
  }, [accessToken, refreshToken, attempt])

  if (!refreshToken) {
    return <Navigate to="/login" replace state={{ from: location }} />
  }

  if (!accessToken) {
    return (
      <div className="flex min-h-dvh flex-col items-center justify-center gap-4 p-6" role={bootstrapError ? 'alert' : 'status'}>
        <p className="text-muted">{bootstrapError ? apiErrorMessage(bootstrapError) : 'Restaurando sesión…'}</p>
        {bootstrapError ? <button className="min-h-11 rounded-xl bg-accent px-5 py-3 font-bold text-accent-foreground" onClick={() => { setBootstrapError(null); setAttempt(value => value + 1) }}>Reintentar conexión</button> : null}
      </div>
    )
  }

  return <Outlet />
}
