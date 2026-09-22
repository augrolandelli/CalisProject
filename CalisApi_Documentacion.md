# CalisApi Documentacion de la API - BACKEND

## Informacion General

| Campo | Valor |
|---|---|
| **Framework** | ASP.NET Core 10 Web API (.NET 10) |
| **ORM** | Entity Framework Core 10 (Code-First) |
| **Base de datos** | Microsoft SQL Server |
| **Almacenamiento de videos** | AWS S3 (`calisapp-exercises`, region `us-east-2`) |
| **Autenticacion** | JWT Bearer (HMAC-SHA256, expira en 7 dias) |
| **Base URL Dev** | `https://localhost:7222` / `http://localhost:5260` |

---

## Autenticacion

Se utiliza **JWT Bearer**. Para rutas protegidas, incluye el header:
```
Authorization: Bearer <token>
```

### Claims en el token
| Claim | Valor |
|---|---|
| `NameIdentifier` | `user.Id` |
| `Name` | `user.FullName` |
| `Role` | `"Clover"`,`"Guerrero"` o `"Admin"` |
| `Email` | `user.Email` |

### Roles
| Rol | Acceso |
|---|---|
| `Admin` | Subir y eliminar videos |
| `Clover` | Acceso a la comunidad, a las sesiones. |
| `Guerrero` | Poder ver rutinas y videos de ejercicios |

---

## Endpoints de la API.

### Usuarios — `/api/user`

#### `GET /api/user` · `GET /api/user/{id}`
Retorna todos los usuarios q no sean admins, o uno en especifico si se proporciona `id`.

- **Auth:** No requerida
- **Params:** `id` (int, opcional, ruta)
- **200 OK:**
```json
{
  "id": 1,
  "fullName": "string",
  "phone": "string",
  "email": "string",
  "password": "<hash>",
  "role": "string",
  "state": "Activo"
}
```
- **404:** `"No existe este usuario"`


---

#### `GET /api/user/id/achievements`
Retorna una lista de todos los achievements(logros) que obtuvo el usuario. Hay que pasar el id del usuario

- **Auth:** No requerida
- **Params:** `id` (int)
- **200 OK:**
```json
[
  {
    "id": 1,
    "name": "string",
    "description": "string",
    "icon": "string",
  },
]
```
- **404:** `"No existe este usuario"`


---

#### `POST /api/user/login`
Autentica un usuario y retorna un JWT.

- **Body:**
```json
{ "email": "string", "password": "string" }
```
- **200 OK:** Token JWT (string plano)
- **409:** Email no registrado / contrasena incorrecta

---

#### `POST /api/user/register`
Registra un nuevo usuario y retorna un JWT. El usuario hay ue registrtarlo en primera instacion con rol 'Guerrero'

- **Body:**
```json
{
  "fullName": "string",
  "phone": "string",
  "email": "string",
  "password": "string",
  "role": "string"
}
```
- **200 OK:** Token JWT (string plano)
- **409:** Email ya registrado


---

### Sesiones — `/api/session`

#### `GET /api/session` · `GET /api/session?datetime=2025-12-25`
Lista todas las sesiones, opcionalmente filtradas por fecha.

- **Query:** `datetime` (DateTime, opcional — formato `YYYY-MM-DD`)
- **200 OK:** Array de `SessionResponseDto`
```json
[{
  "id": 1,
  "title": "string",
  "description": "string",
  "date": "2025-12-25T10:00:00",
  "limitedSpots": 20,
  "enrolled": 5,
  "achievementIds": [1, 2]
}]
```
- **204:** Sin sesiones encontradas

---

#### `GET /api/session/{id}`
Retorna una sesion por ID.

- **200 OK:** `SessionResponseDto`
- **404:** `"La sesión no existe."`

---

#### `GET /api/session/{id}/details`
Retorna la sesion con la lista de usuarios inscritos.

- **200 OK:**
```json
{
  "session": { "...entidad Session completa..." },
  "enrolledUsers": [{ "id": 1, "fullName": "string" }]
}
```
- **404:** `"La sesión no existe."`

---

#### `GET /api/session/{id}/Users`
Retorna solo los usuarios inscritos en una sesion.

- **200 OK:** Array de `{ "id": 1, "fullName": "string" }`
- **404 / 500:** Error de sesion no encontrada o interno

