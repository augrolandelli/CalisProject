# Fase 5 — Panel de administración

Implementada en septiembre de 2026. Este documento registra las decisiones acordadas
y la referencia técnica del panel.

## Decisiones

- **Videos de ejercicios**: MP4 H.264/AAC, **máximo 30 segundos y 50 MB**.
  Sin miniatura propia: la lista muestra el video (primer frame).
  Usan el mismo flujo de subida de la comunidad con `purpose: "exercise"`.
- **Borrados en uso — bloqueados con aviso claro**:
  - Video referenciado por ejercicios de rutinas → indica cuántas rutinas lo usan.
  - Categoría con videos o rutinas → indica las cantidades.
- **Logros**: si nadie lo ganó → borrado físico (sus asociaciones a clases caen en cascada).
  Si tiene dueños → se **oculta** del catálogo (`IsHidden`), el historial de alumnos se
  conserva y ya no se puede otorgar.
- **Clases**: editables solo antes del inicio; el cupo no puede bajar de los inscritos.
  Si cambia fecha/horario, los inscritos reciben **notificación push** (best-effort).
- **Borrado por mes**: solo meses **ya finalizados**, con preview de cantidad antes
  de confirmar. Reseñas y logros de usuarios se conservan (FK SetNull).
- **Usuarios**: buscador por nombre/email + filtro por rol (máx. 200 resultados).
- **Layout**: hub en `/admin` con accesos; mismo estilo mobile del resto de la app.

## Pantallas

| Ruta | Sección |
|---|---|
| `/admin` | Hub con accesos |
| `/admin/classes` | Clases: crear, editar, eliminar, limpieza por mes |
| `/admin/videos` | Videoteca: subir (progreso + validación 30 s), editar metadatos, reemplazar archivo, eliminar |
| `/admin/routines` | Constructor de rutinas: ejercicios ordenables (↑↓), tipo, series/reps/descanso/obs y selector de video con lupa + filtro por categoría |
| `/admin/achievements` | Catálogo: crear, editar, eliminar/ocultar con conteo de dueños |
| `/admin/categories` | CRUD con nombre único (case-insensitive) |
| `/admin/users` | Buscador + filtro por rol + cambio Guerrero↔Clover |
| `/admin/community` | (Fase 4) Novedades, eventos, moderación |

Acceso desde Perfil → "Panel de administración" (solo rol Admin; el servidor valida siempre).

## API (todas AdminOnly salvo lectura)

| Método | Ruta | Operación |
|---|---|---|
| PUT | `/api/session/{id}` | Editar clase (antes del inicio; cupo ≥ inscritos; push si cambia la fecha) |
| GET | `/api/session/purge-preview?year=&month=` | Cantidad de clases del mes (solo pasados) |
| DELETE | `/api/session?year=&month=` | Borrado masivo del mes (solo pasados) |
| POST / PUT / DELETE | `/api/video[/{id}]` | Crear / editar (+reemplazo de archivo) / eliminar (bloqueado si lo usan rutinas) |
| POST / PUT / DELETE | `/api/rutine[/{id}]` | CRUD de rutinas; el servidor ordena calentamiento primero |
| POST / PUT / DELETE | `/api/category[/{id}]` | CRUD; nombre único; borrado bloqueado con contenido |
| GET | `/api/achievement/admin` | Catálogo con ocultos y conteo de dueños |
| PUT / DELETE | `/api/achievement/{id}` | Editar / eliminar u ocultar según dueños |
| GET | `/api/user?search=&role=` | Lista con buscador y filtro (Guerrero/Clover) |
| POST | `/api/media` con `purpose:"exercise"` | Subida de video de ejercicio (MP4 ≤30 s, ≤50 MB) |

El detalle de video (`GET /api/video/{id}`) devuelve la **URL de reproducción firmada**
(30 min) cuando el archivo está en el almacenamiento; los videos externos/semilla
conservan su URL directa.

Migración: `AddPhase5Admin` (`Achievements.IsHidden`, `MediaAssets.Purpose`, `MediaAssets.VideoId`).

## Verificación

- `dotnet test backend/CalisApi.Tests/CalisApi.Tests.csproj` — **29 pruebas** (9 nuevas de
  Fase 5: edición/borrado de clases y purge, reglas de subida exercise, videos con URL
  firmada y borrado bloqueado, rutinas con normalización y referencias, categorías,
  logros con ocultamiento, buscador de usuarios).
- `npm run test:e2e` en `frontend/` — **5 flujos**: los 4 de comunidad + panel Admin
  (hub, clase crear/editar/eliminar, categoría con duplicado, constructor de rutinas
  visible para alumnos, buscador de usuarios).
