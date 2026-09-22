using System.Security.Cryptography;
using System.Text;
using CalisApi.Common;
using Microsoft.Extensions.Options;

namespace CalisApi.Services.Storage;

/// <summary>
/// Almacenamiento en disco del propio servidor (proveedor por defecto para desarrollo
/// y despliegues simples). Las URLs de subida/lectura son relativas a la API y llevan
/// una firma HMAC con expiración, equivalente a una URL prefirmada de R2:
/// el archivo no requiere JWT para descargarse, pero la firma solo la emite la API
/// tras validar permisos.
/// </summary>
public sealed class LocalFileStorage(
    IOptions<StorageSettings> options,
    IHostEnvironment environment,
    IConfiguration configuration) : IObjectStorage
{
    private readonly StorageSettings settings = options.Value;

    public bool IsConfigured =>
        settings.Provider.Equals("Local", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(settings.LocalPath);

    /// <summary>Raíz absoluta del almacenamiento (relativa al ContentRoot si no es absoluta).</summary>
    public string Root => Path.GetFullPath(Path.IsPathRooted(settings.LocalPath)
        ? settings.LocalPath
        : Path.Combine(environment.ContentRootPath, settings.LocalPath));

    /// <summary>Ruta física de una clave, con protección contra path traversal.</summary>
    public string PathFor(string key)
    {
        var path = Path.GetFullPath(Path.Combine(Root, key.Replace('/', Path.DirectorySeparatorChar)));
        return path.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            ? path
            : throw new AppException("Ruta de archivo inválida.");
    }

    /// <summary>Firma HMAC-SHA256 de una clave + expiración, usando la clave JWT del servidor.</summary>
    public string Sign(string key, long expiresUnix)
    {
        var secret = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Falta Jwt:Key para firmar URLs locales.");
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"local-media\n{key}\n{expiresUnix}"));
        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>Valida firma y expiración de una URL emitida por esta API.</summary>
    public bool Validate(string key, long expiresUnix, string? signature)
    {
        if (string.IsNullOrEmpty(signature) || expiresUnix < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            return false;
        var expected = Sign(key, expiresUnix);
        return expected.Length == signature.Length
            && CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(signature));
    }

    /// <inheritdoc />
    public Task<string> UploadUrlAsync(string key, string contentType, DateTime expiresAt)
    {
        var expires = new DateTimeOffset(expiresAt, TimeSpan.Zero).ToUnixTimeSeconds();
        return Task.FromResult($"/api/media/staging/{key}?expires={expires}&signature={Sign(key, expires)}");
    }

    /// <inheritdoc />
    public Task<string> ReadUrlAsync(string key, DateTime expiresAt)
    {
        var expires = new DateTimeOffset(expiresAt, TimeSpan.Zero).ToUnixTimeSeconds();
        return Task.FromResult($"/api/media/files/{key}?expires={expires}&signature={Sign(key, expires)}");
    }

    /// <inheritdoc />
    public async Task<byte[]> ReadAsync(string key, long expectedSize, CancellationToken ct)
    {
        var info = new FileInfo(PathFor(key));
        if (!info.Exists || info.Length != expectedSize || expectedSize is <= 0 or > 104857600)
            throw new AppException("El tamaño del archivo subido no coincide con el declarado.");
        return await File.ReadAllBytesAsync(info.FullName, ct);
    }

    /// <inheritdoc />
    public async Task WriteAsync(string key, byte[] contents, string contentType, CancellationToken ct)
    {
        await using var stream = new MemoryStream(contents, writable: false);
        await WriteStreamAsync(key, stream, ct);
    }

    /// <summary>Escritura atómica de un stream (temporal + move) dentro de la raíz configurada.</summary>
    public async Task WriteStreamAsync(string key, Stream source, CancellationToken ct)
    {
        var path = PathFor(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            await using (var output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
                await source.CopyToAsync(output, ct);
            File.Move(temp, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    /// <inheritdoc />
    public Task DeleteAsync(string key, CancellationToken ct)
    {
        var path = PathFor(key);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }
}
