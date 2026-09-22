using CalisApi.Common;
using CalisApi.Services.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalisApi.Tests;

/// <summary>Pruebas del proveedor de disco local sobre una carpeta temporal real.</summary>
[TestClass]
public class LocalFileStorageTests
{
    private string root = null!;
    private LocalFileStorage storage = null!;

    [TestInitialize]
    public void Setup()
    {
        root = Path.Combine(Path.GetTempPath(), $"calis-storage-{Guid.NewGuid():N}");
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-signing-key-for-local-storage-0123456789abcdef",
            })
            .Build();
        storage = new LocalFileStorage(
            Options.Create(new StorageSettings { Provider = "Local", LocalPath = root }),
            new StubHostEnvironment(root),
            config);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }

    [TestMethod]
    public async Task WriteReadDelete_RoundTripsBytes()
    {
        var contents = new byte[] { 1, 2, 3, 250, 251 };
        await storage.WriteAsync("community/test", contents, "image/png", CancellationToken.None);
        CollectionAssert.AreEqual(contents, await storage.ReadAsync("community/test", contents.Length, CancellationToken.None));
        await storage.DeleteAsync("community/test", CancellationToken.None);
        await Assert.ThrowsExactlyAsync<AppException>(() =>
            storage.ReadAsync("community/test", contents.Length, CancellationToken.None));
        // Borrado idempotente: no falla si ya no existe
        await storage.DeleteAsync("community/test", CancellationToken.None);
    }

    [TestMethod]
    public async Task WriteStream_IsAtomic_AndDoesNotLeaveTempFiles()
    {
        var contents = Enumerable.Range(0, 10000).Select(i => (byte)(i % 251)).ToArray();
        await using (var stream = new MemoryStream(contents))
            await storage.WriteStreamAsync("staging/abc", stream, CancellationToken.None);
        CollectionAssert.AreEqual(contents, await File.ReadAllBytesAsync(storage.PathFor("staging/abc")));
        Assert.AreEqual(1, Directory.GetFiles(root, "*", SearchOption.AllDirectories).Length);
    }

    [TestMethod]
    public async Task SizeMismatch_RejectsRead()
    {
        await storage.WriteAsync("community/x", new byte[] { 9, 9 }, "image/png", CancellationToken.None);
        await Assert.ThrowsExactlyAsync<AppException>(() =>
            storage.ReadAsync("community/x", 3, CancellationToken.None));
    }

    [TestMethod]
    public void PathTraversal_IsRejected()
    {
        Assert.ThrowsExactly<AppException>(() => storage.PathFor("../outside"));
        Assert.ThrowsExactly<AppException>(() => storage.PathFor("..\\outside"));
        Assert.ThrowsExactly<AppException>(() => storage.PathFor("a/../../outside"));
    }

    [TestMethod]
    public async Task SignedUrls_AreRelative_AndExpire()
    {
        var upload = await storage.UploadUrlAsync("staging/abc", "image/png", DateTime.UtcNow.AddMinutes(15));
        StringAssert.StartsWith(upload, "/api/media/staging/staging/abc?expires=");
        StringAssert.Contains(upload, "&signature=");
        var read = await storage.ReadUrlAsync("community/abc", DateTime.UtcNow.AddMinutes(5));
        StringAssert.StartsWith(read, "/api/media/files/community/abc?expires=");
    }

    [TestMethod]
    public void Signature_Validates_AndRejectsTampering()
    {
        var expires = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds();
        var signature = storage.Sign("staging/abc", expires);
        Assert.IsTrue(storage.Validate("staging/abc", expires, signature));
        Assert.IsFalse(storage.Validate("staging/other", expires, signature));   // clave alterada
        Assert.IsFalse(storage.Validate("staging/abc", expires + 1, signature)); // expiración alterada
        Assert.IsFalse(storage.Validate("staging/abc", expires, signature + "x")); // firma alterada
        Assert.IsFalse(storage.Validate("staging/abc", DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds(), signature)); // vencida
        Assert.IsFalse(storage.Validate("staging/abc", expires, null));
    }

    [TestMethod]
    public void DifferentProvider_IsNotConfigured()
    {
        var other = new LocalFileStorage(
            Options.Create(new StorageSettings { Provider = "R2", LocalPath = root }),
            new StubHostEnvironment(root),
            new ConfigurationBuilder().Build());
        Assert.IsFalse(other.IsConfigured);
    }

    private sealed class StubHostEnvironment(string contentRoot) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = contentRoot;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
