import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  deleteRutine, getRutine, saveRutine,
  type RutineDetail, type WriteRutineExercise,
} from '../api/adminApi'
import { getCategories, getVideos } from '../../exercises/api/exercisesApi'
import { getRutines } from '../../routines/api/routinesApi'
import { DifficultyBadge } from '../../../shared/components/DifficultyBadge'
import { EmptyState, LoadingState } from '../../../shared/components/QueryStates'
import { buttonClass, cardClass, CommunityHeader, ErrorNotice, inputClass, secondaryClass } from '../../community/components/CommunityUI'

const emptyExercise = (): WriteRutineExercise => ({
  exercise: '', tipo: 'Principal', reps: 10, series: 3, descanso: '60s', obs: '', videoId: null,
})

/** Gestión de rutinas con constructor de ejercicios (videos precargados, filtro por categoría + lupa). */
export default function AdminRoutinesPage() {
  const [editing, setEditing] = useState<number | 'new' | null>(null)
  const [search, setSearch] = useState('')
  const routines = useQuery({ queryKey: ['admin', 'routines', search], queryFn: () => getRutines({ searchTerm: search || undefined }) })

  return (
    <section className="p-4">
      <CommunityHeader title="Rutinas" subtitle="El calentamiento siempre se ordena primero al guardar." back="/admin">
        <button className={`${buttonClass} mt-3`} onClick={() => setEditing('new')}>+ Nueva rutina</button>
      </CommunityHeader>

      {editing !== null && <RoutineForm key={editing} id={editing === 'new' ? null : editing} onClose={() => setEditing(null)} />}

      {editing === null && (
        <input className={`${inputClass} mb-4`} placeholder="Buscar rutina…" aria-label="Buscar rutinas" value={search} onChange={(e) => setSearch(e.target.value)} />
      )}

      {routines.isLoading && <LoadingState />}
      <ErrorNotice error={routines.error} />
      {!routines.isLoading && routines.data?.length === 0 && <EmptyState message="No hay rutinas con ese criterio." />}

      <ul className="space-y-3">
        {routines.data?.map((routine) => (
          <RoutineRow key={routine.id} id={routine.id} title={routine.title} duration={routine.duration}
            difficulty={routine.difficulty} categoryName={routine.categoryName} onEdit={() => setEditing(routine.id)} />
        ))}
      </ul>
    </section>
  )
}

