using TravelPlanner.Core.Domain;
using Xunit;

namespace TravelPlanner.Tests;

/// <summary>Бизнес-правила домена проверяются без БД и без HTTP.</summary>
public class DomainTests
{
    private static Trip NewTrip()
    {
        var trip = new Trip { City = "Санкт-Петербург", Country = "Россия" };
        trip.Rename("Флот и крепости");
        trip.SetDates(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 7));
        trip.SetBudget(50_000m);
        trip.SetCurrency("rub");
        return trip;
    }

    [Fact]
    public void DurationDays_IncludesBothEndpoints() => Assert.Equal(7, NewTrip().DurationDays);

    [Fact]
    public void SetCurrency_NormalizesToUpperCase() => Assert.Equal("RUB", NewTrip().Currency);

    [Fact]
    public void SetDates_Throws_WhenEndBeforeStart()
    {
        var trip = NewTrip();
        Assert.Throws<ValidationException>(() => trip.SetDates(new DateOnly(2026, 6, 7), new DateOnly(2026, 6, 1)));
    }

    [Fact]
    public void PlannedCost_ExcludesCancelledActivities()
    {
        var trip = NewTrip();
        trip.AddActivity(new ActivityItem { Title = "Катер", Date = new DateOnly(2026, 6, 2), Cost = 2600m, Status = ActivityStatus.Booked });
        trip.AddActivity(new ActivityItem { Title = "Отменённый тур", Date = new DateOnly(2026, 6, 3), Cost = 5000m, Status = ActivityStatus.Cancelled });

        Assert.Equal(2600m, trip.PlannedCost);
        Assert.Equal(2600m, trip.BookedCost);
        Assert.Equal(47_400m, trip.Remaining);
    }

    [Fact]
    public void AddActivity_AssignsSequentialOrder_AndRejectsForeignDate()
    {
        var trip = NewTrip();
        var first = trip.AddActivity(new ActivityItem { Title = "Музей", Date = new DateOnly(2026, 6, 1) });
        var second = trip.AddActivity(new ActivityItem { Title = "Крепость", Date = new DateOnly(2026, 6, 2) });

        Assert.Equal(1, first.Order);
        Assert.Equal(2, second.Order);
        Assert.Throws<ValidationException>(() => trip.AddActivity(new ActivityItem { Title = "Вне дат", Date = new DateOnly(2026, 7, 1) }));
    }

    [Fact]
    public void ChangeStatus_RejectsInvalidTransition()
    {
        var trip = NewTrip();
        Assert.Throws<ConflictException>(() => trip.ChangeStatus(TripStatus.Completed));
        trip.ChangeStatus(TripStatus.Planned);
        trip.ChangeStatus(TripStatus.Booked);
        Assert.Equal(TripStatus.Booked, trip.Status);
    }

    [Fact]
    public void CompletedTrip_IsReadOnly()
    {
        var trip = NewTrip();
        trip.ChangeStatus(TripStatus.Planned);
        trip.ChangeStatus(TripStatus.InProgress);
        trip.ChangeStatus(TripStatus.Completed);

        Assert.Throws<ConflictException>(() => trip.AddActivity(new ActivityItem { Title = "Поздняя правка", Date = new DateOnly(2026, 6, 2) }));
    }

    [Fact]
    public void SetDates_RejectsShrinking_WhenActivitiesOutsideRange()
    {
        var trip = NewTrip();
        trip.AddActivity(new ActivityItem { Title = "Последний день", Date = new DateOnly(2026, 6, 7) });
        Assert.Throws<ValidationException>(() => trip.SetDates(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 5)));
    }

    [Fact]
    public void Touch_ChangesVersion()
    {
        var trip = NewTrip();
        var before = trip.Version;
        trip.Touch();
        Assert.NotEqual(before, trip.Version);
    }
}