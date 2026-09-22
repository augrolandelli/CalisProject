# Deploy beta — Hostinger VPS + Vercel

> Última actualización: septiembre 2026.
> Objetivo: dejar la app funcional para la presentación de esta semana.

## Arquitectura

```
Usuario → HTTPS → Vercel (PWA)
                 ↓ HTTPS / JSON
          Hostinger VPS (API .NET + SQL Server Docker + storage local)
```

- **Frontend:** Vercel (gratis, HTTPS automático).
- **Backend:** Hostinger VPS (ya lo pagás).
- **Base de datos:** SQL Server en Docker dentro del VPS.
- **Storage de archivos:** disco local del VPS (`storage/`).

---

## Opción recomendada: deploy con Easy Panel

Si usás **Easy Panel**, el camino más simple es levantar el backend como un stack Docker Compose con dos servicios: la **API .NET** y **SQL Server**.

> **Importante:** la app usa **SQL Server** (EF Core `UseSqlServer`). El servicio de tipo MySQL/Postgres de Easy Panel **no sirve** para este backend. Tenés que usar SQL Server.

### Archivos que ya están en el repo

- `Dockerfile` — imagen de producción de `CalisApi`.
- `docker-compose.prod.yml` — stack con `api` + `sqlserver` + volúmenes persistentes.
- `.env.example` — variables de entorno requeridas.

### Pasos en Easy Panel

1. **Crear el proyecto** `calisapp` en Easy Panel.
2. **Subir el código**:
   - Conectar el repositorio Git (raíz del repo) y elegir la rama.
   - O subir por SFTP los archivos: `Dockerfile`, `docker-compose.prod.yml` y `.env` a la carpeta del proyecto.
3. **Crear el servicio de base de datos SQL Server**:
   - Si Easy Panel tiene plantilla de SQL Server, usala con la imagen `mcr.microsoft.com/mssql/server:2022-latest`.
   - Si no, creá un servicio Docker con esa misma imagen.
   - Puerto: `1433`.
   - Variable de entorno: `ACCEPT_EULA=Y`.
   - Variable de entorno: `MSSQL_SA_PASSWORD=<muy-segura>`.
   - Agregale un volumen persistente para `/var/opt/mssql`.
4. **Crear el servicio de aplicación**:
   - Tipo: **Docker Compose** o **App from Dockerfile**, según lo que soporte tu plan.
   - Si usás Docker Compose, apuntá a `docker-compose.prod.yml`.
   - Si usás Dockerfile solo, asegurate de que el servicio `sqlserver` exista en el mismo proyecto/red y esté saludable.
   - Puerto del contenedor: `5000`.
   - Volumen persistente para `/app/storage` (ahí se guardan los videos/imágenes subidos).
5. **Configurar variables de entorno** (copiar de `.env.example` y completar):

   ```bash
   MSSQL_SA_PASSWORD=TuPasswordMuySeguro123!
   JWT_KEY=una-clave-larga-y-aleatoria-de-al-menos-32-caracteres
   FRONTEND_URL=https://calisapp.vercel.app
   VAPID_PUBLIC_KEY=
   VAPID_PRIVATE_KEY=
   VAPID_SUBJECT=mailto:tu-email@calisapp.com
   ```

   - `JWT_KEY`: generala con `openssl rand -base64 32`.
   - `FRONTEND_URL`: dominio final del PWA en Vercel (para CORS).
6. **Dominio y SSL**:
   - Asignale un dominio/subdominio al servicio `api` (ej. `api.calisapp.com`).
   - Activá HTTPS desde Easy Panel.
7. **Health check**: configurá Easy Panel para verificar `GET http://<tu-dominio>/api/health`.
8. **Migraciones**: el contenedor aplica automáticamente las migraciones al arrancar en producción, así que la base de datos se crea sola la primera vez.
9. **Storage local**: creá la carpeta persistente y asegurate de que el contenedor pueda escribir en `/app/storage`.

### Probar localmente antes de subir

```bash
cp .env.example .env
# editá .env con tus valores
docker compose -f docker-compose.prod.yml up -d --build
```

Después probá `http://localhost:5000/api/health`.

---

## 1. Backend en Hostinger VPS (alternativa manual)

### Requisitos del VPS

- Ubuntu 22.04/24.04 (o la distro que te dé Hostinger).
- Mínimo **2 GB de RAM** para SQL Server Docker.
- Docker y Docker Compose instalados.
- .NET 10 runtime instalado (o publicás self-contained).

### 1.1 SQL Server en Docker

```bash
docker run -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=TuPasswordMuySeguro123!" \
  -p 127.0.0.1:1433:1433 \
  --name calis-sql \
  -v sqlvolume:/var/opt/mssql \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

> Guardá bien la contraseña del `sa`. No la subas al repo.

### 1.2 Base de datos

Cadena de conexión para la API:

```
Server=127.0.0.1,1433;Database=CalisAppDb;User Id=sa;Password=TuPasswordMuySeguro123!;TrustServerCertificate=True
```

### 1.3 Publicar la API

Desde tu máquina local (PowerShell):

```powershell
cd backend/CalisApi
dotnet publish -c Release -o ./publish
```

Subís la carpeta `publish` al VPS (por ejemplo con `scp` o FTP).

### 1.4 Variables de entorno en el VPS

No uses `appsettings.json` para secretos. Configurá variables de entorno o un archivo `.env` que no se suba al repo:

```bash
export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__DefaultConnection="Server=127.0.0.1,1433;Database=CalisAppDb;User Id=sa;Password=TuPasswordMuySeguro123!;TrustServerCertificate=True"
export Jwt__Key="generar-una-clave-larga-y-aleatoria-de-al-menos-32-caracteres"
export Jwt__AccessTokenMinutes=30
export Jwt__RefreshTokenDays=14
export Cors__AllowedOrigins__0="https://calisapp.vercel.app"
# Si luego usás dominio propio, agregalo aquí.
export Storage__Provider=Local
export Storage__LocalPath=/home/calisapp/storage
export Push__VapidPublicKey="tu-public-key"
export Push__VapidPrivateKey="tu-private-key"
export Push__Subject="mailto:tu-email@calisapp.com"
```

Para generar la clave JWT:

```bash
openssl rand -base64 32
```

### 1.5 Correr la API

Opción simple con systemd:

```bash
sudo nano /etc/systemd/system/calisapi.service
```

Contenido:

```ini
[Unit]
Description=CalisApp API
After=network.target

