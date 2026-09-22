import { Link } from 'react-router-dom'
import { CommunityHeader, CommunityIcon, cardClass } from '../../community/components/CommunityUI'
import { ReviewList } from '../../community/components/Reviews'

export default function AdminCommunityPage() {
  return <section className="p-4">
    <CommunityHeader title="Gestionar comunidad" subtitle="Novedades del gimnasio, encuentros y experiencias de alumnos." />
    <div className="mb-8 grid gap-3">
      <Link to="/admin/community/posts/new" className={`${cardClass} flex items-center gap-3 font-bold hover:border-accent/40`}><CommunityIcon name="plus" className="text-accent" />Crear novedad o competencia</Link>
      <Link to="/admin/community/events/new" className={`${cardClass} flex items-center gap-3 font-bold hover:border-accent/40`}><CommunityIcon name="calendar" className="text-accent" />Crear evento</Link>
      <Link to="/community" className={`${cardClass} flex items-center gap-3 font-bold hover:border-accent/40`}><CommunityIcon name="community" className="text-accent" />Ver y editar publicaciones</Link>
      <Link to="/community?tab=events" className={`${cardClass} flex items-center gap-3 font-bold hover:border-accent/40`}><CommunityIcon name="calendar" className="text-accent" />Ver eventos e inscritos</Link>
    </div>
    <h2 className="mb-2 text-xl font-bold">Moderación de reseñas</h2>
    <p className="mb-4 text-sm text-muted">Las reseñas retiradas dejan de mostrarse y de contar en el promedio. Puedes restaurarlas desde aquí.</p>
    <ReviewList moderation />
  </section>
}
