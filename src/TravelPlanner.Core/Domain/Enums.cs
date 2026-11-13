namespace TravelPlanner.Core.Domain;

/// <summary>Жизненный цикл поездки. Переходы ограничены методом Trip.ChangeStatus.</summary>
public enum TripStatus
{
    Draft = 0,        // черновик: идея поездки
    Planned = 1,      // программа составлена
    Booked = 2,       // ключевые позиции забронированы
    InProgress = 3,   // поездка идёт сейчас
    Completed = 4,    // завершена (программа только для чтения)
    Cancelled = 5     // отменена
}

/// <summary>Категория активности — используется для группировки расходов.</summary>
public enum ActivityCategory
{
    Transport = 0,
    Accommodation = 1,
    Food = 2,
    Museum = 3,
    SeaTrip = 4,
    Extreme = 5,
    Walk = 6,
    Shopping = 7,
    Other = 8
}

public enum ActivityStatus
{
    Planned = 0,
    Booked = 1,
    Done = 2,
    Cancelled = 3
}

public enum UserRole
{
    User = 0,
    Admin = 1
}
