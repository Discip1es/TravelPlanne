namespace TravelPlanner.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "TravelPlanner";
    public string Audience { get; set; } = "TravelPlanner.Spa";

    /// <summary>Симметричный ключ HS256: не менее 32 символов. В продакшене — только через секрет/ENV.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 14;
}