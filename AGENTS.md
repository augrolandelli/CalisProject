# CalisApp — Monorepo

Plataforma de calistenia reconstruida como **PWA** + **API REST .NET**.
La especificación funcional y técnica completa está en **[ESPECIFICACION_CALISAPP.md](./ESPECIFICACION_CALISAPP.md)** — leerla antes de implementar cualquier feature.

## Estructura

```
├── backend/CalisApi/     # API REST — ASP.NET Core 10 + EF Core 10 (SQL Server)
├── frontend/             # PWA — React 19 + TypeScript + Vite + Tailwind v4 + vite-plugin-pwa
├── docker-compose.yml    # SQL Server para desarrollo local
├── CalisApp.slnx         # Solución .NET
├── .agents/skills/       # Skills instaladas (dotnet, PWA, UI/UX, seguridad)
├── system_design/        # Mockups originales de referencia
└── ESPECIFICACION_CALISAPP.md
```

## Comandos

### Backend (`backend/CalisApi`)
```bash
dotnet build CalisApp.slnx          # compilar
dotnet run --project backend/CalisApi   # correr (https://localhost:7222 / http://localhost:5260)
dotnet ef migrations add <Nombre> --project backend/CalisApi   # nueva migración
dotnet ef database update --project backend/CalisApi           # aplicar migraciones
```
- Health check: `GET /api/health`
- OpenAPI (dev): `/openapi/v1.json`

### Frontend (`frontend/`)
```bash
npm install
npm run dev      # http://localhost:5173 (proxea /api → http://localhost:5260)
npm run build    # build de producción (genera SW + manifest)
npm run lint
npm run test:e2e # Playwright + API/preview, DB LocalDB separada (ver FASE4_COMUNIDAD.md)
```

### Base de datos local
Se usa **SQL Server LocalDB** (Docker no está instalado en esta máquina):
```
Server=(localdb)\MSSQLLocalDB;Database=CalisAppDb
```
Ya configurada en `backend/CalisApi/appsettings.Development.json`.
Si se instala Docker en el futuro, `docker-compose.yml` levanta SQL Server en `localhost:1433`.
La API aplica migraciones y **siembra datos de dev** automáticamente al arrancar en Development
(usuarios: `admin@calisapp.com` / `Admin123!`, `demo@calisapp.com` / `Demo1234!` (Guerrero)
y `clover@calisapp.com` / `Clover1234!` (Clover); clases de la semana actual, una llena).

## Convenciones

- **Idioma del dominio/código:** inglés en código (clases, métodos, rutas), español en textos de UI y documentación.
- **Backend — arquitectura en capas** (Clean simplificada):
  - `Controllers/` finos, sin lógica de negocio.
  - `Services/` toda la lógica y reglas de negocio.
  - `Data/` DbContext + configuraciones EF (`ApplyConfigurationsFromAssembly`).
  - `Models/` entidades de dominio. `Dtos/` contratos de entrada/salida.
  - **Nunca exponer entidades EF ni hashes** en respuestas — siempre DTOs.
  - Validación con FluentValidation; logging con Serilog; auth JWT + refresh tokens (Fase 1).
- **Frontend — estructura por features:** `src/features/<feature>/{pages,components,api}` + `src/shared/` para UI kit y cliente HTTP (`shared/api/client.ts`).
- **PWA:** estrategias de caché en `vite.config.ts` (NetworkFirst para `/api/*`, CacheFirst para imágenes). No precachear videos.
- **Seguridad:** ver §10 de la especificación. Ningún secreto en el repo; appsettings solo con placeholders.
- **Roles:** `Guerrero` (gratis), `Clover` (pago), `Admin` (staff) — el servidor siempre valida autorización.

## Skills disponibles (`.agents/skills/`)

