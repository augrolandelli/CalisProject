# Fase 4 — Comunidad

Implementada en septiembre de 2026. Este documento registra las decisiones acordadas,
la configuración de archivos y los comandos de verificación.

## Acceso y reglas

| Capacidad | Guerrero | Clover | Admin |
|---|---|---|---|
| Consultar reseñas y promedios | Sí | Sí | Sí |
| Ver tablón, logros sociales y competencias | — | Sí | Sí |
| Poner / quitar me gusta | — | Sí | Sí |
| Crear / editar publicaciones | — | — | Sí |
| Retirar publicaciones (incluidos logros automáticos) | — | — | Sí |
| Reseñar una clase terminada con reserva previa | — | Sí | Sí, si tiene reserva |
| Eliminar la reseña propia | Sí, incluso tras perder Clover | Sí | Sí |
| Retirar / restaurar reseñas de otros | — | — | Sí |
| Ver eventos / inscribirse / cancelar inscripción | — | Sí | Sí |
| Crear / editar / cancelar eventos y ver inscritos | — | — | Sí |

- El tablón es cronológico y paginado. Los alumnos participan mediante **me gusta**.
- Las publicaciones admiten título, texto plano, hasta **4 fotos + 1 video**.
- Las competencias son anuncios con fecha y lugar; la organización se coordina fuera de la app.
- Un nuevo logro otorgado a un Clover genera un post en la **misma transacción**.
  Los reintentos no generan duplicados. Retirar el post conserva el logro y evita que reaparezca al reintentar.
- Los logros se otorgan a inscritos, después de finalizar la clase, y deben estar asociados a ella.
- Una reseña por usuario/clase: 1–5 estrellas, texto opcional de hasta 1500 caracteres.
  Requiere reserva creada antes del inicio y que haya pasado `Date + DurationMinutes`.
  Las clases existentes reciben **60 minutos**; al crear una clase se aceptan 15–480 minutos.
- Reservas, cancelaciones y lista de espera de clases se cierran al comenzar.
- Las reseñas se publican directamente. Moderación puede retirarlas/restaurarlas;
  las retiradas no cuentan en el promedio. El autor no puede recrear una reseña moderada.
  Al eliminar una clase se conserva su título/fecha en las reseñas.
- Eventos con fecha de inicio y fin, lugar y cupo opcional. Reserva/cancelación/edición
  serializadas mediante `UPDLOCK, HOLDLOCK`; constraint única por evento/usuario.
  Los cambios se cierran al inicio. Cancelar un evento conserva el historial e inscritos.
- Todas las fechas se serializan en UTC y la interfaz muestra la hora local.

## Pantallas

- `/community`: quinta pestaña inferior. **Novedades · Eventos · Reseñas**.
  Guerrero ve las reseñas y una invitación a Clover mediante el WhatsApp configurado.
- `/community/posts/:id`: publicación, archivos y reacción.
- `/community/events/:id`: detalle, inscripción propia e inscritos para Admin.
- `/classes/:id`: reseñas, formulario según elegibilidad y duración de la clase.
- `/admin/community`: accesos de gestión y moderación de reseñas.
- `/admin/community/posts/new`, `/admin/community/posts/:id/edit`.
- `/admin/community/events/new`, `/admin/community/events/:id/edit`.

## Almacenamiento de archivos

El backend soporta dos proveedores, seleccionados con `Storage:Provider`:

### Proveedor Local (por defecto)

Los archivos se guardan en el disco del propio servidor, en `Storage:LocalPath`
(por defecto `storage/`, relativa al directorio de la API, **ignorada por git**).
Las subidas y descargas pasan por endpoints de la API con **URLs firmadas HMAC + expiración**
(la firma usa `Jwt:Key`), con la misma semántica de seguridad que una URL prefirmada:
la URL es la credencial y solo la emite la API tras validar permisos.

- `PUT /api/media/staging/{key}` — subida del navegador con URL firmada de 15 minutos (sin JWT).
- `GET /api/media/files/{key}` — descarga firmada de 5 minutos, con rangos para video.

No requiere cuentas externas y funciona sin internet. Ideal para desarrollo y pruebas.
**Consideración para producción:** al desplegar, usar un volumen persistente o cambiar
a R2; en hostings con disco efímero los archivos locales se pierden al redesplegar,
y servir los videos consume ancho de banda del servidor de la API.

### Proveedor R2 (opcional, recomendado para producción)

Cloudflare R2 (S3 compatible): archivos independientes del servidor, 10 GB/mes gratis
y transferencia de salida sin cargo. Para activarlo:

