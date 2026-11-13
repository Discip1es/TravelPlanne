using TravelPlanner.Core.Contracts;

namespace TravelPlanner.Core.Abstractions;

public interface ITripService
{
    Task<PagedResult<TripListItemDto>> GetPagedAsync(TripQuery query, CancellationToken ct);
    Task<TripDetailsDto> GetAsync(Guid id, CancellationToken ct);
    Task<TripDetailsDto> CreateAsync(TripWriteRequest request, CancellationToken ct);
    Task<TripDetailsDto> UpdateAsync(Guid id, TripWriteRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);

    Task<TripDetailsDto> AddActivityAsync(Guid tripId, ActivityWriteRequest request, CancellationToken ct);
    Task<TripDetailsDto> UpdateActivityAsync(Guid tripId, Guid activityId, ActivityWriteRequest request, CancellationToken ct);
    Task<TripDetailsDto> DeleteActivityAsync(Guid tripId, Guid activityId, CancellationToken ct);
}

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(CancellationToken ct);
}

public interface ICurrencyService
{
    IReadOnlyCollection<string> SupportedCurrencies { get; }
    Task<IReadOnlyDictionary<string, decimal>> GetRatesAsync(string baseCurrency, CancellationToken ct);
    Task<decimal> ConvertAsync(decimal amount, string from, string to, CancellationToken ct);
}

/// <summary>Транспорт уведомлений (реализация — SignalR).</summary>
public interface INotificationService
{
    Task NotifyAsync(Guid userId, NotificationDto notification, CancellationToken ct);
}