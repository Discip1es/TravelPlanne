using System.Security.Claims;
using TravelPlanner.Core.Abstractions;
using TravelPlanner.Core.Domain;

namespace TravelPlanner.Api.Security;

public sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal Principal =>
        accessor.HttpContext?.User ?? throw new AuthenticationException("Требуется аутентификация.");

    public Guid UserId
    {
        get
        {
            var raw = Principal.FindFirst("sub")?.Value ?? Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(raw, out var id)
                ? id
                : throw new AuthenticationException("Токен не содержит идентификатор пользователя.");
        }
    }

    public bool IsAdmin => Principal.IsInRole("Admin");
}