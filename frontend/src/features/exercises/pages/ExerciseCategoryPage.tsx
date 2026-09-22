import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { getVideos } from '../api/exercisesApi'
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
 * Videos de ejercicios por categoría con filtro de dificultad y búsqueda local.
 * Diseño según mockup videosexercises_by_category.png.
 */
export default function ExerciseCategoryPage() {
  const { categoryId } = useParams<{ categoryId: string }>()
  const [difficulty, setDifficulty] = useState('')
  const [search, setSearch] = useState('')

  const { data: videos, isLoading, error } = useQuery({
    queryKey: ['videos', categoryId],
    queryFn: () => getVideos({ categoryId: Number(categoryId) }),
    enabled: !!categoryId,
  })

  const filtered = useMemo(() => {
    if (!videos) return []
    return videos.filter((video) => {
      const matchesDifficulty = !difficulty || video.difficulty.toLowerCase() === difficulty
      const term = search.trim().toLowerCase()
      const matchesSearch =
        !term ||
        video.title.toLowerCase().includes(term) ||
        video.description.toLowerCase().includes(term)
      return matchesDifficulty && matchesSearch
    })
  }, [videos, difficulty, search])

  const categoryName = videos?.[0]?.category.name ?? 'Ejercicios'

  return (
    <section className="p-4">
      <header className="mb-4 flex items-center gap-3 pt-2">
        <Link to="/exercises" aria-label="Volver" className="text-2xl text-muted hover:text-foreground">
          ←
        </Link>
        <h1 className="text-2xl font-black">{categoryName}</h1>
      </header>

      <label className="sr-only" htmlFor="search">Buscar ejercicio</label>
      <input
        id="search"
        type="search"
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        placeholder="Buscar ejercicio…"
        className="mb-4 w-full rounded-2xl border border-white/10 bg-surface px-4 py-3 placeholder:text-muted/60 focus:border-accent focus:outline-none"
      />

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
        <EmptyState message="No hay ejercicios con estos filtros." />
      )}

      <ul className="space-y-3">
        {filtered.map((video) => (
          <li key={video.id}>
            <Link
              to={`/exercises/${video.id}`}
              className="flex items-center gap-4 rounded-2xl border border-white/10 bg-surface p-4 transition-colors hover:border-accent/50"
            >
              <span aria-hidden="true" className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-surface-raised text-xl">
                ▶
              </span>
              <div className="min-w-0 flex-1">
                <h2 className="truncate font-bold">{video.title}</h2>
                <div className="mt-1.5">
                  <DifficultyBadge difficulty={video.difficulty} />
                </div>
              </div>
              <span aria-hidden="true" className="text-muted">›</span>
            </Link>
          </li>
        ))}
      </ul>
    </section>
  )
}