| Skill | Uso |
|---|---|
| `dotnet-best-practices` | Escribir/revisar código C# del backend |
| `dotnet-design-pattern-review` | Revisión de patrones de diseño .NET |
| `pwa-development` | Service workers, caché, offline, manifest |
| `ui-ux-pro-max` | Decisiones de diseño UI/UX (script `search.py` con Python) |
| `security-best-practices` | Revisiones de seguridad (JS/TS; para C# usar §10 de la spec) |

## Roadmap

Ver §12 de la especificación.

- ✅ **Fase 0** — scaffolding (solución, estructura en capas, PWA config, iconos).
- ✅ **Fase 1** — auth JWT (registro/login/refresh con rotación, PBKDF2, guards en React),
  catálogos (categorías, videoteca con filtros + reproductor fullscreen, rutinas con
  calentamiento-primero + PDF lazy-loaded) y dashboard con datos reales.
- ✅ **Fase 2** — clases en vivo: reserva/cancelación **transaccional con UPDLOCK** +
  constraint única (anti-overbooking), waitlist con **promoción automática + push**,
  upgrade manual Guerrero→Clover (WhatsApp → admin cambia rol en `/admin/users`),
  Web Push (VAPID) con SW custom (`injectManifest`, `src/sw.ts`).
  Entidades nuevas: Session, UserSession, SessionWaitlist, PushSubscription.
- ✅ **Fase 3** — logros (catálogo, mis logros, otorgamiento por admin/profesor en el
  detalle de clase con anti-duplicado), perfil (`/api/me` + edición + stats),
  pantallas `/profile` y `/achievements`, logros recientes en dashboard.
  Entidades nuevas: Achievement, SessionAchievement, UserAchievement.
  **Cronómetro descartado** por decisión del cliente.
- ✅ **Fase 4** — comunidad: tablón Admin con fotos/clips privados R2, me gusta,
  publicaciones automáticas de logros Clover, reseñas verificadas visibles para Guerrero,
  moderación, eventos con inscripción transaccional y competencias como anuncios.
  Gestión en `/admin/community`; nueva pestaña `/community`.
  Configuración, endpoints y reglas: **[FASE4_COMUNIDAD.md](./FASE4_COMUNIDAD.md)**.
  **Storage:** proveedor Local en disco por defecto (`Storage:Provider`, carpeta `storage/`
  ignorada por git, URLs firmadas HMAC); R2 opcional para producción con variables de entorno.
- ✅ **Fase 5** — panel Admin completo en `/admin`: CRUD de clases (edición con push a
  inscritos + borrado por mes con preview), videoteca con subida (MP4 ≤30 s, purpose
  `exercise`), constructor de rutinas (selector de video con lupa), logros (ocultar si
  tiene dueños), categorías (nombre único, borrado bloqueado en uso), usuarios con
  buscador/filtro. Decisiones y endpoints: **[FASE5_ADMIN.md](./FASE5_ADMIN.md)**.

Tests: `dotnet test backend/CalisApi.Tests/CalisApi.Tests.csproj` (30 pruebas,
LocalDB aislada), `npm run test:e2e` en `frontend/` (6 flujos Playwright en Chromium).
Para instalar navegador local al proyecto: `$env:PLAYWRIGHT_BROWSERS_PATH="0"; npx playwright install chromium`.
Mantener esa variable al ejecutar E2E. Detalles en `FASE4_COMUNIDAD.md`.

Migración pendiente de aplicar en producción: `AddAttendanceAndAdminNotification`.

## Estado para presentación beta (esta semana)

Objetivo: estabilizar lo implementado y dejar la app funcional para mostrar.

- [x] **Logros**: corregir bug en el catálogo (los obtenidos no se marcan), selector
  de logros en el formulario de clases del admin, y otorgamiento solo a asistentes
  que aún no tengan el logro.
- [x] **Asistencia**: lista de asistencia en el detalle de clase finalizada; contadores
  de asistidas/faltadas en el perfil.
- [x] **Push al admin**: notificación 15 min después de que termina una clase para
  recordar marcar asistencia y otorgar logros.
- [x] **Deploy**: notas y configuración para Hostinger VPS + Vercel en
  **[DEPLOY.md](./DEPLOY.md)**; agregado `Dockerfile`, `docker-compose.prod.yml` y
  `.env.example` para deployar con Easy Panel. Falta ejecutar el deploy en el VPS
  (necesita dominio o IP pública y certificado HTTPS).
- [x] **Upgrade a Clover**: manual por WhatsApp; el número ya está en `frontend/src/shared/config.ts`.
- [x] **Storage**: proveedor Local por defecto (`storage/` ignorado por git, URLs firmadas HMAC).

## Postergados para la versión oficial

Funcionalidades que no entran en la beta pero quedan anotadas para después:

- Recuperación de contraseña por email.
- i18n (textos hoy hardcodeados en español).
- Refresh token en cookie `HttpOnly` (hoy en `localStorage`, ver §10 de la especificación).
- Tests de cobertura automatizada de fases anteriores.
- Push notifications en desarrollo (hoy solo funcionan en build/preview).
- Login social (Google/Apple).
- Pasarela de pago para upgrade automático Guerrero→Clover (hoy manual por WhatsApp).
- Precio por clase individual, follow a coaches y estadísticas avanzadas.
