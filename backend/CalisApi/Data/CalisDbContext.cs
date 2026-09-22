using CalisApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CalisApi.Data;

/// <summary>
/// Contexto principal de Entity Framework Core para CalisApp.
/// Las entidades se agregan por fases segun el roadmap (ver ESPECIFICACION_CALISAPP.md).
/// </summary>
public class CalisDbContext(DbContextOptions<CalisDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Video> Videos => Set<Video>();
    public DbSet<Rutine> Rutines => Set<Rutine>();
    public DbSet<RutineExercise> RutineExercises => Set<RutineExercise>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<SessionWaitlist> SessionWaitlists => Set<SessionWaitlist>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<SessionAchievement> SessionAchievements => Set<SessionAchievement>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostLike> PostLikes => Set<PostLike>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<SessionReview> SessionReviews => Set<SessionReview>();
    public DbSet<CommunityEvent> CommunityEvents => Set<CommunityEvent>();
    public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CalisDbContext).Assembly);
    }
}

/// <summary>SQL datetime2 no almacena Kind: todas las fechas del dominio se materializan como UTC.</summary>
public sealed class UtcDateTimeConverter() : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
    value => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value,
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
