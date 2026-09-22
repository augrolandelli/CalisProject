using System.Net;
using System.Net.Http.Json;
using CalisApi.Data;
using CalisApi.Dtos;
using CalisApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalisApi.Tests;

[TestClass]
public class CommunityApiTests
{
    private static CommunityFixture fixture = null!;
    private static User admin = null!;
    private static User clover = null!;
    private static User warrior = null!;

    [ClassInitialize]
    public static async Task Initialize(TestContext _)
    {
        fixture = new CommunityFixture();
        using var scope = fixture.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<CalisDbContext>().Database.MigrateAsync();
        admin = await fixture.AddUserAsync(Roles.Admin);
        clover = await fixture.AddUserAsync(Roles.Clover);
        warrior = await fixture.AddUserAsync(Roles.Guerrero);
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

    private static WritePostRequest PostRequest(Guid[]? media = null) => new("Novedad de prueba", "Contenido exclusivo", PostKinds.Announcement, null, null, media ?? []);
    private static WriteEventRequest EventRequest(int? capacity = 2) => new("Evento de prueba", "Entrenamos juntos", "Gimnasio", DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(2).AddHours(2), capacity);

    [TestMethod]
    public async Task Roles_ProtectPrivateContent_AndExposeReviewsToWarriors()
    {
        using var anonymous = fixture.Client();
        using var free = fixture.Client(warrior);
        using var paid = fixture.Client(clover);
        foreach (var path in new[] { "/api/post", "/api/event", $"/api/media/{Guid.NewGuid()}/link" })
        {
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(path)).StatusCode);
            Assert.AreEqual(HttpStatusCode.Forbidden, (await free.GetAsync(path)).StatusCode);
        }
        Assert.AreEqual(HttpStatusCode.OK, (await free.GetAsync("/api/review")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, (await paid.PostAsJsonAsync("/api/post", PostRequest())).StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, (await free.PutAsJsonAsync("/api/review/session/1", new WriteReviewRequest(5, "Texto"))).StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, (await paid.GetAsync("/api/review/moderation")).StatusCode);
    }

