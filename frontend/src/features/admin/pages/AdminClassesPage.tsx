import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  createSession, deleteSession, getAllSessions, getPurgePreview, purgeSessionsByMonth,
  updateSession, type WriteSessionRequest, getAdminAchievements,
} from '../api/adminApi'
import type { SessionDto } from '../../classes/api/classesApi'
import { getSessionAchievements, type AchievementDto } from '../../achievements/api/achievementsApi'
import { DifficultyBadge } from '../../../shared/components/DifficultyBadge'
import { EmptyState, LoadingState } from '../../../shared/components/QueryStates'
import { buttonClass, cardClass, CommunityHeader, ErrorNotice, inputClass, secondaryClass } from '../../community/components/CommunityUI'
import { localDateInput, useNow } from '../../community/communitySupport'

const emptyForm: WriteSessionRequest = {
  title: '', description: '', date: '', limitedSpots: 15, difficulty: 'basica', coachName: '', durationMinutes: 60, achievementIds: [],
}

/** Gestión de clases: crear, editar, eliminar y limpieza masiva por mes (solo meses pasados). */
export default function AdminClassesPage() {
  const [editing, setEditing] = useState<SessionDto | 'new' | null>(null)
  const { data: sessions, isLoading, error } = useQuery({ queryKey: ['admin', 'sessions'], queryFn: getAllSessions })
  const editingSessionId = typeof editing === 'object' && editing !== null ? editing.id : null
  const { data: sessionAchievements, isLoading: sessionAchievementsLoading } = useQuery({
    queryKey: ['session', editingSessionId, 'achievements'],
    queryFn: () => getSessionAchievements(editingSessionId!),
    enabled: editingSessionId !== null,
  })
  const now = useNow()
  const sorted = sessions ? [...sessions].sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime()) : []

  return (
    <section className="p-4">
      <CommunityHeader title="Clases" subtitle="Crear, editar y eliminar clases. Si cambia el horario, los inscritos reciben un aviso push." back="/admin">
        <button className={`${buttonClass} mt-3`} onClick={() => setEditing('new')}>+ Nueva clase</button>
      </CommunityHeader>

      {editing && typeof editing === 'object' && sessionAchievementsLoading && <LoadingState />}
      {editing && !(typeof editing === 'object' && sessionAchievementsLoading) && (
        <SessionForm
          key={editing === 'new' ? 'new' : editing.id}
          session={editing === 'new' ? null : editing}
          sessionAchievements={sessionAchievements ?? []}
          onClose={() => setEditing(null)}
        />
      )}

      {isLoading && <LoadingState />}
      <ErrorNotice error={error} />
      {!isLoading && sorted.length === 0 && <EmptyState message="Todavía no hay clases creadas." />}

      <ul className="space-y-3">
        {sorted.map((session) => (
          <SessionRow
            key={session.id}
            session={session}
            started={new Date(session.date).getTime() <= now}
            onEdit={() => setEditing(session)}
          />
        ))}
      </ul>

      <MonthlyPurge />
    </section>
  )
}

