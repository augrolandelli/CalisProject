# CalisApp — Especificación Funcional y Técnica para Reconstrucción como PWA

> **Estado del proyecto:** reconstrucción desde cero (greenfield). Este documento consolida
> toda la información recuperada del proyecto original (documentación de API, mockups de UI,
> lista de tareas y notas de desarrollo) y la reescribe como una especificación completa para
> construir la aplicación de cero como **Progressive Web App (PWA)**.
>
> **Audiencia:** desarrollador(a) responsable de la implementación.
> **Última actualización:** septiembre 2026.

---

## Tabla de contenidos

1. [Visión general](#1-visión-general)
2. [Roles y modelo de negocio](#2-roles-y-modelo-de-negocio)
3. [Módulos funcionales](#3-módulos-funcionales)
4. [Contrato de API (diseño objetivo)](#4-contrato-de-api-diseño-objetivo)
5. [Modelo de datos](#5-modelo-de-datos)
6. [Diseño UI/UX](#6-diseño-uiux)
7. [Stack tecnológico recomendado](#7-stack-tecnológico-recomendado)
8. [Arquitectura propuesta](#8-arquitectura-propuesta)
9. [Capacidades PWA](#9-capacidades-pwa)
10. [Seguridad](#10-seguridad)
11. [Buenas prácticas de ingeniería](#11-buenas-prácticas-de-ingeniería)
12. [Roadmap por fases](#12-roadmap-por-fases)
13. [Anexo: fuentes originales](#13-anexo-fuentes-originales)

---

## 1. Visión general

**CalisApp** (nombre de trabajo: *"Calisthenics Elite"*) es una plataforma para un gimnasio /
comunidad de **calistenia** que combina:

- **Reserva de clases en vivo** con cupos limitados.
- **Biblioteca de rutinas de entrenamiento** organizadas por categoría y dificultad.
- **Videoteca de ejercicios** con videos alojados en la nube.
- **Sistema de logros (achievements)** gamificado, otorgados por los profesores.
- **Comunidad**: posts, reseñas de clases, eventos y competencias.
- **Modelo freemium**: acceso gratuito limitado + suscripción paga.

### Componentes del sistema

| Componente | Descripción |
|---|---|
| **PWA (cliente)** | Aplicación web progresiva, mobile-first, instalable. Unifica lo que antes era la app móvil de alumnos y (con rutas separadas por rol) el panel web de administración. |
| **API REST (backend)** | Servidor que expone todos los recursos: usuarios, sesiones, rutinas, videos, logros, comunidad. |
| **Base de datos relacional** | Persistencia de todas las entidades. |
| **Almacenamiento de objetos** | Videos de ejercicios (en el proyecto original: AWS S3). |

---

## 2. Roles y modelo de negocio

La aplicación se divide en dos niveles de acceso más administración:

| Rol | Tipo | Acceso |
|---|---|---|
| **Guerrero** | Gratuito | Rutinas genéricas, videoteca de ejercicios y lectura de reseñas de clases. |
| **Clover** | Pago (suscripción) | Todo lo de Guerrero + reserva de clases, logros, comunidad, eventos y competencias. |
| **Admin** | Staff | Todo lo anterior + gestión de contenido: subir/eliminar videos, crear rutinas, crear clases, asignar logros, moderar comunidad. |

**Reglas de negocio clave:**

- Todo usuario nuevo se registra inicialmente con rol **Guerrero**.
- Existe una sección de **"Upgrade"** dentro de la app para pasar de Guerrero a Clover (pago).
- El upgrade a Clover habilita: clases, logros, eventos, comunidad, etc.
- En Fase 4, Guerrero accede a todas las reseñas visibles como muestra de la experiencia
  Clover; el tablón, los archivos y los eventos requieren Clover/Admin.
- El cambio de rol lo puede ejecutar el Admin desde el panel, o automáticamente tras el pago
  (integración de pasarela de pago queda como fase posterior — ver [Roadmap](#12-roadmap-por-fases)).

---

## 3. Módulos funcionales

### 3.1 Autenticación y cuenta

**Pantallas:** Login, Registro, Recuperar contraseña.

- Login con email + contraseña.
- Registro con: nombre completo, teléfono, email, contraseña (rol inicial `Guerrero`, estado `Activo`).
- Opción de login social (Google / Apple) — visible en el diseño original; prioridad baja para el MVP.
- Recuperación de contraseña ("Forgot?") vía email.
- Sesión mediante **JWT** (ver [Seguridad](#10-seguridad)).
- Logout desde el menú lateral.

### 3.2 Dashboard (Home)

Pantalla principal tras el login. Contenido:

- Saludo personalizado ("Hey, {nombre}!") y campana de notificaciones.
- **Tarjeta de próxima clase en vivo**: nombre, fecha/hora, cupos restantes ("3 spots left")
  y botón **"Book my spot"**.
- **Rutina actual del usuario**: nombre, duración en semanas, cantidad de ejercicios,
  barra de progreso (%) y enlace a detalle. Botón "Change" para cambiar de rutina.
- **Logros recientes**: carrusel con medallas (icono + nombre), total de logros y
  placeholders de logros bloqueados.
- **Cronómetro / temporizador de entrenamiento** (estaba en las tareas pendientes).
- Acceso al **menú lateral (sidebar)**.

### 3.3 Clases (sesiones en vivo)

**Pantallas:** lista de clases, detalle de clase.

**Lista / reserva:**
- Selector de día de la semana (tira horizontal Lunes–Domingo).
- Filtrado de clases por fecha.
- Tarjeta por clase: imagen, badge de dificultad (Beginner / Intermediate / Advanced),
  título, horario, nombre del coach, cupos ocupados/disponibles con barra de progreso.
- Estados de la tarjeta:
  - Con cupos → botón **"Book Spot"**.
  - Clase llena → botón **"Join Waitlist"** (lista de espera).
- **Reglas de reserva (críticas):**
  - El usuario no puede inscribirse dos veces a la misma clase.
  - Solo se puede reservar si hay cupos (`inscritos < cupo máximo`).
  - El incremento del contador de inscritos debe hacerse **dentro de una transacción de BD**
    para evitar overbooking concurrente.
  - Cancelación de reserva disponible (desinscripción).

**Detalle de clase:**
- Imagen de portada, badge de dificultad, título, fecha y horario.
- Tarjeta del **coach principal** (foto, nombre, botón "Follow").
- Descripción completa de la clase.
- Lista de **participantes inscritos** (nombre) con contador de cupos.
- Botón de reserva (puede mostrar precio, ej. "Book Spot — $25.00").
- **Logros asociados a la clase**: al finalizar la clase, el profesor puede otorgar logros
  específicos a los asistentes (ej. en una clase de plancha, otorgar "Tuck Planche").

### 3.4 Rutinas

**Pantallas:** categorías de rutinas, rutinas por categoría, detalle de rutina.

- **Explorar por categoría** (Pull, Push, Legs, etc.): tarjetas con imagen, nombre,
  cantidad de rutinas disponibles y descripción.
- **Lista por categoría**: buscador + filtros por objetivo (ej. Hypertrophy, Strength) y
  dificultad. Tarjeta por rutina: imagen, badge de dificultad, título, descripción,
  duración aproximada y cantidad de ejercicios. Opción de descarga (para uso offline — ver PWA).
- **Detalle de rutina:**
  - Badge de dificultad, duración estimada, título y descripción.
  - Botón **"Download routine (PDF)"**.
  - **Lista de ejercicios ordenada**: los de tipo **Calentamiento** primero, luego los
    **Principales**. Cada ítem: thumbnail, nombre, series, reps (o tiempo de hold),
    y navegación al detalle del ejercicio/video asociado.
  - Cada ejercicio de la rutina incluye: `series`, `reps`, `descanso` (ej. "60s"),
    `observaciones` y referencia opcional a un video de la videoteca.
  - **"Coach's tip"**: consejo destacado del profesor.

### 3.5 Videoteca de ejercicios

**Pantallas:** biblioteca (categorías), ejercicios por categoría, detalle de ejercicio.

- **Biblioteca**: buscador global ("Search push, pull, legs…"), tarjetas de categorías
  (Pull / Push / Legs / …) con cantidad de ejercicios y acceso a videos.
  Sección destacada de **rutina personalizada** ("sync to watch offline").
- **Lista por categoría**: filtros por dificultad (All / Beginner / Intermediate / Advanced).
  Ítem: thumbnail, nombre, badge de dificultad, rango de reps sugerido.
- **Detalle de ejercicio:**
  - **Reproductor de video** (videos servidos desde almacenamiento en la nube) con opción
    de **pantalla completa**.
  - Badges de dificultad y grupo muscular.
  - Datos: duración, calorías estimadas, series x reps.
  - **Prerequisitos** del ejercicio (ej. "8 dominadas estrictas").
  - **Observaciones del profesor** (técnica, errores comunes).
  - Descarga de rutina PDF relacionada.

### 3.6 Logros (Achievements)

- Catálogo de logros: nombre, descripción, icono.
- Logros obtenidos por el usuario (visibles en su perfil/dashboard).
- **Logros asociados a clases**: cada clase puede tener logros específicos; al finalizar,
  el profesor otorga el logro a los asistentes que lo consiguieron, quedando registrado
  con fecha y la clase de origen.
- Consulta inversa: qué usuarios obtuvieron un logro dado.

### 3.7 Comunidad

**Decisiones confirmadas e implementadas en Fase 4 (septiembre de 2026):**

- **Tablón exclusivo Clover/Admin**: publicaciones del Admin con título, texto,
  hasta 4 fotos y un clip corto. Interacción mediante me gusta.
- **Logros sociales**: al otorgar un nuevo logro a un Clover se crea una publicación
  automática en la misma transacción, sin duplicación ante reintentos.
- **Reseñas de clases**: 1–5 estrellas y texto opcional; una por alumno/clase.
  Requieren reserva previa al inicio y clase finalizada (`DurationMinutes`, 60 por defecto).
  Todas las reseñas visibles son consultables por Guerrero, Clover y Admin.
  Admin puede retirar/restaurar reseñas; las retiradas no cuentan en el promedio.
- **Eventos**: Admin crea encuentros con fecha, lugar y cupo opcional; miembros
  pueden anotarse/cancelar antes del comienzo. Cupos transaccionales e historial.
- **Competencias**: anuncios con fecha, lugar y descripción.
- Archivos privados en **Cloudflare R2 (S3 compatible)** con URLs temporales.
  La cuenta y credenciales del servicio se configuran externamente.
- Pantallas, API vigente, límites, configuración y pruebas en
  **[FASE4_COMUNIDAD.md](./FASE4_COMUNIDAD.md)**.

### 3.8 Perfil de usuario

- Foto de perfil, nombre, información personal (teléfono, email).
- Edición de perfil.
- **Rutina personalizada** asignada.
- Estadísticas de progreso ("Progress Stats").
- Configuración de la app ("App Settings").
- Logout.

### 3.9 Panel de administración (mismo PWA, rutas protegidas por rol Admin)

- CRUD de **clases**: crear/editar/eliminar. **Eliminación masiva por mes** (limpieza de clases pasadas).
- CRUD de **videos** de ejercicios: subida a almacenamiento en la nube, con categoría,
  dificultad y prerequisitos.
- CRUD de **rutinas**: se arman seleccionando **ejercicios ya cargados previamente** en la
  videoteca: se filtra por categoría y se buscan con lupa. Cada ejercicio agregado lleva
  series, reps, descanso, tipo (calentamiento/principal) y observaciones. La rutina tiene
  **duración aproximada**.
- CRUD de **logros** y su **asignación a usuarios al finalizar una clase**.
- Gestión de **categorías**.
- Moderación de **comunidad** (posts).
- Gestión de usuarios y roles (upgrade Guerrero → Clover).

### 3.10 Menú lateral (sidebar)

- Header con foto y nombre del usuario + "Edit Profile".
- Navegación: Dashboard, Achievements, Progress Stats, App Settings.
- Sección "Upcoming": próxima clase agendada.
- Logout.

### 3.11 Navegación principal (bottom nav)

5 pestañas desde Fase 4: **Home · Classes · Exercises · Routine · Comunidad**
(Profile accesible desde el header).

---

## 4. Contrato de API (diseño objetivo)

> Esta es la especificación del contrato REST a implementar. Replica (con mejoras de
> seguridad) el contrato del sistema original, para preservar las decisiones de dominio.
>
> Base URL propuesta: `https://api.<dominio>/api/`
> Autenticación: `Authorization: Bearer <JWT>` en rutas protegidas.
> Todos los responses de error deben usar un formato uniforme:
> `{ "message": "...", "code": "..." }` con el HTTP status adecuado.
> **Nunca devolver el hash de contraseña** ni datos sensibles en ningún endpoint
> (el sistema original lo hacía en `/api/user` — corregir en la reconstrucción).

### 4.1 Usuarios — `/api/user`

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| GET | `/api/user` | Admin | Lista usuarios (excluye admins). Soportar paginación. |
| GET | `/api/user/{id}` | Sí | Usuario por id. 404 si no existe. |
| GET | `/api/user/{id}/achievements` | Sí | Logros obtenidos por el usuario. |
| POST | `/api/user/register` | No | Registro. Body: `{ fullName, phone, email, password }`. Devuelve JWT (+ refresh token). Rol inicial `Guerrero`. 409 si el email ya existe. |
| POST | `/api/user/login` | No | Body: `{ email, password }`. Devuelve JWT (+ refresh token). 401 credenciales inválidas. |
| POST | `/api/user/refresh` | Refresh token | Renueva el access token (rotación de refresh tokens). |
| POST | `/api/user/forgot-password` | No | Inicia flujo de recuperación por email. |
| PATCH | `/api/user/{id}` | Sí (propio o Admin) | Editar perfil (nombre, teléfono, foto). |
| PATCH | `/api/user/{id}/role` | Admin | Cambio de rol (Guerrero ↔ Clover ↔ Admin). |

### 4.2 Sesiones (clases) — `/api/session`

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| GET | `/api/session` | No | Lista sesiones. Query opcional `?datetime=YYYY-MM-DD` para filtrar por día. 204 si no hay. |
| GET | `/api/session/{id}` | No | Sesión por id. 404 si no existe. |
| GET | `/api/session/{id}/details` | Sí | Sesión + lista de usuarios inscritos (`{ id, fullName }`). |
| GET | `/api/session/{id}/users` | Sí | Solo usuarios inscritos. |
| GET | `/api/session/{id}/achievements` | No | Logros asociados a la sesión. |
| POST | `/api/session` | Admin | Crear clase. |
| PUT | `/api/session/{id}` | Admin | Editar clase. |
| DELETE | `/api/session/{id}` | Admin | Eliminar clase. |
| DELETE | `/api/session?month=YYYY-MM` | Admin | Eliminación masiva de clases de un mes (limpieza). |

**DTO de sesión (response):**
```json
{
  "id": 1,
  "title": "string",
  "description": "string",
  "date": "2026-12-25T10:00:00",
  "limitedSpots": 20,
  "enrolled": 5,
  "achievementIds": [1, 2]
}
```

### 4.3 Inscripciones — `/api/usersession`

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| POST | `/api/usersession/{sessionId}` | Rol **Clover** | Inscribe al usuario autenticado (id extraído del JWT). |
| DELETE | `/api/usersession/{sessionId}` | Rol **Clover** | Desinscribe al usuario autenticado. |

**Reglas de negocio (obligatorias):**
- La sesión y el usuario deben existir (404 en caso contrario).
- No permitir doble inscripción (400).
- No permitir inscripción sin cupos: `enrolled < limitedSpots` (400).
- El chequeo de cupo + inserción + incremento del contador **en una única transacción**,
  idealmente con constraint única `(userId, sessionId)` en BD y bloqueo a nivel de fila
  (`SELECT ... FOR UPDATE` o equivalente) para evitar race conditions.
- Desinscripción: 400 "No estás inscrito en esta clase" si no existe la inscripción.
- **Lista de espera (waitlist)**: modelar como tabla aparte (`SessionWaitlist`) con
  posición ordenada; cuando se libera un cupo se notifica al primero (push — ver PWA).

### 4.4 Logros — `/api/achievement`

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| GET | `/api/achievement` | No | Catálogo completo de logros. |
| GET | `/api/achievement/{id}/users` | Sí | Ids de usuarios que obtuvieron el logro. |
| POST | `/api/achievement` | Admin | Crear logro. |
| POST | `/api/achievement/{id}/grant` | Admin/Coach | Otorgar logro a usuario(s) al finalizar una clase (body: `{ userIds: [], sessionId }`). |

### 4.5 Categorías — `/api/category`

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| GET | `/api/category` | No | Todas las categorías `{ id, name, description }`. |
| GET | `/api/category/{id}` | No | Categoría por id. |
| POST / PUT / DELETE | `/api/category[/{id}]` | Admin | Gestión. |

### 4.6 Rutinas — `/api/rutine`

> Nota: el nombre original del recurso era `rutine`; puede mantenerse por compatibilidad
> o normalizarse a `/api/routine` en la reconstrucción (decisión del equipo, documentarla).

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| GET | `/api/rutine` | Sí | Lista resumida: `{ id, title, description, duration, difficulty, categoryName }`. Soportar filtro por `categoryId` y `searchTerm`. |
| GET | `/api/rutine/{id}` | Sí | Detalle completo con ejercicios. **Los de tipo `Calentamiento` primero**, luego `Principal`. |
| POST / PUT / DELETE | `/api/rutine[/{id}]` | Admin | Gestión de rutinas. |

**Detalle de rutina (response):**
```json
{
  "title": "string",
  "description": "string",
  "duration": "45 min",
  "difficulty": "basica | intermedia | avanzada",
  "categoryId": 2,
  "exercises": [
    {
      "exercise": "string",
      "tipo": "Calentamiento | Principal",
      "reps": 10,
      "series": 3,
      "descanso": "60s",
      "obs": "string",
      "videoId": 5
    }
  ]
}
```

### 4.7 Videos — `/api/video`

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| GET | `/api/video` | Sí | Lista videos. Query: `categoryId`, `searchTerm` (busca en título y descripción, case-insensitive). |
| GET | `/api/video/{id}` | Sí | Video por id (con categoría anidada). 404 si no existe. |
| POST | `/api/video` | Admin | Subida de video (multipart o subida directa a S3 con URL prefirmada — recomendado). |
| DELETE | `/api/video/{id}` | Admin | Eliminar video (BD + objeto en S3). |

**Video (response):**
```json
{
  "id": 1,
  "title": "string",
  "description": "string",
  "difficulty": "string",
  "requisites": "string",
  "url": "https://<bucket>.s3.<region>.amazonaws.com/videos/...",
  "categoryId": 2,
  "category": { "id": 2, "name": "string", "description": "string" }
}
```

### 4.8 Comunidad / Posts — `/api/post`

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| GET | `/api/post` · `/api/post/{id}` | Clover/Admin | Lista / detalle de posts. Paginación + orden por fecha desc. |
| POST / PUT | `/api/post[/{id}]` | Admin | Crear/editar novedad o competencia con adjuntos validados. |
| DELETE | `/api/post/{id}` | Admin | Retirar post; conservar identidad de publicaciones automáticas. |
| PUT / DELETE | `/api/post/{id}/like` | Clover/Admin | Dar/quitar me gusta, idempotente. |

Los contratos de reseñas (`/api/review`), eventos (`/api/event`) y archivos (`/api/media`)
están detallados en [FASE4_COMUNIDAD.md](./FASE4_COMUNIDAD.md#api).

### 4.9 Convenciones generales de la API

- Versionado: `/api/v1/...` (recomendado desde el día uno).
- Paginación en listados grandes: `?page=1&pageSize=20` + metadatos `{ total, page, pageSize }`.
- Respuestas de validación: `400` con detalle campo por campo.
- Documentación automática con **OpenAPI/Swagger** publicada en `/swagger`.
- Timestamps en UTC (`datetime2` / `timestamptz`), formato ISO-8601.
- CORS restringido al dominio del PWA.

---

## 5. Modelo de datos

> Diseño relacional (Code-First con migraciones). Nombres de tabla en plural.

### User — `Users`
| Campo | Tipo | Notas |
|---|---|---|
| Id | int PK identity | |
| FullName | string, requerido | |
| Phone | string, requerido | |
| Email | string, requerido, **único** | índice único |
| PasswordHash | string, requerido | **PBKDF2/bcrypt/Argon2** — nunca texto plano |
| Role | string, requerido | `Guerrero` \| `Clover` \| `Admin` |
| State | string, requerido | `Activo` al registrarse |
| PhotoUrl | string, nullable | foto de perfil |

### Session — `Sessions`
| Campo | Tipo | Notas |
|---|---|---|
| Id | int PK | |
| Title | string, requerido | |
| Description | string, requerido | |
| Date | datetime, requerido | UTC |
| LimitedSpots | int, requerido | cupo máximo |
| Enrolled | int, requerido | contador gestionado con transacciones |
| SessionAchievements | navegación | 1:N con `SessionAchievement` |

### UserSession — `UserSessions`
| Campo | Tipo | Notas |
|---|---|---|
| Id | int PK | |
| UserId | FK → Users (CASCADE) | |
| SessionId | FK → Sessions (CASCADE) | |
| — | **Constraint única (UserId, SessionId)** | previene doble inscripción |

### SessionWaitlist — `SessionWaitlists` *(nuevo, para lista de espera)*
| Campo | Tipo | Notas |
|---|---|---|
| Id | int PK | |
| UserId | FK → Users | |
| SessionId | FK → Sessions | |
| Position | int | orden de llegada |
| CreatedAt | datetime | |

### Achievement — `Achievements`
| Campo | Tipo | Notas |
|---|---|---|
| Id | int PK | |
| Name | string, requerido | |
| Description | string, requerido | |
| Icon | string, requerido | nombre/URL del icono |

### SessionAchievement — `SessionAchievements`
| Campo | Tipo | Notas |
|---|---|---|
| Id | int PK | |
| SessionId | FK → Sessions (CASCADE) | |
| AchievementId | FK → Achievements (CASCADE) | logros que se pueden ganar en esa clase |

### UserAchievement — `UserAchievements`
| Campo | Tipo | Notas |
|---|---|---|
| Id | int PK | |
| UserId | FK → Users (CASCADE) | |
| AchievementId | FK → Achievements (CASCADE) | |
| DateEarned | datetime, default UtcNow | |
| SessionId | int, nullable, FK → Sessions | clase en la que se ganó (si aplica) |

### Category — `Categories`
| Campo | Tipo | Notas |
|---|---|---|
| Id | int PK | |
| Name | string, requerido | ej. Pull, Push, Legs |
| Description | string, requerido | |

### Video — `Videos`
| Campo | Tipo | Notas |
|---|---|---|
| Id | int PK | |
| Title | string, requerido | |
| Description | string, requerido | |
| Difficulty | string, requerido | `basica` \| `intermedia` \| `avanzada` |
| Requisites | string, requerido | prerequisitos |
| Url | string, requerido | URL del objeto en S3 (generada al subir) |
| CategoryId | FK → Categories (CASCADE) | |

### Rutine — `Rutines`
| Campo | Tipo | Notas |
|---|---|---|
| Id | int PK | |
| Title | string, requerido | |
| Description | string, requerido | |
| Duration | string, requerido | ej. "45 min" |
| Difficulty | string, requerido | `basica` \| `intermedia` \| `avanzada` |
| CategoryId | FK → Categories (CASCADE) | |
| Exercises | navegación 1:N | → `RutineExercises` |

### RutineExercise — `RutineExercises`
| Campo | Tipo | Notas |
|---|---|---|
| Id | int PK | |
| Exercise | string, requerido | nombre del ejercicio |
| Tipo | string, requerido | `Calentamiento` \| `Principal` |
| Reps | int, requerido | |
| Series | int, requerido | |
| Descanso | string, requerido | ej. "60s" |
| Obs | string, requerido | observaciones |
| VideoId | int, nullable, FK → Videos (**RESTRICT**) | video de referencia |
| RutineId | FK → Rutines (CASCADE) | |
| Order | int | orden dentro de la rutina |

### Post — `Posts`
| Campo | Tipo | Notas |
|---|---|---|
| Id | int PK | |
| Title | string, requerido | |
| Content | string, requerido | |
| CreatedAt | datetime, automático | |
| AuthorId | FK → Users | autor Admin o miembro que obtuvo el logro |
| Kind | string | announcement / achievement / competition |
| UserAchievementId | FK nullable → UserAchievements, único | deduplicación de publicaciones automáticas |
| UpdatedAt / IsDeleted | datetime nullable / bool | edición y retiro lógico |
| StartsAt / Location | datetime nullable / string nullable | datos de competencia |

Fase 4 agrega `PostLikes`, `MediaAssets`, `SessionReviews`, `CommunityEvents` y
`EventRegistrations`, y `Sessions.DurationMinutes`. Ver las configuraciones EF y
la migración `AddCommunity` para constraints e índices.

### Diagrama de relaciones (resumen)

```
Users 1───N UserSessions N───1 Sessions 1───N SessionAchievements N───1 Achievements
Users 1───N UserAchievements N───1 Achievements
Users 1───N SessionWaitlists N───1 Sessions
Categories 1───N Videos
Categories 1───N Rutines 1───N RutineExercises N───0..1 Videos
Users 1───N Posts
```

---

## 6. Diseño UI/UX

> Basado en los 10 mockups originales recuperados (carpeta `system_design/`).
> Estilo general: **tema oscuro, mobile-first, estética "elite/gym"**.

### Paleta y estilo

| Elemento | Valor |
|---|---|
| Fondo principal | Negro / gris muy oscuro (`#0A0A0A`–`#121212`) |
| Acento principal | Verde lima brillante (mockups: `#22C55E`/`#4ADE80`) — **decisión pendiente: migrar a DORADO**, como ya se usó en la pantalla de login original (amarillo `#FACC15`) — definir en Fase 1 |
| Texto | Blanco / gris claro |
| Badges de dificultad | Beginner = verde, Intermediate = naranja, Advanced = rojo |
| Componentes | Tarjetas con bordes redondeados, botones pill grandes de alto contraste, tipografía bold para títulos |

### Pantallas de referencia (mockups originales)

| Mockup | Pantalla |
|---|---|
| `login.png` | Login: logo, email, password, "Forgot?", login social, link a Sign Up |
| `dashboard.png` | Home: próxima clase, rutina actual con progreso, logros recientes, bottom nav |
| `sidebar.png` | Menú lateral: perfil, navegación, upcoming, logout |
| `session_list.png` | Reserva de clases: selector de día, tarjetas con cupos, waitlist |
| `session_detail.png` | Detalle de clase: coach, descripción, participantes, book spot |
| `ruines_category_flter.png` | Categorías de rutinas |
| `rutines_by_category.png` | Rutinas por categoría con filtros |
| `rutine_Detail.png` | Detalle de rutina: lista de ejercicios, PDF, coach's tip |
| `videosexercises_category_filter.png` | Biblioteca de ejercicios por categoría |
| `videosexercises_by_category.png` | Lista de ejercicios con filtros de dificultad |
| `videosexercise_detail.png` | Reproductor + observaciones del profesor |

### Directrices

- **Mobile-first**: diseñar para pantalla de teléfono; en desktop el panel admin aprovecha
  el ancho (layout con sidebar fijo).
- Navegación inferior (bottom nav) de 5 pestañas desde Fase 4; sidebar en desktop planificado para el panel completo.
- Estados de carga (skeletons), estados vacíos y mensajes de error en todas las listas.
- Accesibilidad: contraste AA, foco visible, labels en formularios, `prefers-reduced-motion`.
- Los textos de los mockups están en inglés; **definir idioma(s)** del producto
  (español/inglés) y centralizarlos en un sistema de i18n desde el inicio.

---

## 7. Stack tecnológico recomendado

> Recomendación concreta para no abrir debates innecesarios; se listan alternativas
> solo donde el cambio es razonable.

### Frontend (PWA)

| Capa | Recomendado | Alternativas |
|---|---|---|
| Framework | **React 18+ con TypeScript** | Vue 3 / Svelte |
| Build / tooling | **Vite** + `vite-plugin-pwa` (Workbox) | Next.js (si se quiere SSR/SEO) |
| Estilos | **Tailwind CSS** | CSS Modules |
| Estado / datos del servidor | **TanStack Query** (cache, reintentos, estados de carga) | Redux Toolkit + RTK Query |
| Estado local | Zustand o Context | — |
| Routing | React Router | — |
| Formularios | React Hook Form + Zod | — |
| i18n | `react-i18next` | — |
| Tests | Vitest + Testing Library + Playwright (E2E) | — |

### Backend (API)

| Capa | Recomendado | Alternativas |
|---|---|---|
| Framework | **ASP.NET Core Web API (.NET 8 LTS o superior)** | Node.js (NestJS) si el equipo prefiere un solo lenguaje |
| ORM | **Entity Framework Core (Code-First + migraciones)** | Dapper |
| Base de datos | **SQL Server** | **PostgreSQL** (alternativa recomendable para reducir costos de hosting) |
| Auth | **JWT Bearer + refresh tokens** | — |
| Hashing de passwords | **PBKDF2 / bcrypt / Argon2** | — |
| Validación | FluentValidation | DataAnnotations |
| Logging | Serilog | — |
| Docs API | Swagger / OpenAPI (Swashbuckle o Scalar) | — |
| Tests | xUnit + WebApplicationFactory (integración) + Testcontainers | — |

### Infraestructura

| Recurso | Recomendado | Notas |
|---|---|---|
| Videos / archivos | **AWS S3** (bucket privado + **URLs prefirmadas** o CloudFront) | El original usaba bucket `calisapp-exercises` en `us-east-2` |
| Hosting API | Azure App Service / contenedor en Railway / Fly.io / VPS | El original estaba en `runasp.net` (válido para empezar barato) |
| Hosting PWA | Netlify / Vercel / Cloudflare Pages (estático + HTTPS) | |
| Base de datos | Según hosting (Azure SQL / Railway Postgres / etc.) | |
| Pasarela de pago (fase posterior) | Stripe / MercadoPago | para el upgrade Guerrero → Clover |
| Notificaciones push | **Web Push (VAPID)** — Firebase Cloud Messaging como servicio puente si se prefiere | |
| Email transaccional | SendGrid / Resend / Amazon SES | recuperación de contraseña, confirmaciones |
| CI/CD | **GitHub Actions** | build, tests, migraciones y deploy |

---

## 8. Arquitectura propuesta

```
┌─────────────────────────────────────────────────────┐
│                    PWA (SPA estática)                │
│  React + TS + Vite  ·  Service Worker  ·  IndexedDB │
│  /app/*        → experiencia alumno (Guerrero/Clover)│
│  /admin/*      → panel Admin (guard por rol)         │
└──────────────┬──────────────────────────────────────┘
               │ HTTPS / JSON (JWT Bearer)
┌──────────────▼──────────────────────────────────────┐
│              API REST (ASP.NET Core)                 │
│  Controllers → Services (lógica de negocio)          │
│              → EF Core → Base de datos               │
│  Auth: JWT + refresh · Autorización por roles/claims │
└──────┬───────────────────────────┬──────────────────┘
       │                           │
┌──────▼──────┐            ┌───────▼────────┐
│  SQL Server  │            │   AWS S3        │
│  (o Postgres)│            │  (videos, fotos)│
└─────────────┘            └────────────────┘
```

### Backend — estructura en capas (Clean Architecture simplificada)

```
CalisApi/
├── Controllers/        # endpoints HTTP (finos, sin lógica)
├── Services/           # lógica de negocio y reglas (cupos, transacciones)
├── Data/               # DbContext, configuraciones EF, migraciones
├── Models/             # entidades de dominio
├── Dtos/               # contratos de entrada/salida (nunca exponer entidades)
├── Auth/               # emisión/validación JWT, hashing, políticas por rol
└── Common/             # errores uniformes, validación, logging
```

**Reglas arquitectónicas:**
- Los controllers nunca devuelven entidades EF: siempre DTOs (evita fugas como el
  `password` hash del sistema original).
- Toda regla de negocio vive en Services, con tests unitarios.
- Las operaciones de inscripción/desinscripción usan transacciones explícitas.
- Un `ProblemDetails` uniforme para todos los errores.

### Frontend — estructura por features

```
src/
├── features/
│   ├── auth/          # login, registro, guards
│   ├── dashboard/
│   ├── classes/       # lista, detalle, reserva, waitlist
│   ├── routines/
│   ├── exercises/     # videoteca
│   ├── achievements/
│   ├── community/
│   ├── profile/
│   └── admin/         # panel (lazy-loaded, guard Admin)
├── shared/            # UI kit, hooks, api client (fetch wrapper con JWT)
├── pwa/               # service worker config, push, offline
└── i18n/
```

---

## 9. Capacidades PWA

> La PWA reemplaza a la app móvil nativa original. Mapeo de capacidades:

| Capacidad | Implementación | Feature que habilita |
|---|---|---|
| **Instalable** | `manifest.webmanifest` (nombre, iconos maskable 192/512, `display: standalone`, `theme_color` oscuro) | Ícono en el home del teléfono, splash screen |
| **Service Worker** | `vite-plugin-pwa` (Workbox) | App shell instantáneo, funcionamiento offline |
| **Caché de estáticos** | Cache-first para JS/CSS/imágenes con versionado | Carga rápida |
| **Caché de API** | Network-first con fallback a caché (stale-while-revalidate) para catálogos: categorías, rutinas, ejercicios, logros | Consultar rutinas sin conexión |
| **Datos offline del usuario** | **IndexedDB** (vía TanStack Query persister o Dexie) | Mi rutina actual, mis logros, mis reservas visibles offline |
| **Descarga de rutina** | Botón "download" → guardar rutina + metadatos en Cache Storage/IndexedDB | "Sync to watch offline" (ya estaba en el diseño) |
| **Videos offline** | Descarga **opt-in por video** a Cache API con gestión de cuota (`navigator.storage.estimate()`); **no** precachear videos automáticamente | Ver ejercicios sin internet (limitado por cuota del dispositivo) |
| **Notificaciones push** | Web Push (VAPID) + suscripción guardada en BD por usuario | Recordatorio de clase próxima, cupo liberado en waitlist, nuevo logro otorgado |
| **Background Sync** | Cola de mutaciones fallidas (ej. reserva) para reintentar al recuperar conexión | Robustez de reservas con mala señal |
| **Pantalla completa de video** | Fullscreen API + Media Session API | Reproducción de ejercicios (pendiente en el proyecto original) |
| **PDF de rutina** | Generación en backend o cliente (jsPDF) + descarga | "Download routine (PDF)" |

### Limitaciones PWA a comunicar al cliente

- **Push en iOS**: funciona desde iOS 16.4+ **solo si la PWA está instalada** en el home.
- Cuota de almacenamiento variable por navegador/dispositivo (afecta videos offline).
- Sin acceso a tiendas de apps (ventaja: un solo deploy, sin revisión de stores; decisión ya tomada).

---

## 10. Seguridad

> Lista de requisitos **obligatorios** para la reconstrucción (varios corrigen
> debilidades del sistema original):

### Autenticación y sesiones
- [ ] Contraseñas con **hash fuerte** (PBKDF2 con salt, bcrypt o Argon2). Nunca texto plano.
- [ ] **Nunca devolver** el hash ni datos sensibles en respuestas de la API.
- [ ] **Access token JWT corto** (15–60 min; el original duraba 7 días — demasiado) + **refresh token** con rotación y revocación.
- [ ] Claims del JWT: `sub` (user id), `name`, `email`, `role`. Firma HMAC-SHA256 con secreto fuerte en variable de entorno / vault.
- [ ] Autorización **por políticas de rol** en el servidor (Guerrero / Clover / Admin). El cliente oculta UI, pero la API siempre valida.
- [ ] Endpoint de login con **rate limiting** y bloqueo progresivo (anti brute-force).
- [ ] Recuperación de contraseña con token de un solo uso y expiración corta.

### API y datos
- [ ] **HTTPS obligatorio** en todo (HSTS).
- [ ] **CORS** restringido al dominio del PWA (nunca `*` con credenciales).
- [ ] Validación de **toda entrada** en el servidor (FluentValidation); parámetros de ruta/query tipados.
- [ ] ORM con consultas parametrizadas (EF Core lo hace por defecto) — prohibido concatenar SQL.
- [ ] **Transacciones + constraints de BD** para cupos e inscripciones (anti race conditions y doble inscripción).
- [ ] Headers de seguridad: `X-Content-Type-Options`, `Referrer-Policy`, `Content-Security-Policy`.
- [ ] Rate limiting global y por endpoint sensible.
- [ ] Errores sin stack traces en producción (formato `ProblemDetails` genérico + logging interno).

### Archivos (videos/fotos)
- [ ] Bucket S3 **privado**; acceso mediante URLs prefirmadas con expiración (o CloudFront con signed URLs).
- [ ] Validación de tipo/tamaño de archivo en subida (MIME sniffing, límite de MB).
- [ ] Subida directa a S3 con URL prefirmada (el backend no proxifica el binario).

### Secretos y configuración
- [ ] Ningún secreto en el repositorio: variables de entorno / user-secrets en dev / vault en prod.
- [ ] Rotación de claves (JWT signing key, AWS keys) documentada.

### Frontend
- [ ] Tokens en memoria + refresh con cookie `HttpOnly; Secure; SameSite=Strict` (preferido) o, mínimo, `sessionStorage` — evitar `localStorage` para tokens si es posible (riesgo XSS).
- [ ] Sanitización de contenido de comunidad (posts) contra XSS (renderizado escapado por defecto; si se permite HTML, sanitizar con DOMPurify).
- [ ] Service worker con scope controlado y actualizaciones versionadas.

---

## 11. Buenas prácticas de ingeniería

- [ ] **Control de versiones**: Git + GitHub. Ramas `main` (prod), `develop`, features con PR + code review. Conventional Commits.
- [ ] **CI/CD (GitHub Actions)**: lint + build + tests en cada PR; deploy automático a staging; migraciones de BD como paso explícito del pipeline.
- [ ] **Migraciones EF Core** versionadas en el repo; jamás editar la BD a mano.
- [ ] **Tests**:
  - Unitarios: reglas de negocio (cupos, inscripciones, orden de ejercicios, filtros).
  - Integración: endpoints críticos (login, reserva con concurrencia, autorización por rol).
  - E2E (Playwright): flujos felices — registro → login → reservar clase → ver rutina.
- [ ] **OpenAPI/Swagger** siempre actualizado y publicado; el frontend consume el contrato documentado.
- [ ] **Logging estructurado** (Serilog) + monitoreo de errores (Sentry o similar) en API y PWA.
- [ ] **Variables de entorno** por ambiente (dev / staging / prod) y `appsettings` sin secretos.
- [ ] **Dockerfile** para la API (despliegue portable) y `docker-compose` local con BD para desarrollo.
- [ ] **Linting/formato** obligatorios: ESLint + Prettier (front), `.editorconfig` + analyzers (back), con pre-commit hooks.
- [ ] **Paginación y límites** en todos los listados desde la v1.
- [ ] **i18n** desde el inicio (no hardcodear textos).
- [ ] **Accesibilidad** como criterio de aceptación (contraste, teclado, aria-labels).
- [ ] **Lighthouse PWA audit** en CI (objetivo: installable + performance > 90 en móvil).

---

## 12. Roadmap por fases

> Orden sugerido de construcción. Cada fase entrega valor usable.

### Fase 0 — Fundaciones (1–2 semanas)
- Repo(s), CI/CD, ambientes, BD + migraciones iniciales, Swagger.
- Definir decisión pendiente: **color acento** (verde vs dorado) e **idioma(s)**.

### Fase 1 — MVP gratuito (rol Guerrero)
- Auth completa (registro, login, JWT + refresh, recuperar contraseña).
- Categorías, videoteca con reproductor (pantalla completa) y filtros.
- Rutinas: explorar, filtrar, detalle con ejercicios ordenados, PDF.
- PWA instalable con app shell offline y caché de catálogos.
- Dashboard básico con datos reales.

### Fase 2 — Clases y rol Clover
- Sesiones: listado por fecha, detalle con participantes.
- Reserva/cancelación con transacciones + constraint única + waitlist.
- Sección "Upgrade" (flujo de pago — Stripe/MercadoPago).
- Notificaciones push (recordatorio de clase, cupo liberado).

### Fase 3 — Logros y perfil
- Catálogo de logros, logros del usuario, logros por clase (otorgamiento por el profesor).
- Perfil: foto, datos, rutina personalizada, estadísticas de progreso.
- Cronómetro de entrenamiento en dashboard.

### Fase 4 — Comunidad
- ✅ Tablón Admin, adjuntos privados R2, me gusta y logros automáticos.
- ✅ Reseñas verificadas, lectura gratuita y moderación.
- ✅ Eventos con cupos/inscripción e historial; competencias como anuncios.
- ✅ Gestión de comunidad, migración y pruebas automatizadas API/SQL Server + navegador.
- Configuración operativa pendiente: cuenta/bucket/credenciales R2 para subidas reales.
- Detalle de entrega: [FASE4_COMUNIDAD.md](./FASE4_COMUNIDAD.md).

### Fase 5 — Panel Admin completo
- ✅ CRUD de clases (edición con push a inscritos, borrado por mes solo de meses pasados
  con preview), videos (subida MP4 ≤30 s al storage Local/R2, borrado bloqueado si los
  usan rutinas), rutinas (constructor con buscador de videos, calentamiento normalizado
  primero), logros (ocultar si tienen dueños, conservando historial), categorías (nombre
  único, borrado bloqueado en uso) y usuarios (buscador + filtro de rol).
- Hub de administración en `/admin`. Decisiones y contratos: [FASE5_ADMIN.md](./FASE5_ADMIN.md).

### Backlog / futuro
- Login social (Google/Apple).
- Precio por clase individual (el mockup mostraba "Book Spot — $25.00").
- Follow a coaches.
- Estadísticas avanzadas.

---

## 13. Anexo: fuentes originales

Este documento consolida la información recuperada de:

| Archivo | Contenido |
|---|---|
| `CalisApi_Documentacion.md` | Documentación completa de la API original (endpoints, DTOs, modelos, roles JWT) |
| `API_DOCS.docx` | Resumen de endpoints con URL de producción original (`calisapi.runasp.net`) |
| `Tareas.docx` | Lista de tareas pendientes del proyecto original (incorporadas al roadmap) |
| `deviceToLocalhost.docx` | Nota de desarrollo (adb reverse) — evidencia de la app móvil original |
| `system_design/*.png` (11 mockups) | Diseño de todas las pantallas principales |
| `api_imgs/*.png` | Capturas de pruebas de endpoints |
| `cwlogo.jpeg`, `vtlogo.jpeg` | Logos originales |

**Notas de reconstrucción (decisiones tomadas respecto al original):**
1. App móvil nativa → **PWA única** (móvil + panel admin en la misma codebase).
2. JWT de 7 días → **access token corto + refresh token rotativo**.
3. El endpoint de usuarios devolvía el hash de contraseña → **prohibido** en la nueva API.
4. Roles `Guerrero`/`Clover`/`Admin` con modelo freemium: queda como diseño central.
5. Recurso `/api/rutine` puede renombrarse a `/api/routine` (decisión del equipo).
