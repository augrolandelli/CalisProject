import { useState } from 'react'
import { pushSupport, subscribeToPush } from '../pwa/push'

/**
 * Banner para que usuarios Clover/Admin activen notificaciones push
 * (aviso cuando se libera un cupo de la waitlist).
 */
export function PushOptIn() {
  const [{ supported, permission }] = useState(pushSupport)
  const [state, setState] = useState<'idle' | 'loading' | 'done' | 'error'>('idle')

  if (!supported || permission !== 'default' || state === 'done') {
    return null
  }

  const handleSubscribe = async () => {
    setState('loading')
    try {
      const ok = await subscribeToPush()
      setState(ok ? 'done' : 'error')
    } catch {
      setState('error')
    }
  }

  return (
    <div className="mb-5 flex items-center gap-3 rounded-2xl border border-accent/30 bg-accent/5 p-4">
      <span aria-hidden="true" className="text-xl">🔔</span>
      <p className="flex-1 text-sm text-foreground/90">
        Activa notificaciones para avisos de cupos liberados y recordatorios de logros.
      </p>
      <button
        onClick={handleSubscribe}
        disabled={state === 'loading'}
        className="min-h-11 shrink-0 rounded-xl bg-accent px-4 text-sm font-bold text-accent-foreground disabled:opacity-50"
      >
        {state === 'loading' ? 'Activando…' : 'Activar'}
      </button>
    </div>
  )
}
