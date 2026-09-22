import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { getRutine, type RutineDetailDto } from '../api/routinesApi'
import { apiErrorMessage } from '../../../shared/api/client'
import { DifficultyBadge } from '../../../shared/components/DifficultyBadge'
import { ErrorState, LoadingState } from '../../../shared/components/QueryStates'

/** Genera el PDF de la rutina en el cliente (spec §3.4: "Download routine PDF"). */
async function downloadRutinePdf(rutine: RutineDetailDto) {
  // Lazy-load: jsPDF es pesado y solo se usa bajo demanda
  const { jsPDF } = await import('jspdf')
  const doc = new jsPDF()
  let y = 20

  doc.setFontSize(20)
  doc.text(rutine.title, 14, y)
  y += 10

  doc.setFontSize(11)
  doc.setTextColor(120)
  doc.text(`Dificultad: ${rutine.difficulty}  |  Duracion: ${rutine.duration}`, 14, y)
  y += 8

  doc.setFontSize(12)
  doc.setTextColor(40)
  const description = doc.splitTextToSize(rutine.description, 180)
  doc.text(description, 14, y)
  y += description.length * 6 + 8

  rutine.exercises.forEach((exercise, index) => {
    if (y > 270) {
      doc.addPage()
      y = 20
    }
    doc.setFontSize(12)
    doc.setTextColor(0)
    doc.text(`${index + 1}. ${exercise.exercise}  [${exercise.tipo}]`, 14, y)
    y += 6
    doc.setFontSize(10)
    doc.setTextColor(100)
    doc.text(
      `Series: ${exercise.series}  Reps: ${exercise.reps}  Descanso: ${exercise.descanso}`,
      18,
      y,
    )
    y += 5
    if (exercise.obs) {
      const obs = doc.splitTextToSize(`Obs: ${exercise.obs}`, 175)
      doc.text(obs, 18, y)
      y += obs.length * 5
    }
    y += 5
  })

  doc.save(`${rutine.title.replace(/\s+/g, '-').toLowerCase()}.pdf`)
}

/**
 * Detalle de rutina: ejercicios ordenados (calentamiento primero), descarga PDF
 * y tip del coach. Diseño según mockup rutine_Detail.png.
 */
export default function RoutineDetailPage() {
  const { id } = useParams<{ id: string }>()

  const { data: rutine, isLoading, error } = useQuery({
    queryKey: ['rutine', id],
    queryFn: () => getRutine(Number(id)),
    enabled: !!id,
  })

  if (isLoading) return <LoadingState />
  if (error) return <ErrorState message={apiErrorMessage(error)} />
  if (!rutine) return <ErrorState message="La rutina no existe." />

  const warmup = rutine.exercises.filter((e) => e.tipo === 'Calentamiento')
  const main = rutine.exercises.filter((e) => e.tipo !== 'Calentamiento')
  const coachTip = [...main].reverse().find((e) => e.obs)?.obs

  return (
    <section className="p-4">
      <header className="mb-5 pt-2">
        <Link to={`/routines/category/${rutine.categoryId}`} aria-label="Volver" className="text-muted hover:text-foreground">
          ← Volver
        </Link>
        <div className="mt-3 mb-2 flex items-center gap-3">
          <DifficultyBadge difficulty={rutine.difficulty} />
          <span className="text-sm text-muted">{rutine.duration}</span>
        </div>
        <h1 className="text-3xl font-black">{rutine.title}</h1>
        <p className="mt-2 text-muted">{rutine.description}</p>
      </header>

      <button
        onClick={() => downloadRutinePdf(rutine)}
        className="mb-6 w-full rounded-2xl bg-accent py-4 font-bold text-accent-foreground shadow-lg shadow-accent/25"
      >
        Descargar rutina (PDF)
      </button>

      <ExerciseGroup title="Calentamiento" exercises={warmup} />
      <ExerciseGroup title="Principal" exercises={main} />

      {coachTip && (
        <aside className="mt-6 rounded-2xl border border-accent/30 bg-accent/5 p-4">
          <h2 className="mb-1 text-sm font-bold tracking-wider text-accent uppercase">Tip del coach</h2>
          <p className="text-sm italic text-foreground/90">"{coachTip}"</p>
        </aside>
      )}
    </section>
  )
}

function ExerciseGroup({
  title,
  exercises,
}: {
  title: string
  exercises: RutineDetailDto['exercises']
}) {
  if (exercises.length === 0) return null
  return (
    <div className="mb-6">
      <h2 className="mb-3 text-lg font-bold">
        {title} <span className="text-sm font-normal text-muted">({exercises.length})</span>
      </h2>
      <ul className="space-y-3">
        {exercises.map((exercise, index) => (
          <li key={`${exercise.exercise}-${index}`} className="rounded-2xl border border-white/10 bg-surface p-4">
            <div className="flex items-center justify-between gap-3">
              <h3 className="font-bold">{exercise.exercise}</h3>
              <p className="shrink-0 text-sm text-muted">
                {exercise.series} × {exercise.reps}
              </p>
            </div>
            <p className="mt-1 text-sm text-muted">Descanso: {exercise.descanso}</p>
            {exercise.obs && <p className="mt-1 text-sm text-muted/80 italic">{exercise.obs}</p>}
            {exercise.videoId && (
              <Link to={`/exercises/${exercise.videoId}`} className="mt-2 inline-block text-sm font-bold text-accent">
                Ver video →
              </Link>
            )}
          </li>
        ))}
      </ul>
    </div>
  )
}
