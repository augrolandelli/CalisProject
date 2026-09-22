import { useEffect, useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { getMediaLink, setLike, type Media, type Post } from '../api/communityApi'
import { cardClass, CommunityIcon, ErrorNotice, secondaryClass } from './CommunityUI'
import { dateLabel, useCommunityIdentity } from '../communitySupport'

export function PrivateMedia({ media }: { media: Media }) {
  const identity = useCommunityIdentity()
  const ref = useRef<HTMLDivElement>(null)
  const [visible, setVisible] = useState(false)
  const [failed, setFailed] = useState(false)
  useEffect(() => {
    const observer = new IntersectionObserver(entries => { if (entries.some(e => e.isIntersecting)) setVisible(true) }, { rootMargin: '150px' })
    if (ref.current) observer.observe(ref.current)
    return () => observer.disconnect()
  }, [])
  const query = useQuery({
    queryKey: ['community', identity, 'media', media.id], queryFn: ({ signal }) => getMediaLink(media.id, signal),
    enabled: visible, staleTime: 180000, gcTime: 0, refetchInterval: visible ? 240000 : false,
  })
  const retry = () => { setFailed(false); void query.refetch() }
  return <div ref={ref} className="overflow-hidden rounded-xl bg-background">
    {query.error || failed ? <div className="p-3"><p className="mb-2 text-sm text-muted">No se pudo cargar {media.fileName}.</p><button type="button" className={secondaryClass} onClick={retry}>Reintentar archivo</button></div>
      : !query.data ? <div className="flex aspect-video items-center justify-center text-sm text-muted" aria-busy="true">Cargando archivo…</div>
      : media.contentType === 'video/mp4'
        ? <video key={query.data.url} src={query.data.url} controls playsInline preload="metadata" aria-label={media.fileName} onError={() => setFailed(true)} className="max-h-96 w-full" />
        : <img src={query.data.url} alt={media.fileName} loading="lazy" decoding="async" onError={() => setFailed(true)} className="max-h-96 w-full object-contain" />}
  </div>
}

export function PostCard({ post, detail = false }: { post: Post; detail?: boolean }) {
  const client = useQueryClient()
  const identity = useCommunityIdentity()
  const mutation = useMutation({
    networkMode: 'always',
    mutationFn: () => setLike(post.id, !post.isLiked),
    onSuccess: () => client.invalidateQueries({ queryKey: ['community', identity, 'posts'] }),
  })
  const kind = post.kind === 'achievement' ? 'Logro de la comunidad' : post.kind === 'competition' ? 'Competencia' : 'Novedades del gimnasio'
  return <article className={`${cardClass} ${post.kind === 'achievement' ? 'border-accent/25' : ''}`}>
    <div className="mb-3 flex items-center gap-2 text-xs font-semibold text-accent">
      <CommunityIcon name={post.kind === 'achievement' ? 'trophy' : post.kind === 'competition' ? 'calendar' : 'community'} />{kind}
    </div>
    <h2 className="text-lg font-bold leading-snug break-words">{detail ? post.title : <Link className="hover:text-accent" to={`/community/posts/${post.id}`}>{post.title}</Link>}</h2>
    <p className="mt-1 text-xs text-muted">{post.authorName} · {dateLabel(post.createdAt)}{post.updatedAt && ' · editado'}</p>
    <p className={`mt-4 whitespace-pre-wrap break-words text-sm leading-relaxed ${detail ? '' : 'line-clamp-5'}`}>{post.content}</p>
    {post.startsAt && <p className="mt-3 text-sm text-accent">{dateLabel(post.startsAt)} · {post.location}</p>}
    {post.media.length > 0 && <div className="mt-4 space-y-3">{post.media.map(media => <PrivateMedia key={media.id} media={media} />)}</div>}
    <div className="mt-4 flex items-center justify-between gap-2 border-t border-white/10 pt-3">
      <button type="button" aria-pressed={post.isLiked} aria-label={`${post.isLiked ? 'Quitar' : 'Dar'} me gusta a ${post.title}`} disabled={mutation.isPending} onClick={() => mutation.mutate()} className={`${secondaryClass} ${post.isLiked ? 'border-accent/40 text-accent' : 'text-muted'}`}>
        <CommunityIcon name="heart" filled={post.isLiked} /><span>{post.likeCount}</span><span className="text-xs">Me gusta</span>
      </button>
      {!detail && <Link to={`/community/posts/${post.id}`} className="inline-flex min-h-11 items-center text-sm font-semibold text-accent">Ver publicación <CommunityIcon name="arrow" /></Link>}
    </div>
    <ErrorNotice error={mutation.error} />
  </article>
}
