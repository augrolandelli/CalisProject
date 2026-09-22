import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { getEvent, saveEvent, type CommunityEvent } from '../../community/api/communityApi'
import { CommunityHeader, ErrorNotice, buttonClass, inputClass, secondaryClass } from '../../community/components/CommunityUI'
import { localDateInput, useCommunityIdentity } from '../../community/communitySupport'
import { LoadingState } from '../../../shared/components/QueryStates'

export default function EventEditorPage() {
  const id = Number(useParams().id) || undefined
  const identity = useCommunityIdentity()
  const query = useQuery({ queryKey: ['community', identity, 'events', id], queryFn: ({ signal }) => getEvent(id!, signal), enabled: !!id })
  return <section className="p-4">
    <CommunityHeader title={id ? 'Editar evento' : 'Nuevo evento'} subtitle="Un nuevo motivo para entrenar y encontrarnos." back="/admin/community" />
    {id && query.isLoading && <LoadingState />}<ErrorNotice error={query.error} />
    {(!id || query.data) && <EventForm key={id ?? 'new'} existing={query.data} />}
  </section>
}

function EventForm({ existing }: { existing?: CommunityEvent }) {
  const [title, setTitle] = useState(existing?.title ?? '')
  const [description, setDescription] = useState(existing?.description ?? '')
  const [location, setLocation] = useState(existing?.location ?? '')
  const [startsAt, setStartsAt] = useState(existing ? localDateInput(existing.startsAt) : '')
  const [endsAt, setEndsAt] = useState(existing ? localDateInput(existing.endsAt) : '')
  const [capacity, setCapacity] = useState(existing?.capacity?.toString() ?? '')
  const navigate = useNavigate()
  const client = useQueryClient()
  const mutation = useMutation({
    networkMode: 'always',
    mutationFn: () => saveEvent(existing?.id, { title, description, location, startsAt: new Date(startsAt).toISOString(), endsAt: new Date(endsAt).toISOString(), capacity: capacity ? Number(capacity) : null }),
    onSuccess: async item => { await client.invalidateQueries({ queryKey: ['community'] }); navigate(`/community/events/${item.id}`, { replace: true }) },
  })
  const submit = (event: FormEvent) => { event.preventDefault(); mutation.mutate() }
  return <form onSubmit={submit} className="space-y-5">
    <label className="block text-sm font-semibold">Título<input className={`${inputClass} mt-2`} value={title} onChange={e => setTitle(e.target.value)} maxLength={200} required /></label>
    <label className="block text-sm font-semibold">Descripción<textarea className={`${inputClass} mt-2`} rows={5} value={description} onChange={e => setDescription(e.target.value)} maxLength={5000} required /></label>
    <label className="block text-sm font-semibold">Lugar<input className={`${inputClass} mt-2`} value={location} onChange={e => setLocation(e.target.value)} maxLength={300} required /></label>
    <label className="block text-sm font-semibold">Inicio (hora local)<input className={`${inputClass} mt-2`} type="datetime-local" value={startsAt} onChange={e => setStartsAt(e.target.value)} required /></label>
    <label className="block text-sm font-semibold">Finalización (hora local)<input className={`${inputClass} mt-2`} type="datetime-local" value={endsAt} min={startsAt || undefined} onChange={e => setEndsAt(e.target.value)} required /></label>
    <label className="block text-sm font-semibold">Cupo máximo <span className="font-normal text-muted">(opcional)</span><input className={`${inputClass} mt-2`} type="number" inputMode="numeric" min={Math.max(1, existing?.registeredCount ?? 0)} max={10000} step={1} value={capacity} onChange={e => setCapacity(e.target.value)} /><span className="mt-2 block text-xs font-normal text-muted">Deja el campo vacío para un evento sin límite de cupos.</span></label>
    <ErrorNotice error={mutation.error} />
    <div className="flex flex-wrap gap-3"><button className={buttonClass} disabled={mutation.isPending}>{mutation.isPending ? 'Guardando…' : existing ? 'Guardar cambios' : 'Crear evento'}</button><Link className={secondaryClass} to="/admin/community">Volver a gestión</Link></div>
  </form>
}
