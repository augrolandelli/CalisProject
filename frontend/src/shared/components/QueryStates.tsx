/** Estado de carga centrado. */
export function LoadingState({ label = 'Cargando…' }: { label?: string }) {
  return (
    <div className="flex items-center justify-center py-16" role="status">
      <p className="text-muted">{label}</p>
    </div>
  )
}

/** Estado de error con estilo consistente. */
export function ErrorState({ message }: { message: string }) {
  return (
    <div className="mx-4 mt-6 rounded-2xl border border-danger/40 bg-danger/10 px-4 py-3 text-sm text-danger" role="alert">
      {message}
    </div>
  )
}

/** Estado vacío. */
export function EmptyState({ message }: { message: string }) {
  return (
    <div className="flex items-center justify-center py-16">
      <p className="text-muted">{message}</p>
    </div>
  )
}
