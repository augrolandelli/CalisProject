import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { useAuthStore } from '../../auth/authStore'
import { getEvents, getPosts } from '../api/communityApi'
import { EmptyState, LoadingState } from '../../../shared/components/QueryStates'
import { CloverInvitation, CommunityIcon, ErrorNotice, Pagination, cardClass, secondaryClass } from '../components/CommunityUI'
import { dateLabel, useCommunityIdentity, useNow } from '../communitySupport'
import { PostCard } from '../components/PostCard'
import { ReviewList } from '../components/Reviews'

export default function CommunityPage() {
  const user = useAuthStore(s => s.user)
  const member = user?.role === 'Clover' || user?.role === 'Admin'
  const [params, setParams] = useSearchParams()
  const tab = params.get('tab') ?? (member ? 'posts' : 'reviews')
  const tabs = [{ id: 'posts', label: 'Novedades' }, { id: 'events', label: 'Eventos' }, { id: 'reviews', label: 'Reseñas' }]
  return <section className="p-4">
    <header className="mb-6 pt-4">
      <div className="mb-2 flex items-center gap-2 text-xs font-bold tracking-widest text-accent uppercase"><CommunityIcon name="community" />CalisApp / juntos</div>
      <h1 className="text-3xl font-black tracking-tight">Comunidad</h1>
      <p className="mt-2 text-sm text-muted">Cada avance cuenta. Y se celebra en equipo.</p>
      {user?.role === 'Admin' && <Link className={`${secondaryClass} mt-4`} to="/admin/community">Gestionar comunidad <CommunityIcon name="arrow" /></Link>}
    </header>
    <nav aria-label="Secciones de comunidad" className="mb-5 flex gap-1 rounded-xl bg-surface p-1">
      {tabs.map(item => <button key={item.id} onClick={() => setParams({ tab: item.id })} aria-current={tab === item.id ? 'page' : undefined} className={`flex min-h-11 flex-1 items-center justify-center gap-1 rounded-lg px-2 text-sm font-bold ${tab === item.id ? 'bg-accent text-accent-foreground' : 'text-muted hover:text-foreground'}`}>
        {item.label}{!member && item.id !== 'reviews' && <CommunityIcon name="lock" className="h-3 w-3" />}
      </button>)}
    </nav>
    {!member && <CloverInvitation />}
    {tab === 'reviews' ? <ReviewList /> : member && tab === 'events' ? <EventList /> : member && <PostList />}
    {!member && tab !== 'reviews' && <Link to="/community?tab=reviews" className="inline-flex min-h-11 items-center font-semibold text-accent">Ver experiencias de alumnos →</Link>}
  </section>
}

function PostList() {
  const [page, setPage] = useState(1)
  const identity = useCommunityIdentity()
  const query = useQuery({ queryKey: ['community', identity, 'posts', 'list', page], queryFn: ({ signal }) => getPosts(page, signal) })
  return <>
    {query.isLoading && <LoadingState />}
    <ErrorNotice error={query.error} />
    {query.data && <>
      {query.data.items.length === 0 && <EmptyState message="El tablón está listo. Pronto encontrarás novedades y nuevos logros." />}
      <div className="space-y-4">{query.data.items.map(post => <PostCard key={post.id} post={post} />)}</div>
      <Pagination page={page} total={query.data.total} pageSize={query.data.pageSize} onChange={setPage} />
    </>}
  </>
}

function EventList() {
  const [history, setHistory] = useState(false)
  const [page, setPage] = useState(1)
  const identity = useCommunityIdentity()
  const now = useNow()
  const query = useQuery({ queryKey: ['community', identity, 'events', 'list', history, page], queryFn: ({ signal }) => getEvents(page, history, signal) })
  return <>
    <div className="mb-4 flex gap-2" role="group" aria-label="Filtrar eventos">
      {[false, true].map(value => <button key={String(value)} aria-pressed={history === value} className={`${secondaryClass} ${history === value ? 'border-accent/40 text-accent' : ''}`} onClick={() => { setHistory(value); setPage(1) }}>{value ? 'Historial' : 'Próximos'}</button>)}
    </div>
    {query.isLoading && <LoadingState />}
    <ErrorNotice error={query.error} />
    {query.data && <>
      {query.data.items.length === 0 && <EmptyState message={history ? 'Todavía no hay eventos en el historial.' : 'Pronto tendremos nuevos encuentros. ¡Estate atento!'} />}
      <div className="space-y-3">{query.data.items.map(item => {
        const started = new Date(item.startsAt).getTime() <= now
        return <Link key={item.id} to={`/community/events/${item.id}`} className={`${cardClass} block transition-colors hover:border-accent/40`}>
          <div className="mb-2 flex items-center gap-2 text-xs font-semibold text-accent"><CommunityIcon name="calendar" />{dateLabel(item.startsAt)}</div>
          <h2 className="text-lg font-bold break-words">{item.title}</h2><p className="mt-1 text-sm text-muted">{item.location}</p>
          <p className="mt-3 line-clamp-2 text-sm leading-relaxed">{item.description}</p>
          <div className="mt-4 flex flex-wrap items-center gap-2 text-xs font-bold">
            {item.isCancelled ? <span className="text-danger">Cancelado</span> : started ? <span className="text-muted">Inscripciones cerradas</span> : <span className="text-accent">{item.capacity === null ? 'Sin límite de cupos' : `${Math.max(0, item.capacity - item.registeredCount)} lugares disponibles`}</span>}
            {item.isRegistered && <span className="rounded-full bg-success/15 px-2 py-1 text-success">{item.isCancelled ? 'Tenías una inscripción' : 'Estás inscrito'}</span>}
          </div>
        </Link>
      })}</div>
      <Pagination page={page} total={query.data.total} pageSize={query.data.pageSize} onChange={setPage} />
    </>}
  </>
}