```powershell
$env:Storage__Provider = "R2"
$env:Storage__AccountId = "<ACCOUNT_ID>"
$env:Storage__BucketName = "<BUCKET_NAME>"
$env:Storage__AccessKeyId = "<ACCESS_KEY_ID>"
$env:Storage__SecretAccessKey = "<SECRET_ACCESS_KEY>"
dotnet run --project backend/CalisApi
```

Pasos en Cloudflare:

1. Crear un bucket R2 **privado** con almacenamiento Standard.
2. Crear credenciales S3 de lectura/escritura de objetos limitadas a ese bucket.
3. Configurar las siguientes variables para el proceso de la API. Ejemplo PowerShell
   desde la raíz del proyecto (reemplazar los placeholders en el entorno local):

```powershell
$env:Storage__AccountId = "<ACCOUNT_ID>"
$env:Storage__BucketName = "<BUCKET_NAME>"
$env:Storage__AccessKeyId = "<ACCESS_KEY_ID>"
$env:Storage__SecretAccessKey = "<SECRET_ACCESS_KEY>"
dotnet run --project backend/CalisApi
```

En producción, configurar estas variables en el proveedor de hosting / gestor de secretos.
`appsettings.json` contiene solamente campos vacíos.

4. Configurar CORS del bucket. Ejemplo para desarrollo y preview (agregar el origen HTTPS real en producción):

```json
[
  {
    "AllowedOrigins": ["http://localhost:5173", "http://localhost:4173"],
    "AllowedMethods": ["GET", "HEAD", "PUT"],
    "AllowedHeaders": ["Content-Type", "Range"],
    "ExposeHeaders": ["ETag", "Content-Length", "Content-Range", "Accept-Ranges"],
    "MaxAgeSeconds": 3600
  }
]
```

5. Configurar una **regla de ciclo de vida**: eliminar objetos con prefijo `staging/`
   después de **1 día**. El original de cada subida vive allí; la versión validada
   se guarda bajo `community/` y debe conservarse.

### Flujo de archivos

1. Admin solicita una subida indicando nombre, MIME y tamaño.
2. API crea el registro y una URL PUT de staging válida por 15 minutos (firmada,
   según el proveedor: prefirmada S3 en R2 o endpoint firmado en Local).
3. El navegador sube directamente al almacenamiento, mostrando progreso,
   **sin enviar el JWT** (la firma de la URL es la credencial).
4. API verifica el tamaño real, firma del formato y, para MP4, estructura, duración y codecs.
5. Los bytes validados se escriben en una clave privada distinta e inmutable, evitando
   que un PUT posterior sobre staging cambie un adjunto ya publicado; luego se borra staging.
6. El editor recibe el ID del adjunto y puede previsualizarlo / asociarlo al post.
7. Clover/Admin solicitan enlaces GET temporales de 5 minutos para adjuntos publicados.
   Un Admin también puede previsualizar sus propias subidas sin publicar durante 24 horas.

Formatos: JPG, PNG, WebP (máximo 10 MiB por imagen); MP4 H.264 con audio AAC opcional
(máximo 100 MiB y 120 segundos). Se valida el contenedor MP4; el almacenamiento guarda
los archivos sin transcodificarlos. Clips en HEVC/MOV deben exportarse a MP4 H.264 antes de subir.

El proceso de limpieza revisa cada hora hasta 100 archivos retirados o sin asociar,
con al menos 24 horas de antigüedad. Borra los objetos antes de retirar sus registros;
los errores se registran y reintentan en el próximo ciclo. Hay límites por usuario
para las subidas y un máximo de dos validaciones de clips simultáneas por proceso.

### Costos de referencia

R2 Standard incluye 10 GB-mes, 1 millón de operaciones clase A y 10 millones clase B
por mes; la transferencia de salida de R2 no tiene cargo. El almacenamiento adicional
cuesta USD 0,015/GB-mes, más operaciones excedentes según tarifa.
Fuente: <https://developers.cloudflare.com/r2/pricing/> (consultada en septiembre de 2026).

## API

Todos estos recursos requieren autenticación. Los permisos de la tabla anterior
se validan en el servidor. Páginas: `page=1`, `pageSize=20`, máximo 50.

