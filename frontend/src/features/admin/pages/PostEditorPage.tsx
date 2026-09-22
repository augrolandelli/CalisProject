import { useEffect, useRef, useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { abandonMedia, getPost, savePost, uploadMedia, type Media, type Post, type WritePost } from '../../community/api/communityApi'
import { CommunityHeader, ErrorNotice, buttonClass, inputClass, secondaryClass } from '../../community/components/CommunityUI'
import { localDateInput, useCommunityIdentity } from '../../community/communitySupport'
import { PrivateMedia } from '../../community/components/PostCard'
import { LoadingState } from '../../../shared/components/QueryStates'

export default function PostEditorPage() {
  const id = Number(useParams().id) || undefined
  const identity = useCommunityIdentity()
  const query = useQuery({ queryKey: ['community', identity, 'posts', id], queryFn: ({ signal }) => getPost(id!, signal), enabled: !!id })
  return <section className="p-4">
    <CommunityHeader title={id ? 'Editar publicación' : 'Nueva publicación'} subtitle="Comparte novedades y momentos del gimnasio con los Clover." back="/admin/community" />
    {id && query.isLoading && <LoadingState />}<ErrorNotice error={query.error} />
    {query.data?.kind === 'achievement' ? <p>Los logros automáticos se gestionan desde el tablón.</p>
      : (!id || query.data) && <PostForm key={id ?? 'new'} existing={query.data} />}
  </section>
}

function PostForm({ existing }: { existing?: Post }) {
  const [title, setTitle] = useState(existing?.title ?? '')
  const [content, setContent] = useState(existing?.content ?? '')
  const [kind, setKind] = useState<WritePost['kind']>(existing?.kind === 'competition' ? 'competition' : 'announcement')
  const [startsAt, setStartsAt] = useState(existing?.startsAt ? localDateInput(existing.startsAt) : '')
  const [location, setLocation] = useState(existing?.location ?? '')
  const [media, setMedia] = useState<Media[]>(existing?.media ?? [])
  const [upload, setUpload] = useState<{ name: string; progress: number } | null>(null)
  const [uploadError, setUploadError] = useState<unknown>(null)
  const pendingUploads = useRef(new Set<string>())
  const controller = useRef<AbortController | null>(null)
  const saved = useRef(false)
  const client = useQueryClient()
  const navigate = useNavigate()
  useEffect(() => () => {
    controller.current?.abort()
    if (!saved.current) for (const id of pendingUploads.current) void abandonMedia(id).catch(() => {})
  }, [])
  const mutation = useMutation({
    networkMode: 'always',
    mutationFn: () => savePost(existing?.id, { title, content, kind, mediaIds: media.map(m => m.id), startsAt: kind === 'competition' && startsAt ? new Date(startsAt).toISOString() : null, location: kind === 'competition' ? location : null }),
    onSuccess: async post => {
      saved.current = true
      await client.invalidateQueries({ queryKey: ['community'] })
      navigate(`/community/posts/${post.id}`, { replace: true })
    },
  })
  const addFiles = async (files: File[]) => {
    setUploadError(null)
    const existingPhotos = media.filter(m => m.contentType.startsWith('image/')).length
    const newPhotos = files.filter(f => f.type.startsWith('image/')).length
    const existingVideos = media.filter(m => m.contentType === 'video/mp4').length
    if (existingPhotos + newPhotos > 4 || existingVideos + files.filter(f => f.type === 'video/mp4').length > 1) {
      setUploadError('Puedes adjuntar hasta 4 fotos y un video por publicación.'); return
    }
    if (files.some(f => !['image/jpeg', 'image/png', 'image/webp', 'video/mp4'].includes(f.type) || f.size === 0 || f.size > (f.type === 'video/mp4' ? 100 : 10) * 1024 * 1024)) {
      setUploadError('Usa JPG, PNG o WebP de hasta 10 MB, o MP4 de hasta 100 MB.'); return
    }
    controller.current = new AbortController()
    const signal = controller.current.signal
    try {
      for (const file of files) {
        if (signal.aborted) break
        setUpload({ name: file.name, progress: 0 })
        const uploaded = await uploadMedia(file, progress => setUpload({ name: file.name, progress }), signal)
        pendingUploads.current.add(uploaded.id)
        setMedia(current => [...current, uploaded])
      }
    } catch (error) { if (!signal.aborted) setUploadError(error) }
    finally { setUpload(null) }
  }
  const removeMedia = (item: Media) => {
    setMedia(current => current.filter(m => m.id !== item.id))
    if (pendingUploads.current.delete(item.id)) void abandonMedia(item.id).catch(() => {})
  }
  const submit = (event: FormEvent) => { event.preventDefault(); if (!upload) mutation.mutate() }
  return <form onSubmit={submit} className="space-y-5">
    <label className="block text-sm font-semibold">Tipo<select className={`${inputClass} mt-2`} value={kind} onChange={e => setKind(e.target.value as WritePost['kind'])}><option value="announcement">Novedad del gimnasio</option><option value="competition">Anuncio de competencia</option></select></label>
    <label className="block text-sm font-semibold">Título<input className={`${inputClass} mt-2`} value={title} onChange={e => setTitle(e.target.value)} maxLength={200} required /></label>
    <label className="block text-sm font-semibold">Contenido<textarea className={`${inputClass} mt-2`} value={content} onChange={e => setContent(e.target.value)} maxLength={5000} rows={7} required /><span className="mt-1 block text-xs font-normal text-muted">{content.length}/5000 caracteres. El contenido se publica como texto.</span></label>
    {kind === 'competition' && <fieldset className="space-y-4 rounded-2xl border border-white/10 p-4"><legend className="px-2 text-sm font-bold">Datos de la competencia</legend>
      <label className="block text-sm font-semibold">Fecha y hora local<input className={`${inputClass} mt-2`} type="datetime-local" value={startsAt} onChange={e => setStartsAt(e.target.value)} required /></label>
      <label className="block text-sm font-semibold">Lugar<input className={`${inputClass} mt-2`} value={location} onChange={e => setLocation(e.target.value)} maxLength={300} required /></label>
    </fieldset>}
    <fieldset className="space-y-3"><legend className="mb-2 text-sm font-bold">Fotos y video</legend>
      <p id="upload-help" className="text-xs leading-relaxed text-muted">Hasta 4 fotos JPG, PNG o WebP (10 MB cada una) y un MP4 H.264/AAC (2 minutos, 100 MB).</p>
      <label className="block text-sm font-semibold">Agregar archivos<input className="mt-2 block w-full text-sm file:mr-3 file:rounded-lg file:border-0 file:bg-accent file:px-3 file:py-3 file:font-bold file:text-accent-foreground" type="file" multiple accept="image/jpeg,image/png,image/webp,video/mp4" aria-describedby="upload-help" disabled={!!upload || mutation.isPending} onChange={e => { const files = Array.from(e.target.files ?? []); e.target.value = ''; if (files.length) void addFiles(files) }} /></label>
      {upload && <div role="status" className="rounded-xl bg-surface p-3"><p className="mb-2 truncate text-sm">{upload.progress >= 90 ? 'Validando' : 'Subiendo'}: {upload.name}</p><progress className="h-2 w-full accent-accent" value={upload.progress} max={100} aria-label="Progreso de subida" /><button type="button" className={`${secondaryClass} mt-2`} onClick={() => controller.current?.abort()}>Cancelar subida</button></div>}
      <ErrorNotice error={uploadError} />
      {media.map(item => <div key={item.id} className="rounded-xl border border-white/10 p-2"><PrivateMedia media={item} /><div className="mt-2 flex items-center justify-between gap-2"><span className="truncate text-xs text-muted">{item.fileName}</span><button type="button" className={secondaryClass} disabled={!!upload || mutation.isPending} onClick={() => removeMedia(item)}>Quitar</button></div></div>)}
    </fieldset>
    <ErrorNotice error={mutation.error} />
    <div className="flex flex-wrap gap-3"><button className={buttonClass} disabled={!!upload || mutation.isPending}>{mutation.isPending ? 'Guardando…' : existing ? 'Guardar cambios' : 'Publicar'}</button><Link className={secondaryClass} to="/admin/community">Volver a gestión</Link></div>
  </form>
}
