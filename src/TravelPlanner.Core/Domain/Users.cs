namespace TravelPlanner.Core.Domain;

/// <summary>Пользователь. Пароль хранится только в виде PBKDF2-хеша.</summary>
public class AppUser : EntityBase
{
    public string Email { get; set; } = string.Empty;        // нормализованный (lowercase) e-mail
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; // формат: iterations.salt.hash
    public UserRole Role { get; set; } = UserRole.User;

    public List<RefreshToken> RefreshTokens { get; set; } = [];
    public List<Trip> Trips { get; set; } = [];
}

/// <summary>
/// Refresh-токен. Применяется ротация: при обновлении старый токен отзывается
/// и связывается с новым (ReplacedByToken) — это позволяет обнаруживать повторное использование.
/// </summary>
public class RefreshToken : EntityBase
{
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByToken { get; set; }

    /// <summary>Вычисляемое свойство; в БД не отображается (см. конфигурацию EF).</summary>
    public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;
}