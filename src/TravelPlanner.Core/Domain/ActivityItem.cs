namespace TravelPlanner.Core.Domain;

/// <summary>Пункт программы поездки (экскурсия, переезд, ужин, морская прогулка и т. п.).</summary>
public class ActivityItem : EntityBase
{
    public Guid TripId { get; set; }
    public Trip? Trip { get; set; }

    public string Title { get; set; } = string.Empty;
    public ActivityCategory Category { get; set; } = ActivityCategory.Other;
    public ActivityStatus Status { get; set; } = ActivityStatus.Planned;

    public DateOnly Date { get; set; }
    public TimeOnly? StartTime { get; set; }
    public int DurationMinutes { get; set; } = 60;

    public decimal Cost { get; set; }          // в валюте поездки
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public int Order { get; set; }             // порядок внутри дня
}