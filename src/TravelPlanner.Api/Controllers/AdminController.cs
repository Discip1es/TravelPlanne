using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Core.Abstractions;
using TravelPlanner.Core.Contracts;

namespace TravelPlanner.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin")]
public sealed class AdminController(IAuthService auth) : ControllerBase
{
    /// <summary>Список пользователей (только роль Admin).</summary>
    [HttpGet("users")]
    public async Task<ActionResult<PagedResult<UserDto>>> Users(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await auth.GetUsersAsync(page, pageSize, ct));
}