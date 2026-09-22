import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuthStore } from '../../auth/authStore'

/**
 * Guía de uso condicional al rol.
 * - Admin: ve la guía completa, incluyendo panel de administración.
 * - Guerrero / Clover: ven solo la información orientada al uso de la app como alumno.
 */

const ADMIN_WHATSAPP_URL =
  'https://wa.me/5492323211719?text=' + encodeURIComponent('Hola! Quiero empezar a ser un clover.')

type SectionProps = {
  id: string
  title: string
  children: React.ReactNode
}

function Section({ id, title, children }: SectionProps) {
  return (
    <section id={id} className="scroll-mt-6 border-t border-white/10 py-8 first:border-t-0 first:pt-0">
      <h2 className="mb-4 text-2xl font-black text-accent">{title}</h2>
      <div className="space-y-4 text-foreground/90">{children}</div>
    </section>
  )
}

function InfoBox({ children, variant = 'info' }: { children: React.ReactNode; variant?: 'info' | 'warning' | 'tip' }) {
  const styles = {
    info: 'border-accent/30 bg-accent/5 text-foreground/90',
    warning: 'border-warning/30 bg-warning/10 text-warning',
    tip: 'border-success/30 bg-success/10 text-success',
  }
  return <div className={`rounded-2xl border p-4 text-sm leading-relaxed ${styles[variant]}`}>{children}</div>
}

function Step({ number, title, children }: { number: number; title: string; children: React.ReactNode }) {
  return (
    <div className="flex gap-4">
      <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-accent text-sm font-black text-accent-foreground">
        {number}
      </span>
      <div className="min-w-0 flex-1">
        <h3 className="mb-1 font-bold">{title}</h3>
        <div className="text-sm text-muted">{children}</div>
      </div>
    </div>
  )
}

const adminSections = [
  { id: 'intro', label: 'Antes de empezar' },
  { id: 'acceso', label: 'Acceso e instalación' },
  { id: 'auth', label: 'Registro y login' },
  { id: 'home', label: 'Home / Dashboard' },
  { id: 'clases', label: 'Clases en vivo' },
  { id: 'detalle-clase', label: 'Detalle de clase' },
  { id: 'ejercicios', label: 'Videoteca' },
  { id: 'rutinas', label: 'Rutinas' },
  { id: 'logros', label: 'Logros' },
  { id: 'comunidad', label: 'Comunidad' },
  { id: 'perfil', label: 'Perfil y upgrade' },
  { id: 'admin', label: 'Panel de administración' },
  { id: 'notas', label: 'Notas de la beta' },
]

const userSections = [
  { id: 'intro', label: 'Antes de empezar' },
  { id: 'acceso', label: 'Acceso e instalación' },
  { id: 'auth', label: 'Registro y login' },
  { id: 'home', label: 'Home / Dashboard' },
  { id: 'clases', label: 'Clases en vivo' },
  { id: 'detalle-clase', label: 'Detalle de clase' },
  { id: 'ejercicios', label: 'Videoteca' },
  { id: 'rutinas', label: 'Rutinas' },
  { id: 'logros', label: 'Logros' },
  { id: 'comunidad', label: 'Comunidad' },
  { id: 'perfil', label: 'Perfil y upgrade' },
  { id: 'notas', label: 'Notas de la beta' },
]

