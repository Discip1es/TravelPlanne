using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TravelPlanner.Core.Domain;

namespace TravelPlanner.Infrastructure.Persistence;

/// <summary>
/// DateOnly/TimeOnly хранятся как ISO-строки. Это гарантирует корректную сортировку
/// и сравнение дат в SQL на любом провайдере (в частности, в SQLite).
/// </summary>
internal static class Converters
{
    public static readonly ValueConverter<DateOnly, string> Date = new(
        d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        s => DateOnly.ParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture));

    public static readonly ValueConverter<TimeOnly, string> Time = new(
        t => t.ToString("HH:mm", CultureInfo.InvariantCulture),
        s => TimeOnly.ParseExact(s, "HH:mm", CultureInfo.InvariantCulture));
}

public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> b)
    {
        b.ToTable("Users");
        b.HasKey(u => u.Id);
        b.Property(u => u.Email).IsRequired().HasMaxLength(256);
        b.HasIndex(u => u.Email).IsUnique();
        b.Property(u => u.DisplayName).IsRequired().HasMaxLength(64);
        b.Property(u => u.PasswordHash).IsRequired().HasMaxLength(512);
        b.Property(u => u.Role).HasConversion<string>().HasMaxLength(16);

        b.HasMany(u => u.Trips).WithOne(t => t.Owner!)
            .HasForeignKey(t => t.OwnerId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(u => u.RefreshTokens).WithOne(t => t.User!)
            .HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokens");
        b.HasKey(t => t.Id);
        b.Property(t => t.Token).IsRequired().HasMaxLength(256);
        b.HasIndex(t => t.Token).IsUnique();
        b.HasIndex(t => t.UserId);
        b.Property(t => t.ReplacedByToken).HasMaxLength(256);
        b.Ignore(t => t.IsActive); // вычисляемое свойство
    }
}

public sealed class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> b)
    {
        b.ToTable("Trips");
        b.HasKey(t => t.Id);
        b.Property(t => t.Title).IsRequired().HasMaxLength(120);
        b.Property(t => t.City).IsRequired().HasMaxLength(80);
        b.Property(t => t.Country).IsRequired().HasMaxLength(80);
        b.Property(t => t.Description).HasMaxLength(2000);
        b.Property(t => t.Currency).IsRequired().HasMaxLength(3);
        b.Property(t => t.Budget).HasPrecision(18, 2);
        b.Property(t => t.Status).HasConversion<string>().HasMaxLength(16);
        b.Property(t => t.StartDate).HasConversion(Converters.Date);
        b.Property(t => t.EndDate).HasConversion(Converters.Date);
        b.Property(t => t.Version).IsConcurrencyToken();

        // Travelers (List<string>) EF Core 8+ отображает в JSON-колонку автоматически.
        b.HasIndex(t => new { t.OwnerId, t.StartDate });
        b.HasIndex(t => t.Status);

        b.HasMany(t => t.Activities).WithOne(a => a.Trip!)
            .HasForeignKey(a => a.TripId).OnDelete(DeleteBehavior.Cascade);

        b.Ignore(t => t.DurationDays);
        b.Ignore(t => t.PlannedCost);
        b.Ignore(t => t.BookedCost);
        b.Ignore(t => t.Remaining);
        b.Ignore(t => t.BudgetUsagePercent);
    }
}

public sealed class ActivityItemConfiguration : IEntityTypeConfiguration<ActivityItem>
{
    public void Configure(EntityTypeBuilder<ActivityItem> b)
    {
        b.ToTable("Activities");
        b.HasKey(a => a.Id);
        b.Property(a => a.Title).IsRequired().HasMaxLength(160);
        b.Property(a => a.Category).HasConversion<string>().HasMaxLength(24);
        b.Property(a => a.Status).HasConversion<string>().HasMaxLength(16);
        b.Property(a => a.Date).HasConversion(Converters.Date);
        b.Property(a => a.StartTime).HasConversion(Converters.Time);
        b.Property(a => a.Cost).HasPrecision(18, 2);
        b.Property(a => a.Location).HasMaxLength(200);
        b.Property(a => a.Notes).HasMaxLength(1000);
        b.HasIndex(a => new { a.TripId, a.Date, a.Order });
    }
}