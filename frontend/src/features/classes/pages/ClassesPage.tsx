import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { getSessions } from '../api/classesApi'
import { apiErrorMessage } from '../../../shared/api/client'
import { DifficultyBadge } from '../../../shared/components/DifficultyBadge'
import { EmptyState, ErrorState, LoadingState } from '../../../shared/components/QueryStates'
import { PushOptIn } from '../../../pwa/PushOptIn'
import { useAuthStore } from '../../auth/authStore'
import type { SessionDto } from '../api/classesApi'

const dayNames = ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb']

function toISODate(date: Date): string {
  return date.toISOString().slice(0, 10)
}

/** Semana actual (lunes a domingo) para el selector de días. */
function getCurrentWeek(): Date[] {
  const today = new Date()
  const mondayOffset = (today.getDay() + 6) % 7
  const monday = new Date(today)
  monday.setDate(today.getDate() - mondayOffset)
  return Array.from({ length: 7 }, (_, i) => {
    const day = new Date(monday)
    day.setDate(monday.getDate() + i)
    return day
  })
}

/**
 * Reserva de clases: selector de semana + tarjetas con cupos.
 * Diseño según mockup session_list.png.
 */
export default function ClassesPage() {
  const week = useMemo(() => getCurrentWeek(), [])
  const todayISO = toISODate(new Date())
  const [selectedDay, setSelectedDay] = useState(todayISO)
  const user = useAuthStore((s) => s.user)
  const canBook = user?.role === 'Clover' || user?.role === 'Admin'

  const { data: sessions, isLoading, error } = useQuery({
    queryKey: ['sessions', selectedDay],
    queryFn: () => getSessions(selectedDay),
  })

  return (
    <section className="p-4">
      <header className="mb-4 pt-2">
        <h1 className="text-2xl font-black">Reserva de clases</h1>
      </header>

      {canBook && <PushOptIn />}

      <div className="mb-5 flex gap-2 overflow-x-auto pb-1" role="group" aria-label="Elegir día">
        {week.map((day) => {
          const iso = toISODate(day)
          const isSelected = iso === selectedDay
          return (
            <button
              key={iso}
              onClick={() => setSelectedDay(iso)}
              aria-pressed={isSelected}
              className={`flex min-h-16 w-14 shrink-0 flex-col items-center justify-center rounded-2xl text-sm font-bold transition-colors ${
                isSelected ? 'bg-accent text-accent-foreground' : 'bg-surface-raised text-muted'
              }`}
            >
              <span className="text-xs uppercase">{dayNames[day.getDay()]}</span>
              <span className="text-lg">{day.getDate()}</span>
            </button>
          )
        })}
      </div>

      {isLoading && <LoadingState />}
      {error && <ErrorState message={apiErrorMessage(error)} />}
      {!isLoading && !error && sessions?.length === 0 && (
        <EmptyState message="No hay clases este día." />
      )}

      <ul className="space-y-4">
        {sessions?.map((session) => {
          const spotsPercent = Math.round((session.enrolled / session.limitedSpots) * 100)
          const isFull = session.availableSpots === 0
          const time = new Date(session.date).toLocaleTimeString('es-AR', {
            hour: '2-digit',
            minute: '2-digit',
          })
          const status = getSessionStatus(session)

          return (
            <li key={session.id}>
              <Link
                to={`/classes/${session.id}`}
                className="block rounded-3xl border border-white/10 bg-surface p-5 transition-colors hover:border-accent/50"
              >
                <div className="mb-2 flex items-start justify-between gap-3">
                  <DifficultyBadge difficulty={session.difficulty} />
                  <SessionStatusBadge status={status} availableSpots={session.availableSpots} isFull={isFull} />
                </div>
                <h2 className="text-lg font-bold">{session.title}</h2>
                <p className="mt-1 text-sm text-muted">
                  🕒 {time} · {session.coachName}
                </p>
                {status === 'upcoming' && (
                  <div className="mt-3">
                    <div className="mb-1 flex justify-between text-xs text-muted">
                      <span>{session.enrolled}/{session.limitedSpots} lugares</span>
                    </div>
                    <div className="h-2 overflow-hidden rounded-full bg-surface-raised">
                      <div
                        className={`h-full rounded-full ${isFull ? 'bg-danger' : 'bg-success'}`}
                        style={{ width: `${spotsPercent}%` }}
                      />
                    </div>
                  </div>
                )}
              </Link>
            </li>
          )
        })}
      </ul>
    </section>
  )
}

type SessionStatus = 'upcoming' | 'in-progress' | 'finished'

function getSessionStatus(session: SessionDto): SessionStatus {
  const now = Date.now()
  const start = new Date(session.date).getTime()
  const end = start + session.durationMinutes * 60000
  if (now < start) return 'upcoming'
  if (now >= end) return 'finished'
  return 'in-progress'
}

function SessionStatusBadge({
  status,
  availableSpots,
  isFull,
}: {
  status: SessionStatus
  availableSpots: number
  isFull: boolean
}) {
  if (status === 'in-progress') {
    return <span className="text-xs font-bold text-accent">EN PROCESO</span>
  }
  if (status === 'finished') {
    return <span className="text-xs font-bold text-muted">FINALIZADA</span>
  }
  return (
    <span className={`text-xs font-bold ${isFull ? 'text-danger' : 'text-success'}`}>
      {isFull ? 'CLASE LLENA' : `${availableSpots} DISPONIBLES`}
    </span>
  )
}
