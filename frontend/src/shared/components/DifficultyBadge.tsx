const styles: Record<string, string> = {
  basica: 'bg-success/15 text-success',
  intermedia: 'bg-warning/15 text-warning',
  avanzada: 'bg-danger/15 text-danger',
}

const labels: Record<string, string> = {
  basica: 'Básica',
  intermedia: 'Intermedia',
  avanzada: 'Avanzada',
}

/** Badge de dificultad de ejercicios/rutinas (verde/naranja/rojo según spec §6). */
export function DifficultyBadge({ difficulty }: { difficulty: string }) {
  const key = difficulty.toLowerCase()
  return (
    <span
      className={`inline-block rounded-lg px-2.5 py-1 text-xs font-bold tracking-wide uppercase ${styles[key] ?? 'bg-surface-raised text-muted'}`}
    >
      {labels[key] ?? difficulty}
    </span>
  )
}
