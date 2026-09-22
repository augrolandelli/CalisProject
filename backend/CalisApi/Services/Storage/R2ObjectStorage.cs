using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using CalisApi.Common;
using Microsoft.Extensions.Options;

namespace CalisApi.Services.Storage;

/// <summary>
/// Configuración de almacenamiento. Provider "Local" guarda en disco del servidor;
/// "R2" usa Cloudflare R2 (S3 compatible). Credenciales por variables de entorno o user-secrets.
/// </summary>
public class StorageSettings
{
    public string Provider { get; set; } = "Local";
    public string LocalPath { get; set; } = "storage";
    public string AccountId { get; set; } = "";
    public string BucketName { get; set; } = "";
    public string AccessKeyId { get; set; } = "";
    public string SecretAccessKey { get; set; } = "";
}

/// <summary>Almacén privado con subidas directas a staging y publicación de bytes validados.</summary>
public interface IObjectStorage
{
    bool IsConfigured { get; }
    Task<string> UploadUrlAsync(string key, string contentType, DateTime expiresAt);
    Task<string> ReadUrlAsync(string key, DateTime expiresAt);
    Task<byte[]> ReadAsync(string key, long expectedSize, CancellationToken ct);
    Task WriteAsync(string key, byte[] contents, string contentType, CancellationToken ct);
    Task DeleteAsync(string key, CancellationToken ct);
}

/// <summary>Adaptador S3 para Cloudflare R2. Nunca hace público el bucket.</summary>
public sealed class R2ObjectStorage(IOptions<StorageSettings> options) : IObjectStorage, IDisposable
{
    private readonly StorageSettings settings = options.Value;
    private AmazonS3Client? client;
    private readonly object clientLock = new();

    public bool IsConfigured => !string.IsNullOrWhiteSpace(settings.AccountId)
        && !string.IsNullOrWhiteSpace(settings.BucketName)
        && !string.IsNullOrWhiteSpace(settings.AccessKeyId)
        && !string.IsNullOrWhiteSpace(settings.SecretAccessKey);

    private AmazonS3Client Client
    {
        get
        {
            if (!IsConfigured) throw new AppException("La subida de archivos requiere configurar Cloudflare R2. Contacta al administrador.", 503, "storage_not_configured");
            lock (clientLock)
                return client ??= new AmazonS3Client(new BasicAWSCredentials(settings.AccessKeyId, settings.SecretAccessKey),
                    new AmazonS3Config
                    {
                        ServiceURL = $"https://{settings.AccountId}.r2.cloudflarestorage.com",
                        AuthenticationRegion = "auto", ForcePathStyle = true,
                        RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                        ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
                    });
        }
    }

    public Task<string> UploadUrlAsync(string key, string contentType, DateTime expiresAt) =>
        Client.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = settings.BucketName, Key = key, Verb = HttpVerb.PUT,
            ContentType = contentType, Expires = expiresAt,
        });

    public Task<string> ReadUrlAsync(string key, DateTime expiresAt) =>
        Client.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = settings.BucketName, Key = key, Verb = HttpVerb.GET, Expires = expiresAt,
            ResponseHeaderOverrides = new ResponseHeaderOverrides { CacheControl = "private, no-store" },
        });

    public async Task<byte[]> ReadAsync(string key, long expectedSize, CancellationToken ct)
    {
        using var response = await Client.GetObjectAsync(settings.BucketName, key, ct);
        if (response.ContentLength != expectedSize || expectedSize is <= 0 or > 104857600)
            throw new AppException("El tamaño del archivo subido no coincide con el declarado.");
        var bytes = new byte[(int)expectedSize];
        try { await response.ResponseStream.ReadExactlyAsync(bytes, ct); }
        catch (EndOfStreamException) { throw new AppException("La subida está incompleta. Vuelve a intentarlo."); }
        return bytes;
    }

    public async Task WriteAsync(string key, byte[] contents, string contentType, CancellationToken ct)
    {
        using var stream = new MemoryStream(contents, writable: false);
        await Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = settings.BucketName, Key = key, InputStream = stream, ContentType = contentType,
            Headers = { CacheControl = "private, no-store" },
            DisablePayloadSigning = true,
        }, ct);
    }

    public async Task DeleteAsync(string key, CancellationToken ct) =>
        await Client.DeleteObjectAsync(settings.BucketName, key, ct);

    public void Dispose() => client?.Dispose();
}
