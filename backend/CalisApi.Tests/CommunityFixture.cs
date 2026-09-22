using System.Collections.Concurrent;
using System.Net.Http.Headers;
using CalisApi.Auth;
using CalisApi.Common;
using CalisApi.Data;
using CalisApi.Models;
using CalisApi.Services.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: DoNotParallelize]

namespace CalisApi.Tests;

/// <summary>API real + SQL Server real en una BD aislada. Solo el transporte a R2 se sustituye.</summary>
public class CommunityFixture : WebApplicationFactory<Program>
{
    public string DatabaseName { get; } = $"CalisAppTests_{Guid.NewGuid():N}";
    public MemoryObjectStorage Storage { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Key", Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"));
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Server=(localdb)\\MSSQLLocalDB;Database={DatabaseName};Trusted_Connection=True;TrustServerCertificate=True");
        builder.UseSetting("Serilog:MinimumLevel:Default", "Warning");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IObjectStorage>();
            services.AddSingleton<IObjectStorage>(Storage);
        });
    }

    public async Task<User> AddUserAsync(string role)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CalisDbContext>();
        var user = new User { FullName = $"Test {role}", Email = $"{Guid.NewGuid():N}@test.invalid", Phone = "12345678", PasswordHash = "unused-test-hash", Role = role };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public HttpClient Client(User? user = null)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        if (user is not null)
        {
            using var scope = Services.CreateScope();
            var token = scope.ServiceProvider.GetRequiredService<ITokenService>().CreateAccessToken(user).Token;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return client;
    }

    public async Task<Session> AddSessionAsync(DateTime date, User? member = null, DateTime? reservedAt = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CalisDbContext>();
        var session = new Session
        {
            Title = "Clase de prueba", Description = "Prueba de comunidad", Date = date, DurationMinutes = 60,
            CoachName = "Coach", Difficulty = "basica", LimitedSpots = 10, Enrolled = member is null ? 0 : 1,
        };
        if (member is not null) session.Enrollments.Add(new UserSession { UserId = member.Id, CreatedAt = reservedAt ?? date.AddHours(-1) });
        db.Sessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }
}

public sealed class MemoryObjectStorage : IObjectStorage
{
    public ConcurrentDictionary<string, byte[]> Objects { get; } = new();
    public bool IsConfigured => true;
    public Task<string> UploadUrlAsync(string key, string contentType, DateTime expiresAt) => Task.FromResult($"https://storage.test/{key}");
    public Task<string> ReadUrlAsync(string key, DateTime expiresAt) => Task.FromResult($"https://storage.test/{key}?expires={expiresAt.Ticks}");
    public Task<byte[]> ReadAsync(string key, long expectedSize, CancellationToken ct)
    {
        if (!Objects.TryGetValue(key, out var bytes) || bytes.Length != expectedSize) throw new AppException("Tamaño inválido.");
        return Task.FromResult(bytes.ToArray());
    }
    public Task WriteAsync(string key, byte[] contents, string contentType, CancellationToken ct)
    { Objects[key] = contents.ToArray(); return Task.CompletedTask; }
    public Task DeleteAsync(string key, CancellationToken ct)
    { Objects.TryRemove(key, out _); return Task.CompletedTask; }
}
