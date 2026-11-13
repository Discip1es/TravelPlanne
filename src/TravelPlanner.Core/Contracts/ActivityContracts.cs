using System.ComponentModel.DataAnnotations;
using TravelPlanner.Core.Domain;

namespace TravelPlanner.Core.Contracts;

/// <remarks>StartTime передаётся строкой «HH:mm» — это формат HTML-элемента input[type=time].</remarks>
public sealed record ActivityDto(
    Guid Id, string Title, string Category, string Status,
    DateOnly Date, string? StartTime, int DurationMinutes,
    decimal Cost, string? Location, string? Notes, int Order);

public sealed record ActivityWriteRequest
{
    [Required, StringLength(160, MinimumLength = 2)] public string Title { get; init; } = string.Empty;
    public ActivityCategory Category { get; init; } = ActivityCategory.Other;
    public ActivityStatus Status { get; init; } = ActivityStatus.Planned;

    [Required] public DateOnly Date { get; init; }
    public string? StartTime { get; init; }
    [Range(5, 1440)] public int DurationMinutes { get; init; } = 60;
    [Range(0, 100_000_000)] public decimal Cost { get; init; }

    [StringLength(200)] public string? Location { get; init; }
    [StringLength(1000)] public string? Notes { get; init; }
    [Range(0, 1000)] public int Order { get; init; }
}