import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  getSessionAchievements,
  grantAchievement,
} from '../api/achievementsApi'
import type { EnrolledUserDto } from '../../classes/api/classesApi'
import { apiErrorMessage } from '../../../shared/api/client'

/**
 * Panel del profesor/admin: al finalizar la clase, otorga los logros
 * asociados a los participantes que los consiguieron (spec §3.6).
 */
export function GrantAchievements({
  sessionId,
  participants,
}: {
  sessionId: number
  participants: EnrolledUserDto[]
}) {
  const queryClient = useQueryClient()
  const [selectedAchievement, setSelectedAchievement] = useState<number | null>(null)
  const [selectedUsers, setSelectedUsers] = useState<Set<number>>(new Set())
  const [result, setResult] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const { data: achievements } = useQuery({
    queryKey: ['session', sessionId, 'achievements'],
    queryFn: () => getSessionAchievements(sessionId),
  })

  const mutation = useMutation({
    mutationFn: () =>
      grantAchievement(selectedAchievement!, sessionId, [...selectedUsers]),
    onSuccess: (res) => {
      setResult(`Otorgado a ${res.granted} usuario(s)${res.skipped > 0 ? ` (${res.skipped} ya lo tenían)` : ''}.`)
      setError(null)
      setSelectedUsers(new Set())
      void queryClient.invalidateQueries({ queryKey: ['me', 'achievements'] })
    },
    onError: (err) => {
      setError(apiErrorMessage(err))
      setResult(null)
    },
  })

  const attendedParticipants = participants.filter((p) => p.attended === true)

  if (!achievements?.length) {
    return null // esta clase no tiene logros asociados
  }

  if (attendedParticipants.length === 0) {
    return (
      <div className="mb-6 rounded-2xl border border-accent/30 bg-accent/5 p-4">
        <h2 className="mb-2 font-bold text-accent">Otorgar logros</h2>
        <p className="text-sm text-muted">Marcá la asistencia primero para poder otorgar logros.</p>
      </div>
    )
  }

  const toggleUser = (userId: number) => {
    setSelectedUsers((prev) => {
      const next = new Set(prev)
      if (next.has(userId)) {
        next.delete(userId)
      } else {
        next.add(userId)
      }
      return next
    })
  }

  return (
    <div className="mb-6 rounded-2xl border border-accent/30 bg-accent/5 p-4">
      <h2 className="mb-3 font-bold text-accent">Otorgar logros (profesor)</h2>

      <label htmlFor="achievement-select" className="mb-1 block text-xs font-bold tracking-wider uppercase">
        Logro
      </label>
      <select
        id="achievement-select"
        value={selectedAchievement ?? ''}
        onChange={(e) => setSelectedAchievement(e.target.value ? Number(e.target.value) : null)}
        className="mb-3 w-full rounded-2xl border border-white/15 bg-surface px-4 py-3.5 focus:border-accent focus:outline-none"
      >
        <option value="">Elegí un logro…</option>
        {achievements.map((a) => (
          <option key={a.id} value={a.id}>
            {a.icon} {a.name}
          </option>
        ))}
      </select>

      <p className="mb-2 text-xs font-bold tracking-wider uppercase">Asistentes que lo lograron</p>
      <ul className="mb-3 space-y-2">
        {attendedParticipants.map((p) => (
          <li key={p.id}>
            <label className="flex min-h-11 cursor-pointer items-center gap-3 rounded-xl bg-surface px-3 py-2">
              <input
                type="checkbox"
                checked={selectedUsers.has(p.id)}
                onChange={() => toggleUser(p.id)}
                className="h-5 w-5 accent-yellow-400"
              />
              <span>{p.fullName}</span>
            </label>
          </li>
        ))}
      </ul>

      {result && <p className="mb-2 text-sm text-success">{result}</p>}
      {error && <p role="alert" className="mb-2 text-sm text-danger">{error}</p>}

      <button
        onClick={() => mutation.mutate()}
        disabled={!selectedAchievement || selectedUsers.size === 0 || mutation.isPending}
        className="w-full rounded-2xl bg-accent py-3.5 font-bold text-accent-foreground disabled:opacity-50"
      >
        {mutation.isPending ? 'Otorgando…' : `Otorgar a ${selectedUsers.size} seleccionado(s)`}
      </button>
    </div>
  )
}
