import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { deleteAchievement, getAdminAchievements, saveAchievement, type AchievementAdminDto } from '../api/adminApi'
import { EmptyState, LoadingState } from '../../../shared/components/QueryStates'
import { buttonClass, cardClass, CommunityHeader, ErrorNotice, inputClass, secondaryClass } from '../../community/components/CommunityUI'

/** Catálogo de logros: crear, editar y eliminar/ocultar (si ya fue otorgado, se oculta conservando el historial). */
export default function AdminAchievementsPage() {
  const [editing, setEditing] = useState<AchievementAdminDto | 'new' | null>(null)
  const query = useQuery({ queryKey: ['admin', 'achievements'], queryFn: getAdminAchievements })

  return (
    <section className="p-4">
      <CommunityHeader title="Logros" subtitle="Eliminar un logro con dueños lo oculta del catálogo; el historial de los alumnos se conserva." back="/admin">
        <button className={`${buttonClass} mt-3`} onClick={() => setEditing('new')}>+ Nuevo logro</button>
      </CommunityHeader>

      {editing && (
        <AchievementForm
          key={editing === 'new' ? 'new' : editing.id}
          achievement={editing === 'new' ? null : editing}
          onClose={() => setEditing(null)}
        />
      )}

      {query.isLoading && <LoadingState />}
      <ErrorNotice error={query.error} />
      {!query.isLoading && query.data?.length === 0 && <EmptyState message="Todavía no hay logros en el catálogo." />}

      <ul className="space-y-3">
        {query.data?.map((achievement) => (
          <AchievementRow key={achievement.id} achievement={achievement} onEdit={() => setEditing(achievement)} />
        ))}
      </ul>
    </section>
  )
}

function AchievementRow({ achievement, onEdit }: { achievement: AchievementAdminDto; onEdit: () => void }) {
  const queryClient = useQueryClient()
  const mutation = useMutation({
    mutationFn: () => deleteAchievement(achievement.id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin', 'achievements'] }),
  })
  const handleDelete = () => {
    const message = achievement.grantedCount > 0
      ? `"${achievement.name}" ya lo ganaron ${achievement.grantedCount} alumno(s). Se ocultará del catálogo conservando su historial. ¿Continuar?`
      : `¿Eliminar "${achievement.name}" definitivamente?`
    if (confirm(message)) mutation.mutate()
  }
  return (
    <li className={`${cardClass} ${achievement.isHidden ? 'opacity-60' : ''}`}>
      <div className="flex items-start gap-3">
        <span aria-hidden="true" className="text-2xl">{achievement.icon}</span>
        <div className="min-w-0 flex-1">
          <p className="font-bold">{achievement.name}</p>
          <p className="mt-0.5 text-sm text-muted">{achievement.description}</p>
          <p className="mt-1 text-xs text-muted">
            {achievement.grantedCount} alumno(s) lo ganaron
            {achievement.isHidden && ' · OCULTO del catálogo'}
          </p>
        </div>
      </div>
      <div className="mt-3 flex gap-2">
        <button className={secondaryClass} onClick={onEdit}>Editar</button>
        {!achievement.isHidden && (
          <button className={`${secondaryClass} text-danger`} disabled={mutation.isPending} onClick={handleDelete}>
            {achievement.grantedCount > 0 ? 'Ocultar' : 'Eliminar'}
          </button>
        )}
      </div>
      <ErrorNotice error={mutation.error} />
    </li>
  )
}

function AchievementForm({ achievement, onClose }: { achievement: AchievementAdminDto | null; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [form, setForm] = useState({
    name: achievement?.name ?? '',
    description: achievement?.description ?? '',
    icon: achievement?.icon ?? '',
  })
  const mutation = useMutation({
    mutationFn: () => saveAchievement(achievement?.id, form.name, form.description, form.icon),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['admin', 'achievements'] })
      await queryClient.invalidateQueries({ queryKey: ['achievements'] })
      onClose()
    },
  })
  const submit = (e: FormEvent) => { e.preventDefault(); mutation.mutate() }

  return (
    <form onSubmit={submit} className={`${cardClass} mb-6 space-y-4 border-accent/30`}>
      <h2 className="font-bold">{achievement ? 'Editar logro' : 'Nuevo logro'}</h2>
      <label className="block text-sm font-semibold">Nombre
        <input className={`${inputClass} mt-1`} value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} maxLength={100} required />
      </label>
      <label className="block text-sm font-semibold">Descripción
        <textarea className={`${inputClass} mt-1`} rows={2} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} maxLength={500} required />
      </label>
      <label className="block text-sm font-semibold">Icono <span className="font-normal text-muted">(emoji o texto corto)</span>
        <input className={`${inputClass} mt-1`} value={form.icon} onChange={(e) => setForm({ ...form, icon: e.target.value })} maxLength={50} placeholder="🏅" required />
      </label>
      <ErrorNotice error={mutation.error} />
      <div className="flex gap-2">
        <button className={buttonClass} disabled={mutation.isPending}>{mutation.isPending ? 'Guardando…' : 'Guardar'}</button>
        <button type="button" className={secondaryClass} onClick={onClose}>Cancelar</button>
      </div>
    </form>
  )
}
