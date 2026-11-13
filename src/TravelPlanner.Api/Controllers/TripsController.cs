using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Core.Abstractions;
using TravelPlanner.Core.Contracts;

namespace TravelPlanner.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/trips")]
[Produces("application/json")]
public sealed class TripsController(ITripService trips) : ControllerBase
{
    /// <summary>Список поездок с поиском, фильтрами, сортировкой и пагинацией.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TripListItemDto>>> List([FromQuery] TripQuery query, CancellationToken ct)
        => Ok(await trips.GetPagedAsync(query, ct));

    /// <summary>Поездка вместе с программой и разбивкой расходов.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TripDetailsDto>> Get(Guid id, CancellationToken ct)
        => Ok(await trips.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<TripDetailsDto>> Create(TripWriteRequest request, CancellationToken ct)
    {
        var created = await trips.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Обновление. Передайте Version из GET — при конфликте вернётся 409.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TripDetailsDto>> Update(Guid id, TripWriteRequest request, CancellationToken ct)
        => Ok(await trips.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await trips.DeleteAsync(id, ct);
        return NoContent();
    }
}