import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { cancelEvent, getEvent, getEventParticipants, setEventRegistration } from '../api/communityApi'
import { CommunityHeader, ErrorNotice, Pagination, buttonClass, cardClass, secondaryClass } from '../components/CommunityUI'
import { dateLabel, useCommunityIdentity, useNow } from '../communitySupport'
import { EmptyState, LoadingState } from '../../../shared/components/QueryStates'
import { useAuthStore } from '../../auth/authStore'

export default function EventDetailPage() {
  const id = Number(useParams().id)
  const identity = useCommunityIdentity()
  const admin = useAuthStore(s => s.user?.role === 'Admin')
  const client = useQueryClient()
  const now = useNow()
  const query = useQuery({ queryKey: ['community', identity, 'events', id], queryFn: ({ signal }) => getEvent(id, signal) })
  const mutation = useMutation({ networkMode: 'always', mutationFn: (action: 'join' | 'leave' | 'cancel') => action === 'cancel' ? cancelEvent(id) : setEventRegistration(id, action === 'join'), onSettled: () => client.invalidateQueries({ queryKey: ['community', identity, 'events'] }) })
  const item = query.data
  const open = item && !item.isCancelled && new Date(item.startsAt).getTime() > now
  const full = item && item.capacity !== null && item.registeredCount >= item.capacity
  return <section className="p-4">
    <CommunityHeader title={item?.title ?? 'Evento'} back="/community?tab=events" />
    {query.isLoading && <LoadingState />}<ErrorNotice error={query.error || mutation.error} />
    {item && <>
      <div className={`${cardClass} mb-5 space-y-3 text-sm`}>
        <p className="font-bold text-accent">{dateLabel(item.startsAt)}</p>
        <p className="text-muted">Hasta {dateLabel(item.endsAt)}</p><p className="font-semibold">{item.location}</p>
        <p>{item.registeredCount} inscritos{item.capacity !== null ? ` · cupo ${item.capacity}` : ' · sin límite de cupos'}</p>
        {item.isCancelled && <p role="status" className="font-bold text-danger">Este evento fue cancelado por el gimnasio.</p>}
        {item.isRegistered && <p className="font-bold text-success">{item.isCancelled ? 'Tu inscripción quedó cancelada junto con el evento.' : 'Tu lugar está confirmado.'}</p>}
      </div>
      <p className="mb-6 whitespace-pre-wrap break-words text-sm leading-relaxed">{item.description}</p>
      {open ? <button className={`${item.isRegistered ? secondaryClass : buttonClass} w-full`} disabled={mutation.isPending || (!item.isRegistered && !!full)} onClick={() => mutation.mutate(item.isRegistered ? 'leave' : 'join')}>
        {mutation.isPending ? 'Actualizando…' : item.isRegistered ? 'Cancelar mi inscripción' : full ? 'Evento completo' : 'Anotarme al evento'}
      </button> : !item.isCancelled && <p className="text-sm text-muted">El evento ya comenzó. Las inscripciones están cerradas.</p>}
      {admin && <>
        {open && <div className="my-4 flex flex-wrap gap-2"><Link className={secondaryClass} to={`/admin/community/events/${id}/edit`}>Editar evento</Link><button className={`${secondaryClass} text-danger`} disabled={mutation.isPending} onClick={() => { if (confirm('¿Cancelar el evento para todos los inscritos?')) mutation.mutate('cancel') }}>Cancelar evento</button></div>}
        <Participants id={id} />
      </>}
    </>}
  </section>
}

function Participants({ id }: { id: number }) {
  const [page, setPage] = useState(1)
  const identity = useCommunityIdentity()
  const query = useQuery({ queryKey: ['community', identity, 'events', id, 'participants', page], queryFn: ({ signal }) => getEventParticipants(id, page, signal) })
  return <section className="mt-8 border-t border-white/10 pt-5"><h2 className="mb-3 text-lg font-bold">Inscritos</h2>
    {query.isLoading && <LoadingState />}<ErrorNotice error={query.error} />
    {query.data && <>
      {query.data.items.length === 0 && <EmptyState message="Todavía no hay inscritos." />}
      <ul className="space-y-2">{query.data.items.map(p => <li className={cardClass} key={p.userId}><p className="font-semibold">{p.fullName}</p><p className="mt-1 text-xs text-muted">Inscrito el {dateLabel(p.registeredAt)}</p></li>)}</ul>
      <Pagination page={page} total={query.data.total} pageSize={query.data.pageSize} onChange={setPage} />
    </>}
  </section>
}
