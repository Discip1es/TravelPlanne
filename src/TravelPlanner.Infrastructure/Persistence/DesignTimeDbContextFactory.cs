using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TravelPlanner.Infrastructure.Persistence;

/// <summary>
/// Фабрика для команд `dotnet ef`. Благодаря ей миграции создаются
/// без запуска веб-хоста и без обязательного appsettings.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Data Source=App_Data/travelplanner.db";

        Directory.CreateDirectory("App_Data");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}