export default function GuidePage() {
  const [showNav, setShowNav] = useState(false)
  const { user } = useAuthStore()
  const isAdmin = user?.role === 'Admin'
  const sections = isAdmin ? adminSections : userSections

  return (
    <div className="min-h-dvh bg-background">
      <header className="sticky top-0 z-40 border-b border-white/10 bg-background/95 backdrop-blur">
        <div className="mx-auto flex max-w-2xl items-center justify-between px-4 py-3">
          <Link to="/" className="text-sm font-bold text-accent">
            ← Volver a la app
          </Link>
          <button
            onClick={() => setShowNav((v) => !v)}
            className="rounded-xl border border-white/20 px-3 py-2 text-sm font-bold"
          >
            {showNav ? 'Cerrar índice' : 'Índice'}
          </button>
        </div>
        {showNav && (
          <nav className="mx-auto max-w-2xl border-t border-white/10 px-4 py-3">
            <ul className="grid grid-cols-2 gap-2 text-sm">
              {sections.map((s) => (
                <li key={s.id}>
                  <a href={`#${s.id}`} onClick={() => setShowNav(false)} className="block rounded-lg px-3 py-2 hover:bg-surface">
                    {s.label}
                  </a>
                </li>
              ))}
            </ul>
          </nav>
        )}
      </header>

      <main className="mx-auto max-w-2xl px-4 pb-24 pt-6">
        <div className="mb-8 text-center">
          <div className="mx-auto mb-4 flex h-20 w-20 items-center justify-center rounded-3xl border border-accent/40 bg-surface text-3xl font-black text-accent">
            C
          </div>
          <h1 className="text-3xl font-black">Guía de uso — Beta CalisApp</h1>
          <p className="mt-2 text-sm text-muted">
            Todo lo que necesitás saber para probar la app, según tu rol.
          </p>
          <span className="mt-3 inline-block rounded-full bg-accent/15 px-3 py-1 text-xs font-bold uppercase text-accent">
            {user?.role ?? 'Usuario'}
          </span>
        </div>

        <InfoBox variant="warning">
          <strong>Esta es una versión beta.</strong> Algunas funciones están simplificadas o pendientes. Si encontrás
          algo raro, avisá al administrador.
        </InfoBox>

        <IntroSection isAdmin={isAdmin} />
        <AccessSection />
        <AuthSection />
        <HomeSection />
        <ClassesSection />
        <ClassDetailSection />
        <ExercisesSection />
        <RoutinesSection />
        <AchievementsSection />
        <CommunitySection />
        <ProfileSection />
        {isAdmin && <AdminSection />}
        <NotesSection isAdmin={isAdmin} />

        <div className="rounded-3xl border border-accent/30 bg-surface p-6 text-center">
          <h2 className="mb-2 text-xl font-black">¿Dudas o problemas?</h2>
          <p className="mb-4 text-sm text-muted">
            Contactá al administrador por WhatsApp para reportar bugs o solicitar el upgrade a Clover.
          </p>
          <a
            href={ADMIN_WHATSAPP_URL}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex min-h-11 items-center justify-center gap-2 rounded-xl bg-success px-6 py-3 text-sm font-bold text-white"
          >
            Escribir por WhatsApp
          </a>
        </div>
      </main>
    </div>
  )
}

function IntroSection({ isAdmin }: { isAdmin: boolean }) {
  return (
    <Section id="intro" title="1. Antes de empezar">
      <p>
        <strong>CalisApp</strong> es la PWA del gimnasio para gestionar clases de calistenia, ver rutinas y
        ejercicios, ganar logros y participar de la comunidad.
      </p>
      <p>Existen tres roles:</p>
      <ul className="space-y-2 text-sm">
        <li className="rounded-xl border border-white/10 bg-surface p-3">
          <strong className="text-success">Guerrero</strong> — cuenta gratuita. Puede ver rutinas, ejercicios y
          reseñas de clases como muestra.
        </li>
        <li className="rounded-xl border border-white/10 bg-surface p-3">
          <strong className="text-accent">Clover</strong> — miembro pago. Puede reservar clases, ganar logros,
          ver el tablón de comunidad, eventos y competencias.
        </li>
        {isAdmin && (
          <li className="rounded-xl border border-white/10 bg-surface p-3">
            <strong className="text-warning">Admin</strong> — staff. Gestiona contenido, usuarios, clases,
            logros y comunidad.
          </li>
        )}
      </ul>

      <InfoBox variant="info">
        <strong>Credenciales de prueba</strong>
        <ul className="mt-2 list-inside list-disc space-y-1">
          {isAdmin && (
            <li>
              Admin: <code className="rounded bg-surface-raised px-1.5 py-0.5 text-xs">admin@calisapp.com</code> /{' '}
              <code className="rounded bg-surface-raised px-1.5 py-0.5 text-xs">Admin123!</code>
            </li>
          )}
          <li>
            Guerrero: <code className="rounded bg-surface-raised px-1.5 py-0.5 text-xs">demo@calisapp.com</code> /{' '}
            <code className="rounded bg-surface-raised px-1.5 py-0.5 text-xs">Demo1234!</code>
          </li>
          <li>
            Clover: <code className="rounded bg-surface-raised px-1.5 py-0.5 text-xs">clover@calisapp.com</code> /{' '}
            <code className="rounded bg-surface-raised px-1.5 py-0.5 text-xs">Clover1234!</code>
          </li>
        </ul>
      </InfoBox>
    </Section>
  )
}

