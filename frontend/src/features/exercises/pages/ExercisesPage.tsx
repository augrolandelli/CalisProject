import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { getCategories } from '../api/exercisesApi'
import { apiErrorMessage } from '../../../shared/api/client'
import { ErrorState, LoadingState } from '../../../shared/components/QueryStates'

/**
 * Biblioteca de ejercicios: buscador + categorías (Pull, Push, Legs, Core…).
 * Diseño según mockup videosexercises_category_filter.png.
 */
export default function ExercisesPage() {
  const { data: categories, isLoading, error } = useQuery({
    queryKey: ['categories'],
    queryFn: getCategories,
  })

  return (
    <section className="p-4">
      <header className="mb-6 pt-2">
        <h1 className="text-2xl font-black">Biblioteca de ejercicios</h1>
        <p className="mt-1 text-sm text-muted">Elige una categoría para ver los videos.</p>
      </header>

      {isLoading && <LoadingState />}
      {error && <ErrorState message={apiErrorMessage(error)} />}

      <div className="space-y-4">
        {categories?.map((category) => (
          <Link
            key={category.id}
            to={`/exercises/category/${category.id}`}
            className="block rounded-3xl border border-white/10 bg-surface p-5 transition-colors hover:border-accent/50"
          >
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-xl font-bold">{category.name}</h2>
                <p className="mt-1 text-sm text-muted">{category.description}</p>
              </div>
              <span aria-hidden="true" className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-accent text-xl font-black text-accent-foreground">
                →
              </span>
            </div>
          </Link>
        ))}
      </div>
    </section>
  )
}
