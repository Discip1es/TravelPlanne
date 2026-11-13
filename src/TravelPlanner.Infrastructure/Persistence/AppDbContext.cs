using Microsoft.EntityFrameworkCore;
using TravelPlanner.Core.Domain;

namespace TravelPlanner.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<ActivityItem> Activities => Set<ActivityItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Подхватывает все IEntityTypeConfiguration из этой сборки.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}