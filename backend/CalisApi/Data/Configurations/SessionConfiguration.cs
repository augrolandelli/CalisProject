using CalisApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CalisApi.Data.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("Sessions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Description).IsRequired().HasMaxLength(2000);
        builder.Property(s => s.Difficulty).IsRequired().HasMaxLength(20);
        builder.Property(s => s.CoachName).IsRequired().HasMaxLength(100);
        builder.HasIndex(s => s.Date);
        builder.Property(s => s.DurationMinutes).HasDefaultValue(60);

        builder.HasMany(s => s.Enrollments)
            .WithOne(e => e.Session)
            .HasForeignKey(e => e.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Waitlist)
            .WithOne(w => w.Session)
            .HasForeignKey(w => w.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions");
        builder.HasKey(e => e.Id);

        // Regla de negocio a nivel BD: un usuario no puede inscribirse dos veces
        builder.HasIndex(e => new { e.UserId, e.SessionId }).IsUnique();

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SessionWaitlistConfiguration : IEntityTypeConfiguration<SessionWaitlist>
{
    public void Configure(EntityTypeBuilder<SessionWaitlist> builder)
    {
        builder.ToTable("SessionWaitlists");
        builder.HasKey(w => w.Id);

        builder.HasIndex(w => new { w.UserId, w.SessionId }).IsUnique();

        builder.HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.ToTable("PushSubscriptions");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Endpoint).IsRequired().HasMaxLength(1024);
        builder.HasIndex(p => p.Endpoint).IsUnique();
        builder.Property(p => p.P256dh).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Auth).IsRequired().HasMaxLength(200);

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