function RoutineRow({ id, title, duration, difficulty, categoryName, onEdit }: {
  id: number; title: string; duration: string; difficulty: string; categoryName: string; onEdit: () => void
}) {
  const queryClient = useQueryClient()
  const mutation = useMutation({
    mutationFn: () => deleteRutine(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin', 'routines'] }),
  })
  return (
    <li className={cardClass}>
      <div className="mb-1 flex items-center justify-between gap-2">
        <DifficultyBadge difficulty={difficulty} />
        <span className="text-xs text-muted">{categoryName} · {duration}</span>
      </div>
      <p className="font-bold">{title}</p>
      <div className="mt-3 flex gap-2">
        <button className={secondaryClass} onClick={onEdit}>Editar</button>
        <button className={`${secondaryClass} text-danger`} disabled={mutation.isPending}
          onClick={() => { if (confirm(`¿Eliminar la rutina "${title}"?`)) mutation.mutate() }}>
          Eliminar
        </button>
      </div>
      <ErrorNotice error={mutation.error} />
    </li>
  )
}

function RoutineForm({ id, onClose }: { id: number | null; onClose: () => void }) {
  const queryClient = useQueryClient()
  const categories = useQuery({ queryKey: ['categories'], queryFn: getCategories })
  const existing = useQuery({ queryKey: ['admin', 'routine', id], queryFn: () => getRutine(id!), enabled: id !== null })

  if (id !== null && existing.isLoading) return <LoadingState />
  if (existing.error) return <ErrorNotice error={existing.error} />
  return (
    <RoutineEditor
      routine={existing.data ?? null}
      categories={categories.data ?? []}
      onClose={onClose}
      onSaved={async () => {
        await queryClient.invalidateQueries({ queryKey: ['admin', 'routines'] })
        await queryClient.invalidateQueries({ queryKey: ['routines'] })
        onClose()
      }}
    />
  )
}

function RoutineEditor({ routine, categories, onClose, onSaved }: {
  routine: RutineDetail | null
  categories: { id: number; name: string }[]
  onClose: () => void
  onSaved: () => Promise<void>
}) {
  const [form, setForm] = useState({
    title: routine?.title ?? '',
    description: routine?.description ?? '',
    duration: routine?.duration ?? '',
    difficulty: routine?.difficulty ?? 'basica',
    categoryId: routine?.categoryId ?? categories[0]?.id ?? 0,
  })
  const [exercises, setExercises] = useState<WriteRutineExercise[]>(
    routine?.exercises.map((e) => ({ ...e })) ?? [emptyExercise()],
  )
  // Las categorías cargan async: se deriva la primera disponible sin efecto.
  const categoryId = form.categoryId || categories[0]?.id || 0
  const mutation = useMutation({
    mutationFn: () => saveRutine(routine?.id, { ...form, categoryId, exercises }),
    onSuccess: onSaved,
  })

  const setExercise = (index: number, patch: Partial<WriteRutineExercise>) =>
    setExercises((list) => list.map((e, i) => (i === index ? { ...e, ...patch } : e)))
  const move = (index: number, delta: number) =>
    setExercises((list) => {
      const target = index + delta
      if (target < 0 || target >= list.length) return list
      const copy = [...list]
      ;[copy[index], copy[target]] = [copy[target], copy[index]]
      return copy
    })

  const submit = (e: FormEvent) => { e.preventDefault(); mutation.mutate() }

  return (
    <form onSubmit={submit} className={`${cardClass} mb-6 space-y-4 border-accent/30`}>
      <h2 className="font-bold">{routine ? 'Editar rutina' : 'Nueva rutina'}</h2>
      <label className="block text-sm font-semibold">Título
        <input className={`${inputClass} mt-1`} value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} maxLength={200} required />
      </label>
      <label className="block text-sm font-semibold">Descripción
        <textarea className={`${inputClass} mt-1`} rows={3} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} maxLength={2000} required />
      </label>
      <div className="grid grid-cols-3 gap-3">
        <label className="block text-sm font-semibold">Duración
          <input className={`${inputClass} mt-1`} value={form.duration} onChange={(e) => setForm({ ...form, duration: e.target.value })} maxLength={50} placeholder="45 min" required />
        </label>
        <label className="block text-sm font-semibold">Dificultad
          <select className={`${inputClass} mt-1`} value={form.difficulty} onChange={(e) => setForm({ ...form, difficulty: e.target.value })}>
            <option value="basica">Básica</option>
            <option value="intermedia">Intermedia</option>
            <option value="avanzada">Avanzada</option>
          </select>
        </label>
        <label className="block text-sm font-semibold">Categoría
          <select className={`${inputClass} mt-1`} value={categoryId} onChange={(e) => setForm({ ...form, categoryId: Number(e.target.value) })} required>
            {categories.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </label>
      </div>

      <fieldset className="space-y-3">
        <legend className="mb-1 text-sm font-bold">Ejercicios ({exercises.length})</legend>
        {exercises.map((exercise, index) => (
          <ExerciseCard
            key={index}
            index={index}
            exercise={exercise}
            categories={categories}
            isFirst={index === 0}
            isLast={index === exercises.length - 1}
            onChange={(patch) => setExercise(index, patch)}
            onMove={(delta) => move(index, delta)}
            onRemove={() => setExercises((list) => list.filter((_, i) => i !== index))}
          />
        ))}
        <button type="button" className={secondaryClass} onClick={() => setExercises((list) => [...list, emptyExercise()])}>
          + Agregar ejercicio
        </button>
      </fieldset>

      <ErrorNotice error={mutation.error} />
      <div className="flex gap-2">
        <button className={buttonClass} disabled={mutation.isPending}>{mutation.isPending ? 'Guardando…' : 'Guardar rutina'}</button>
        <button type="button" className={secondaryClass} onClick={onClose}>Cancelar</button>
      </div>
    </form>
  )
}

