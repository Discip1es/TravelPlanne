using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Core.Abstractions;
using TravelPlanner.Core.Contracts;

namespace TravelPlanner.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController(IDashboardService dashboard) : ControllerBase
{
    /// <summary>Сводные показатели по всем доступным пользователю поездкам.</summary>
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get(CancellationToken ct) => Ok(await dashboard.GetAsync(ct));
}