function AccessSection() {
  return (
    <Section id="acceso" title="2. Acceso e instalación">
      <p>La app funciona desde el navegador, pero se recomienda instalarla como PWA para la mejor experiencia.</p>
      <Step number={1} title="Abrí la URL">
        Ingresá al enlace que te pasó el administrador. La app se adapta automáticamente a celular y desktop.
      </Step>
      <Step number={2} title="Agregá la app a tu pantalla de inicio">
        <ul className="mt-2 list-inside list-disc space-y-1">
          <li>
            <strong>Android (Chrome):</strong> tocá los tres puntos → “Agregar a pantalla de inicio”.
          </li>
          <li>
            <strong>iPhone (Safari):</strong> tocá el botón compartir → “Agregar a pantalla de inicio”.
          </li>
          <li>
            <strong>Chrome en desktop:</strong> aparece un ícono de instalación en la barra de direcciones.
          </li>
        </ul>
      </Step>
      <Step number={3} title="Permití notificaciones (opcional)">
        Si sos Clover/Admin, la app puede avisarte cuando se libera un cupo en una clase llena o cuando hay
        recordatorios. Aparecerá un banner para activarlas.
      </Step>
    </Section>
  )
}

function AuthSection() {
  return (
    <Section id="auth" title="3. Registro y login">
      <p>En la pantalla de inicio elegí <strong>“Regístrate”</strong> si es tu primera vez.</p>
      <ul className="list-inside list-disc space-y-1 text-sm">
        <li>Completá nombre completo, teléfono, email y contraseña (mínimo 8 caracteres).</li>
        <li>Todo nuevo usuario nace como <strong>Guerrero</strong>.</li>
        <li>Si ya tenés cuenta, tocá <strong>“Inicia sesión”</strong>.</li>
      </ul>
      <InfoBox variant="tip">
        Para la beta podés usar las cuentas de prueba y saltear el registro.
      </InfoBox>
    </Section>
  )
}

function HomeSection() {
  return (
    <Section id="home" title="4. Home / Dashboard">
      <p>Es la pantalla principal. Desde acá podés:</p>
      <ul className="list-inside list-disc space-y-1 text-sm">
        <li>Ver la <strong>próxima clase en vivo</strong> con fecha, hora y lugares disponibles.</li>
        <li>Navegar por <strong>categorías</strong> de ejercicios (Pull, Push, Legs, etc.).</li>
        <li>Ver tus <strong>logros recientes</strong>.</li>
        <li>Ver <strong>rutinas destacadas</strong>.</li>
      </ul>
      <p className="text-sm text-muted">
        La navegación inferior tiene 5 pestañas: Home, Clases, Ejercicios, Rutinas y Comunidad. Tu perfil se
        accede desde el ícono superior izquierdo.
      </p>
    </Section>
  )
}