| Método | Ruta | Operación |
|---|---|---|
| GET | `/api/post`, `/api/post/{id}` | Tablón / detalle |
| POST / PUT / DELETE | `/api/post[/{id}]` | Crear / editar / retirar (Admin) |
| PUT / DELETE | `/api/post/{id}/like` | Dar / quitar me gusta, idempotente |
| GET | `/api/review?sessionId={id}` | Reseñas visibles, promedio y cantidad; filtro opcional |
| GET | `/api/review/session/{id}/mine` | Elegibilidad y reseña propia |
| PUT | `/api/review/session/{id}` | Crear o actualizar reseña propia |
| DELETE | `/api/review/{id}` | Eliminar reseña propia; si estaba moderada se vacía su texto conservando el bloqueo |
| GET | `/api/review/moderation` | Reseñas visibles y retiradas (Admin) |
| PUT | `/api/review/{id}/visibility` | `{ isHidden }` (Admin) |
| GET | `/api/event?history=false`, `/api/event/{id}` | Próximos / detalle; `history=true` incluye pasados y cancelados |
| POST / PUT | `/api/event[/{id}]` | Crear / editar (Admin) |
| POST | `/api/event/{id}/cancel` | Cancelar evento (Admin) |
| PUT / DELETE | `/api/event/{id}/registration` | Inscribirse / cancelar, idempotente |
| GET | `/api/event/{id}/participants` | Inscritos paginados (Admin) |
| POST | `/api/media` | Iniciar subida (Admin) |
| POST | `/api/media/{guid}/complete` | Validar y finalizar subida (Admin propietario) |
| GET | `/api/media/{guid}/link` | Obtener enlace privado temporal |
| DELETE | `/api/media/{guid}` | Abandonar subida propia sin asociar |
| PUT | `/api/media/staging/{key}` | Subida firmada del proveedor Local (sin JWT; firma + expiración) |
| GET | `/api/media/files/{key}` | Descarga firmada del proveedor Local (rangos para video) |

DTOs y validadores: `backend/CalisApi/Dtos/CommunityDtos.cs` y
`backend/CalisApi/Dtos/Validators/CommunityValidators.cs`.
Migración: `AddCommunity`.

## PWA y sesión

- El app shell y las rutas pueden abrirse desde el service worker.
- `/api/post`, `/api/event`, `/api/review`, `/api/media` y R2 usan **NetworkOnly**.
  Los adjuntos privados y videos no se precachean.
- Se retiran las antiguas cachés amplias; solo se cachean catálogos compartidos y
  las imágenes estáticas del mismo origen. Los detalles personalizados de clases
  quedan fuera de la caché de catálogos.
- Las claves de consulta de comunidad incluyen usuario/rol. Cambiar de cuenta,
  rol o cerrar sesión cancela consultas y limpia TanStack Query.
- Hay aviso explícito de desconexión. Las mutaciones de comunidad fallan sin conexión
  en lugar de quedar pendientes para ejecutarse inesperadamente al volver.
- La restauración de sesión usa una única renovación compartida con el cliente HTTP,
  para evitar el bucle de rotación del refresh token al recargar. Ante una falla de
  red se permite reintentar sin borrar la sesión persistida.

## Datos de demostración

Al arrancar en Development se agregan, si faltan: bienvenida, anuncio de competencia,
evento próximo y una clase pasada con reserva, reseña de demostración del Clover y
un logro asociado listo para otorgar. Se mantienen las cuentas documentadas en `AGENTS.md`.

## Verificación

### Backend (raíz del proyecto)

```powershell
dotnet build CalisApp.slnx
dotnet test backend/CalisApi.Tests/CalisApi.Tests.csproj
```

20 pruebas MSTest: autorización, validación, paginación, likes concurrentes, elegibilidad
de reseñas, moderación, conservación tras borrar clases, cupos concurrentes, eventos
ilimitados/cancelados, logros sin duplicación, archivos privados/inmutables, metadatos
MP4, firmas R2 y proveedor Local (unitarias de disco real + flujo HTTP completo:
PUT firmado → completar → publicar → descarga firmada, incluido rechazo de firmas alteradas).

Las pruebas de API usan una **BD LocalDB aislada** `CalisAppTests_<guid>`, con migraciones
reales, y la eliminan al terminar. El transporte de objetos se sustituye por un almacén
en memoria salvo en la prueba de integración Local, que escribe en una carpeta temporal
real: **las pruebas no certifican conectividad con una cuenta R2 real**.

### Frontend (desde `frontend/`)

```powershell
npm install
npm run build
npm run lint
$env:PLAYWRIGHT_BROWSERS_PATH = "0"
npx playwright install chromium
npm run test:e2e
```

4 pruebas Playwright en Chromium con viewport móvil: acceso Guerrero, ciclo de posts
y likes con cambio de cuenta, creación/inscripción/cancelación de eventos, edición y
moderación de reseñas, caché del SW y estado offline. Incluyen recargas de rutas,
capturas móvil/landscape y movimiento reducido.

El runner levanta API en `5261` y preview en `4174`, contra la BD **CalisAppE2E**;
no usa `CalisAppDb`. Esa BD conserva datos de prueba entre ejecuciones. Las capturas
y traces se guardan en `frontend/test-results/` (ignorado). Para apuntar Vite a otra
API, usar la variable `CALIS_API_URL`.
