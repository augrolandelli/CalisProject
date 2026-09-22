import { Link } from 'react-router-dom'
import { cardClass, CommunityIcon } from '../../community/components/CommunityUI'

const sections = [
  { to: '/admin/classes', icon: 'calendar' as const, title: 'Clases', description: 'Crear, editar y eliminar clases. Limpieza por mes.' },
  { to: '/admin/videos', icon: 'image' as const, title: 'Videoteca', description: 'Subir videos de ejercicios (MP4, hasta 30 s).' },
  { to: '/admin/routines', icon: 'community' as const, title: 'Rutinas', description: 'Armar rutinas con el constructor de ejercicios.' },
  { to: '/admin/achievements', icon: 'trophy' as const, title: 'Logros', description: 'Catálogo de logros: crear, editar y ocultar.' },
  { to: '/admin/categories', icon: 'plus' as const, title: 'Categorías', description: 'Gestionar categorías de videos y rutinas.' },
  { to: '/admin/users', icon: 'community' as const, title: 'Usuarios', description: 'Buscar usuarios y cambiar roles (upgrade manual).' },
  { to: '/admin/community', icon: 'heart' as const, title: 'Comunidad', description: 'Novedades, eventos y moderación de reseñas.' },
]

/** Panel de administración: hub con accesos a cada sección. */
export default function AdminHubPage() {
  return (
    <section className="p-4">
      <header className="mb-6 pt-2">
        <h1 className="text-2xl font-black">Administración</h1>
        <p className="mt-1 text-sm text-muted">Gestión del contenido de CalisApp.</p>
      </header>
      <nav aria-label="Secciones de administración" className="grid gap-3">
        {sections.map((section) => (
          <Link key={section.to} to={section.to} className={`${cardClass} flex items-center gap-3 transition-colors hover:border-accent/40`}>
            <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-accent/10 text-accent">
              <CommunityIcon name={section.icon} />
            </span>
            <span className="min-w-0">
              <span className="block font-bold">{section.title}</span>
              <span className="block truncate text-sm text-muted">{section.description}</span>
            </span>
            <CommunityIcon name="arrow" className="ml-auto text-muted" />
          </Link>
        ))}
      </nav>
    </section>
  )
}