function ClassesSection() {
  return (
    <Section id="clases" title="5. Clases en vivo">
      <p>En la pestaña <strong>Clases</strong> vas a ver un selector de días (lunes a domingo) y la lista de clases.</p>
      <Step number={1} title="Elegí el día">
        Deslizá la tira horizontal para cambiar de día. La app carga las clases de esa fecha.
      </Step>
      <Step number={2} title="Revisá la tarjeta de la clase">
        Muestra dificultad, título, hora, coach, lugares ocupados/disponibles y una barra de progreso.
      </Step>
      <Step number={3} title="Reservá tu lugar">
        Si hay cupos y sos Clover/Admin, tocá la tarjeta para entrar al detalle y luego{' '}
        <strong>“Reservar mi lugar”</strong>. Si la clase está llena, aparece{' '}
        <strong>“Unirme a la lista de espera”</strong>.
      </Step>
      <Step number={4} title="Cancelá si no podés ir">
        Dentro del detalle, si ya estás inscrito, el botón cambia a <strong>“Cancelar mi reserva”</strong>. Las
        cancelaciones se cierran cuando la clase comienza.
      </Step>
      <InfoBox variant="warning">
        Los usuarios <strong>Guerrero</strong> no pueden reservar. En su lugar ven un botón para escribir por
        WhatsApp y pasar a Clover.
      </InfoBox>
    </Section>
  )
}

function ClassDetailSection() {
  return (
    <Section id="detalle-clase" title="6. Detalle de clase">
      <p>Dentro de una clase vas a encontrar:</p>
      <ul className="list-inside list-disc space-y-1 text-sm">
        <li>
          <strong>Datos principales:</strong> título, fecha, hora, duración, dificultad y coach.
        </li>
        <li>
          <strong>Logros asociados:</strong> insignias que se pueden ganar en esa clase.
        </li>
        <li>
          <strong>Participantes inscritos:</strong> lista de nombres y contador de cupos.
        </li>
        <li>
          <strong>Botón de acción:</strong> reservar, cancelar, unirse a waitlist o salir de ella, según tu
          situación.
        </li>
        <li>
          <strong>Reseñas de la clase:</strong> promedio de estrellas y opiniones de alumnos.
        </li>
      </ul>
      <InfoBox variant="info">
        <strong>Cómo dejar una reseña</strong> (solo Clover/Admin): necesitás haber reservado la clase antes de
        que empiece y esperar a que finalice. Entonces aparecerá el formulario de estrellas y comentario opcional
        en el detalle.
      </InfoBox>
    </Section>
  )
}

function ExercisesSection() {
  return (
    <Section id="ejercicios" title="7. Videoteca de ejercicios">
      <p>En la pestaña <strong>Ejercicios</strong>:</p>
      <Step number={1} title="Elegí una categoría">
        Pull, Push, Legs, Core, etc. Cada categoría muestra su descripción.
      </Step>
      <Step number={2} title="Filtrá y buscá">
        Usá el buscador y los filtros de dificultad (Básica / Intermedia / Avanzada).
      </Step>
      <Step number={3} title="Mirá el video">
        Tocá un ejercicio para ver el reproductor con controles y pantalla completa. También se muestran
        prerequisitos y descripción.
      </Step>
    </Section>
  )
}

function RoutinesSection() {
  return (
    <Section id="rutinas" title="8. Rutinas">
      <p>En la pestaña <strong>Rutinas</strong>:</p>
      <Step number={1} title="Elegí una categoría">
        Por ejemplo “Rutinas Pull”, “Rutinas Push”.
      </Step>
      <Step number={2} title="Elegí una rutina">
        Cada tarjeta indica dificultad, duración aproximada y descripción.
      </Step>
      <Step number={3} title="Descargá el PDF">
        En el detalle hay un botón <strong>“Descargar rutina (PDF)”</strong> que genera un archivo con todos los
        ejercicios.
      </Step>
      <p className="text-sm text-muted">
        El detalle separa automáticamente los ejercicios de <strong>Calentamiento</strong> y los{' '}
        <strong>Principales</strong>. Cada uno indica series, repeticiones, descanso y observaciones. Si tiene video
        vinculado, aparece un enlace para verlo.
      </p>
    </Section>
  )
}

function AchievementsSection() {
  return (
    <Section id="logros" title="9. Logros">
      <p>En la pestaña <strong>Logros</strong> (o desde tu perfil):</p>
      <ul className="list-inside list-disc space-y-1 text-sm">
        <li>Se muestra el catálogo completo.</li>
        <li>Los logros que ya ganaste aparecen destacados con el ícono real.</li>
        <li>Los que todavía no tenés aparecen bloqueados con un candado.</li>
      </ul>
      <InfoBox variant="tip">
        Los logros se otorgan por los profesores/administradores al finalizar una clase, solo a quienes asistieron
        y estén inscritos.
      </InfoBox>
    </Section>
  )
}

