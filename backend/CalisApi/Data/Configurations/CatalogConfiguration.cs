using CalisApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CalisApi.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(c => c.Name).IsUnique();
        builder.Property(c => c.Description).IsRequired().HasMaxLength(500);
    }
}

public class VideoConfiguration : IEntityTypeConfiguration<Video>
{
    public void Configure(EntityTypeBuilder<Video> builder)
    {
        builder.ToTable("Videos");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Title).IsRequired().HasMaxLength(200);
        builder.Property(v => v.Description).IsRequired().HasMaxLength(2000);
        builder.Property(v => v.Difficulty).IsRequired().HasMaxLength(20);
        builder.Property(v => v.Requisites).IsRequired().HasMaxLength(500);
        builder.Property(v => v.Url).IsRequired().HasMaxLength(1024);

        builder.HasOne(v => v.Category)
            .WithMany(c => c.Videos)
            .HasForeignKey(v => v.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
