using TravelPlanner.Core.Contracts;
using TravelPlanner.Core.Domain;

namespace TravelPlanner.Infrastructure.Mapping;

/// <summary>Ручной маппинг сущность → DTO: прозрачно, без зависимостей и без «магии».</summary>
public static class Mappers
{
    public static UserDto ToUserDto(this AppUser u) => new(u.Id, u.Email, u.DisplayName, u.Role.ToString());

    public static TripListItemDto ToListItem(Trip t) => new(
        t.Id, t.Title, t.City, t.Country, t.StartDate, t.EndDate, t.Status.ToString(),
        t.Budget, t.Currency, t.PlannedCost, t.Remaining, t.BudgetUsagePercent,
        t.DurationDays, t.Activities.Count, t.Travelers.Count);

    public static TripDetailsDto ToDetails(Trip t) => new(
        t.Id, t.Title, t.City, t.Country, t.Description, t.StartDate, t.EndDate, t.Status.ToString(),
        t.Budget, t.Currency, t.PlannedCost, t.BookedCost, t.Remaining, t.BudgetUsagePercent,
        t.DurationDays,
        t.Travelers.ToList(),
        t.Activities.OrderBy(a => a.Date).ThenBy(a => a.Order).ThenBy(a => a.StartTime).Select(ToActivityDto).ToList(),
        CostByCategory(t),
        t.Version, t.CreatedAtUtc, t.UpdatedAtUtc);

    public static ActivityDto ToActivityDto(ActivityItem a) => new(
        a.Id, a.Title, a.Category.ToString(), a.Status.ToString(),
        a.Date, a.StartTime?.ToString("HH:mm"), a.DurationMinutes,
        a.Cost, a.Location, a.Notes, a.Order);

    public static IReadOnlyList<NameValueDto> CostByCategory(Trip t) => t.Activities
        .Where(a => a.Status != ActivityStatus.Cancelled)
        .GroupBy(a => a.Category)
        .Select(g => new NameValueDto(g.Key.ToString(), g.Sum(a => a.Cost)))
        .OrderByDescending(x => x.Value)
        .ToList();
}