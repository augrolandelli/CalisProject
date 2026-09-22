import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { deleteCategory, saveCategory } from '../api/adminApi'
import { getCategories } from '../../exercises/api/exercisesApi'
import type { CategoryDto } from '../../../shared/types/catalog'
import { EmptyState, LoadingState } from '../../../shared/components/QueryStates'
import { buttonClass, cardClass, CommunityHeader, ErrorNotice, inputClass, secondaryClass } from '../../community/components/CommunityUI'

/** Gestión de categorías: crear, editar y eliminar (bloqueado si tiene videos o rutinas). */
export default function AdminCategoriesPage() {
  const [editing, setEditing] = useState<CategoryDto | 'new' | null>(null)
  const query = useQuery({ queryKey: ['categories'], queryFn: getCategories })

  return (
    <section className="p-4">
      <CommunityHeader title="Categorías" subtitle="No se puede eliminar una categoría con videos o rutinas asociados." back="/admin">
        <button className={`${buttonClass} mt-3`} onClick={() => setEditing('new')}>+ Nueva categoría</button>
      </CommunityHeader>

      {editing && (
        <CategoryForm
          key={editing === 'new' ? 'new' : editing.id}
          category={editing === 'new' ? null : editing}
          onClose={() => setEditing(null)}
        />
      )}

      {query.isLoading && <LoadingState />}
      <ErrorNotice error={query.error} />
      {!query.isLoading && query.data?.length === 0 && <EmptyState message="No hay categorías." />}

      <ul className="space-y-3">
        {query.data?.map((category) => (
          <CategoryRow key={category.id} category={category} onEdit={() => setEditing(category)} />
        ))}
      </ul>
    </section>
  )
}

function CategoryRow({ category, onEdit }: { category: CategoryDto; onEdit: () => void }) {
  const queryClient = useQueryClient()
  const mutation = useMutation({
    mutationFn: () => deleteCategory(category.id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['categories'] }),
  })
  return (
    <li className={cardClass}>
      <p className="font-bold">{category.name}</p>
      <p className="mt-1 text-sm text-muted">{category.description}</p>
      <div className="mt-3 flex gap-2">
        <button className={secondaryClass} onClick={onEdit}>Editar</button>
        <button className={`${secondaryClass} text-danger`} disabled={mutation.isPending}
          onClick={() => { if (confirm(`¿Eliminar la categoría "${category.name}"?`)) mutation.mutate() }}>
          Eliminar
        </button>
      </div>
      <ErrorNotice error={mutation.error} />
    </li>
  )
}

function CategoryForm({ category, onClose }: { category: CategoryDto | null; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [form, setForm] = useState({ name: category?.name ?? '', description: category?.description ?? '' })
  const mutation = useMutation({
    mutationFn: () => saveCategory(category?.id, form.name, form.description),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['categories'] })
      onClose()
    },
  })
  const submit = (e: FormEvent) => { e.preventDefault(); mutation.mutate() }

  return (
    <form onSubmit={submit} className={`${cardClass} mb-6 space-y-4 border-accent/30`}>
      <h2 className="font-bold">{category ? 'Editar categoría' : 'Nueva categoría'}</h2>
      <label className="block text-sm font-semibold">Nombre
        <input className={`${inputClass} mt-1`} value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} maxLength={100} required />
      </label>
      <label className="block text-sm font-semibold">Descripción
        <textarea className={`${inputClass} mt-1`} rows={2} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} maxLength={500} required />
      </label>
      <ErrorNotice error={mutation.error} />
      <div className="flex gap-2">
        <button className={buttonClass} disabled={mutation.isPending}>{mutation.isPending ? 'Guardando…' : 'Guardar'}</button>
        <button type="button" className={secondaryClass} onClick={onClose}>Cancelar</button>
      </div>
    </form>
  )
}