function CommunitySection() {
  return (
    <Section id="comunidad" title="10. Comunidad">
      <p>La pestaña <strong>Comunidad</strong> tiene tres secciones:</p>
      <ul className="list-inside list-disc space-y-1 text-sm">
        <li>
          <strong>Novedades:</strong> publicaciones del gimnasio (fotos, videos, competencias y logros sociales
          automáticos). Solo visibles para Clover/Admin.
        </li>
        <li>
          <strong>Eventos:</strong> encuentros con fecha, lugar y cupo. Los Clover/Admin se pueden anotar o
          cancelar. El Admin ve el listado de inscritos.
        </li>
        <li>
          <strong>Reseñas:</strong> valoraciones de clases. Visibles para todos, incluso Guerrero.
        </li>
      </ul>
      <InfoBox variant="warning">
        Si sos Guerrero, en Novedades y Eventos vas a ver un mensaje invitándote a ser Clover. Las reseñas sí
        están disponibles para que veas la experiencia de los alumnos.
      </InfoBox>
    </Section>
  )
}

function ProfileSection() {
  return (
    <Section id="perfil" title="11. Perfil y upgrade a Clover">
      <p>Desde el ícono superior izquierdo en Home o desde la navegación:</p>
      <ul className="list-inside list-disc space-y-1 text-sm">
        <li>Revisá y editá tu nombre y teléfono.</li>
        <li>Consultá tu rol actual.</li>
        <li>Mirá tus estadísticas: clases asistidas, faltas, logros, etc.</li>
        <li>Cerrá sesión.</li>
      </ul>
      <p>Para pasar de Guerrero a Clover:</p>
      <Step number={1} title="Escribí por WhatsApp">
        En la sección de comunidad o en el detalle de una clase vas a ver el botón para contactar al gimnasio.
      </Step>
      <Step number={2} title="El administrador te cambia el rol">
        Una vez hecho el pago/arreglo, el admin te sube a Clover desde su panel.
      </Step>
      <Step number={3} title="Recargá la app">
        Cerrá y volvé a abrir, o tocá alguna sección distinta. Tu rol se actualiza automáticamente.
      </Step>
      <a
        href={ADMIN_WHATSAPP_URL}
        target="_blank"
        rel="noopener noreferrer"
        className="mt-4 inline-flex min-h-11 items-center justify-center gap-2 rounded-xl bg-success px-4 py-3 text-sm font-bold text-white"
      >
        Quiero ser Clover
      </a>
    </Section>
  )
}

