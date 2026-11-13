using Microsoft.EntityFrameworkCore;
using TravelPlanner.Core.Abstractions;
using TravelPlanner.Core.Domain;

namespace TravelPlanner.Infrastructure.Persistence;

/// <summary>Демонстрационные данные. Пароли заданы константами ТОЛЬКО для учебного стенда.</summary>
public static class DbSeeder
{
    public const string AdminEmail = "admin@travel.local";
    public const string AdminPassword = "Admin#12345";
    public const string DemoEmail = "demo@travel.local";
    public const string DemoPassword = "Demo#12345";

    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct)) return;

        var admin = new AppUser
        {
            Email = AdminEmail,
            DisplayName = "Администратор",
            Role = UserRole.Admin,
            PasswordHash = hasher.Hash(AdminPassword)
        };
        var demo = new AppUser
        {
            Email = DemoEmail,
            DisplayName = "Александр",
            Role = UserRole.User,
            PasswordHash = hasher.Hash(DemoPassword)
        };
        db.Users.AddRange(admin, demo);

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        // 1. Ближайшая поездка — через 2 дня: воркер пришлёт напоминание в первую же минуту работы.
        var spb = NewTrip(demo.Id, "Санкт-Петербург: флот и крепости", "Санкт-Петербург", "Россия",
            today.AddDays(2), today.AddDays(6), 60_000m, TripStatus.Booked,
            "Морские прогулки, военно-морские музеи и аутентичная еда. Передвижение — метро и автобусы.",
            ["Александр", "Анна"]);
        Add(spb, "Подорожник и метро", ActivityCategory.Transport, ActivityStatus.Booked, 0, "09:00", 60, 500m, "Метро");
        Add(spb, "Военно-морской музей", ActivityCategory.Museum, ActivityStatus.Booked, 0, "11:00", 150, 900m, "Площадь Труда");
        Add(spb, "Прогулка на катере по Неве", ActivityCategory.SeaTrip, ActivityStatus.Booked, 0, "15:00", 90, 2600m, "Причал у Дворцовой");
        Add(spb, "Петропавловская крепость", ActivityCategory.Museum, ActivityStatus.Planned, 1, "10:30", 180, 700m, "Заячий остров");
        Add(spb, "Музей артиллерии", ActivityCategory.Museum, ActivityStatus.Planned, 1, "14:00", 120, 800m, "Александровский парк");
        Add(spb, "Уличная еда на Рубинштейна", ActivityCategory.Food, ActivityStatus.Planned, 2, "19:00", 120, 2200m, "ул. Рубинштейна");
        Add(spb, "Аренда самокатов по набережным", ActivityCategory.Walk, ActivityStatus.Planned, 3, "12:00", 120, 1200m, "Набережная");

        // 2. План на месяц вперёд.
        var shanghai = NewTrip(demo.Id, "Шанхай: уличная еда и набережные", "Шанхай", "Китай",
            today.AddDays(30), today.AddDays(37), 180_000m, TripStatus.Planned,
            "Гастрономический маршрут: сяолунбао, шэнцзяньбао, ночные рынки.", ["Александр", "Анна"]);
        Add(shanghai, "Маглев и метро", ActivityCategory.Transport, ActivityStatus.Planned, 0, "10:00", 90, 1500m, "Pudong");
        Add(shanghai, "Сяолунбао в старом городе", ActivityCategory.Food, ActivityStatus.Planned, 0, "13:00", 90, 1800m, "Yuyuan");
        Add(shanghai, "Набережная Вайтань", ActivityCategory.Walk, ActivityStatus.Planned, 1, "19:00", 120, 0m, "The Bund");
        Add(shanghai, "Катер по Хуанпу", ActivityCategory.SeaTrip, ActivityStatus.Planned, 2, "20:00", 60, 2400m, "Shiliupu");

        // 3. Завершённая поездка — для статистики дашборда.
        var sochi = NewTrip(demo.Id, "Сочи: экстрим-выходные", "Сочи", "Россия",
            today.AddDays(-40), today.AddDays(-35), 45_000m, TripStatus.Completed,
            "Джиппинг, скайпарк и море.", ["Александр", "Анна"]);
        Add(sochi, "Джиппинг в горах", ActivityCategory.Extreme, ActivityStatus.Done, 0, "09:00", 300, 6500m, "Красная Поляна");
        Add(sochi, "Скайпарк: банджи", ActivityCategory.Extreme, ActivityStatus.Done, 1, "12:00", 180, 12000m, "Казачий брод");
        Add(sochi, "Морская прогулка", ActivityCategory.SeaTrip, ActivityStatus.Done, 2, "16:00", 120, 3000m, "Морпорт");
        Add(sochi, "Хинкальная", ActivityCategory.Food, ActivityStatus.Done, 2, "20:00", 90, 1800m, "Адлер");

        db.Trips.AddRange(spb, shanghai, sochi);
        await db.SaveChangesAsync(ct);
    }

    private static Trip NewTrip(Guid ownerId, string title, string city, string country,
        DateOnly start, DateOnly end, decimal budget, TripStatus status, string description, List<string> travelers)
    {
        var trip = new Trip { OwnerId = ownerId, City = city, Country = country, Description = description };
        trip.Rename(title);
        trip.SetDates(start, end);
        trip.SetBudget(budget);
        trip.SetCurrency("RUB");
        trip.SetTravelers(travelers);
        trip.Status = status; // при сидировании правила переходов не применяются
        return trip;
    }

    private static void Add(Trip trip, string title, ActivityCategory category, ActivityStatus status,
        int dayOffset, string startTime, int minutes, decimal cost, string location)
    {
        trip.Activities.Add(new ActivityItem
        {
            TripId = trip.Id,
            Title = title,
            Category = category,
            Status = status,
            Date = trip.StartDate.AddDays(dayOffset),
            StartTime = TimeOnly.Parse(startTime),
            DurationMinutes = minutes,
            Cost = cost,
            Location = location,
            Order = trip.Activities.Count(a => a.Date == trip.StartDate.AddDays(dayOffset)) + 1
        });
    }
}