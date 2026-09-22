# syntax=docker/dockerfile:1
# Dockerfile de producción para CalisApi (ASP.NET Core 10 + EF Core 10).
# Contexto de build: raíz del repositorio.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar solo el proyecto primero para aprovechar la caché de NuGet.
COPY backend/CalisApi/CalisApi.csproj ./backend/CalisApi/
RUN dotnet restore ./backend/CalisApi/CalisApi.csproj

# Copiar el resto del código y publicar.
COPY backend/CalisApi/ ./backend/CalisApi/
WORKDIR /src/backend/CalisApi
RUN dotnet publish CalisApi.csproj -c Release -o /app/publish --no-restore

# ---------------------------------------------------------------------------

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Puerto expuesto. Easy Panel descubrirá este puerto o podés mapearlo al 5000.
EXPOSE 5000

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000

# Storage local por defecto (se persiste con un volumen en docker-compose.prod.yml).
ENV Storage__Provider=Local
ENV Storage__LocalPath=/app/storage

# Crear la carpeta de storage dentro del contenedor.
RUN mkdir -p /app/storage

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "CalisApi.dll"]