    [TestMethod]
    public async Task Posts_Paginate_Edit_AndSerializeLikesIdempotently()
    {
        using var staff = fixture.Client(admin);
        using var paid = fixture.Client(clover);
        var post = await Body<PostDto>(await staff.PostAsJsonAsync("/api/post", PostRequest()));
        var likes = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => paid.PutAsync($"/api/post/{post.Id}/like", null)));
        Assert.IsTrue(likes.All(r => r.IsSuccessStatusCode));
        var liked = await Body<PostDto>(await paid.GetAsync($"/api/post/{post.Id}"));
        Assert.AreEqual(1, liked.LikeCount);
        Assert.IsTrue(liked.IsLiked);
        var edited = await Body<PostDto>(await staff.PutAsJsonAsync($"/api/post/{post.Id}", PostRequest() with { Title = "Editada" }));
        Assert.AreEqual("Editada", edited.Title);
        Assert.IsNotNull(edited.UpdatedAt);
        var page = await Body<PageDto<PostDto>>(await paid.GetAsync("/api/post?pageSize=1"));
        Assert.AreEqual(1, page.Items.Count);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await paid.GetAsync("/api/post?pageSize=999")).StatusCode);
        await paid.DeleteAsync($"/api/post/{post.Id}/like");
        Assert.AreEqual(0, (await Body<PostDto>(await paid.GetAsync($"/api/post/{post.Id}"))).LikeCount);
        Assert.AreEqual(HttpStatusCode.Forbidden, (await paid.DeleteAsync($"/api/post/{post.Id}")).StatusCode);
        await staff.DeleteAsync($"/api/post/{post.Id}");
        Assert.AreEqual(HttpStatusCode.NotFound, (await paid.GetAsync($"/api/post/{post.Id}")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await paid.PutAsync($"/api/post/{post.Id}/like", null)).StatusCode);
    }

    [TestMethod]
    public async Task InvalidPosts_CannotForgeAchievements_AndReturnValidationErrors()
    {
        using var staff = fixture.Client(admin);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await staff.PostAsJsonAsync("/api/post", PostRequest() with { Kind = PostKinds.Achievement })).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await staff.PostAsJsonAsync("/api/post", PostRequest() with { Kind = PostKinds.Competition })).StatusCode);
        var response = await staff.PostAsJsonAsync("/api/post", PostRequest() with { Title = " " });
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "errors");
    }

    [TestMethod]
    public async Task Reviews_RequirePriorReservation_AndActualEndTime()
    {
        using var paid = fixture.Client(clover);
        var pastWithoutReservation = await fixture.AddSessionAsync(DateTime.UtcNow.AddDays(-1));
        var future = await fixture.AddSessionAsync(DateTime.UtcNow.AddHours(2), clover);
        var inProgress = await fixture.AddSessionAsync(DateTime.UtcNow.AddMinutes(-10), clover);
        var lateReservation = await fixture.AddSessionAsync(DateTime.UtcNow.AddHours(-3), clover, DateTime.UtcNow);
        foreach (var session in new[] { pastWithoutReservation, future, inProgress, lateReservation })
        {
            Assert.IsFalse((await Body<ReviewEligibilityDto>(await paid.GetAsync($"/api/review/session/{session.Id}/mine"))).CanReview);
            Assert.AreEqual(HttpStatusCode.BadRequest, (await paid.PutAsJsonAsync($"/api/review/session/{session.Id}", new WriteReviewRequest(5, "Bien"))).StatusCode);
        }
        Assert.AreEqual(HttpStatusCode.BadRequest, (await paid.PostAsync($"/api/usersession/{pastWithoutReservation.Id}", null)).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await paid.DeleteAsync($"/api/usersession/{inProgress.Id}")).StatusCode);
    }

    [TestMethod]
    public async Task Reviews_UpsertOnce_ModerateAndPreserveWhenClassDeleted()
    {
        using var paid = fixture.Client(clover);
        using var staff = fixture.Client(admin);
        using var free = fixture.Client(warrior);
        var session = await fixture.AddSessionAsync(DateTime.UtcNow.AddDays(-1), clover);
        var reviews = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => paid.PutAsJsonAsync($"/api/review/session/{session.Id}", new WriteReviewRequest(5, "Buena técnica"))));
        Assert.IsTrue(reviews.All(r => r.IsSuccessStatusCode));
        var data = await Body<ReviewPageDto>(await free.GetAsync($"/api/review?sessionId={session.Id}"));
        Assert.AreEqual(1, data.RatingCount);
        Assert.AreEqual(5d, data.AverageRating);
        var reviewId = data.Reviews.Items.Single().Id;
        var edited = await Body<ReviewDto>(await paid.PutAsJsonAsync($"/api/review/session/{session.Id}", new WriteReviewRequest(4, "Editada")));
        Assert.AreEqual(reviewId, edited.Id);
        Assert.AreEqual(HttpStatusCode.Forbidden, (await free.DeleteAsync($"/api/review/{reviewId}")).StatusCode);
        await staff.PutAsJsonAsync($"/api/review/{reviewId}/visibility", new ReviewVisibilityRequest(true));
        Assert.AreEqual(0, (await Body<ReviewPageDto>(await free.GetAsync($"/api/review?sessionId={session.Id}"))).RatingCount);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await paid.PutAsJsonAsync($"/api/review/session/{session.Id}", new WriteReviewRequest(5, "Intento de recrear"))).StatusCode);
        await paid.DeleteAsync($"/api/review/{reviewId}");
        Assert.AreEqual(HttpStatusCode.BadRequest, (await paid.PutAsJsonAsync($"/api/review/session/{session.Id}", new WriteReviewRequest(5, "Recreación"))).StatusCode);
        await staff.PutAsJsonAsync($"/api/review/{reviewId}/visibility", new ReviewVisibilityRequest(false));
        Assert.AreEqual(HttpStatusCode.NoContent, (await staff.DeleteAsync($"/api/session/{session.Id}")).StatusCode);
        using var scope = fixture.Services.CreateScope();
        var stored = await scope.ServiceProvider.GetRequiredService<CalisDbContext>().SessionReviews.FindAsync(reviewId);
        Assert.IsNotNull(stored);
        Assert.IsNull(stored.SessionId);
        Assert.AreEqual(session.Title, stored.SessionTitle);
    }

    [TestMethod]
    public async Task Events_ConcurrentRegistrations_NeverExceedCapacity_AndCancellationKeepsHistory()
    {
        using var staff = fixture.Client(admin);
        var item = await Body<EventDto>(await staff.PostAsJsonAsync("/api/event", EventRequest(2)));
        var users = new List<User>();
        for (var i = 0; i < 8; i++) users.Add(await fixture.AddUserAsync(Roles.Clover));
        var clients = users.Select(fixture.Client).ToArray();
        try
        {
            var responses = await Task.WhenAll(clients.Select(c => c.PutAsync($"/api/event/{item.Id}/registration", null)));
            Assert.AreEqual(2, responses.Count(r => r.IsSuccessStatusCode));
            Assert.AreEqual(6, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
            Assert.AreEqual(2, (await Body<EventDto>(await staff.GetAsync($"/api/event/{item.Id}"))).RegisteredCount);
            Assert.AreEqual(HttpStatusCode.BadRequest, (await staff.PutAsJsonAsync($"/api/event/{item.Id}", EventRequest(1))).StatusCode);
            var winner = clients[Array.FindIndex(responses, r => r.IsSuccessStatusCode)];
            Assert.IsTrue((await winner.PutAsync($"/api/event/{item.Id}/registration", null)).IsSuccessStatusCode);
            Assert.IsTrue((await winner.DeleteAsync($"/api/event/{item.Id}/registration")).IsSuccessStatusCode);
            Assert.AreEqual(1, (await Body<EventDto>(await staff.GetAsync($"/api/event/{item.Id}"))).RegisteredCount);
            Assert.AreEqual(HttpStatusCode.Forbidden, (await winner.GetAsync($"/api/event/{item.Id}/participants")).StatusCode);
            Assert.AreEqual(1, (await Body<PageDto<EventParticipantDto>>(await staff.GetAsync($"/api/event/{item.Id}/participants"))).Total);
            Assert.AreEqual(HttpStatusCode.NoContent, (await staff.PostAsync($"/api/event/{item.Id}/cancel", null)).StatusCode);
            Assert.AreEqual(HttpStatusCode.BadRequest, (await winner.PutAsync($"/api/event/{item.Id}/registration", null)).StatusCode);
            var history = await Body<PageDto<EventDto>>(await staff.GetAsync("/api/event?history=true"));
            Assert.IsTrue(history.Items.Any(e => e.Id == item.Id && e.IsCancelled && e.RegisteredCount == 1));
        }
        finally { foreach (var client in clients) client.Dispose(); }
    }

    [TestMethod]
    public async Task Events_UnlimitedAndStartedRules_AreEnforced()
    {
        using var staff = fixture.Client(admin);
        using var paid = fixture.Client(clover);
        var item = await Body<EventDto>(await staff.PostAsJsonAsync("/api/event", EventRequest(null)));
        Assert.IsTrue((await paid.PutAsync($"/api/event/{item.Id}/registration", null)).IsSuccessStatusCode);
        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CalisDbContext>();
            await db.CommunityEvents.Where(e => e.Id == item.Id).ExecuteUpdateAsync(s => s.SetProperty(e => e.StartsAt, DateTime.UtcNow.AddMinutes(-10)));
        }
        Assert.AreEqual(HttpStatusCode.BadRequest, (await paid.DeleteAsync($"/api/event/{item.Id}/registration")).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await staff.PutAsJsonAsync($"/api/event/{item.Id}", EventRequest())).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await staff.PostAsJsonAsync("/api/event", EventRequest() with { EndsAt = DateTime.UtcNow })).StatusCode);
    }

    [TestMethod]
    public async Task AchievementGrant_CreatesExactlyOnePost_AndRemovalDoesNotRevokeAward()
    {
        using var staff = fixture.Client(admin);
        using var paid = fixture.Client(clover);
        var session = await fixture.AddSessionAsync(DateTime.UtcNow.AddDays(-1), clover);
        int achievementId;
        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CalisDbContext>();
            var achievement = new Achievement { Name = "Tuck test", Description = "Logro de prueba", Icon = "star" };
            db.Achievements.Add(achievement);
            db.SessionAchievements.Add(new SessionAchievement { SessionId = session.Id, Achievement = achievement });
            await db.SaveChangesAsync();
            achievementId = achievement.Id;
        }
        await staff.PutAsJsonAsync($"/api/session/{session.Id}/attendance", new RecordAttendanceRequest([clover.Id]));
        var request = new { achievementId, sessionId = session.Id, userIds = new[] { clover.Id } };
        var grants = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => staff.PostAsJsonAsync("/api/achievement/grant", request)));
        foreach (var response in grants) Assert.IsTrue(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        using var checkScope = fixture.Services.CreateScope();
        var checkDb = checkScope.ServiceProvider.GetRequiredService<CalisDbContext>();
        var award = await checkDb.UserAchievements.SingleAsync(a => a.SessionId == session.Id);
        var post = await checkDb.Posts.SingleAsync(p => p.UserAchievementId == award.Id);
        Assert.AreEqual(PostKinds.Achievement, post.Kind);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await staff.PutAsJsonAsync($"/api/post/{post.Id}", PostRequest())).StatusCode);
        await staff.DeleteAsync($"/api/post/{post.Id}");
        await staff.PostAsJsonAsync("/api/achievement/grant", request);
        Assert.AreEqual(1, await checkDb.Posts.CountAsync(p => p.UserAchievementId == award.Id));
        Assert.IsTrue(await checkDb.UserAchievements.AnyAsync(a => a.Id == award.Id));
        Assert.AreEqual(HttpStatusCode.NotFound, (await paid.GetAsync($"/api/post/{post.Id}")).StatusCode);
    }

    [TestMethod]
    public async Task Files_AreValidatedBeforePublishing_PrivateAndImmutableAfterCompletion()
    {
        using var staff = fixture.Client(admin);
        using var paid = fixture.Client(clover);
        using var free = fixture.Client(warrior);
        // PNG real de 1x1 píxel.
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aWQAAAABJRU5ErkJggg==");
        Assert.AreEqual(HttpStatusCode.Forbidden, (await paid.PostAsJsonAsync("/api/media", new BeginUploadRequest("a.png", "image/png", png.Length))).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await staff.PostAsJsonAsync("/api/media", new BeginUploadRequest("a.svg", "image/svg+xml", 100))).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await staff.PostAsJsonAsync("/api/media", new BeginUploadRequest("a.png", "image/png", 20_000_000))).StatusCode);
        var upload = await Body<UploadDto>(await staff.PostAsJsonAsync("/api/media", new BeginUploadRequest("a.png", "image/png", png.Length)));
        Assert.AreEqual(HttpStatusCode.BadRequest, (await staff.PostAsJsonAsync("/api/post", PostRequest([upload.Id]))).StatusCode);
        fixture.Storage.Objects[$"staging/{upload.Id:N}"] = new byte[png.Length];
        Assert.AreEqual(HttpStatusCode.BadRequest, (await staff.PostAsync($"/api/media/{upload.Id}/complete", null)).StatusCode);
        fixture.Storage.Objects[$"staging/{upload.Id:N}"] = png;
        await Body<MediaDto>(await staff.PostAsync($"/api/media/{upload.Id}/complete", null));
        fixture.Storage.Objects[$"staging/{upload.Id:N}"] = new byte[png.Length];
        await Body<MediaDto>(await staff.PostAsync($"/api/media/{upload.Id}/complete", null));
        CollectionAssert.AreEqual(png, fixture.Storage.Objects[$"community/{upload.Id:N}"]);
        Assert.AreEqual(HttpStatusCode.NotFound, (await paid.GetAsync($"/api/media/{upload.Id}/link")).StatusCode);
        var post = await Body<PostDto>(await staff.PostAsJsonAsync("/api/post", PostRequest([upload.Id])));
        var linkResponse = await paid.GetAsync($"/api/media/{upload.Id}/link");
        var link = await Body<MediaLinkDto>(linkResponse);
        Assert.IsTrue(link.ExpiresAt <= DateTime.UtcNow.AddMinutes(6));
        Assert.IsTrue(linkResponse.Headers.CacheControl?.NoStore);
        Assert.AreEqual(HttpStatusCode.Forbidden, (await free.GetAsync($"/api/media/{upload.Id}/link")).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await staff.PostAsJsonAsync("/api/post", PostRequest([upload.Id]))).StatusCode);
        await staff.DeleteAsync($"/api/post/{post.Id}");
        Assert.AreEqual(HttpStatusCode.NotFound, (await paid.GetAsync($"/api/media/{upload.Id}/link")).StatusCode);
    }
}
