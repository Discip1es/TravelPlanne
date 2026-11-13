using Microsoft.EntityFrameworkCore;
using TravelPlanner.Core.Abstractions;
using TravelPlanner.Core.Contracts;
using TravelPlanner.Core.Domain;
using TravelPlanner.Infrastructure.Mapping;
using TravelPlanner.Infrastructure.Persistence;

namespace TravelPlanner.Infrastructure.Services;

public sealed class DashboardService(
    AppDbContext db,
    ICurrentUser currentUser,
    ICurrencyService currencies) : IDashboardService
{
    private const string BaseCurrency = "RUB";

    public async Task<DashboardDto> GetAsync(CancellationToken ct)
    {
        var trips = await (currentUser.IsAdmin ? db.Trips : db.Trips.Where(t => t.OwnerId == currentUser.UserId))
            .AsNoTracking().Include(t => t.Activities).AsSplitQuery().ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var active = trips.Where(t => t.Status != TripStatus.Cancelled).ToList();

        // Суммы приводятся к базовой валюте — иначе складывать рубли с юанями бессмысленно.
        decimal budget = 0, planned = 0, booked = 0;
        var byCategory = new Dictionary<ActivityCategory, decimal>();

        foreach (var trip in active)
        {
            budget += await currencies.ConvertAsync(trip.Budget, trip.Currency, BaseCurrency, ct);
            planned += await currencies.ConvertAsync(trip.PlannedCost, trip.Currency, BaseCurrency, ct);
            booked += await currencies.ConvertAsync(trip.BookedCost, trip.Currency, BaseCurrency, ct);

            foreach (var activity in trip.Activities.Where(a => a.Status != ActivityStatus.Cancelled))
            {
                var cost = await currencies.ConvertAsync(activity.Cost, trip.Currency, BaseCurrency, ct);
                byCategory[activity.Category] = byCategory.GetValueOrDefault(activity.Category) + cost;
            }
        }

        var tripsByStatus = trips.GroupBy(t => t.Status)
            .Select(g => new NameValueDto(g.Key.ToString(), g.Count()))
            .OrderBy(x => x.Name).ToList();

        var costByCategory = byCategory
            .Select(kv => new NameValueDto(kv.Key.ToString(), decimal.Round(kv.Value, 2)))
            .OrderByDescending(x => x.Value).ToList();

        var next = trips
            .Where(t => t.StartDate >= today && t.Status is TripStatus.Planned or TripStatus.Booked or TripStatus.Draft)
            .OrderBy(t => t.StartDate).Take(5)
            .Select(Mappers.ToListItem).ToList();

        return new DashboardDto(
            BaseCurrency,
            trips.Count,
            trips.Count(t => t.StartDate > today && t.Status is TripStatus.Planned or TripStatus.Booked),
            trips.Count(t => t.Status == TripStatus.InProgress),
            trips.Count(t => t.Status == TripStatus.Completed),
            decimal.Round(budget, 2), decimal.Round(planned, 2), decimal.Round(booked, 2),
            tripsByStatus, costByCategory, next);
    }
}