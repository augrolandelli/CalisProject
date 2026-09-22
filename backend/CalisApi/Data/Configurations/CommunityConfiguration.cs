using CalisApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CalisApi.Data.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> b)
    {
        b.Property(p => p.Title).HasMaxLength(200);
        b.Property(p => p.Content).HasMaxLength(5000);
        b.Property(p => p.Kind).HasMaxLength(20);
        b.Property(p => p.Location).HasMaxLength(300);
        b.HasIndex(p => new { p.IsDeleted, p.CreatedAt, p.Id });
        b.HasIndex(p => p.UserAchievementId).IsUnique();
        b.HasOne(p => p.Author).WithMany().HasForeignKey(p => p.AuthorId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(p => p.UserAchievement).WithMany().HasForeignKey(p => p.UserAchievementId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class PostLikeConfiguration : IEntityTypeConfiguration<PostLike>
{
    public void Configure(EntityTypeBuilder<PostLike> b)
    {
        b.HasKey(p => new { p.PostId, p.UserId });
        b.HasOne(p => p.Post).WithMany(p => p.Likes).HasForeignKey(p => p.PostId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> b)
    {
        b.Property(m => m.FileName).HasMaxLength(200);
        b.Property(m => m.ContentType).HasMaxLength(50);
        b.Property(m => m.StagingKey).HasMaxLength(200);
        b.Property(m => m.ObjectKey).HasMaxLength(200);
        b.Property(m => m.Purpose).HasMaxLength(20);
        b.HasIndex(m => new { m.IsDeleted, m.PostId, m.CreatedAt });
        b.HasOne(m => m.UploadedBy).WithMany().HasForeignKey(m => m.UploadedById).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(m => m.Post).WithMany(p => p.Media).HasForeignKey(m => m.PostId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(m => m.Video).WithMany().HasForeignKey(m => m.VideoId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class SessionReviewConfiguration : IEntityTypeConfiguration<SessionReview>
{
    public void Configure(EntityTypeBuilder<SessionReview> b)
    {
        b.ToTable("SessionReviews", t => t.HasCheckConstraint("CK_SessionReviews_Rating", "[Rating] BETWEEN 1 AND 5"));
        b.Property(r => r.Content).HasMaxLength(1500);
        b.Property(r => r.SessionTitle).HasMaxLength(200);
        b.HasIndex(r => new { r.UserId, r.SessionId }).IsUnique();
        b.HasIndex(r => new { r.IsHidden, r.CreatedAt, r.Id });
        b.HasOne(r => r.Session).WithMany().HasForeignKey(r => r.SessionId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CommunityEventConfiguration : IEntityTypeConfiguration<CommunityEvent>
{
    public void Configure(EntityTypeBuilder<CommunityEvent> b)
    {
        b.ToTable("CommunityEvents", t =>
        {
            t.HasCheckConstraint("CK_CommunityEvents_Capacity", "[Capacity] IS NULL OR [Capacity] > 0");
            t.HasCheckConstraint("CK_CommunityEvents_Dates", "[EndsAt] > [StartsAt]");
        });
        b.Property(e => e.Title).HasMaxLength(200);
        b.Property(e => e.Description).HasMaxLength(5000);
        b.Property(e => e.Location).HasMaxLength(300);
        b.HasIndex(e => new { e.StartsAt, e.Id });
    }
}

public class EventRegistrationConfiguration : IEntityTypeConfiguration<EventRegistration>
{
    public void Configure(EntityTypeBuilder<EventRegistration> b)
    {
        b.HasKey(r => new { r.EventId, r.UserId });
        b.HasOne(r => r.Event).WithMany(e => e.Registrations).HasForeignKey(r => r.EventId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