[Service]
WorkingDirectory=/home/calisapp/publish
ExecStart=/usr/bin/dotnet /home/calisapp/publish/CalisApi.dll
Restart=always
RestartSec=10
User=calisapp
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000
Environment=ConnectionStrings__DefaultConnection=Server=127.0.0.1,1433;Database=CalisAppDb;User Id=sa;Password=TuPasswordMuySeguro123!;TrustServerCertificate=True
Environment=Jwt__Key=tu-clave-jwt
Environment=Cors__AllowedOrigins__0=https://calisapp.vercel.app
Environment=Storage__Provider=Local
Environment=Storage__LocalPath=/home/calisapp/storage

[Install]
WantedBy=multi-user.target
```

Activar:

```bash
sudo systemctl daemon-reload
sudo systemctl enable calisapi
sudo systemctl start calisapi
```

Verificá que levante:

```bash
curl http://localhost:5000/api/health
```

### 1.6 Aplicar migraciones

La primera vez:

```bash
cd /home/calisapp/publish
export ConnectionStrings__DefaultConnection="Server=127.0.0.1,1433;Database=CalisAppDb;User Id=sa;Password=TuPasswordMuySeguro123!;TrustServerCertificate=True"
dotnet CalisApi.dll --apply-migrations
```

> Si no existe un comando para aplicar migraciones, podés correr `dotnet ef database update` desde el código fuente apuntando al VPS, o habilitar `EnsureCreated`/`Migrate` automático en producción (no recomendado a largo plazo, pero sirve para la beta).

### 1.7 Storage local

```bash
mkdir -p /home/calisapp/storage
chown -R calisapp:calisapp /home/calisapp/storage
```

---

## 2. Nginx + SSL

### 2.1 Instalar Nginx

```bash
sudo apt update
sudo apt install nginx certbot python3-certbot-nginx
```

### 2.2 Configurar Nginx

```bash
sudo nano /etc/nginx/sites-available/calisapi
```

```nginx
server {
    listen 80;
    server_name api.tudominio.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Activar:

```bash
sudo ln -s /etc/nginx/sites-available/calisapi /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

### 2.3 SSL con Let's Encrypt

```bash
sudo certbot --nginx -d api.tudominio.com
```

Ahora la API está en `https://api.tudominio.com`.

> Si todavía no tenés dominio propio, podés usar la IP pública del VPS con HTTP solo para probar, **pero no para producción real**.

---

## 3. Frontend en Vercel

### 3.1 Variables de entorno

En el dashboard de Vercel, agregá:

```
VITE_API_URL=https://api.tudominio.com/api
```

> Si todavía no tenés dominio para la API, usá `https://<ip-del-vps>/api` temporalmente (con certificado autofirmado Vercel no va a aceptar; necesitás HTTPS real).

### 3.2 Build settings

Vercel detecta automáticamente Vite. Asegurate de que:

- **Root Directory:** `frontend`
- **Build Command:** `npm run build`
- **Output Directory:** `dist`

### 3.3 Dominio provisional

Vercel te da un dominio tipo `https://calisapp.vercel.app`. Ese dominio debe estar en `Cors__AllowedOrigins__0` del backend.

---

## 4. Checklist final antes de mostrar

- [ ] API responde `GET /api/health` desde el VPS.
- [ ] Migraciones aplicadas y datos de demo cargados (opcional).
- [ ] Frontend en Vercel puede loguearse contra la API (probar login con `admin@calisapp.com`).
- [ ] CORS configurado con el dominio de Vercel.
- [ ] Storage local funciona: subir un video/ejercicio desde `/admin/videos` y reproducirlo.
- [ ] Logros: crear, asociar a clase y otorgar desde `/admin/classes`.
- [ ] PWA: en Chrome en el celu, "Agregar a pantalla de inicio" funciona.
- [ ] WhatsApp de upgrade apunta al número correcto.

---

## 5. Postergados para después de la beta

Ver `AGENTS.md` sección "Postergados para la versión oficial".

- Recuperación de contraseña por email.
- Refresh token en cookie `HttpOnly`.
- i18n.
- Login social.
- Pasarela de pago (upgrade manual por WhatsApp por ahora).
- Push notifications en desarrollo.

---

## Notas

- **No commitees secretos** (`Jwt__Key`, contraseñas de BD, VAPID keys). Usá variables de entorno.
- **Backup de BD:** configurá un backup diario de SQL Server en el VPS.
- **Storage:** si más adelante migrás a Cloudflare R2, solo cambiás `Storage__Provider` y las credenciales.
