using CalisApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CalisApi.Data.Configurations;

public class AchievementConfiguration : IEntityTypeConfiguration<Achievement>
{
    public void Configure(EntityTypeBuilder<Achievement> builder)
    {
        builder.ToTable("Achievements");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Description).IsRequired().HasMaxLength(500);
        builder.Property(a => a.Icon).IsRequired().HasMaxLength(50);
    }
}

public class SessionAchievementConfiguration : IEntityTypeConfiguration<SessionAchievement>
{
    public void Configure(EntityTypeBuilder<SessionAchievement> builder)
    {
        builder.ToTable("SessionAchievements");
        builder.HasKey(sa => sa.Id);

        builder.HasIndex(sa => new { sa.SessionId, sa.AchievementId }).IsUnique();

        builder.HasOne(sa => sa.Session)
            .WithMany()
            .HasForeignKey(sa => sa.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sa => sa.Achievement)
            .WithMany(a => a.SessionAchievements)
            .HasForeignKey(sa => sa.AchievementId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserAchievementConfiguration : IEntityTypeConfiguration<UserAchievement>
{
    public void Configure(EntityTypeBuilder<UserAchievement> builder)
    {
        builder.ToTable("UserAchievements");
        builder.HasKey(ua => ua.Id);

        // Un logro por usuario por clase (o único global si no tiene clase)
        builder.HasIndex(ua => new { ua.UserId, ua.AchievementId, ua.SessionId }).IsUnique();

        builder.HasOne(ua => ua.User)
            .WithMany()
            .HasForeignKey(ua => ua.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ua => ua.Achievement)
            .WithMany(a => a.UserAchievements)
            .HasForeignKey(ua => ua.AchievementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ua => ua.Session)
            .WithMany()
            .HasForeignKey(ua => ua.SessionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