function AdminSection() {
  return (
    <Section id="admin" title="12. Panel de administración">
      <p>
        <strong>Solo para usuarios Admin.</strong> Se accede desde tu perfil → <strong>Panel de administración</strong>.
      </p>

      <h3 className="mb-2 mt-6 text-lg font-bold">12.1 Clases</h3>
      <ul className="list-inside list-disc space-y-1 text-sm">
        <li>
          <strong>Crear:</strong> título, descripción, fecha/hora, duración, cupo, dificultad, coach y logros
          asociados.
        </li>
        <li>
          <strong>Editar:</strong> solo antes de que empiece. Si cambia la fecha/hora, los inscritos reciben un
          aviso push (si lo permitieron).
        </li>
        <li>
          <strong>Eliminar:</strong> podés borrar una clase individual. Las reseñas y logros otorgados se
          conservan.
        </li>
        <li>
          <strong>Limpieza por mes:</strong> al final de la pantalla podés eliminar todas las clases de un mes ya
          finalizado. Primero se muestra un preview de cuántas clases se borrarán.
        </li>
      </ul>

      <h3 className="mb-2 mt-6 text-lg font-bold">12.2 Asistencia y logros (después de una clase)</h3>
      <ol className="list-inside list-decimal space-y-1 text-sm">
        <li>Entrá al detalle de una clase finalizada.</li>
        <li>Marcá quiénes asistieron en la <strong>Lista de asistencia</strong> y guardá.</li>
        <li>En <strong>Otorgar logros</strong>, seleccioná el logro y los asistentes que lo consiguieron.</li>
        <li>Tocá <strong>“Otorgar”</strong>. La app saltea automáticamente a quienes ya tengan ese logro.</li>
      </ol>
      <InfoBox variant="tip">
        Al otorgar un logro a un Clover, se crea automáticamente una publicación en el tablón de comunidad para
        celebrarlo.
      </InfoBox>

      <h3 className="mb-2 mt-6 text-lg font-bold">12.3 Videoteca</h3>
      <ul className="list-inside list-disc space-y-1 text-sm">
        <li>Subí videos de ejercicios en formato MP4 H.264, máximo 30 segundos y 50 MB.</li>
        <li>Completá título, descripción, dificultad, categoría y prerequisitos.</li>
        <li>No se puede eliminar un video que esté siendo usado por una rutina.</li>
      </ul>

      <h3 className="mb-2 mt-6 text-lg font-bold">12.4 Rutinas</h3>
      <ul className="list-inside list-disc space-y-1 text-sm">
        <li>Creá rutinas con título, descripción, duración, dificultad y categoría.</li>
        <li>Agregá ejercicios indicando tipo (Calentamiento / Principal), series, reps, descanso y observaciones.</li>
        <li>Vinculá un video de la videoteca usando el buscador con lupa.</li>
        <li>El servidor ordena automáticamente el calentamiento primero al guardar.</li>
      </ul>

      <h3 className="mb-2 mt-6 text-lg font-bold">12.5 Logros</h3>
      <ul className="list-inside list-disc space-y-1 text-sm">
        <li>Creá logros con nombre, descripción e ícono (emoji o texto corto).</li>
        <li>
          Si un logro ya fue ganado por alumnos, al “eliminarlo” se oculta del catálogo pero se conserva el
          historial.
        </li>
      </ul>

      <h3 className="mb-2 mt-6 text-lg font-bold">12.6 Categorías</h3>
      <p className="text-sm">
        Creá/editá categorías para videos y rutinas. No se pueden eliminar categorías que tengan videos o rutinas
        asociados.
      </p>

      <h3 className="mb-2 mt-6 text-lg font-bold">12.7 Usuarios</h3>
      <p className="text-sm">
        Buscá usuarios por nombre o email y filtrá por rol. Desde acá el admin puede subir un Guerrero a Clover o
        bajar un Clover a Guerrero manualmente.
      </p>

      <h3 className="mb-2 mt-6 text-lg font-bold">12.8 Comunidad (Admin)</h3>
      <ul className="list-inside list-disc space-y-1 text-sm">
        <li>
          <strong>Novedades / Competencias:</strong> crear publicaciones con texto, hasta 4 fotos y 1 video.
        </li>
        <li>
          <strong>Eventos:</strong> crear encuentros con fecha de inicio, fin, lugar y cupo opcional.
        </li>
        <li>
          <strong>Moderación de reseñas:</strong> retirar o restaurar reseñas de alumnos.
        </li>
      </ul>
    </Section>
  )
}

function NotesSection({ isAdmin }: { isAdmin: boolean }) {
  return (
    <Section id="notas" title={isAdmin ? '13. Notas de la beta' : '12. Notas de la beta'}>
      <ul className="list-inside list-disc space-y-2 text-sm">
        <li>
          <strong>Upgrade a Clover:</strong> es manual por WhatsApp. No hay pasarela de pago automática en esta
          beta.
        </li>
        <li>
          <strong>Recuperación de contraseña:</strong> no está habilitada todavía. Si la olvidás, pedile al admin
          que te ayude.
        </li>
        <li>
          <strong>Idioma:</strong> la interfaz está en español solamente por ahora.
        </li>
        {isAdmin && (
          <li>
            <strong>Almacenamiento de archivos:</strong> en esta beta se usa almacenamiento local en el servidor para
            videos y fotos de comunidad.
          </li>
        )}
      </ul>
    </Section>
  )
}