function SessionRow({ session, started, onEdit }: { session: SessionDto; started: boolean; onEdit: () => void }) {
  const queryClient = useQueryClient()
  const mutation = useMutation({
    mutationFn: () => deleteSession(session.id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin', 'sessions'] }),
  })
  const date = new Date(session.date)
  return (
    <li className={cardClass}>
      <div className="mb-1 flex items-center justify-between gap-2">
        <DifficultyBadge difficulty={session.difficulty} />
        <span className="text-xs text-muted">{session.enrolled}/{session.limitedSpots} inscritos</span>
      </div>
      <p className="font-bold">{session.title}</p>
      <p className="mt-1 text-sm text-muted">
        {date.toLocaleDateString('es-AR', { day: 'numeric', month: 'short' })} ·{' '}
        {date.toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' })} · {session.durationMinutes} min · {session.coachName}
      </p>
      <div className="mt-3 flex flex-wrap gap-2">
        {!started && <button className={secondaryClass} onClick={onEdit}>Editar</button>}
        {started && <span className="py-2 text-xs text-muted">Ya comenzó — no editable</span>}
        <button
          className={`${secondaryClass} text-danger`}
          disabled={mutation.isPending}
          onClick={() => {
            if (confirm(`¿Eliminar "${session.title}"?${session.enrolled > 0 ? ` Tiene ${session.enrolled} inscritos.` : ''}`)) mutation.mutate()
          }}
        >
          Eliminar
        </button>
      </div>
      <ErrorNotice error={mutation.error} />
    </li>
  )
}

function SessionForm({
  session,
  sessionAchievements,
  onClose,
}: {
  session: SessionDto | null
  sessionAchievements: AchievementDto[]
  onClose: () => void
}) {
  const queryClient = useQueryClient()
  const { data: allAchievements, isLoading: achievementsLoading } = useQuery({
    queryKey: ['admin', 'achievements'],
    queryFn: getAdminAchievements,
  })

  const activeAchievements = allAchievements?.filter((a) => !a.isHidden) ?? []

  const [form, setForm] = useState<WriteSessionRequest>(
    session
      ? {
          title: session.title, description: session.description, date: localDateInput(session.date),
          limitedSpots: session.limitedSpots, difficulty: session.difficulty,
          coachName: session.coachName, durationMinutes: session.durationMinutes,
          achievementIds: sessionAchievements.map((a) => a.id),
        }
      : emptyForm,
  )

  const mutation = useMutation({
    mutationFn: () => {
      const request = { ...form, date: new Date(form.date).toISOString() }
      return session ? updateSession(session.id, request) : createSession(request)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['admin', 'sessions'] })
      await queryClient.invalidateQueries({ queryKey: ['sessions'] })
      if (session) {
        await queryClient.invalidateQueries({ queryKey: ['session', session.id, 'achievements'] })
      }
      onClose()
    },
  })
  const set = (patch: Partial<WriteSessionRequest>) => setForm((f) => ({ ...f, ...patch }))
  const toggleAchievement = (id: number) => {
    setForm((f) => ({
      ...f,
      achievementIds: f.achievementIds.includes(id)
        ? f.achievementIds.filter((x) => x !== id)
        : [...f.achievementIds, id],
    }))
  }
  const submit = (e: FormEvent) => { e.preventDefault(); mutation.mutate() }

  if (achievementsLoading) {
    return <LoadingState />
  }

  return (
    <form onSubmit={submit} className={`${cardClass} mb-6 space-y-4 border-accent/30`}>
      <h2 className="font-bold">{session ? 'Editar clase' : 'Nueva clase'}</h2>
      <label className="block text-sm font-semibold">Título
        <input className={`${inputClass} mt-1`} value={form.title} onChange={(e) => set({ title: e.target.value })} maxLength={200} required />
      </label>
      <label className="block text-sm font-semibold">Descripción
        <textarea className={`${inputClass} mt-1`} rows={3} value={form.description} onChange={(e) => set({ description: e.target.value })} maxLength={2000} required />
      </label>
      <div className="grid grid-cols-2 gap-3">
        <label className="block text-sm font-semibold">Fecha y hora
          <input className={`${inputClass} mt-1`} type="datetime-local" value={form.date} onChange={(e) => set({ date: e.target.value })} required />
        </label>
        <label className="block text-sm font-semibold">Duración (min)
          <input className={`${inputClass} mt-1`} type="number" inputMode="numeric" min={15} max={480} value={form.durationMinutes} onChange={(e) => set({ durationMinutes: Number(e.target.value) })} required />
        </label>
        <label className="block text-sm font-semibold">Cupos
          <input className={`${inputClass} mt-1`} type="number" inputMode="numeric" min={session?.enrolled ?? 1} max={500} value={form.limitedSpots} onChange={(e) => set({ limitedSpots: Number(e.target.value) })} required />
        </label>
        <label className="block text-sm font-semibold">Dificultad
          <select className={`${inputClass} mt-1`} value={form.difficulty} onChange={(e) => set({ difficulty: e.target.value })}>
            <option value="basica">Básica</option>
            <option value="intermedia">Intermedia</option>
            <option value="avanzada">Avanzada</option>
          </select>
        </label>
      </div>
      <label className="block text-sm font-semibold">Coach
        <input className={`${inputClass} mt-1`} value={form.coachName} onChange={(e) => set({ coachName: e.target.value })} maxLength={100} required />
      </label>

      <div>
        <p className="mb-2 text-sm font-semibold">Logros asociados</p>
        {activeAchievements.length === 0 ? (
          <p className="text-sm text-muted">No hay logros activos en el catálogo.</p>
        ) : (
          <ul className="grid grid-cols-1 gap-2 sm:grid-cols-2">
            {activeAchievements.map((a) => (
              <li key={a.id}>
                <label className="flex cursor-pointer items-center gap-3 rounded-xl bg-surface p-3">
                  <input
                    type="checkbox"
                    checked={form.achievementIds.includes(a.id)}
                    onChange={() => toggleAchievement(a.id)}
                    className="h-5 w-5 accent-yellow-400"
                  />
                  <span aria-hidden="true" className="text-xl">{a.icon}</span>
                  <span className="text-sm font-semibold">{a.name}</span>
                </label>
              </li>
            ))}
          </ul>
        )}
      </div>

      <ErrorNotice error={mutation.error} />
      <div className="flex gap-2">
        <button className={buttonClass} disabled={mutation.isPending}>{mutation.isPending ? 'Guardando…' : 'Guardar'}</button>
        <button type="button" className={secondaryClass} onClick={onClose}>Cancelar</button>
      </div>
    </form>
  )
}

