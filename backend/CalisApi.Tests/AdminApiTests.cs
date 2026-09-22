using System.Net;
using System.Net.Http.Json;
using CalisApi.Data;
using CalisApi.Dtos;
using CalisApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalisApi.Tests;

/// <summary>Pruebas del panel Admin (Fase 5): clases, videos, rutinas, categorías, logros y usuarios.</summary>
[TestClass]
public class AdminApiTests
{
    private static CommunityFixture fixture = null!;
    private static User admin = null!;
    private static User clover = null!;

    [ClassInitialize]
    public static async Task Initialize(TestContext _)
    {
        fixture = new CommunityFixture();
        using var scope = fixture.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<CalisDbContext>().Database.MigrateAsync();
        admin = await fixture.AddUserAsync(Roles.Admin);
        clover = await fixture.AddUserAsync(Roles.Clover);
    }

    [ClassCleanup]
    public static async Task Cleanup()
    {
        if (fixture is null) return;
        using (var scope = fixture.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<CalisDbContext>().Database.EnsureDeletedAsync();
        await fixture.DisposeAsync();
    }

    private static async Task<T> Body<T>(HttpResponseMessage response)
    {
        Assert.IsTrue(response.IsSuccessStatusCode, $"{response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private static CreateSessionRequest SessionRequest(DateTime date, int spots = 10) =>
        new("Clase admin", "Descripción", date, spots, "basica", "Coach", 60);

    // ---------- Clases ----------

    [TestMethod]
    public async Task Sessions_Create_WithAchievements_SavesAssociations()
    {
        using var staff = fixture.Client(admin);
        var achievement = await Body<AchievementDto>(await staff.PostAsJsonAsync("/api/achievement",
            new CreateAchievementRequest("Asociable", "Para asociar a clase", "🏆")));

        var future = DateTime.UtcNow.AddDays(2);
        var session = await Body<SessionDto>(await staff.PostAsJsonAsync("/api/session",
            SessionRequest(future) with { AchievementIds = [achievement.Id] }));

        var associated = await Body<AchievementDto[]>(await staff.GetAsync($"/api/session/{session.Id}/achievements"));
        Assert.AreEqual(1, associated.Length);
        Assert.AreEqual(achievement.Id, associated[0].Id);

        // Al editar se mantienen (sin AchievementIds explícito se borran)
        var updated = await Body<SessionDto>(await staff.PutAsJsonAsync($"/api/session/{session.Id}",
            SessionRequest(future.AddHours(1)) with { AchievementIds = [achievement.Id] }));
        var associatedAfterUpdate = await Body<AchievementDto[]>(await staff.GetAsync($"/api/session/{updated.Id}/achievements"));
        Assert.AreEqual(1, associatedAfterUpdate.Length);
    }

    [TestMethod]
    public async Task Sessions_EditRules_CapacityFloor_AndBlockedAfterStart()
    {
        using var staff = fixture.Client(admin);
        var future = await fixture.AddSessionAsync(DateTime.UtcNow.AddDays(2), clover);
        var updated = await Body<SessionDto>(await staff.PutAsJsonAsync($"/api/session/{future.Id}", SessionRequest(future.Date.AddHours(3), 1)));
        Assert.AreEqual(1, updated.LimitedSpots);
        Assert.AreEqual(1, updated.Enrolled);

        // Cupo menor a inscritos → 400
        Assert.AreEqual(HttpStatusCode.BadRequest,
            (await staff.PutAsJsonAsync($"/api/session/{future.Id}", SessionRequest(future.Date, 0))).StatusCode);

        // Clase ya comenzada → 400
        var started = await fixture.AddSessionAsync(DateTime.UtcNow.AddMinutes(-10), clover);
        Assert.AreEqual(HttpStatusCode.BadRequest,
            (await staff.PutAsJsonAsync($"/api/session/{started.Id}", SessionRequest(DateTime.UtcNow.AddDays(1)))).StatusCode);

        // Duración inválida → 400
        Assert.AreEqual(HttpStatusCode.BadRequest,
            (await staff.PutAsJsonAsync($"/api/session/{future.Id}", SessionRequest(DateTime.UtcNow.AddDays(2)) with { DurationMinutes = 5 })).StatusCode);
    }

    [TestMethod]
    public async Task Sessions_MonthlyPurge_OnlyPastMonths_WithPreview_AndPreservesReviews()
    {
        using var staff = fixture.Client(admin);
        var now = DateTime.UtcNow;
        var pastMonth = now.AddMonths(-2);
        var session = await fixture.AddSessionAsync(new DateTime(pastMonth.Year, pastMonth.Month, 15, 10, 0, 0, DateTimeKind.Utc), clover);
        using (var paid = fixture.Client(clover))
            await Body<ReviewDto>(await paid.PutAsJsonAsync($"/api/review/session/{session.Id}", new WriteReviewRequest(5, "Histórica")));

        // Preview del mes pasado
        var preview = await Body<System.Text.Json.JsonElement>(
            await staff.GetAsync($"/api/session/purge-preview?year={pastMonth.Year}&month={pastMonth.Month}"));
        Assert.IsTrue(preview.GetProperty("count").GetInt32() >= 1);

        // Mes actual y futuro → 400 en preview y borrado
        Assert.AreEqual(HttpStatusCode.BadRequest,
            (await staff.GetAsync($"/api/session/purge-preview?year={now.Year}&month={now.Month}")).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest,
            (await staff.DeleteAsync($"/api/session?year={now.Year}&month={now.Month}")).StatusCode);
        var futureMonth = now.AddMonths(1);
        Assert.AreEqual(HttpStatusCode.BadRequest,
            (await staff.DeleteAsync($"/api/session?year={futureMonth.Year}&month={futureMonth.Month}")).StatusCode);

        var deleted = await Body<System.Text.Json.JsonElement>(
            await staff.DeleteAsync($"/api/session?year={pastMonth.Year}&month={pastMonth.Month}"));
        Assert.IsTrue(deleted.GetProperty("deleted").GetInt32() >= 1);

        // La reseña sobrevive con su snapshot (SessionId en null)
        using var scope = fixture.Services.CreateScope();
        var review = await scope.ServiceProvider.GetRequiredService<CalisDbContext>()
            .SessionReviews.FirstAsync(r => r.SessionTitle == session.Title);
        Assert.IsNull(review.SessionId);
        Assert.AreEqual(session.Title, review.SessionTitle);
    }

    // ---------- Videos ----------

    private async Task<Guid> UploadExerciseVideoAsync(HttpClient staff, uint seconds)
    {
        var mp4 = TestMp4.Movie(seconds);
        var upload = await Body<UploadDto>(await staff.PostAsJsonAsync("/api/media",
            new BeginUploadRequest("ejercicio.mp4", "video/mp4", mp4.Length, "exercise")));
        fixture.Storage.Objects[$"staging/{upload.Id:N}"] = mp4;
        await Body<MediaDto>(await staff.PostAsync($"/api/media/{upload.Id}/complete", null));
        return upload.Id;
    }

    [TestMethod]
    public async Task ExerciseUpload_PurposeRules_AreEnforced()
    {
        using var staff = fixture.Client(admin);
        // Imagen como ejercicio → 400
        Assert.AreEqual(HttpStatusCode.BadRequest, (await staff.PostAsJsonAsync("/api/media",
            new BeginUploadRequest("foto.png", "image/png", 1000, "exercise"))).StatusCode);
        // Más de 50 MB → 400
        Assert.AreEqual(HttpStatusCode.BadRequest, (await staff.PostAsJsonAsync("/api/media",
            new BeginUploadRequest("largo.mp4", "video/mp4", 51L * 1024 * 1024, "exercise"))).StatusCode);
        // Más de 30 segundos → 400 al completar
        var tooLong = await Body<UploadDto>(await staff.PostAsJsonAsync("/api/media",
            new BeginUploadRequest("largo.mp4", "video/mp4", 100, "exercise")));
        fixture.Storage.Objects[$"staging/{tooLong.Id:N}"] = new byte[100]; // no es MP4 válido
        Assert.AreEqual(HttpStatusCode.BadRequest, (await staff.PostAsync($"/api/media/{tooLong.Id}/complete", null)).StatusCode);
        // Video de ejercicio no se puede adjuntar a un post de comunidad
        var assetId = await UploadExerciseVideoAsync(staff, 10);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await staff.PostAsJsonAsync("/api/post",
            new WritePostRequest("Post", "Contenido", PostKinds.Announcement, null, null, [assetId]))).StatusCode);
    }

    [TestMethod]
    public async Task Videos_CreateWithPlaybackUrl_ReplaceAndDeleteRules()
    {
        using var staff = fixture.Client(admin);
        var category = await Body<CategoryDto>(await staff.PostAsJsonAsync("/api/category",
            new WriteCategoryRequest("Test Videos", "Categoría de prueba")));

        var assetId = await UploadExerciseVideoAsync(staff, 15);
        var request = new WriteVideoRequest("Dominada estricta", "Técnica completa", "intermedia", "Colgarse 30s", category.Id, assetId);
        var video = await Body<VideoDto>(await staff.PostAsJsonAsync("/api/video", request));
        // La URL de reproducción es firmada por el proveedor activo (Local o R2).
        StringAssert.Contains(video.Url, "exercises/");
        StringAssert.Contains(video.Url, "expires=");

        // El mismo asset no se puede reusar
        Assert.AreEqual(HttpStatusCode.BadRequest,
            (await staff.PostAsJsonAsync("/api/video", request)).StatusCode);

        // Crear sin archivo → 400
        Assert.AreEqual(HttpStatusCode.BadRequest,
            (await staff.PostAsJsonAsync("/api/video", request with { MediaAssetId = null })).StatusCode);

        // Edición de metadatos sin reemplazo
        var edited = await Body<VideoDto>(await staff.PutAsJsonAsync($"/api/video/{video.Id}",
            request with { Title = "Dominada estricta V2", MediaAssetId = null }));
        Assert.AreEqual("Dominada estricta V2", edited.Title);

        // Rutina que referencia el video bloquea el borrado
        var routine = await Body<RutineDetailDto>(await staff.PostAsJsonAsync("/api/rutine", new WriteRutineRequest(
            "Rutina ref", "Descripción", "20 min", "basica", category.Id,
            [new WriteRutineExercise("Dominada", "Principal", 5, 3, "90s", "Controlado", video.Id)])));
        var deleteBlocked = await staff.DeleteAsync($"/api/video/{video.Id}");
        Assert.AreEqual(HttpStatusCode.BadRequest, deleteBlocked.StatusCode);
        StringAssert.Contains(await deleteBlocked.Content.ReadAsStringAsync(), "rutinas");

        // Se quita la referencia y ya se puede borrar
        await Body<RutineDetailDto>(await staff.PutAsJsonAsync($"/api/rutine/{routine.Id}", new WriteRutineRequest(
            "Rutina ref", "Descripción", "20 min", "basica", category.Id,
            [new WriteRutineExercise("Dominada", "Principal", 5, 3, "90s", "Controlado", null)])));
        Assert.AreEqual(HttpStatusCode.NoContent, (await staff.DeleteAsync($"/api/video/{video.Id}")).StatusCode);
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CalisDbContext>();
        Assert.IsFalse(await db.Videos.AnyAsync(v => v.Id == video.Id));
        var asset = await db.MediaAssets.FirstAsync(m => m.Id == assetId);
        Assert.IsTrue(asset.IsDeleted);
        Assert.IsNull(asset.VideoId);
    }

    // ---------- Rutinas ----------

    [TestMethod]
    public async Task Rutines_Crud_NormalizesWarmupFirst_AndValidatesReferences()
    {
        using var staff = fixture.Client(admin);
        var category = await Body<CategoryDto>(await staff.PostAsJsonAsync("/api/category",
            new WriteCategoryRequest("Test Rutinas", "Categoría de prueba")));

        // Principal enviado primero: el servidor reordena con calentamiento primero
        var created = await Body<RutineDetailDto>(await staff.PostAsJsonAsync("/api/rutine", new WriteRutineRequest(
            "Full Body", "Descripción", "40 min", "intermedia", category.Id,
            [
                new WriteRutineExercise("Flexiones", "Principal", 12, 4, "60s", "Cuerpo recto", null),
                new WriteRutineExercise("Movilidad de hombro", "Calentamiento", 10, 2, "30s", "Sin dolor", null),
            ])));
        Assert.AreEqual("Calentamiento", created.Exercises[0].Tipo);
        Assert.AreEqual("Principal", created.Exercises[1].Tipo);

        // Validaciones: sin ejercicios, video inexistente, categoría inexistente
        Assert.AreEqual(HttpStatusCode.BadRequest, (await staff.PostAsJsonAsync("/api/rutine",
            new WriteRutineRequest("X", "Y", "10 min", "basica", category.Id, []))).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await staff.PostAsJsonAsync("/api/rutine",
            new WriteRutineRequest("X", "Y", "10 min", "basica", category.Id,
                [new WriteRutineExercise("E", "Principal", 1, 1, "30s", "obs", 999999)]))).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await staff.PostAsJsonAsync("/api/rutine",
            new WriteRutineRequest("X", "Y", "10 min", "basica", 999999,
                [new WriteRutineExercise("E", "Principal", 1, 1, "30s", "obs", null)]))).StatusCode);

