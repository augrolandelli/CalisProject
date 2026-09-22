import { useRef } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { getVideo } from '../api/exercisesApi'
import { apiErrorMessage } from '../../../shared/api/client'
import { DifficultyBadge } from '../../../shared/components/DifficultyBadge'
import { ErrorState, LoadingState } from '../../../shared/components/QueryStates'

/**
 * Detalle de ejercicio con reproductor de video y pantalla completa
 * (fullscreen nativo — spec §3.5).
 */
export default function ExerciseDetailPage() {
  const { id } = useParams<{ id: string }>()
  const playerRef = useRef<HTMLVideoElement>(null)

  const { data: video, isLoading, error } = useQuery({
    queryKey: ['video', id],
    queryFn: () => getVideo(Number(id)),
    enabled: !!id,
  })

  const goFullscreen = () => {
    const player = playerRef.current
    if (!player) return
    if (document.fullscreenElement) {
      void document.exitFullscreen()
    } else {
      void player.requestFullscreen?.()
    }
  }

  if (isLoading) return <LoadingState />
  if (error) return <ErrorState message={apiErrorMessage(error)} />
  if (!video) return <ErrorState message="El video no existe." />

  return (
    <section>
      <div className="relative bg-black">
        <video
          ref={playerRef}
          src={video.url}
          controls
          playsInline
          className="aspect-video w-full"
          preload="metadata"
        />
        <button
          onClick={goFullscreen}
          aria-label="Pantalla completa"
          className="absolute right-3 top-3 flex h-11 w-11 items-center justify-center rounded-full bg-black/60 text-lg backdrop-blur"
        >
          ⛶
        </button>
        <Link
          to={`/exercises/category/${video.categoryId}`}
          aria-label="Volver"
          className="absolute left-3 top-3 flex h-11 w-11 items-center justify-center rounded-full bg-black/60 text-lg backdrop-blur"
        >
          ←
        </Link>
      </div>

      <div className="space-y-5 p-4">
        <div>
          <div className="mb-2 flex gap-2">
            <DifficultyBadge difficulty={video.difficulty} />
            <span className="rounded-lg bg-surface-raised px-2.5 py-1 text-xs font-bold tracking-wide text-muted uppercase">
              {video.category.name}
            </span>
          </div>
          <h1 className="text-2xl font-black">{video.title}</h1>
          <p className="mt-2 text-muted">{video.description}</p>
        </div>

        <div className="rounded-2xl border border-white/10 bg-surface p-4">
          <h2 className="mb-1 text-sm font-bold tracking-wider text-accent uppercase">Prerequisitos</h2>
          <p className="text-sm text-muted">{video.requisites}</p>
        </div>
      </div>
    </section>
  )
}
