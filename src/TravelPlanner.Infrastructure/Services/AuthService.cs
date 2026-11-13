using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelPlanner.Core.Abstractions;
using TravelPlanner.Core.Contracts;
using TravelPlanner.Core.Domain;
using TravelPlanner.Infrastructure.Mapping;
using TravelPlanner.Infrastructure.Persistence;
using TravelPlanner.Infrastructure.Security;

namespace TravelPlanner.Infrastructure.Services;

public sealed class AuthService(
    AppDbContext db,
    IPasswordHasher hasher,
    ITokenService tokens,
    IOptions<JwtOptions> jwtOptions,
    ILogger<AuthService> logger) : IAuthService
{
    private static string Normalize(string email) => email.Trim().ToLowerInvariant();

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = Normalize(request.Email);
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException("Пользователь с таким e-mail уже зарегистрирован.");

        var user = new AppUser
        {
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = hasher.Hash(request.Password),
            Role = UserRole.User
        };
        db.Users.Add(user);

        var response = await IssueAsync(user, ct);
        logger.LogInformation("Зарегистрирован пользователь {Email}", email);
        return response;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = Normalize(request.Email);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Единое сообщение для «нет пользователя» и «неверный пароль» — не раскрываем наличие аккаунта.
        if (user is null || !hasher.Verify(request.Password, user.PasswordHash))
            throw new AuthenticationException("Неверный e-mail или пароль.");

        // Убираем мёртвые токены, чтобы таблица не росла бесконечно.
        var stale = await db.RefreshTokens
            .Where(t => t.UserId == user.Id && (t.RevokedAtUtc != null || t.ExpiresAtUtc < DateTime.UtcNow))
            .ToListAsync(ct);
        db.RefreshTokens.RemoveRange(stale);

        logger.LogInformation("Вход пользователя {Email}", email);
        return await IssueAsync(user, ct);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var stored = await db.RefreshTokens.Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == refreshToken, ct);

        if (stored is null || !stored.IsActive || stored.User is null)
            throw new AuthenticationException("Refresh-токен недействителен. Требуется повторный вход.");

        stored.RevokedAtUtc = DateTime.UtcNow;              // ротация: старый токен больше не годится
        var response = await IssueAsync(stored.User, ct);   // здесь происходит SaveChanges
        stored.ReplacedByToken = response.RefreshToken;
        await db.SaveChangesAsync(ct);

        return response;
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct)
    {
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken, ct);
        if (stored is null || stored.RevokedAtUtc is not null) return; // идемпотентно
        stored.RevokedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<UserDto> GetProfileAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new NotFoundException("Пользователь", userId);
        return user.ToUserDto();
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Users.AsNoTracking().OrderByDescending(u => u.CreatedAtUtc);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new UserDto(u.Id, u.Email, u.DisplayName, u.Role.ToString()))
            .ToListAsync(ct);

        return new PagedResult<UserDto>(items, page, pageSize, total);
    }

    private async Task<AuthResponse> IssueAsync(AppUser user, CancellationToken ct)
    {
        var (access, expires) = tokens.CreateAccessToken(user);
        var refresh = new RefreshToken
        {
            UserId = user.Id,
            Token = tokens.CreateRefreshToken(),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays)
        };
        db.RefreshTokens.Add(refresh);
        await db.SaveChangesAsync(ct);
        return new AuthResponse(access, refresh.Token, expires, user.ToUserDto());
    }
}