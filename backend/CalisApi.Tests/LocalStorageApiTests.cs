using System.Net;
using System.Net.Http.Json;
using CalisApi.Dtos;
using CalisApi.Models;
using CalisApi.Services.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalisApi.Tests;

/// <summary>Fixture con el proveedor Local real (disco temporal) en lugar del almacén en memoria.</summary>
public sealed class LocalStorageFixture : CommunityFixture
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), $"calis-storage-it-{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Storage:Provider", "Local");
        builder.UseSetting("Storage:LocalPath", Root);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IObjectStorage>();
            services.AddSingleton<LocalFileStorage>();
            services.AddSingleton<IObjectStorage>(sp => sp.GetRequiredService<LocalFileStorage>());
        });
    }
}

/// <summary>Flujo HTTP completo del almacenamiento local: PUT firmado → validación → publicación → descarga firmada.</summary>
[TestClass]
public class LocalStorageApiTests
{
    private static LocalStorageFixture fixture = null!;
    private static User admin = null!;
    private static User clover = null!;

    // PNG real de 1x1 píxel.
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aWQAAAABJRU5ErkJggg==");

    [ClassInitialize]
    public static async Task Initialize(TestContext _)
    {
        fixture = new LocalStorageFixture();
        using var scope = fixture.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<CalisApi.Data.CalisDbContext>().Database.MigrateAsync();
        admin = await fixture.AddUserAsync(Roles.Admin);
        clover = await fixture.AddUserAsync(Roles.Clover);
    }

    [ClassCleanup]
    public static async Task Cleanup()
    {
        if (fixture is null) return;
        using (var scope = fixture.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<CalisApi.Data.CalisDbContext>().Database.EnsureDeletedAsync();
        await fixture.DisposeAsync();
        if (Directory.Exists(fixture.Root)) Directory.Delete(fixture.Root, recursive: true);
    }

    [TestMethod]
    public async Task LocalUpload_ThroughHttpEndpoints_PublishesAndServesSignedFile()
    {
        using var staff = fixture.Client(admin);
        using var paid = fixture.Client(clover);
        using var anonymous = fixture.Client();

        // 1. Iniciar subida → URL firmada relativa
        var begin = await staff.PostAsJsonAsync("/api/media", new BeginUploadRequest("foto.png", "image/png", Png.Length));
        var upload = await begin.Content.ReadFromJsonAsync<UploadDto>();
        Assert.IsNotNull(upload);
        StringAssert.StartsWith(upload.UploadUrl, "/api/media/staging/staging/");

        // 2. PUT directo con la URL firmada (sin JWT, como hace el navegador)
        var put = await anonymous.PutAsync(upload.UploadUrl, new ByteArrayContent(Png));
        Assert.AreEqual(HttpStatusCode.NoContent, put.StatusCode, await put.Content.ReadAsStringAsync());

        // 3. Firma alterada o vencida → 403
        var badPut = await anonymous.PutAsync(upload.UploadUrl.Replace("signature=", "signature=aaaa"), new ByteArrayContent(Png));
        Assert.AreEqual(HttpStatusCode.Forbidden, badPut.StatusCode);

        // 4. Completar: valida bytes y publica en la clave inmutable
        var complete = await staff.PostAsync($"/api/media/{upload.Id}/complete", null);
        Assert.IsTrue(complete.IsSuccessStatusCode, await complete.Content.ReadAsStringAsync());

        // 5. Publicar post con el adjunto
        var postResponse = await staff.PostAsJsonAsync("/api/post",
            new WritePostRequest("Post con foto local", "Contenido", PostKinds.Announcement, null, null, [upload.Id]));
        Assert.IsTrue(postResponse.IsSuccessStatusCode, await postResponse.Content.ReadAsStringAsync());

        // 6. Clover pide el enlace firmado y descarga el archivo original (sin JWT en la descarga)
        var link = await (await paid.GetAsync($"/api/media/{upload.Id}/link")).Content.ReadFromJsonAsync<MediaLinkDto>();
        Assert.IsNotNull(link);
        StringAssert.StartsWith(link.Url, "/api/media/files/community/");
        var download = await anonymous.GetAsync(link.Url);
        Assert.AreEqual(HttpStatusCode.OK, download.StatusCode);
        Assert.AreEqual("image/png", download.Content.Headers.ContentType?.MediaType);
        CollectionAssert.AreEqual(Png, await download.Content.ReadAsByteArrayAsync());

        // 7. Firma alterada en descarga → 404
        var badGet = await anonymous.GetAsync(link.Url.Replace("signature=", "signature=aaaa"));
        Assert.AreEqual(HttpStatusCode.NotFound, badGet.StatusCode);
    }
}