        // Update reemplaza ejercicios
        var updated = await Body<RutineDetailDto>(await staff.PutAsJsonAsync($"/api/rutine/{created.Id}",
            new WriteRutineRequest("Full Body V2", "Descripción", "45 min", "avanzada", category.Id,
                [new WriteRutineExercise("Muscle up", "Principal", 5, 5, "120s", "Explosivo", null)])));
        Assert.AreEqual(1, updated.Exercises.Count);
        Assert.AreEqual("Muscle up", updated.Exercises[0].Exercise);

        // El detalle público refleja los cambios
        using var paid = fixture.Client(clover);
        var publicDetail = await Body<RutineDetailDto>(await paid.GetAsync($"/api/rutine/{created.Id}"));
        Assert.AreEqual("Full Body V2", publicDetail.Title);

        Assert.AreEqual(HttpStatusCode.NoContent, (await staff.DeleteAsync($"/api/rutine/{created.Id}")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await paid.GetAsync($"/api/rutine/{created.Id}")).StatusCode);
    }

    // ---------- Categorías ----------

    [TestMethod]
    public async Task Categories_Crud_UniqueName_AndBlockedDelete()
    {
        using var staff = fixture.Client(admin);
        var category = await Body<CategoryDto>(await staff.PostAsJsonAsync("/api/category",
            new WriteCategoryRequest("Skills", "Habilidades estáticas")));

        // Nombre duplicado (case-insensitive) → 409
        Assert.AreEqual(HttpStatusCode.Conflict, (await staff.PostAsJsonAsync("/api/category",
            new WriteCategoryRequest("skills", "Otra"))).StatusCode);

        var renamed = await Body<CategoryDto>(await staff.PutAsJsonAsync($"/api/category/{category.Id}",
            new WriteCategoryRequest("Skills Pro", "Habilidades estáticas avanzadas")));
        Assert.AreEqual("Skills Pro", renamed.Name);

        // Con contenido → borrado bloqueado con aviso
        var assetId = await UploadExerciseVideoAsync(staff, 5);
        var video = await Body<VideoDto>(await staff.PostAsJsonAsync("/api/video",
            new WriteVideoRequest("Video bloqueo", "Desc", "basica", "Ninguno", category.Id, assetId)));
        var blocked = await staff.DeleteAsync($"/api/category/{category.Id}");
        Assert.AreEqual(HttpStatusCode.BadRequest, blocked.StatusCode);
        StringAssert.Contains(await blocked.Content.ReadAsStringAsync(), "video");

        // Sin contenido → se borra
        Assert.AreEqual(HttpStatusCode.NoContent, (await staff.DeleteAsync($"/api/video/{video.Id}")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NoContent, (await staff.DeleteAsync($"/api/category/{category.Id}")).StatusCode);
    }

    // ---------- Logros ----------

    [TestMethod]
    public async Task Achievements_Edit_HardDeleteWithoutOwners_HideWithOwners()
    {
        using var staff = fixture.Client(admin);
        var achievement = await Body<AchievementDto>(await staff.PostAsJsonAsync("/api/achievement",
            new CreateAchievementRequest("Logro admin", "Descripción", "star")));

        var edited = await Body<AchievementDto>(await staff.PutAsJsonAsync($"/api/achievement/{achievement.Id}",
            new CreateAchievementRequest("Logro admin V2", "Otra", "trophy")));
        Assert.AreEqual("Logro admin V2", edited.Name);

        // Otorgar el logro (clase finalizada + asociación + inscrito + asistencia)
        var session = await fixture.AddSessionAsync(DateTime.UtcNow.AddDays(-1), clover);
        await staff.PostAsync($"/api/session/{session.Id}/achievements/{achievement.Id}", null);
        await staff.PutAsJsonAsync($"/api/session/{session.Id}/attendance", new RecordAttendanceRequest([clover.Id]));
        await staff.PostAsJsonAsync("/api/achievement/grant",
            new { achievementId = achievement.Id, sessionId = session.Id, userIds = new[] { clover.Id } });

        // Borrar con dueños → queda oculto, no se pierde el historial ni se puede otorgar de nuevo
        Assert.AreEqual(HttpStatusCode.NoContent, (await staff.DeleteAsync($"/api/achievement/{achievement.Id}")).StatusCode);
        var adminList = await Body<AchievementAdminDto[]>(await staff.GetAsync("/api/achievement/admin"));
        var hidden = adminList.Single(a => a.Id == achievement.Id);
        Assert.IsTrue(hidden.IsHidden);
        Assert.AreEqual(1, hidden.GrantedCount);
        var publicCatalog = await Body<AchievementDto[]>(await fixture.Client(clover).GetAsync("/api/achievement"));
        Assert.IsFalse(publicCatalog.Any(a => a.Id == achievement.Id));
        var anotherSession = await fixture.AddSessionAsync(DateTime.UtcNow.AddDays(-1), clover);
        await staff.PostAsync($"/api/session/{anotherSession.Id}/achievements/{achievement.Id}", null);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await staff.PostAsJsonAsync("/api/achievement/grant",
            new { achievementId = achievement.Id, sessionId = anotherSession.Id, userIds = new[] { clover.Id } })).StatusCode);

        // Sin dueños → borrado físico
        var free = await Body<AchievementDto>(await staff.PostAsJsonAsync("/api/achievement",
            new CreateAchievementRequest("Logro libre", "Desc", "medal")));
        Assert.AreEqual(HttpStatusCode.NoContent, (await staff.DeleteAsync($"/api/achievement/{free.Id}")).StatusCode);
        Assert.IsFalse((await Body<AchievementAdminDto[]>(await staff.GetAsync("/api/achievement/admin"))).Any(a => a.Id == free.Id));
    }

    // ---------- Usuarios ----------

    [TestMethod]
    public async Task Users_SearchAndRoleFilter()
    {
        using var staff = fixture.Client(admin);
        var needle = await fixture.AddUserAsync(Roles.Guerrero);

        var byEmail = await Body<AdminUserDto[]>(await staff.GetAsync($"/api/user?search={needle.Email[..12]}"));
        Assert.IsTrue(byEmail.Any(u => u.Id == needle.Id));

        var byName = await Body<AdminUserDto[]>(await staff.GetAsync($"/api/user?search={needle.FullName.Replace(" ", "%20")}"));
        Assert.IsTrue(byName.Any(u => u.Id == needle.Id));

        var onlyClovers = await Body<AdminUserDto[]>(await staff.GetAsync("/api/user?role=Clover"));
        Assert.IsTrue(onlyClovers.All(u => u.Role == "Clover"));
        Assert.IsTrue(onlyClovers.Any(u => u.Id == clover.Id));

        Assert.AreEqual(HttpStatusCode.BadRequest, (await staff.GetAsync("/api/user?role=Admin")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, (await fixture.Client(clover).GetAsync("/api/user")).StatusCode);
    }
}
