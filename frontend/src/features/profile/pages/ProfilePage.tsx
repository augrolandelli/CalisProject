import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate } from 'react-router-dom'
import { getMyStats, updateMe } from '../api/profileApi'
import { getMyAchievements } from '../../achievements/api/achievementsApi'
import { useAuthStore } from '../../auth/authStore'
import { logout } from '../../auth/api/authApi'
import { apiErrorMessage } from '../../../shared/api/client'
import { LoadingState } from '../../../shared/components/QueryStates'

const profileSchema = z.object({
  fullName: z.string().min(2, 'Ingresa tu nombre completo.').max(200),
  phone: z.string().min(6, 'Ingresa un teléfono válido.').max(50),
})

type ProfileForm = z.infer<typeof profileSchema>

const inputClass =
  'w-full rounded-2xl border border-white/15 bg-surface px-4 py-3.5 text-foreground placeholder:text-muted/60 focus:border-accent focus:outline-none'

/**
 * Perfil: datos personales (edición), estadísticas de progreso y logros.
 */
export default function ProfilePage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { user, refreshToken, clear, setSession } = useAuthStore()
  const [editing, setEditing] = useState(false)
  const [serverError, setServerError] = useState<string | null>(null)

  const { data: stats, isLoading: statsLoading } = useQuery({
    queryKey: ['me', 'stats'],
    queryFn: getMyStats,
  })
  const { data: achievements, isLoading: achLoading } = useQuery({
    queryKey: ['me', 'achievements'],
    queryFn: getMyAchievements,
  })

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ProfileForm>({
    resolver: zodResolver(profileSchema),
    values: { fullName: user?.fullName ?? '', phone: user?.phone ?? '' },
  })

  const mutation = useMutation({
    mutationFn: updateMe,
    onSuccess: (updated) => {
      // refresca el store conservando los tokens
      const state = useAuthStore.getState()
      setSession({
        accessToken: state.accessToken!,
        refreshToken: state.refreshToken!,
        expiresAt: '',
        user: updated,
      })
      void queryClient.invalidateQueries({ queryKey: ['me'] })
      setEditing(false)
    },
    onError: (err) => setServerError(apiErrorMessage(err)),
  })

  const handleLogout = async () => {
    if (refreshToken) {
      try {
        await logout(refreshToken)
      } catch {
        // limpieza local aunque falle la remota
      }
    }
    clear()
    navigate('/login', { replace: true })
  }

  if (!user) return <LoadingState />

  const initials = user.fullName.split(' ').map((w) => w[0]).join('').slice(0, 2).toUpperCase()

  return (
    <section className="p-4">
      <header className="mb-6 pt-2 text-center">
        <div className="mx-auto mb-3 flex h-24 w-24 items-center justify-center rounded-full border-2 border-accent bg-surface text-3xl font-black text-accent">
          {initials}
        </div>
        <h1 className="text-2xl font-black">{user.fullName}</h1>
        <p className="text-sm text-muted">{user.email}</p>
        <span className="mt-2 inline-block rounded-full bg-accent/15 px-3 py-1 text-xs font-bold text-accent uppercase">
          {user.role}
        </span>
      </header>

      <div className="mb-6 rounded-3xl border border-white/10 bg-surface p-5">
        <div className="mb-3 flex items-center justify-between">
          <h2 className="font-bold">Datos personales</h2>
          <button onClick={() => setEditing(!editing)} className="text-sm font-bold text-accent">
            {editing ? 'Cancelar' : 'Editar'}
          </button>
        </div>

        {editing ? (
          <form onSubmit={handleSubmit((form) => mutation.mutate({ ...form, photoUrl: null }))} noValidate className="space-y-3">
            <div>
              <label htmlFor="fullName" className="mb-1 block text-xs font-bold tracking-wider uppercase">Nombre</label>
              <input id="fullName" className={inputClass} {...register('fullName')} />
              {errors.fullName && <p className="mt-1 text-sm text-danger">{errors.fullName.message}</p>}
            </div>
            <div>
              <label htmlFor="phone" className="mb-1 block text-xs font-bold tracking-wider uppercase">Teléfono</label>
              <input id="phone" type="tel" className={inputClass} {...register('phone')} />
              {errors.phone && <p className="mt-1 text-sm text-danger">{errors.phone.message}</p>}
            </div>
            {serverError && <p role="alert" className="text-sm text-danger">{serverError}</p>}
            <button type="submit" disabled={mutation.isPending} className="w-full rounded-2xl bg-accent py-3.5 font-bold text-accent-foreground disabled:opacity-50">
              {mutation.isPending ? 'Guardando…' : 'Guardar cambios'}
            </button>
          </form>
        ) : (
          <dl className="space-y-2 text-sm">
            <div className="flex justify-between"><dt className="text-muted">Teléfono</dt><dd>{user.phone}</dd></div>
            <div className="flex justify-between"><dt className="text-muted">Email</dt><dd>{user.email}</dd></div>
            <div className="flex justify-between"><dt className="text-muted">Estado</dt><dd>{user.state}</dd></div>
          </dl>
        )}
      </div>

      <h2 className="mb-3 font-bold">Progreso</h2>
      {statsLoading ? (
        <LoadingState />
      ) : stats ? (
        <div className="mb-6 grid grid-cols-2 gap-3">
          <StatCard label="Clases este mes" value={stats.classesThisMonth} />
          <StatCard label="Clases asistidas" value={stats.attendedClasses} />
          <StatCard label="Próximas clases" value={stats.upcomingClasses} />
          <StatCard label="Faltas" value={stats.missedClasses} />
          <StatCard label="Logros" value={stats.achievementsCount} />
        </div>
      ) : null}

      <div className="mb-6 flex items-center justify-between">
        <h2 className="font-bold">Mis logros</h2>
        <Link to="/achievements" className="text-sm font-bold text-accent">Ver todos →</Link>
      </div>
      {achLoading ? (
        <LoadingState />
      ) : !achievements?.length ? (
        <p className="mb-6 text-sm text-muted">Todavía no tienes logros — se ganan en las clases.</p>
      ) : (
        <ul className="mb-6 space-y-3">
          {achievements.map((a) => (
            <li key={a.id} className="flex items-center gap-3 rounded-2xl border border-white/10 bg-surface p-4">
              <span aria-hidden="true" className="text-2xl">{a.icon}</span>
              <div className="min-w-0 flex-1">
                <p className="font-bold">{a.name}</p>
                <p className="truncate text-sm text-muted">{a.description}</p>
              </div>
              <span className="shrink-0 text-xs text-muted">
                {new Date(a.dateEarned).toLocaleDateString('es-AR')}
              </span>
            </li>
          ))}
        </ul>
      )}

      {user.role === 'Admin' && <Link to="/admin" className="mb-4 block rounded-2xl border border-accent/40 p-4 text-center font-bold text-accent">Panel de administración</Link>}

      <Link to="/guia" className="mb-4 block rounded-2xl border border-white/20 bg-surface p-4 text-center font-bold text-foreground hover:border-accent/40">
        Guía de uso
      </Link>

      <button
        onClick={handleLogout}
        className="w-full rounded-2xl border border-danger/50 py-3.5 font-bold text-danger"
      >
        Cerrar sesión
      </button>
    </section>
  )
}

function StatCard({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-surface p-4 text-center">
      <p className="text-3xl font-black text-accent">{value}</p>
      <p className="mt-1 text-xs text-muted">{label}</p>
    </div>
  )
}
