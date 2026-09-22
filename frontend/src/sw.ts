/// <reference lib="webworker" />
import { precacheAndRoute, cleanupOutdatedCaches, createHandlerBoundToURL } from 'workbox-precaching'
import { registerRoute, NavigationRoute } from 'workbox-routing'
import { CacheFirst, NetworkFirst, NetworkOnly } from 'workbox-strategies'
import { ExpirationPlugin } from 'workbox-expiration'
import { CacheableResponsePlugin } from 'workbox-cacheable-response'

declare let self: ServiceWorkerGlobalScope & {
  __WB_MANIFEST: Array<string | { url: string; revision: string | null }>
}

// ---- App shell (precache generado por vite-plugin-pwa en build) ----
cleanupOutdatedCaches()
precacheAndRoute(self.__WB_MANIFEST)
registerRoute(new NavigationRoute(createHandlerBoundToURL('/index.html'), { denylist: [/^\/api\//] }))

// ---- Caché en runtime (spec §9) ----
// El contenido exclusivo y sus URLs firmadas siempre requieren conexión y autorización vigente.
registerRoute(
  ({ url }) => /^\/api\/(post|event|review|media)(\/|$)/.test(url.pathname)
    || url.hostname.endsWith('.r2.cloudflarestorage.com'),
  new NetworkOnly(),
)

// Retira cachés antiguas que podían incluir detalles personalizados o imágenes privadas.
self.addEventListener('activate', (event) => {
  event.waitUntil(Promise.all(['calisapi-cache', 'image-cache'].map(name => caches.delete(name))))
})

// Catálogos de la API: NetworkFirst (datos frescos, fallback offline)
registerRoute(
  ({ url }) => /^\/api\/(category|rutine|video)(\/|$)/.test(url.pathname)
    || /^\/api\/(achievement|session)(\/\d+)?$/.test(url.pathname),
  new NetworkFirst({
    cacheName: 'calisapi-public-v2',
    plugins: [
      new ExpirationPlugin({ maxEntries: 100, maxAgeSeconds: 60 * 60 * 24 }),
      new CacheableResponsePlugin({ statuses: [0, 200] }),
    ],
  }),
)

// Imágenes estáticas propias. Los adjuntos exclusivos no se guardan en Cache Storage.
registerRoute(
  ({ request, url }) => request.destination === 'image' && url.origin === self.location.origin && !url.pathname.startsWith('/api/'),
  new CacheFirst({
    cacheName: 'public-images-v2',
    plugins: [
      new ExpirationPlugin({ maxEntries: 60, maxAgeSeconds: 60 * 60 * 24 * 30 }),
      new CacheableResponsePlugin({ statuses: [0, 200] }),
    ],
  }),
)

// ---- Notificaciones push (Fase 2: cupo liberado en waitlist) ----
self.addEventListener('push', (event) => {
  const data = (event.data?.json() ?? {}) as { title?: string; body?: string; url?: string }
  event.waitUntil(
    self.registration.showNotification(data.title ?? 'CalisApp', {
      body: data.body ?? '',
      icon: '/pwa-192x192.png',
      badge: '/pwa-192x192.png',
      data: { url: data.url ?? '/' },
    }),
  )
})

self.addEventListener('notificationclick', (event) => {
  event.notification.close()
  const url = (event.notification.data as { url?: string })?.url ?? '/'
  event.waitUntil(self.clients.openWindow(url))
})
