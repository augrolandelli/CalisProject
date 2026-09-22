import { useRef, useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { deleteVideo, saveVideo, type WriteVideoRequest } from '../api/adminApi'
import { getCategories, getVideos, type VideoDto } from '../../exercises/api/exercisesApi'
import { abandonMedia, uploadMedia, type Media } from '../../community/api/communityApi'
import { DifficultyBadge } from '../../../shared/components/DifficultyBadge'
import { EmptyState, LoadingState } from '../../../shared/components/QueryStates'
import { buttonClass, cardClass, CommunityHeader, ErrorNotice, inputClass, secondaryClass } from '../../community/components/CommunityUI'
import { PrivateMedia } from '../../community/components/PostCard'

const MAX_SIZE = 50 * 1024 * 1024

/** Gestión de la videoteca: subir videos de ejercicios (MP4 H.264, hasta 30 s) y eliminar. */
export default function AdminVideosPage() {
  const [editing, setEditing] = useState<VideoDto | 'new' | null>(null)
  const [search, setSearch] = useState('')
  const [categoryId, setCategoryId] = useState<number | undefined>()

  const categories = useQuery({ queryKey: ['categories'], queryFn: getCategories })
  const videos = useQuery({
    queryKey: ['admin', 'videos', categoryId, search],
    queryFn: () => getVideos({ categoryId, searchTerm: search || undefined }),
  })

  return (
    <section className="p-4">
      <CommunityHeader title="Videoteca" subtitle="Videos de ejercicios: MP4 H.264 de hasta 30 segundos. Sin miniatura: la lista muestra el video." back="/admin">
        <button className={`${buttonClass} mt-3`} onClick={() => setEditing('new')}>+ Subir video</button>
      </CommunityHeader>

      {editing && (
        <VideoForm
          key={editing === 'new' ? 'new' : editing.id}
          video={editing === 'new' ? null : editing}
          categories={categories.data ?? []}
          onClose={() => setEditing(null)}
        />
      )}

      <div className="mb-4 grid grid-cols-2 gap-2">
        <input
          className={inputClass}
          placeholder="Buscar por título…"
          aria-label="Buscar videos"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <select className={inputClass} aria-label="Filtrar por categoría" value={categoryId ?? ''} onChange={(e) => setCategoryId(e.target.value ? Number(e.target.value) : undefined)}>
          <option value="">Todas las categorías</option>
          {categories.data?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
        </select>
      </div>

      {videos.isLoading && <LoadingState />}
      <ErrorNotice error={videos.error} />
      {!videos.isLoading && videos.data?.length === 0 && <EmptyState message="No hay videos con esos filtros." />}

      <ul className="space-y-3">
        {videos.data?.map((video) => (
          <VideoRow key={video.id} video={video} onEdit={() => setEditing(video)} />
        ))}
      </ul>
    </section>
  )
}

function VideoRow({ video, onEdit }: { video: VideoDto; onEdit: () => void }) {
  const queryClient = useQueryClient()
  const mutation = useMutation({
    mutationFn: () => deleteVideo(video.id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin', 'videos'] }),
  })
  return (
    <li className={cardClass}>
      <div className="mb-1 flex items-center justify-between gap-2">
        <DifficultyBadge difficulty={video.difficulty} />
        <span className="text-xs text-muted">{video.category.name}</span>
      </div>
      <p className="font-bold">{video.title}</p>
      <p className="mt-1 line-clamp-2 text-sm text-muted">{video.description}</p>
      <div className="mt-3 flex gap-2">
        <button className={secondaryClass} onClick={onEdit}>Editar</button>
        <button
          className={`${secondaryClass} text-danger`}
          disabled={mutation.isPending}
          onClick={() => { if (confirm(`¿Eliminar "${video.title}"?`)) mutation.mutate() }}
        >
          Eliminar
        </button>
      </div>
      <ErrorNotice error={mutation.error} />
    </li>
  )
}

function VideoForm({ video, categories, onClose }: {
  video: VideoDto | null
  categories: { id: number; name: string }[]
  onClose: () => void
}) {
  const queryClient = useQueryClient()
  const [form, setForm] = useState({
    title: video?.title ?? '',
    description: video?.description ?? '',
    difficulty: video?.difficulty ?? 'basica',
    requisites: video?.requisites ?? '',
    categoryId: video?.categoryId ?? categories[0]?.id ?? 0,
  })
  const [media, setMedia] = useState<Media | null>(null)
  const [upload, setUpload] = useState<number | null>(null)
  const [uploadError, setUploadError] = useState<unknown>(null)
  const controller = useRef<AbortController | null>(null)
  // Las categorías cargan async: se deriva la primera disponible sin efecto.
  const categoryId = form.categoryId || categories[0]?.id || 0

  const pickFile = async (file: File | undefined) => {
    setUploadError(null)
    if (!file) return
    if (file.type !== 'video/mp4' || file.size === 0 || file.size > MAX_SIZE) {
      setUploadError('Usa un MP4 (H.264/AAC) de hasta 30 segundos y 50 MB.')
      return
    }
    controller.current = new AbortController()
    try {
      setUpload(0)
      const uploaded = await uploadMedia(file, setUpload, controller.current.signal, 'exercise')
      setMedia(uploaded)
    } catch (error) {
      if (!controller.current.signal.aborted) setUploadError(error)
    } finally {
      setUpload(null)
    }
  }

  const mutation = useMutation({
    mutationFn: () => {
      const request: WriteVideoRequest = { ...form, categoryId, mediaAssetId: media?.id ?? null }
      return saveVideo(video?.id, request)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['admin', 'videos'] })
      await queryClient.invalidateQueries({ queryKey: ['videos'] })
      onClose()
    },
  })

  const submit = (e: FormEvent) => {
    e.preventDefault()
    if (upload !== null) return
    if (!video && !media) { setUploadError('Sube el archivo de video antes de guardar.'); return }
    mutation.mutate()
  }

  return (
    <form onSubmit={submit} className={`${cardClass} mb-6 space-y-4 border-accent/30`}>
      <h2 className="font-bold">{video ? 'Editar video' : 'Nuevo video'}</h2>

      <fieldset className="space-y-2">
        <legend className="text-sm font-bold">Archivo de video</legend>
        <p id="video-help" className="text-xs text-muted">MP4 H.264/AAC, hasta 30 segundos y 50 MB.{video && ' Opcional: sube uno nuevo solo si quieres reemplazarlo.'}</p>
        <input
          type="file"
          accept="video/mp4"
          aria-describedby="video-help"
          disabled={upload !== null || mutation.isPending}
          className="block w-full text-sm file:mr-3 file:rounded-lg file:border-0 file:bg-accent file:px-3 file:py-3 file:font-bold file:text-accent-foreground"
          onChange={(e) => { const f = e.target.files?.[0]; e.target.value = ''; void pickFile(f) }}
        />
        {upload !== null && (
          <div role="status" className="rounded-xl bg-surface p-3">
            <p className="mb-2 text-sm">{upload >= 90 ? 'Validando video…' : 'Subiendo video…'}</p>
            <progress className="h-2 w-full accent-accent" value={upload} max={100} aria-label="Progreso de subida" />
            <button type="button" className={`${secondaryClass} mt-2`} onClick={() => controller.current?.abort()}>Cancelar</button>
          </div>
        )}
        <ErrorNotice error={uploadError} />
        {media && (
          <div className="rounded-xl border border-white/10 p-2">
            <PrivateMedia media={media} />
            <div className="mt-2 flex items-center justify-between gap-2">
              <span className="truncate text-xs text-muted">{media.fileName}</span>
              <button type="button" className={secondaryClass} onClick={() => { void abandonMedia(media.id).catch(() => {}); setMedia(null) }}>Quitar</button>
            </div>
          </div>
        )}
      </fieldset>

      <label className="block text-sm font-semibold">Título
        <input className={`${inputClass} mt-1`} value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} maxLength={200} required />
      </label>
      <label className="block text-sm font-semibold">Descripción
        <textarea className={`${inputClass} mt-1`} rows={3} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} maxLength={2000} required />
      </label>
      <div className="grid grid-cols-2 gap-3">
        <label className="block text-sm font-semibold">Dificultad
          <select className={`${inputClass} mt-1`} value={form.difficulty} onChange={(e) => setForm({ ...form, difficulty: e.target.value })}>
            <option value="basica">Básica</option>
            <option value="intermedia">Intermedia</option>
            <option value="avanzada">Avanzada</option>
          </select>
        </label>
        <label className="block text-sm font-semibold">Categoría
          <select className={`${inputClass} mt-1`} value={categoryId} onChange={(e) => setForm({ ...form, categoryId: Number(e.target.value) })} required>
            {categories.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </label>
      </div>
      <label className="block text-sm font-semibold">Prerequisitos
        <input className={`${inputClass} mt-1`} value={form.requisites} onChange={(e) => setForm({ ...form, requisites: e.target.value })} maxLength={500} placeholder='Ej. "8 dominadas estrictas"' required />
      </label>

      <ErrorNotice error={mutation.error} />
      <div className="flex gap-2">
        <button className={buttonClass} disabled={mutation.isPending || upload !== null}>{mutation.isPending ? 'Guardando…' : 'Guardar video'}</button>
        <button type="button" className={secondaryClass} onClick={onClose}>Cancelar</button>
      </div>
    </form>
  )
}
