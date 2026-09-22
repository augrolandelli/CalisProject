import { Navigate, Outlet } from 'react-router-dom'
import { useAuthStore } from '../authStore'

/**
 * Guard por rol: solo deja pasar si el usuario tiene uno de los roles dados.
 * La autorización real la valida siempre el servidor; esto es solo UX.
 */
export function RequireRole({ roles }: { roles: string[] }) {
  const user = useAuthStore((s) => s.user)

  if (!user || !roles.includes(user.role)) {
    return <Navigate to="/" replace />
  }

  return <Outlet />
}
