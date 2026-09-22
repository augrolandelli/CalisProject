import { useEffect, useState, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { apiErrorMessage } from '../../../shared/api/client'
import { ADMIN_WHATSAPP_URL } from '../../../shared/config'

export const inputClass = 'w-full rounded-xl border border-white/20 bg-surface-raised px-3 py-3 text-foreground focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent'
export const buttonClass = 'inline-flex min-h-11 items-center justify-center gap-2 rounded-xl bg-accent px-4 py-3 text-sm font-bold text-accent-foreground transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50'
export const secondaryClass = 'inline-flex min-h-11 items-center justify-center gap-2 rounded-xl border border-white/20 px-3 py-2 text-sm font-semibold hover:bg-surface-raised disabled:opacity-50'
export const cardClass = 'rounded-2xl border border-white/10 bg-surface p-4'

export function OfflineNotice() {
  const [online, setOnline] = useState(() => navigator.onLine)
  useEffect(() => {
    const update = () => setOnline(navigator.onLine)
    window.addEventListener('online', update)
    window.addEventListener('offline', update)
    return () => { window.removeEventListener('online', update); window.removeEventListener('offline', update) }
  }, [])
  return online ? null : <p role="alert" className="border-b border-warning/30 bg-warning/10 p-3 text-center text-sm">Estás sin conexión. El contenido exclusivo requiere internet.</p>
}

type IconName = 'community' | 'heart' | 'trophy' | 'calendar' | 'star' | 'lock' | 'plus' | 'arrow' | 'image'
const paths: Record<IconName, string> = {
  community: 'M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2M16 3a4 4 0 0 1 0 8M22 21v-2a4 4 0 0 0-3-3.87M13 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z',
  heart: 'M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.7l-1.1-1.1a5.5 5.5 0 0 0-7.8 7.8L12 21l8.8-8.6a5.5 5.5 0 0 0 0-7.8Z',
  trophy: 'M8 21h8M12 17v4M7 3h10v7a5 5 0 0 1-10 0V3ZM7 5H3v3a4 4 0 0 0 4 4M17 5h4v3a4 4 0 0 1-4 4',
  calendar: 'M8 2v4M16 2v4M3 10h18M5 4h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2Z',
  star: 'm12 3 2.8 5.7 6.2.9-4.5 4.4 1.1 6.2-5.6-3-5.6 3 1.1-6.2L3 9.6l6.2-.9L12 3Z',
  lock: 'M7 10V7a5 5 0 0 1 10 0v3M6 10h12a2 2 0 0 1 2 2v8H4v-8a2 2 0 0 1 2-2ZM12 14v3',
  plus: 'M12 5v14M5 12h14', arrow: 'm9 5 7 7-7 7',
  image: 'M3 3h18v18H3V3Zm0 14 5-5 4 4 3-3 6 6M8 7h.01',
}
export function CommunityIcon({ name, filled = false, className = '' }: { name: IconName; filled?: boolean; className?: string }) {
  return <svg aria-hidden="true" viewBox="0 0 24 24" fill={filled ? 'currentColor' : 'none'} stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" className={`h-5 w-5 shrink-0 ${className}`}><path d={paths[name]} /></svg>
}

export function ErrorNotice({ error }: { error: unknown }) {
  return error ? <p role="alert" className="my-3 rounded-xl border border-danger/40 bg-danger/10 p-3 text-sm text-foreground">{typeof error === 'string' ? error : apiErrorMessage(error)}</p> : null
}

export function CommunityHeader({ title, subtitle, back = '/community', children }: { title: string; subtitle?: string; back?: string; children?: ReactNode }) {
  return <header className="mb-6 pt-2">
    <Link to={back} className="mb-2 inline-flex min-h-11 items-center text-sm text-muted hover:text-foreground">← Volver</Link>
    <h1 className="text-2xl font-black tracking-tight">{title}</h1>
    {subtitle && <p className="mt-2 text-sm leading-relaxed text-muted">{subtitle}</p>}
    {children}
  </header>
}

export function Pagination({ page, total, pageSize, onChange }: { page: number; total: number; pageSize: number; onChange: (page: number) => void }) {
  if (total <= pageSize && page === 1) return null
  return <nav aria-label="Páginas de resultados" className="mt-5 flex items-center justify-between gap-2">
    <button className={secondaryClass} disabled={page <= 1} onClick={() => onChange(page - 1)}>Anterior</button>
    <span className="text-xs text-muted" aria-live="polite">{page} / {Math.max(1, Math.ceil(total / pageSize))}</span>
    <button className={secondaryClass} disabled={page * pageSize >= total} onClick={() => onChange(page + 1)}>Siguiente</button>
  </nav>
}

export function CloverInvitation() {
  return <aside className="mb-6 rounded-2xl border border-accent/25 bg-accent/5 p-5">
    <div className="mb-2 flex items-center gap-2 font-bold text-accent"><CommunityIcon name="lock" />Tu próximo paso, en comunidad</div>
    <p className="mb-4 text-sm leading-relaxed text-muted">Conoce las experiencias de los Clover Warriors. Si sos clover, accedes a las novedades, los logros, clases y competencias.</p>
    <a className={buttonClass} href={ADMIN_WHATSAPP_URL} target="_blank" rel="noopener noreferrer">Quiero ser Clover <CommunityIcon name="arrow" /></a>
  </aside>
}