---

#### `GET /api/session/{id}/Achievements`
Retorna los logros asociados a una sesion.

- **200 OK:** Array de `Achievement`
```json
[{ "id": 1, "name": "string", "description": "string", "icon": "string" }]
```

---

### Inscripciones — `/api/usersession`

#### `POST /api/usersession/{sessionId}`
Inscribe al usuario autenticado en una sesion. El ID del usuario se extrae del JWT.

- **Auth:** Requerida — Rol `"Clover"`
- **Params:** `sessionId` (int, ruta)
- **Body:** Ninguno
- **Reglas de negocio:**
  - La sesion debe existir
  - El usuario debe existir
  - El usuario no debe estar ya inscrito
  - Debe haber cupos disponibles (`Enrolled < LimitedSpots`)
  - El incremento de inscritos se realiza dentro de una transaccion de BD
- **200 OK:** Entidad `Session`
- **400:** Ya inscrito / sin cupos / token invalido
- **401:** Token ausente o invalido
- **404:** Sesion o usuario no encontrado

---

#### `DELETE /api/usersession/{id}`
Desinscribe al usuario autenticado de una sesion (`id` corresponde al `sessionId`).

- **Auth:** Requerida — Rol `"Clover"`
- **Params:** `id` (int, ruta) — es el ID de la sesion, no del registro `UserSession`
- **Body:** Ninguno
- **204:** Exito
- **400:** `"No estás inscrito en esta clase."`
- **401:** Token ausente o invalido
- **500:** Error interno

---

### Logros — `/api/achievement`

#### `GET /api/achievement`
Retorna todos los logros.

- **200 OK:**
```json
[{ "id": 1, "name": "string", "description": "string", "icon": "string" }]
```
- **500:** Error interno

---
#### `GET /api/achievement/id/users`
Retorna todos los usuarios que obtuvieron el logro del cual se paso el id. Arreglo de enteros (id de los usuarios).

- **200 OK:**
```json
[
  1,3,8
]
```
- **500:** Error interno

---

### Categorias — `/api/category`

#### `GET /api/category`
Retorna todas las categorias.

- **200 OK:**
```json
[{ "id": 1, "name": "string", "description": "string" }]
```

---

### Rutinas — `/api/rutine`

#### `GET /api/rutine`
Lista resumida de todas las rutinas.

- **200 OK:**
```json
[{
  "id": 1,
  "title": "string",
  "description": "string",
  "Duration":"string",
  "Dificulty":"string",
  "categoryName": "string"
}]
```

---

#### `GET /api/rutine/{id}`
Detalle completo de una rutina con todos sus ejercicios. Los ejercicios de tipo `"Calentamiento"` aparecen primero. los demas son `"Principal"`.

- **Params:** `id` (int, ruta)
- **200 OK:**
```json
{
  "title": "string",
  "description": "string",
  "duration": "string",
  "Dificulty":"string",
  "categoryId": 2,
  "exercises": [
    {
      "exercise": "string",
      "tipo": "Calentamiento",
      "reps": 10,
      "series": 3,
      "descanso": "60s",
      "obs": "string",
      "videoId": 5
    }
  ]
}
```


### Videos — `/api/video`

> Todos los endpoints de video requieren un JWT valido (`[Authorize]` a nivel de controlador).

#### `GET /api/video`
Lista todos los videos. Soporta filtros opcionales.

- **Query params:**
  - `categoryId` (int, opcional)
  - `searchTerm` (string, opcional — busca en titulo y descripcion, insensible a mayusculas)
- **200 OK:** Array de `Video` con objeto `category` anidado
```json
[{
  "id": 1,
  "title": "string",
  "description": "string",
  "dificulty": "string",
  "requisites": "string",
  "url": "https://calisapp-exercises.s3.us-east-2.amazonaws.com/videos/...",
  "categoryId": 2,
  "category": { "id": 2, "name": "string", "description": "string" }
}]
```

---

#### `GET /api/video/{id}`
Retorna un video por ID (incluye categoria anidada).

- **Auth:** Requerida (cualquier token valido)
- **Params:** `id` (int, ruta)
- **200 OK:** `Video` (mismo esquema que el array anterior)
- **404:** No encontrado

---

### Posts - `/api/post`

