import { Link } from 'react-router-dom'

export default function NotFoundPage() {
  return (
    <section className="flex min-h-dvh flex-col items-center justify-center p-6 text-center">
      <h1 className="text-4xl font-bold">404</h1>
      <p className="mt-2 text-muted">La página que buscas no existe.</p>
      <Link to="/" className="mt-6 rounded-full bg-accent px-6 py-3 font-semibold text-accent-foreground">
        Volver al inicio
      </Link>
    </section>
  )
}