function ExerciseCard({ index, exercise, categories, isFirst, isLast, onChange, onMove, onRemove }: {
  index: number
  exercise: WriteRutineExercise
  categories: { id: number; name: string }[]
  isFirst: boolean
  isLast: boolean
  onChange: (patch: Partial<WriteRutineExercise>) => void
  onMove: (delta: number) => void
  onRemove: () => void
}) {
  const [pickerOpen, setPickerOpen] = useState(false)
  return (
    <div className="space-y-3 rounded-xl border border-white/10 p-3">
      <div className="flex items-center justify-between gap-2">
        <span className={`rounded-lg px-2 py-1 text-xs font-bold ${exercise.tipo === 'Calentamiento' ? 'bg-success/15 text-success' : 'bg-accent/15 text-accent'}`}>
          #{index + 1} {exercise.tipo}
        </span>
        <div className="flex gap-1">
          <button type="button" aria-label="Subir ejercicio" className={secondaryClass} disabled={isFirst} onClick={() => onMove(-1)}>↑</button>
          <button type="button" aria-label="Bajar ejercicio" className={secondaryClass} disabled={isLast} onClick={() => onMove(1)}>↓</button>
          <button type="button" className={`${secondaryClass} text-danger`} onClick={onRemove}>Quitar</button>
        </div>
      </div>
      <label className="block text-sm font-semibold">Ejercicio
        <input className={`${inputClass} mt-1`} value={exercise.exercise} onChange={(e) => onChange({ exercise: e.target.value })} maxLength={200} placeholder="Dominadas estrictas" required />
      </label>
      <div className="grid grid-cols-4 gap-2">
        <label className="block text-xs font-semibold">Tipo
          <select className={`${inputClass} mt-1`} value={exercise.tipo} onChange={(e) => onChange({ tipo: e.target.value as WriteRutineExercise['tipo'] })}>
            <option value="Principal">Principal</option>
            <option value="Calentamiento">Calentamiento</option>
          </select>
        </label>
        <label className="block text-xs font-semibold">Series
          <input className={`${inputClass} mt-1`} type="number" inputMode="numeric" min={1} max={50} value={exercise.series} onChange={(e) => onChange({ series: Number(e.target.value) })} required />
        </label>
        <label className="block text-xs font-semibold">Reps
          <input className={`${inputClass} mt-1`} type="number" inputMode="numeric" min={1} max={1000} value={exercise.reps} onChange={(e) => onChange({ reps: Number(e.target.value) })} required />
        </label>
        <label className="block text-xs font-semibold">Descanso
          <input className={`${inputClass} mt-1`} value={exercise.descanso} onChange={(e) => onChange({ descanso: e.target.value })} maxLength={20} placeholder="60s" required />
        </label>
      </div>
      <label className="block text-sm font-semibold">Observaciones
        <input className={`${inputClass} mt-1`} value={exercise.obs} onChange={(e) => onChange({ obs: e.target.value })} maxLength={500} placeholder="Técnica, errores comunes…" required />
      </label>
      <div>
        <button type="button" className={secondaryClass} onClick={() => setPickerOpen(!pickerOpen)}>
          {exercise.videoId ? `Video vinculado (#${exercise.videoId}) — cambiar` : '🔍 Vincular video de la videoteca'}
        </button>
        {exercise.videoId && (
          <button type="button" className={`${secondaryClass} ml-2`} onClick={() => onChange({ videoId: null })}>Desvincular</button>
        )}
        {pickerOpen && (
          <VideoPicker
            categories={categories}
            onPick={(videoId) => { onChange({ videoId }); setPickerOpen(false) }}
          />
        )}
      </div>
    </div>
  )
}

function VideoPicker({ categories, onPick }: { categories: { id: number; name: string }[]; onPick: (videoId: number) => void }) {
  const [search, setSearch] = useState('')
  const [categoryId, setCategoryId] = useState<number | undefined>()
  const videos = useQuery({
    queryKey: ['admin', 'video-picker', categoryId, search],
    queryFn: () => getVideos({ categoryId, searchTerm: search || undefined }),
  })
  return (
    <div className="mt-2 space-y-2 rounded-xl bg-surface-raised p-3">
      <div className="grid grid-cols-2 gap-2">
        <input className={inputClass} placeholder="Buscar video…" aria-label="Buscar video" value={search} onChange={(e) => setSearch(e.target.value)} />
        <select className={inputClass} aria-label="Categoría del video" value={categoryId ?? ''} onChange={(e) => setCategoryId(e.target.value ? Number(e.target.value) : undefined)}>
          <option value="">Todas</option>
          {categories.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
        </select>
      </div>
      {videos.isLoading && <LoadingState />}
      <ul className="max-h-48 space-y-1 overflow-y-auto">
        {videos.data?.map((video) => (
          <li key={video.id}>
            <button type="button" className="w-full rounded-lg px-2 py-2 text-left text-sm hover:bg-surface" onClick={() => onPick(video.id)}>
              <span className="font-semibold">{video.title}</span>
              <span className="ml-2 text-xs text-muted">{video.category.name} · {video.difficulty}</span>
            </button>
          </li>
        ))}
        {videos.data?.length === 0 && <li className="px-2 py-1 text-sm text-muted">Sin resultados.</li>}
      </ul>
    </div>
  )
}
