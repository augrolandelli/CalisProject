import { useEffect, useState } from 'react'
import { useAuthStore } from '../auth/authStore'

export function useCommunityIdentity() {
  return useAuthStore(s => `${s.user?.id ?? ''}:${s.user?.role ?? ''}`)
}

export function useNow() {
  const [now, setNow] = useState(Date.now)
  useEffect(() => { const timer = setInterval(() => setNow(Date.now()), 30000); return () => clearInterval(timer) }, [])
  return now
}

export function dateLabel(value: string) {
  return new Date(value).toLocaleString('es-AR', { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

export function localDateInput(value: string) {
  const date = new Date(value)
  return new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16)
}
