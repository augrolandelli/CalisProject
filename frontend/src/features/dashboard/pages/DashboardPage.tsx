import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { useAuthStore } from '../../auth/authStore'
import { getRutines } from '../../routines/api/routinesApi'
import { getCategories } from '../../exercises/api/exercisesApi'
import { getSessions } from '../../classes/api/classesApi'
import { getMyAchievements } from '../../achievements/api/achievementsApi'
import { DifficultyBadge } from '../../../shared/components/DifficultyBadge'
import { ErrorState, LoadingState } from '../../../shared/components/QueryStates'
import { apiErrorMessage } from '../../../shared/api/client'

/**
 * Dashboard: saludo, próxima clase en vivo, categorías, logros recientes
 * y rutinas destacadas. Diseño según mockup dashboard.png.
 */
export default function DashboardPage() {
  const { user } = useAuthStore()
  const firstName = user?.fullName.split(' ')[0] ?? 'Atleta'

  const { data: rutines, isLoading, error } = useQuery({
    queryKey: ['rutines', 'all'],
    queryFn: () => getRutines(),
  })
  const { data: categories } = useQuery({ queryKey: ['categories'], queryFn: getCategories })
  const { data: sessions } = useQuery({ queryKey: ['sessions', 'all'], queryFn: () => getSessions() })
  const { data: achievements } = useQuery({ queryKey: ['me', 'achievements'], queryFn: getMyAchievements })

  const nextSession = sessions
    ?.filter((s) => new Date(s.date) > new Date())
    .sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime())[0]

  const recentAchievements = achievements?.slice(0, 4) ?? []

  return (
    <section className="p-4">
      <header className="mb-6 flex items-center justify-between pt-2">
        <div className="flex items-center gap-3">
          <Link
            to="/profile"
            aria-label="Ir al perfil"
            className="flex h-12 w-12 items-center justify-center rounded-full border-2 border-accent bg-surface font-black text-accent"
          >
            {firstName[0]}
          </Link>
          <div>
            <h1 className="text-2xl font-black">¡Hola, {firstName}!</h1>
            <p className="text-sm text-success">¿Listo para entrenar?</p>
          </div>
        </div>
        <div className="flex gap-2">
          {user?.role === 'Admin' && (
            <Link
              to="/admin/users"
              className="flex min-h-11 items-center rounded-full border border-accent/40 px-4 text-sm font-bold text-accent"
            >
              Admin
            </Link>
          )}
        </div>
      </header>

      {nextSession ? (
        <Link
          to={`/classes/${nextSession.id}`}
          className="mb-6 block rounded-3xl border border-accent/30 bg-surface p-5"
        >
          <div className="mb-2 flex items-center justify-between">
            <p className="text-xs font-bold tracking-wider text-success uppercase">
              ● Próxima clase en vivo
            </p>
            <span className="rounded-full bg-success/15 px-2.5 py-1 text-xs font-bold text-success">
              {nextSession.availableSpots > 0
                ? `${nextSession.availableSpots} lugares`
                : 'Llena'}
            </span>
          </div>
          <p className="text-xl font-black">{nextSession.title}</p>
          <p className="mt-1 text-sm text-muted">
            📅{' '}
            {new Date(nextSession.date).toLocaleDateString('es-AR', {
              weekday: 'long',
              day: 'numeric',
              month: 'long',
            })}{' '}
            · 🕒{' '}
            {new Date(nextSession.date).toLocaleTimeString('es-AR', {
              hour: '2-digit',
              minute: '2-digit',
            })}
          </p>
        </Link>
      ) : (
        <div className="mb-6 rounded-3xl border border-white/10 bg-surface p-5">
          <p className="text-xs font-bold tracking-wider text-accent uppercase">Tu plan</p>
          <p className="mt-1 font-bold">
            Rol {user?.role === 'Guerrero' ? 'Guerrero (gratis)' : user?.role}
          </p>
          <p className="mt-1 text-sm text-muted">No hay clases programadas próximamente.</p>
        </div>
      )}

      <h2 className="mb-3 text-lg font-bold">Categorías</h2>
      <div className="mb-6 flex gap-2 overflow-x-auto pb-1">
        {categories?.map((category) => (
          <Link
            key={category.id}
            to={`/exercises/category/${category.id}`}
            className="shrink-0 rounded-full bg-surface-raised px-4 py-2.5 text-sm font-bold text-foreground"
          >
            {category.name}
          </Link>
        ))}
      </div>

      <div className="mb-3 flex items-center justify-between">
        <h2 className="text-lg font-bold">Logros recientes</h2>
        <Link to="/achievements" className="text-sm font-bold text-accent">
          Ver todos →
        </Link>
      </div>
      {recentAchievements.length === 0 ? (
        <p className="mb-6 text-sm text-muted">
          Los logros se ganan en las clases en vivo. ¡A por el primero!
        </p>
      ) : (
        <ul className="mb-6 flex gap-3 overflow-x-auto pb-1">
          {recentAchievements.map((a) => (
            <li
              key={a.id}
              className="flex w-28 shrink-0 flex-col items-center rounded-2xl border border-accent/30 bg-accent/5 p-3 text-center"
            >
              <span aria-hidden="true" className="text-2xl">{a.icon}</span>
              <p className="mt-1 line-clamp-2 text-xs font-bold">{a.name}</p>
            </li>
          ))}
        </ul>
      )}

      <h2 className="mb-3 text-lg font-bold">Rutinas destacadas</h2>
      {isLoading && <LoadingState />}
      {error && <ErrorState message={apiErrorMessage(error)} />}

      <ul className="space-y-4">
        {rutines?.map((rutine) => (
          <li key={rutine.id}>
            <Link
              to={`/routines/${rutine.id}`}
              className="block rounded-3xl border border-white/10 bg-surface p-5 transition-colors hover:border-accent/50"
            >
              <div className="mb-2 flex items-start justify-between gap-3">
                <h3 className="font-bold">{rutine.title}</h3>
                <DifficultyBadge difficulty={rutine.difficulty} />
              </div>
              <p className="line-clamp-2 text-sm text-muted">{rutine.description}</p>
              <p className="mt-2 text-sm text-muted">
                {rutine.duration} · {rutine.categoryName}
              </p>
            </Link>
          </li>
        ))}
      </ul>
    </section>
  )
}
