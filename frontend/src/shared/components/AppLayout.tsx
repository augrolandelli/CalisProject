import { Outlet } from 'react-router-dom'
import { BottomNav } from './BottomNav'
import { OfflineNotice } from '../../features/community/components/CommunityUI'

/**
 * Layout principal de la app (alumno): contenido + bottom nav.
 * El panel admin tendra su propio layout con sidebar (Fase 5).
 */
export function AppLayout() {
  return (
    <div className="mx-auto min-h-dvh max-w-md bg-background">
      <main className="pb-20">
        <OfflineNotice />
        <Outlet />
      </main>
      <BottomNav />
    </div>
  )
}
