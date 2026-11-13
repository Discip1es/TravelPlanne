using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TravelPlanner.Core.Abstractions;
using TravelPlanner.Infrastructure.Persistence;
using TravelPlanner.Infrastructure.Security;
using TravelPlanner.Infrastructure.Services;

namespace TravelPlanner.Infrastructure;

/// <summary>Единая точка регистрации инфраструктурных сервисов (composition root вызывает Api).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=App_Data/travelplanner.db";

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<ICurrencyService, CurrencyService>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITripService, TripService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}