function MonthlyPurge() {
  const lastMonth = new Date()
  lastMonth.setUTCDate(1)
  lastMonth.setUTCMonth(lastMonth.getUTCMonth() - 1)
  const [month, setMonth] = useState(lastMonth.toISOString().slice(0, 7))
  const [result, setResult] = useState<string | null>(null)
  const queryClient = useQueryClient()

  const [year, monthNum] = month.split('-').map(Number)
  const preview = useQuery({
    queryKey: ['admin', 'purge-preview', year, monthNum],
    queryFn: () => getPurgePreview(year, monthNum),
    enabled: /^\d{4}-\d{2}$/.test(month),
    retry: false,
  })
  const purge = useMutation({
    mutationFn: () => purgeSessionsByMonth(year, monthNum),
    onSuccess: async (data) => {
      setResult(`Se eliminaron ${data.deleted} clases.`)
      await queryClient.invalidateQueries({ queryKey: ['admin', 'sessions'] })
    },
  })

  return (
    <section className="mt-8 border-t border-white/10 pt-5" aria-labelledby="purge-title">
      <h2 id="purge-title" className="mb-1 text-lg font-bold">Limpieza por mes</h2>
      <p className="mb-3 text-sm text-muted">Elimina todas las clases de un mes ya finalizado. Las reseñas y logros otorgados se conservan.</p>
      <div className="flex flex-wrap items-end gap-3">
        <label className="block text-sm font-semibold">Mes
          <input className={`${inputClass} mt-1`} type="month" value={month} max={lastMonth.toISOString().slice(0, 7)} onChange={(e) => setMonth(e.target.value)} />
        </label>
        <button
          className={`${secondaryClass} text-danger`}
          disabled={preview.isPending || purge.isPending || !preview.data || preview.data.count === 0}
          onClick={() => {
            if (confirm(`¿Eliminar las ${preview.data?.count ?? 0} clases de ${month}? Esta acción no se puede deshacer.`)) purge.mutate()
          }}
        >
          {purge.isPending ? 'Eliminando…' : `Eliminar ${preview.data?.count ?? '…'} clases`}
        </button>
      </div>
      {preview.error && <p className="mt-2 text-sm text-muted">Solo se pueden limpiar meses ya finalizados.</p>}
      {result && <p role="status" className="mt-2 text-sm text-success">{result}</p>}
      <ErrorNotice error={purge.error} />
    </section>
  )
}
