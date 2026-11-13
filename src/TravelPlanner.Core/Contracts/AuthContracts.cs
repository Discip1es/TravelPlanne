using System.ComponentModel.DataAnnotations;

namespace TravelPlanner.Core.Contracts;

public sealed record RegisterRequest
{
    [Required, EmailAddress, StringLength(256)] public string Email { get; init; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 8)] public string Password { get; init; } = string.Empty;
    [Required, StringLength(64, MinimumLength = 2)] public string DisplayName { get; init; } = string.Empty;
}

public sealed record LoginRequest
{
    [Required, EmailAddress] public string Email { get; init; } = string.Empty;
    [Required] public string Password { get; init; } = string.Empty;
}

public sealed record RefreshRequest
{
    [Required] public string RefreshToken { get; init; } = string.Empty;
}

public sealed record UserDto(Guid Id, string Email, string DisplayName, string Role);

public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc, UserDto User);