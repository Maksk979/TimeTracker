using Microsoft.EntityFrameworkCore;
using TimeTracker.Core.Storage.Entities;

namespace TimeTracker.Core.Storage;

/// <summary>
/// EF Core DbContext тайм-трекера. Хранит сессии, категории и правила в одной SQLite-БД.
/// </summary>
public class TimeTrackerDbContext : DbContext
{
    public TimeTrackerDbContext(DbContextOptions<TimeTrackerDbContext> options)
        : base(options)
    {
    }

    public DbSet<ActivitySession> Sessions => Set<ActivitySession>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryRule> Rules => Set<CategoryRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.Property(c => c.ColorHex).IsRequired().HasMaxLength(20);
            entity.Property(c => c.Icon).HasMaxLength(20);

            entity.HasMany(c => c.Rules)
                .WithOne(r => r.Category)
                .HasForeignKey(r => r.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<CategoryRule>(entity =>
        {
            entity.ToTable("category_rules");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.FieldType).HasConversion<int>();
            entity.Property(r => r.MatchType).HasConversion<int>();
            entity.Property(r => r.Pattern).IsRequired().HasMaxLength(500);
            entity.Property(r => r.Priority).HasDefaultValue(100);
            entity.HasIndex(r => new { r.CategoryId, r.IsEnabled });
        });

        modelBuilder.Entity<ActivitySession>(entity =>
        {
            entity.ToTable("sessions");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.ProcessName).IsRequired().HasMaxLength(260);
            entity.Property(s => s.ExecutableName).HasMaxLength(260);
            entity.Property(s => s.WindowTitle).HasMaxLength(1000);
            entity.Property(s => s.DurationSeconds);

            entity.HasOne(s => s.Category)
                .WithMany(c => c.Sessions)
                .HasForeignKey(s => s.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Индекс для быстрого построения отчётов по дням.
            entity.HasIndex(s => s.StartedAtUtc);
            entity.HasIndex(s => new { s.StartedAtUtc, s.ProcessName });
        });
    }
}
