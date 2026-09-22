import { apiClient } from '../shared/api/client'

function urlBase64ToUint8Array(base64String: string): Uint8Array<ArrayBuffer> {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4)
  const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/')
  const rawData = window.atob(base64)
  const outputArray = new Uint8Array(rawData.length)
  for (let i = 0; i < rawData.length; ++i) {
    outputArray[i] = rawData.charCodeAt(i)
  }
  return outputArray
}

/**
 * Pide permiso de notificaciones y suscribe el dispositivo al push del servidor.
 * Requiere el service worker activo (build de producción / preview).
 * @returns true si quedó suscripto.
 */
export async function subscribeToPush(): Promise<boolean> {
  if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
    return false
  }

  const permission = await Notification.requestPermission()
  if (permission !== 'granted') {
    return false
  }

  const { data } = await apiClient.get<{ publicKey: string }>('/push/vapid-public-key')

  const registration = await navigator.serviceWorker.ready
  const subscription = await registration.pushManager.subscribe({
    userVisibleOnly: true,
    applicationServerKey: urlBase64ToUint8Array(data.publicKey),
  })

  const json = subscription.toJSON()
  await apiClient.post('/push/subscribe', {
    endpoint: json.endpoint,
    keys: { p256dh: json.keys?.p256dh, auth: json.keys?.auth },
  })

  return true
}

/** Estado actual de la suscripción push del navegador. */
export function pushSupport(): { supported: boolean; permission: NotificationPermission } {
  const supported = 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window
  return { supported, permission: supported ? Notification.permission : 'denied' }
}
