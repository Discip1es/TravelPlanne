using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TravelPlanner.Core.Abstractions;
using TravelPlanner.Core.Contracts;
using TravelPlanner.Core.Domain;
using TravelPlanner.Infrastructure.Mapping;
using TravelPlanner.Infrastructure.Persistence;

namespace TravelPlanner.Infrastructure.Services;

public sealed class TripService(
    AppDbContext db,
    ICurrentUser currentUser,
    ICurrencyService currencies,
    INotificationService notifications,
    ILogger<TripService> logger) : ITripService
{
    /// <summary>Область видимости: обычный пользователь видит свои поездки, администратор — все.</summary>
    private IQueryable<Trip> Scope() =>
        currentUser.IsAdmin ? db.Trips : db.Trips.Where(t => t.OwnerId == currentUser.UserId);

    public async Task<PagedResult<TripListItemDto>> GetPagedAsync(TripQuery query, CancellationToken ct)
    {
        var q = Scope().Include(t => t.Activities).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(t => t.Title.ToLower().Contains(s)
                             || t.City.ToLower().Contains(s)
                             || t.Country.ToLower().Contains(s)
                             || (t.Description != null && t.Description.ToLower().Contains(s)));
        }
        if (query.Status.HasValue) q = q.Where(t => t.Status == query.Status.Value);
        if (!string.IsNullOrWhiteSpace(query.City))
        {
            var city = query.City.Trim().ToLower();
            q = q.Where(t => t.City.ToLower() == city);
        }
        // Даты хранятся как ISO-строки, поэтому сравнение в SQL хронологически корректно.
        if (query.From.HasValue) q = q.Where(t => t.EndDate >= query.From.Value);
        if (query.To.HasValue) q = q.Where(t => t.StartDate <= query.To.Value);

        q = (query.SortBy.ToLowerInvariant(), query.Desc) switch
        {
            ("title", false) => q.OrderBy(t => t.Title),
            ("title", true) => q.OrderByDescending(t => t.Title),
            ("enddate", false) => q.OrderBy(t => t.EndDate),
            ("enddate", true) => q.OrderByDescending(t => t.EndDate),
            ("status", false) => q.OrderBy(t => t.Status).ThenBy(t => t.StartDate),
            ("status", true) => q.OrderByDescending(t => t.Status).ThenBy(t => t.StartDate),
            ("createdat", false) => q.OrderBy(t => t.CreatedAtUtc),
            ("createdat", true) => q.OrderByDescending(t => t.CreatedAtUtc),
            ("startdate", true) => q.OrderByDescending(t => t.StartDate),
            _ => q.OrderBy(t => t.StartDate)
        };

        var total = await q.CountAsync(ct);
        var page = Math.Max(query.Page, 1);
        var size = Math.Clamp(query.PageSize, 1, 100);

        // Активности загружаются отдельным запросом (split query) — без «размножения» строк поездки.
        var trips = await q.Skip((page - 1) * size).Take(size).AsSplitQuery().ToListAsync(ct);

        return new PagedResult<TripListItemDto>(trips.Select(Mappers.ToListItem).ToList(), page, size, total);
    }

    public async Task<TripDetailsDto> GetAsync(Guid id, CancellationToken ct) =>
        Mappers.ToDetails(await LoadAsync(id, ct));

    public async Task<TripDetailsDto> CreateAsync(TripWriteRequest request, CancellationToken ct)
    {
        var trip = new Trip { OwnerId = currentUser.UserId };
        Apply(trip, request);
        trip.Status = TripStatus.Draft; // новая поездка всегда начинается с черновика

        db.Trips.Add(trip);
        await SaveAsync(ct);
        await NotifyAsync(trip, "trip.created", "Поездка создана", $"«{trip.Title}» добавлена в план.", ct);

        return Mappers.ToDetails(trip);
    }

    public async Task<TripDetailsDto> UpdateAsync(Guid id, TripWriteRequest request, CancellationToken ct)
    {
        var trip = await LoadAsync(id, ct);

        // Передаём «ожидаемую» версию в EF: при несовпадении UPDATE затронет 0 строк → 409.
        if (request.Version.HasValue)
            db.Entry(trip).Property(t => t.Version).OriginalValue = request.Version.Value;

        Apply(trip, request);
        trip.ChangeStatus(request.Status);
        trip.Touch();

        await SaveAsync(ct);
        await NotifyAsync(trip, "trip.updated", "Поездка обновлена", $"«{trip.Title}»: изменения сохранены.", ct);

        return Mappers.ToDetails(trip);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var trip = await LoadAsync(id, ct);
        db.Trips.Remove(trip);
        await SaveAsync(ct);
        await NotifyAsync(trip, "trip.deleted", "Поездка удалена", $"«{trip.Title}» удалена.", ct);
    }

    public async Task<TripDetailsDto> AddActivityAsync(Guid tripId, ActivityWriteRequest request, CancellationToken ct)
    {
        var trip = await LoadAsync(tripId, ct);
        var activity = new ActivityItem();
        ApplyActivity(trip, activity, request);

        trip.AddActivity(activity); // домен проверяет даты, статус и назначает порядок
        await SaveAsync(ct);

        if (activity.Status == ActivityStatus.Booked)
            await NotifyAsync(trip, "activity.booked", "Бронирование подтверждено",
                $"«{activity.Title}» — {activity.Cost:N0} {trip.Currency}.", ct);

        return Mappers.ToDetails(trip);
    }

    public async Task<TripDetailsDto> UpdateActivityAsync(Guid tripId, Guid activityId, ActivityWriteRequest request, CancellationToken ct)
    {
        var trip = await LoadAsync(tripId, ct);
        trip.EnsureEditable();

        var activity = trip.GetActivity(activityId);
        var wasBooked = activity.Status == ActivityStatus.Booked;
        ApplyActivity(trip, activity, request);
        trip.Touch();

        await SaveAsync(ct);

        if (!wasBooked && activity.Status == ActivityStatus.Booked)
            await NotifyAsync(trip, "activity.booked", "Бронирование подтверждено", $"«{activity.Title}».", ct);

        return Mappers.ToDetails(trip);
    }

    public async Task<TripDetailsDto> DeleteActivityAsync(Guid tripId, Guid activityId, CancellationToken ct)
    {
        var trip = await LoadAsync(tripId, ct);
        var activity = trip.GetActivity(activityId);
        var title = activity.Title;

        trip.RemoveActivity(activityId);
        await SaveAsync(ct);
        await NotifyAsync(trip, "activity.deleted", "Пункт программы удалён", $"«{title}».", ct);

        return Mappers.ToDetails(trip);
    }

    // ---------------- вспомогательные методы ----------------

    private async Task<Trip> LoadAsync(Guid id, CancellationToken ct) =>
        await Scope().Include(t => t.Activities).FirstOrDefaultAsync(t => t.Id == id, ct)
        ?? throw new NotFoundException("Поездка", id);

    private void Apply(Trip trip, TripWriteRequest r)
    {
        trip.Rename(r.Title);
        trip.City = r.City.Trim();
        trip.Country = r.Country.Trim();
        trip.Description = string.IsNullOrWhiteSpace(r.Description) ? null : r.Description.Trim();
        trip.SetDates(r.StartDate, r.EndDate);
        trip.SetBudget(r.Budget);
        trip.SetCurrency(r.Currency);
        trip.SetTravelers(r.Travelers);

        if (!currencies.SupportedCurrencies.Contains(trip.Currency))
            throw new ValidationException(nameof(r.Currency), $"Валюта {trip.Currency} не поддерживается.");
    }

    private static void ApplyActivity(Trip trip, ActivityItem a, ActivityWriteRequest r)
    {
        trip.EnsureActivityDate(r.Date);
        a.Title = r.Title.Trim();
        a.Category = r.Category;
        a.Status = r.Status;
        a.Date = r.Date;
        a.DurationMinutes = r.DurationMinutes;
        a.Cost = decimal.Round(r.Cost, 2);
        a.Location = string.IsNullOrWhiteSpace(r.Location) ? null : r.Location.Trim();
        a.Notes = string.IsNullOrWhiteSpace(r.Notes) ? null : r.Notes.Trim();
        a.Order = r.Order;
        a.StartTime = ParseTime(r.StartTime);
    }

    private static TimeOnly? ParseTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return TimeOnly.TryParse(value, CultureInfo.InvariantCulture, out var time)
            ? time
            : throw new ValidationException(nameof(ActivityWriteRequest.StartTime), "Ожидается время в формате HH:mm.");
    }

    private async Task SaveAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Конфликт параллельного изменения поездки");
            throw new ConflictException("Данные изменены другим пользователем. Обновите страницу и повторите действие.");
        }
    }

    private Task NotifyAsync(Trip trip, string type, string title, string message, CancellationToken ct) =>
        notifications.NotifyAsync(trip.OwnerId, new NotificationDto(type, title, message, trip.Id, DateTime.UtcNow), ct);
}