import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { useAuthStore } from '../../auth/authStore'
import { deleteReview, getReviewEligibility, getReviews, moderateReview, saveReview, type Review } from '../api/communityApi'
import { EmptyState, LoadingState } from '../../../shared/components/QueryStates'
import { buttonClass, cardClass, CommunityIcon, ErrorNotice, inputClass, Pagination, secondaryClass } from './CommunityUI'
import { dateLabel, useCommunityIdentity } from '../communitySupport'

function Stars({ rating }: { rating: number }) {
  return <span className="inline-flex gap-0.5 text-accent" aria-label={`${rating} de 5 estrellas`}>{[1, 2, 3, 4, 5].map(n => <CommunityIcon key={n} name="star" filled={n <= rating} className="h-4 w-4" />)}</span>
}

export function ReviewList({ sessionId, moderation = false }: { sessionId?: number; moderation?: boolean }) {
  const [page, setPage] = useState(1)
  const identity = useCommunityIdentity()
  const query = useQuery({ queryKey: ['community', identity, 'reviews', sessionId, moderation, page], queryFn: ({ signal }) => getReviews(page, sessionId, moderation, signal) })
  return <div>
    {query.isLoading && <LoadingState />}
    <ErrorNotice error={query.error} />
    {query.data && <>
      {!moderation && <div className="mb-4 flex items-center gap-3 rounded-xl bg-surface-raised p-4">
        <CommunityIcon name="star" filled className="h-7 w-7 text-accent" />
        <div><p className="text-xl font-black">{query.data.averageRating?.toLocaleString('es-AR') ?? '—'} <span className="text-sm font-normal text-muted">/ 5</span></p><p className="text-xs text-muted">{query.data.ratingCount} valoraciones</p></div>
      </div>}
      {query.data.reviews.items.length === 0 && <EmptyState message="Todavía no hay reseñas para mostrar." />}
      <div className="space-y-3">{query.data.reviews.items.map(review => <ReviewCard key={review.id} review={review} moderation={moderation} />)}</div>
      <Pagination page={page} total={query.data.reviews.total} pageSize={query.data.reviews.pageSize} onChange={setPage} />
    </>}
  </div>
}

function ReviewCard({ review, moderation }: { review: Review; moderation: boolean }) {
  const user = useAuthStore(s => s.user)
  const client = useQueryClient()
  const mutation = useMutation({
    networkMode: 'always',
    mutationFn: (action: 'delete' | 'moderate') => action === 'delete' ? deleteReview(review.id) : moderateReview(review.id, !review.isHidden),
    onSuccess: () => client.invalidateQueries({ queryKey: ['community'] }),
  })
  return <article className={cardClass}>
    <div className="flex items-start justify-between gap-2"><p className="font-semibold break-words">{review.authorName}</p><Stars rating={review.rating} /></div>
    <p className="mt-1 text-xs text-muted">{review.sessionId ? <Link className="underline underline-offset-2" to={`/classes/${review.sessionId}`}>{review.sessionTitle}</Link> : review.sessionTitle} · {dateLabel(review.sessionDate)}</p>
    {review.isHidden && <p className="mt-2 text-xs font-bold text-warning">Retirada por moderación</p>}
    {review.content && <p className="mt-3 whitespace-pre-wrap break-words text-sm leading-relaxed">{review.content}</p>}
    <p className="mt-3 text-xs text-muted">Publicada el {dateLabel(review.createdAt)}</p>
    <div className="mt-2 flex flex-wrap gap-2">
      {moderation && <button className={secondaryClass} disabled={mutation.isPending} onClick={() => mutation.mutate('moderate')}>{review.isHidden ? 'Restaurar reseña' : 'Retirar reseña'}</button>}
      {user?.id === review.userId && <button className={`${secondaryClass} text-danger`} disabled={mutation.isPending} onClick={() => { if (confirm('¿Eliminar tu reseña?')) mutation.mutate('delete') }}>Eliminar mi reseña</button>}
    </div>
    <ErrorNotice error={mutation.error} />
  </article>
}

export function ClassReviews({ sessionId }: { sessionId: number }) {
  const user = useAuthStore(s => s.user)
  const member = user?.role === 'Clover' || user?.role === 'Admin'
  const identity = useCommunityIdentity()
  const query = useQuery({
    queryKey: ['community', identity, 'review-eligibility', sessionId],
    queryFn: ({ signal }) => getReviewEligibility(sessionId, signal), enabled: member, refetchInterval: 60000,
  })
  return <section className="mt-8 border-t border-white/10 pt-6" aria-labelledby="class-reviews-title">
    <h2 id="class-reviews-title" className="mb-4 text-lg font-bold">Reseñas de la clase</h2>
    {member && query.isLoading && <LoadingState />}
    <ErrorNotice error={query.error} />
    {query.data?.canReview ? <ReviewForm key={`${sessionId}-${query.data.myReview?.id ?? 'new'}`} sessionId={sessionId} existing={query.data.myReview} />
      : query.data?.reason && <p className="mb-4 text-sm text-muted">{query.data.reason}</p>}
    <ReviewList sessionId={sessionId} />
  </section>
}

function ReviewForm({ sessionId, existing }: { sessionId: number; existing: Review | null }) {
  const [rating, setRating] = useState(existing?.rating ?? 0)
  const [content, setContent] = useState(existing?.content ?? '')
  const client = useQueryClient()
  const mutation = useMutation({ networkMode: 'always', mutationFn: () => saveReview(sessionId, rating, content), onSuccess: () => client.invalidateQueries({ queryKey: ['community'] }) })
  const submit = (event: FormEvent) => { event.preventDefault(); mutation.mutate() }
  return <form onSubmit={submit} className={`${cardClass} mb-5 space-y-3`}>
    <fieldset><legend className="mb-2 text-sm font-bold">{existing ? 'Editar mi valoración' : '¿Cómo estuvo la clase?'}</legend>
      <div className="flex gap-2">{[1, 2, 3, 4, 5].map(value => <label key={value} className="relative block cursor-pointer">
        <input className="peer absolute inset-0 h-full w-full cursor-pointer opacity-0" type="radio" name={`rating-${sessionId}`} value={value} checked={rating === value} onChange={() => setRating(value)} required />
        <span className="flex min-h-11 min-w-11 flex-col items-center justify-center rounded-lg text-accent peer-focus-visible:outline-2 peer-focus-visible:outline-offset-2 peer-focus-visible:outline-accent peer-checked:bg-accent/15"><CommunityIcon name="star" filled={value <= rating} /><span className="sr-only">{value} {value === 1 ? 'estrella' : 'estrellas'}</span></span>
      </label>)}</div>
    </fieldset>
    <label className="block text-sm font-semibold">Tu experiencia <span className="font-normal text-muted">(opcional)</span><textarea className={`${inputClass} mt-2`} maxLength={1500} rows={3} value={content} onChange={e => setContent(e.target.value)} /></label>
    <ErrorNotice error={mutation.error} />
    {mutation.isSuccess && <p role="status" className="text-sm text-success">Tu reseña fue guardada.</p>}
    <button className={buttonClass} disabled={mutation.isPending || rating === 0}>{mutation.isPending ? 'Guardando…' : existing ? 'Actualizar reseña' : 'Publicar reseña'}</button>
  </form>
}