#### `GET /api/post` · `GET /api/post/{id}`
Retorna todos los posts, o uno en especifico si se proporciona `id`.

- **Auth:** No requerida
- **Params:** `id` (int, opcional, ruta)
- **200 OK:**
```json
{
  "id": 1,
  "title": "string",
  "content": "string",
  "createdAt": "2025-01-01T00:00:00"
}
```
- **404:** `"No existe este post"`

---

#### `POST /api/post`
Crea un nuevo post.

- **Auth:** No requerida
- **Body:**
```json
{
  "title": "string",
  "content": "string"
}
```
- **200 OK:** Objeto `Post` creado
- **409:** Post ya existe (`message`: descripcion del conflicto)
- **500:** Error interno

---

## Modelos de Datos

### User
```
Tabla: Users
├── Id          int (PK, auto-increment)
├── FullName    nvarchar(max), requerido
├── Phone       nvarchar(max), requerido
├── Email       nvarchar(max), requerido
├── Password    nvarchar(max), requerido  (hash PBKDF2)
├── Role        nvarchar(max), requerido  — "Usuario" | "Admin"
└── State       nvarchar(max), requerido  — se asigna "Activo" al registrarse
```

### Session
```
Tabla: Sessions
├── Id            int (PK, auto-increment)
├── Title         nvarchar(max), requerido
├── Description   nvarchar(max), requerido
├── Date          datetime2, requerido
├── LimitedSpots  int, requerido
├── Enrolled      int, requerido  (gestionado con transacciones)
└── SessionAchievements → ICollection<SessionAchievement>
```

### UserSession
```
Tabla: UserSessions
├── Id         int (PK, auto-increment)
├── UserId     int (FK → Users.Id, CASCADE)
└── SessionId  int (FK → Sessions.Id, CASCADE)
```

### Achievement
```
Tabla: Achievements
├── Id           int (PK, auto-increment)
├── Name         nvarchar(max), requerido
├── Description  nvarchar(max), requerido
└── Icon         nvarchar(max), requerido
```

### UserAchievement
```
Tabla: UserAchievements
├── Id             int (PK, auto-increment)
├── UserId         int (FK → Users.Id, CASCADE)
├── AchievementId  int (FK → Achievements.Id, CASCADE)
├── DateEarned     datetime2, requerido  (default: UtcNow)
└── SessionId      int?, nullable
```

### Category
```
Tabla: Categories
├── Id           int (PK, auto-increment)
├── Name         nvarchar(max), requerido
└── Description  nvarchar(max), requerido
```

### Video
```
Tabla: Videos
├── Id           int (PK, auto-increment)
├── Title        nvarchar(max), requerido
├── Description  nvarchar(max), requerido
├── Dificulty    nvarchar(max), requerido 
├── Requisites   nvarchar(max), requerido
├── Url          nvarchar(max), requerido  (URL de S3, generada automaticamente)
└── CategoryId   int (FK → Categories.Id, CASCADE)
```

### Rutine
```
Tabla: Rutines
├── Id           int (PK, auto-increment)
├── Title        nvarchar(max), requerido
├── Description  nvarchar(max), requerido
├── Duration     nvarchar(max), requerido  (ej. "45 min")
├── Dificulty    nvarchar(max), requerido  — "basica" | "intermedia" | "avanzada"
├── CategoryId   int (FK → Categories.Id, CASCADE)
└── Exercises    → ICollection<RutineExercise>
```

### RutineExercise
```
Tabla: RutineExercises
├── Id        int (PK, auto-increment)
├── Exercise  nvarchar(max), requerido  (nombre del ejercicio)
├── Tipo      nvarchar(max), requerido  (ej. "Calentamiento")
├── Reps      int, requerido
├── Series    int, requerido
├── Descanso  nvarchar(max), requerido  (ej. "60s")
├── Obs       nvarchar(max), requerido  (observaciones)
├── VideoId   int?, nullable (FK → Videos.Id, RESTRICT)
└── RutineId  int (FK → Rutines.Id, CASCADE)
```

### Post
```
Tabla: Posts
├── Id        int (PK, auto-increment)
├── Title  nvarchar(max), requerido  (titulo del post)
├── Content      nvarchar(max), requerido  (Descripcion del post)
└── CreatedAt      DateTime (automatico cuando se crea el post)
```

---