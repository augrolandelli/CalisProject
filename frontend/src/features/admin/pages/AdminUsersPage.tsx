import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { changeRole, getUsers, type AdminUserDto } from '../api/adminApi'
import { apiErrorMessage } from '../../../shared/api/client'
import { ErrorState, LoadingState } from '../../../shared/components/QueryStates'
import { CommunityHeader, inputClass } from '../../community/components/CommunityUI'

/**
 * Gestión de usuarios (Admin): buscador, filtro por rol y cambio de rol
 * Guerrero ↔ Clover (flujo de upgrade manual por WhatsApp).
 */
export default function AdminUsersPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [role, setRole] = useState('')

  const { data: users, isLoading, error } = useQuery({
    queryKey: ['admin', 'users', search, role],
    queryFn: () => getUsers({ search: search || undefined, role: role || undefined }),
  })

  const mutation = useMutation({
    mutationFn: ({ userId, role }: { userId: number; role: string }) => changeRole(userId, role),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['admin', 'users'] }),
  })

  if (error) return <ErrorState message={apiErrorMessage(error)} />

  return (
    <section className="p-4">
      <CommunityHeader
        title="Usuarios"
        subtitle="Cambia el rol cuando un Guerrero se comunica para pasar a Clover."
        back="/admin"
      />

      <div className="mb-4 grid grid-cols-2 gap-2">
        <input
          className={inputClass}
          placeholder="Buscar por nombre o email…"
          aria-label="Buscar usuarios"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <select className={inputClass} aria-label="Filtrar por rol" value={role} onChange={(e) => setRole(e.target.value)}>
          <option value="">Todos los roles</option>
          <option value="Guerrero">Guerrero</option>
          <option value="Clover">Clover</option>
        </select>
      </div>

      {isLoading && <LoadingState />}
      {mutation.error && <ErrorState message={apiErrorMessage(mutation.error)} />}
      {!isLoading && users?.length === 0 && (
        <p className="text-sm text-muted">No hay usuarios con esos filtros.</p>
      )}

      <ul className="space-y-3">
        {users?.map((user) => (
          <UserRow
            key={user.id}
            user={user}
            isPending={mutation.isPending}
            onToggle={() =>
              mutation.mutate({
                userId: user.id,
                role: user.role === 'Clover' ? 'Guerrero' : 'Clover',
              })
            }
          />
        ))}
      </ul>
    </section>
  )
}

function UserRow({
  user,
  isPending,
  onToggle,
}: {
  user: AdminUserDto
  isPending: boolean
  onToggle: () => void
}) {
  const isClover = user.role === 'Clover'
  return (
    <li className="flex items-center gap-3 rounded-2xl border border-white/10 bg-surface p-4">
      <div className="min-w-0 flex-1">
        <p className="truncate font-bold">{user.fullName}</p>
        <p className="truncate text-sm text-muted">{user.email}</p>
      </div>
      <span
        className={`rounded-lg px-2.5 py-1 text-xs font-bold uppercase ${
          isClover ? 'bg-accent/15 text-accent' : 'bg-surface-raised text-muted'
        }`}
      >
        {user.role}
      </span>
      <button
        onClick={onToggle}
        disabled={isPending}
        className={`min-h-11 shrink-0 rounded-xl px-3 text-sm font-bold disabled:opacity-50 ${
          isClover ? 'border border-white/20 text-muted' : 'bg-accent text-accent-foreground'
        }`}
      >
        {isClover ? 'Bajar a Guerrero' : 'Subir a Clover'}
      </button>
    </li>
  )
}
