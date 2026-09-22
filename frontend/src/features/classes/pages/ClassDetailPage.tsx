import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import {
  enroll,
  getSessionDetails,
  joinWaitlist,
  leaveWaitlist,
  recordAttendance,
  unenroll,
} from '../api/classesApi'
import type { EnrolledUserDto } from '../api/classesApi'
import { useAuthStore } from '../../auth/authStore'
import { GrantAchievements } from '../../achievements/components/GrantAchievements'
import { getSessionAchievements } from '../../achievements/api/achievementsApi'
import { apiErrorMessage } from '../../../shared/api/client'
import { DifficultyBadge } from '../../../shared/components/DifficultyBadge'
import { ErrorState, LoadingState } from '../../../shared/components/QueryStates'
import { ADMIN_WHATSAPP_URL } from '../../../shared/config'
import { ClassReviews } from '../../community/components/Reviews'
import { useNow } from '../../community/communitySupport'

/**
 * Detalle de clase: coach, descripción, participantes y acciones de reserva.
 * Guerrero ve CTA de upgrade por WhatsApp (flujo manual — decisión del cliente).
 * Diseño según mockup session_detail.png.
 */
export default function ClassDetailPage() {
  const { id } = useParams<{ id: string }>()
  const sessionId = Number(id)
  const queryClient = useQueryClient()
  const user = useAuthStore((s) => s.user)
  const [actionError, setActionError] = useState<string | null>(null)
  const now = useNow()

  const { data, isLoading, error } = useQuery({
    queryKey: ['session', sessionId],
    queryFn: () => getSessionDetails(sessionId),
    enabled: !!sessionId,
  })

  const { data: sessionAchievements } = useQuery({
    queryKey: ['session', sessionId, 'achievements'],
    queryFn: () => getSessionAchievements(sessionId),
    enabled: !!sessionId,
  })

  const mutation = useMutation({
    mutationFn: async (action: 'enroll' | 'unenroll' | 'joinWaitlist' | 'leaveWaitlist') => {
      setActionError(null)
      const actions = { enroll, unenroll, joinWaitlist, leaveWaitlist }
      await actions[action](sessionId)
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['session', sessionId] })
      void queryClient.invalidateQueries({ queryKey: ['sessions'] })
    },
    onError: (err) => setActionError(apiErrorMessage(err)),
  })

  if (isLoading) return <LoadingState />
  if (error) return <ErrorState message={apiErrorMessage(error)} />
  if (!data) return <ErrorState message="La clase no existe." />

  const { session, enrolledUsers, currentUserStatus } = data
  const isFull = session.availableSpots === 0
  const canBook = user?.role === 'Clover' || user?.role === 'Admin'
  const date = new Date(session.date)
  const started = date.getTime() <= now
  const finished = date.getTime() + session.durationMinutes * 60000 <= now
  const dateLabel = date.toLocaleDateString('es-AR', { weekday: 'long', day: 'numeric', month: 'long' })
  const timeLabel = date.toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' })

  return (
    <section className="p-4">
      <header className="mb-5 pt-2">
        <Link to="/classes" aria-label="Volver" className="text-muted hover:text-foreground">
          ← Volver
        </Link>
        <div className="mt-3 mb-2">
          <DifficultyBadge difficulty={session.difficulty} />
        </div>
        <h1 className="text-3xl font-black">{session.title}</h1>
        <p className="mt-2 text-muted">
          📅 {dateLabel} · 🕒 {timeLabel} · {session.durationMinutes} min
        </p>
      </header>

      <div className="mb-5 flex items-center gap-3 rounded-2xl border border-white/10 bg-surface p-4">
        <span aria-hidden="true" className="flex h-12 w-12 items-center justify-center rounded-full bg-accent/15 text-lg font-black text-accent">
          {session.coachName.split(' ').map((w) => w[0]).join('').slice(0, 2)}
        </span>
        <div>
          <p className="text-xs tracking-wider text-muted uppercase">Coach</p>
          <p className="font-bold">{session.coachName}</p>
        </div>
      </div>

      <div className="mb-5">
        <h2 className="mb-1 text-lg font-bold">Descripción</h2>
        <p className="text-muted">{session.description}</p>
      </div>

      {sessionAchievements && sessionAchievements.length > 0 && (
        <div className="mb-5">
          <h2 className="mb-2 text-lg font-bold">Logros de esta clase</h2>
          <ul className="flex flex-wrap gap-2">
            {sessionAchievements.map((a) => (
              <li
                key={a.id}
                className="flex items-center gap-2 rounded-full border border-accent/20 bg-accent/5 px-3 py-1.5 text-sm"
              >
                <span aria-hidden="true">{a.icon}</span>
                <span>{a.name}</span>
              </li>
            ))}
          </ul>
        </div>
      )}

      <div className="mb-6">
        <h2 className="mb-2 text-lg font-bold">
          Participantes{' '}
          <span className="text-sm font-normal text-muted">
            {session.enrolled}/{session.limitedSpots} lugares
          </span>
        </h2>
        {enrolledUsers.length === 0 ? (
          <p className="text-sm text-muted">Todavía no hay inscritos — sé el primero.</p>
        ) : (
          <ul className="flex flex-wrap gap-2">
            {enrolledUsers.map((u) => (
              <li key={u.id} className="rounded-full bg-surface-raised px-3 py-1.5 text-sm">
                {u.fullName}
              </li>
            ))}
          </ul>
        )}
      </div>

      {actionError && (
        <p role="alert" className="mb-4 rounded-xl border border-danger/40 bg-danger/10 px-4 py-3 text-sm text-danger">
          {actionError}
        </p>
      )}

      {user?.role === 'Admin' && finished && (
        <AttendanceList sessionId={sessionId} enrolledUsers={enrolledUsers} />
      )}

      {user?.role === 'Admin' && finished && (
        <GrantAchievements sessionId={sessionId} participants={enrolledUsers} />
      )}

      {started ? <p className="rounded-xl bg-surface p-4 text-sm text-muted">{finished ? 'Clase finalizada.' : 'Clase en curso.'} Las reservas y cancelaciones están cerradas.</p> : !canBook ? (
        <a
          href={ADMIN_WHATSAPP_URL}
          target="_blank"
          rel="noopener noreferrer"
          className="block w-full rounded-2xl bg-success py-4 text-center font-bold text-white"
        >
          Pásate a Clover para anotarte
        </a>
      ) : (
        <ActionButton
          status={currentUserStatus}
          isFull={isFull}
          isPending={mutation.isPending}
          onAction={(action) => mutation.mutate(action)}
        />
      )}
      <ClassReviews sessionId={sessionId} />
    </section>
  )
}

