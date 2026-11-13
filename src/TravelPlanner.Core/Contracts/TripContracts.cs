using System.ComponentModel.DataAnnotations;
using TravelPlanner.Core.Domain;

namespace TravelPlanner.Core.Contracts;

public sealed record TripListItemDto(
    Guid Id, string Title, string City, string Country,
    DateOnly StartDate, DateOnly EndDate, string Status,
    decimal Budget, string Currency, decimal PlannedCost, decimal Remaining, decimal BudgetUsagePercent,
    int DurationDays, int ActivitiesCount, int TravelersCount);

public sealed record TripDetailsDto(
    Guid Id, string Title, string City, string Country, string? Description,
    DateOnly StartDate, DateOnly EndDate, string Status,
    decimal Budget, string Currency, decimal PlannedCost, decimal BookedCost, decimal Remaining, decimal BudgetUsagePercent,
    int DurationDays,
    IReadOnlyList<string> Travelers,
    IReadOnlyList<ActivityDto> Activities,
    IReadOnlyList<NameValueDto> CostByCategory,
    Guid Version, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

/// <summary>Единая модель записи для POST и PUT (Status и Version учитываются только при PUT).</summary>
public sealed record TripWriteRequest
{
    [Required, StringLength(120, MinimumLength = 3)] public string Title { get; init; } = string.Empty;
    [Required, StringLength(80)] public string City { get; init; } = string.Empty;
    [Required, StringLength(80)] public string Country { get; init; } = string.Empty;
    [StringLength(2000)] public string? Description { get; init; }

    [Required] public DateOnly StartDate { get; init; }
    [Required] public DateOnly EndDate { get; init; }

    [Range(0, 100_000_000)] public decimal Budget { get; init; }
    [Required, StringLength(3, MinimumLength = 3)] public string Currency { get; init; } = "RUB";

    public TripStatus Status { get; init; } = TripStatus.Draft;
    public List<string> Travelers { get; init; } = [];

    /// <summary>Версия, полученная клиентом при чтении. Несовпадение → HTTP 409.</summary>
    public Guid? Version { get; init; }
}