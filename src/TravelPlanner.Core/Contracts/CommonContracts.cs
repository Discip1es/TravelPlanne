using System.ComponentModel.DataAnnotations;
using TravelPlanner.Core.Domain;

namespace TravelPlanner.Core.Contracts;

/// <summary>Универсальный результат постраничной выборки.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)Math.Max(PageSize, 1));
}

/// <summary>Параметры списка поездок (биндятся из query string).</summary>
public sealed record TripQuery
{
    public string? Search { get; init; }
    public TripStatus? Status { get; init; }
    public string? City { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }

    /// <summary>startDate | endDate | title | status | createdAt.</summary>
    public string SortBy { get; init; } = "startDate";
    public bool Desc { get; init; }

    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 10;
}

public sealed record NameValueDto(string Name, decimal Value);

public sealed record DashboardDto(
    string BaseCurrency,
    int TotalTrips,
    int UpcomingTrips,
    int InProgressTrips,
    int CompletedTrips,
    decimal TotalBudget,
    decimal TotalPlannedCost,
    decimal TotalBookedCost,
    IReadOnlyList<NameValueDto> TripsByStatus,
    IReadOnlyList<NameValueDto> CostByCategory,
    IReadOnlyList<TripListItemDto> NextTrips);

/// <summary>Сообщение, рассылаемое клиентам через SignalR.</summary>
public sealed record NotificationDto(string Type, string Title, string Message, Guid? TripId, DateTime CreatedAtUtc);

public sealed record EnumItemDto(string Name, int Value);

public sealed record MetaDto(
    IReadOnlyList<EnumItemDto> TripStatuses,
    IReadOnlyList<EnumItemDto> ActivityCategories,
    IReadOnlyList<EnumItemDto> ActivityStatuses,
    IReadOnlyList<string> Currencies);

public sealed record ConversionDto(decimal Amount, string From, string To, decimal Rate, decimal Converted);