function AttendanceList({
  sessionId,
  enrolledUsers,
}: {
  sessionId: number
  enrolledUsers: EnrolledUserDto[]
}) {
  const queryClient = useQueryClient()
  const initialAttended = new Set(enrolledUsers.filter((u) => u.attended === true).map((u) => u.id))
  const [selected, setSelected] = useState<Set<number>>(initialAttended)
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: () => recordAttendance(sessionId, [...selected]),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['session', sessionId] })
      setSaved(true)
      setError(null)
      setTimeout(() => setSaved(false), 3000)
    },
    onError: (err) => {
      setError(apiErrorMessage(err))
    },
  })

  const toggle = (id: number) => {
    setSelected((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  if (enrolledUsers.length === 0) {
    return null
  }

  return (
    <div className="mb-6 rounded-2xl border border-white/10 bg-surface p-4">
      <h2 className="mb-3 font-bold">Lista de asistencia</h2>
      <ul className="mb-3 space-y-2">
        {enrolledUsers.map((u) => (
          <li key={u.id}>
            <label className="flex min-h-11 cursor-pointer items-center gap-3 rounded-xl bg-surface-raised px-3 py-2">
              <input
                type="checkbox"
                checked={selected.has(u.id)}
                onChange={() => toggle(u.id)}
                className="h-5 w-5 accent-yellow-400"
              />
              <span>{u.fullName}</span>
              {u.attended === true && <span className="ml-auto text-xs text-success">Guardado</span>}
            </label>
          </li>
        ))}
      </ul>
      {saved && <p className="mb-2 text-sm text-success">Asistencia guardada.</p>}
      {error && (
        <p role="alert" className="mb-2 text-sm text-danger">{error}</p>
      )}
      <button
        onClick={() => mutation.mutate()}
        disabled={mutation.isPending}
        className="w-full rounded-2xl bg-accent py-3.5 font-bold text-accent-foreground disabled:opacity-50"
      >
        {mutation.isPending ? 'Guardando…' : 'Guardar asistencia'}
      </button>
    </div>
  )
}

function ActionButton({
  status,
  isFull,
  isPending,
  onAction,
}: {
  status: 'enrolled' | 'waitlist' | 'none'
  isFull: boolean
  isPending: boolean
  onAction: (action: 'enroll' | 'unenroll' | 'joinWaitlist' | 'leaveWaitlist') => void
}) {
  const base = 'w-full rounded-2xl py-4 font-bold transition-opacity disabled:opacity-50'

  if (status === 'enrolled') {
    return (
      <button onClick={() => onAction('unenroll')} disabled={isPending} className={`${base} border border-danger/50 bg-danger/10 text-danger`}>
        {isPending ? 'Cancelando…' : 'Cancelar mi reserva'}
      </button>
    )
  }

  if (status === 'waitlist') {
    return (
      <button onClick={() => onAction('leaveWaitlist')} disabled={isPending} className={`${base} border border-white/20 bg-surface-raised text-foreground`}>
        {isPending ? 'Saliendo…' : 'Salir de la lista de espera'}
      </button>
    )
  }

  if (isFull) {
    return (
      <button onClick={() => onAction('joinWaitlist')} disabled={isPending} className={`${base} bg-surface-raised text-foreground`}>
        {isPending ? 'Uniéndote…' : 'Unirme a la lista de espera'}
      </button>
    )
  }

  return (
    <button onClick={() => onAction('enroll')} disabled={isPending} className={`${base} bg-accent text-accent-foreground shadow-lg shadow-accent/25`}>
      {isPending ? 'Reservando…' : 'Reservar mi lugar'}
    </button>
  )
}
