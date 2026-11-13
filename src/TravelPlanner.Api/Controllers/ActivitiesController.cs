using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Core.Abstractions;
using TravelPlanner.Core.Contracts;

namespace TravelPlanner.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/trips/{tripId:guid}/activities")]
[Produces("application/json")]
public sealed class ActivitiesController(ITripService trips) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TripDetailsDto>> Create(Guid tripId, ActivityWriteRequest request, CancellationToken ct)
        => Ok(await trips.AddActivityAsync(tripId, request, ct));

    [HttpPut("{activityId:guid}")]
    public async Task<ActionResult<TripDetailsDto>> Update(Guid tripId, Guid activityId, ActivityWriteRequest request, CancellationToken ct)
        => Ok(await trips.UpdateActivityAsync(tripId, activityId, request, ct));

    [HttpDelete("{activityId:guid}")]
    public async Task<ActionResult<TripDetailsDto>> Delete(Guid tripId, Guid activityId, CancellationToken ct)
        => Ok(await trips.DeleteActivityAsync(tripId, activityId, ct));
}