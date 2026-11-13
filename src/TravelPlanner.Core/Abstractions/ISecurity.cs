using TravelPlanner.Core.Contracts;
using TravelPlanner.Core.Domain;

namespace TravelPlanner.Core.Abstractions;

/// <summary>Абстракция текущего пользователя (реализация — поверх HttpContext).</summary>
public interface ICurrentUser
{
    Guid UserId { get; }
    bool IsAdmin { get; }
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateAccessToken(AppUser user);
    string CreateRefreshToken();
}

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct);
    Task LogoutAsync(string refreshToken, CancellationToken ct);
    Task<UserDto> GetProfileAsync(Guid userId, CancellationToken ct);
    Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, CancellationToken ct);
}