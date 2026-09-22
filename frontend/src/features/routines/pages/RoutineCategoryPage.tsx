import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { getRutines } from '../api/routinesApi'
import { apiErrorMessage } from '../../../shared/api/client'
import { DifficultyBadge } from '../../../shared/components/DifficultyBadge'
import { EmptyState, ErrorState, LoadingState } from '../../../shared/components/QueryStates'

const difficulties = [
  { value: '', label: 'Todas' },
  { value: 'basica', label: 'Básica' },
  { value: 'intermedia', label: 'Intermedia' },
  { value: 'avanzada', label: 'Avanzada' },
] as const

/**
 * Rutinas de una categoría con filtro de dificultad.
 * Diseño según mockup rutines_by_category.png.
 */
export default function RoutineCategoryPage() {
  const { categoryId } = useParams<{ categoryId: string }>()
  const [difficulty, setDifficulty] = useState('')

  const { data: rutines, isLoading, error } = useQuery({
    queryKey: ['rutines', categoryId],
    queryFn: () => getRutines({ categoryId: Number(categoryId) }),
    enabled: !!categoryId,
  })

  const filtered = useMemo(() => {
    if (!rutines) return []
    if (!difficulty) return rutines
    return rutines.filter((r) => r.difficulty.toLowerCase() === difficulty)
  }, [rutines, difficulty])

  const categoryName = rutines?.[0]?.categoryName ?? 'Rutinas'

  return (
    <section className="p-4">
      <header className="mb-4 flex items-center gap-3 pt-2">
        <Link to="/routines" aria-label="Volver" className="text-2xl text-muted hover:text-foreground">
          ←
        </Link>
        <h1 className="text-2xl font-black">Rutinas {categoryName}</h1>
      </header>

      <div className="mb-5 flex gap-2 overflow-x-auto pb-1" role="group" aria-label="Filtrar por dificultad">
        {difficulties.map((d) => (
          <button
            key={d.value}
            onClick={() => setDifficulty(d.value)}
            aria-pressed={difficulty === d.value}
            className={`min-h-11 shrink-0 rounded-full px-4 text-sm font-bold transition-colors ${
              difficulty === d.value ? 'bg-accent text-accent-foreground' : 'bg-surface-raised text-muted'
            }`}
          >
            {d.label}
          </button>
        ))}
      </div>

      {isLoading && <LoadingState />}
      {error && <ErrorState message={apiErrorMessage(error)} />}
      {!isLoading && !error && filtered.length === 0 && (
        <EmptyState message="No hay rutinas con estos filtros." />
      )}

      <ul className="space-y-4">
        {filtered.map((rutine) => (
          <li key={rutine.id}>
            <Link
              to={`/routines/${rutine.id}`}
              className="block rounded-3xl border border-white/10 bg-surface p-5 transition-colors hover:border-accent/50"
            >
              <div className="mb-2 flex items-start justify-between gap-3">
                <h2 className="text-lg font-bold">{rutine.title}</h2>
                <DifficultyBadge difficulty={rutine.difficulty} />
              </div>
              <p className="mb-3 line-clamp-2 text-sm text-muted">{rutine.description}</p>
              <p className="text-sm text-muted">
                <span aria-hidden="true">🕒</span> {rutine.duration}
              </p>
            </Link>
          </li>
        ))}
      </ul>
    </section>
  )
}
