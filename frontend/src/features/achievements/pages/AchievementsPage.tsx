import { useQuery } from '@tanstack/react-query'
import { getAchievements, getMyAchievements } from '../api/achievementsApi'
import { apiErrorMessage } from '../../../shared/api/client'
import { ErrorState, LoadingState } from '../../../shared/components/QueryStates'

/**
 * Catálogo de logros: los obtenidos aparecen destacados, el resto bloqueados.
 */
export default function AchievementsPage() {
  const { data: catalog, isLoading: catalogLoading, error } = useQuery({
    queryKey: ['achievements'],
    queryFn: getAchievements,
  })
  const { data: mine, isLoading: mineLoading } = useQuery({
    queryKey: ['me', 'achievements'],
    queryFn: getMyAchievements,
  })

  if (catalogLoading || mineLoading) return <LoadingState />
  if (error) return <ErrorState message={apiErrorMessage(error)} />

  const earnedIds = new Set(mine?.map((a) => a.achievementId) ?? [])

  return (
    <section className="p-4">
      <header className="mb-6 pt-2">
        <h1 className="text-2xl font-black">Logros</h1>
        <p className="mt-1 text-sm text-muted">
          {earnedIds.size} de {catalog?.length ?? 0} desbloqueados.
        </p>
      </header>

      <ul className="space-y-3">
        {catalog?.map((achievement) => {
          const earned = earnedIds.has(achievement.id)
          return (
            <li
              key={achievement.id}
              className={`flex items-center gap-4 rounded-2xl border p-4 ${
                earned
                  ? 'border-accent/40 bg-accent/5'
                  : 'border-white/10 bg-surface opacity-60'
              }`}
            >
              <span
                aria-hidden="true"
                className={`flex h-14 w-14 shrink-0 items-center justify-center rounded-full text-2xl ${
                  earned ? 'bg-accent/15' : 'bg-surface-raised grayscale'
                }`}
              >
                {earned ? achievement.icon : '🔒'}
              </span>
              <div className="min-w-0 flex-1">
                <p className="font-bold">{achievement.name}</p>
                <p className="text-sm text-muted">{achievement.description}</p>
              </div>
              {earned && <span className="shrink-0 text-xs font-bold text-accent">OBTENIDO</span>}
            </li>
          )
        })}
      </ul>
    </section>
  )
}
