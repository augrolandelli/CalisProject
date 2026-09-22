import { NavLink } from 'react-router-dom'
import { CommunityIcon } from '../../features/community/components/CommunityUI'

const tabs = [
  { to: '/', label: 'Home', icon: '⌂' },
  { to: '/classes', label: 'Clases', icon: '▣' },
  { to: '/exercises', label: 'Ejercicios', icon: '⚒' },
  { to: '/routines', label: 'Rutinas', icon: '☰' },
  { to: '/community', label: 'Comunidad', icon: 'community' },
] as const

/**
 * Navegación inferior mobile-first, incluida la comunidad (Fase 4).
 * En desktop el layout usara sidebar (Fase 1+).
 */
export function BottomNav() {
  return (
    <nav
      aria-label="Navegación principal"
      className="fixed inset-x-0 bottom-0 z-50 border-t border-white/10 bg-surface pb-[env(safe-area-inset-bottom)]"
    >
      <ul className="mx-auto flex max-w-md items-stretch justify-between">
        {tabs.map((tab) => (
          <li key={tab.to} className="flex-1">
            <NavLink
              to={tab.to}
              end={tab.to === '/'}
              className={({ isActive }) =>
                `flex min-h-14 flex-col items-center justify-center gap-1 text-xs font-medium transition-colors ${
                  isActive ? 'text-accent' : 'text-muted hover:text-foreground'
                }`
              }
            >
              <span aria-hidden="true" className="text-lg leading-none">
                {tab.icon === 'community' ? <CommunityIcon name="community" /> : tab.icon}
              </span>
              {tab.label}
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  )
}
