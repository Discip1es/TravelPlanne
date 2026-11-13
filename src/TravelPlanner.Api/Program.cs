using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TravelPlanner.Api.Hubs;
using TravelPlanner.Api.Middleware;
using TravelPlanner.Api.OpenApi;
using TravelPlanner.Api.Realtime;
using TravelPlanner.Api.Security;
using TravelPlanner.Api.Workers;
using TravelPlanner.Core.Abstractions;
using TravelPlanner.Infrastructure;
using TravelPlanner.Infrastructure.Persistence;
using TravelPlanner.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// ---------- 1. Конфигурация и проверки ----------
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
    throw new InvalidOperationException(
        "Не задан Jwt:SigningKey (минимум 32 символа). Укажите переменную окружения Jwt__SigningKey или user-secrets.");

Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "App_Data"));

// ---------- 2. Сервисы ----------
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
builder.Services.AddSingleton<INotificationService, SignalRNotificationService>();
builder.Services.AddHostedService<TripMaintenanceWorker>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    // Перечисления передаются строками ("Booked"), что удобнее для TypeScript.
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSignalR();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("sqlite");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false; // оставляем короткие имена claims: sub, role, email
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwt.Issuer,
        ValidateAudience = true,
        ValidAudience = jwt.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
        NameClaimType = "sub",
        RoleClaimType = "role"
    };

    // Браузерный WebSocket не умеет ставить заголовок Authorization — токен берём из query string.
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                context.Token = accessToken;
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin")));

const string SpaCorsPolicy = "spa";
builder.Services.AddCors(options => options.AddPolicy(SpaCorsPolicy, policy => policy
    .WithOrigins("http://localhost:5095", "http://127.0.0.1:5095")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

// Защита эндпоинтов аутентификации от перебора: 10 запросов в минуту с одного IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

builder.Services.AddOutputCache(options =>
    options.AddPolicy("meta", policy => policy.Expire(TimeSpan.FromMinutes(10))));

var app = builder.Build();

// ---------- 3. Конвейер обработки ----------
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapGet("/swagger", () => Results.Content(SwaggerUiPage.Html, "text/html; charset=utf-8"));
}

app.UseCors(SpaCorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

// SPA раздаётся тем же процессом: wwwroot + fallback на index.html (клиентский роутинг).
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapHub<NotificationsHub>("/hubs/notifications");
app.MapHealthChecks("/health");
app.MapFallbackToFile("index.html");

// ---------- 4. Миграции и демонстрационные данные ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    // Если миграции ещё не созданы командой dotnet ef — создаём схему напрямую (удобно при первом запуске).
    if (db.Database.GetMigrations().Any())
        await db.Database.MigrateAsync();
    else
        await db.Database.EnsureCreatedAsync();

    await DbSeeder.SeedAsync(db, hasher);
}

await app.RunAsync();
