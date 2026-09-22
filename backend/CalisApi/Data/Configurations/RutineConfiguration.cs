using CalisApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CalisApi.Data.Configurations;

public class RutineConfiguration : IEntityTypeConfiguration<Rutine>
{
    public void Configure(EntityTypeBuilder<Rutine> builder)
    {
        builder.ToTable("Rutines");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Title).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Description).IsRequired().HasMaxLength(2000);
        builder.Property(r => r.Duration).IsRequired().HasMaxLength(50);
        builder.Property(r => r.Difficulty).IsRequired().HasMaxLength(20);

        builder.HasOne(r => r.Category)
            .WithMany(c => c.Rutines)
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Exercises)
            .WithOne(e => e.Rutine)
            .HasForeignKey(e => e.RutineId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RutineExerciseConfiguration : IEntityTypeConfiguration<RutineExercise>
{
    public void Configure(EntityTypeBuilder<RutineExercise> builder)
    {
        builder.ToTable("RutineExercises");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Exercise).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Tipo).IsRequired().HasMaxLength(20);
        builder.Property(e => e.Descanso).IsRequired().HasMaxLength(20);
        builder.Property(e => e.Obs).IsRequired().HasMaxLength(1000);

        // RESTRICT: no se puede borrar un video referenciado por una rutina
        builder.HasOne(e => e.Video)
            .WithMany(v => v.RutineExercises)
            .HasForeignKey(e => e.VideoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
