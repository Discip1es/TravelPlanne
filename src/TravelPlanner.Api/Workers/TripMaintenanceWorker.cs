using Microsoft.EntityFrameworkCore;
using TravelPlanner.Core.Abstractions;
using TravelPlanner.Core.Contracts;
using TravelPlanner.Core.Domain;
using TravelPlanner.Infrastructure.Persistence;

namespace TravelPlanner.Api.Workers;

/// <summary>
/// Фоновая служба: раз в минуту
/// 1) переводит поездки в InProgress/Completed по календарю;
/// 2) отправляет напоминание о поездке, начинающейся в течение 3 дней (однократно).
/// </summary>
public sealed class TripMaintenanceWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<TripMaintenanceWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        try
        {
            await TickAsync(stoppingToken); // первый прогон сразу после старта
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await TickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // штатная остановка приложения
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var horizon = today.AddDays(3);

            // Кандидаты отбираются по статусу в SQL, даты сравниваются в памяти — набор небольшой.
            var candidates = await db.Trips
                .Where(t => t.Status == TripStatus.Planned || t.Status == TripStatus.Booked || t.Status == TripStatus.InProgress)
                .ToListAsync(ct);

            foreach (var trip in candidates)
            {
                if (trip.Status is TripStatus.Planned or TripStatus.Booked
                    && trip.StartDate <= today && trip.EndDate >= today)
                {
                    trip.ChangeStatus(TripStatus.InProgress);
                    trip.Touch();
                    await notifications.NotifyAsync(trip.OwnerId,
                        new NotificationDto("trip.started", "Поездка началась", $"«{trip.Title}» — хорошего пути!", trip.Id, DateTime.UtcNow), ct);
                }
                else if (trip.Status == TripStatus.InProgress && trip.EndDate < today)
                {
                    trip.ChangeStatus(TripStatus.Completed);
                    trip.Touch();
                    await notifications.NotifyAsync(trip.OwnerId,
                        new NotificationDto("trip.completed", "Поездка завершена", $"«{trip.Title}» перенесена в архив.", trip.Id, DateTime.UtcNow), ct);
                }
                else if (trip.Status is TripStatus.Planned or TripStatus.Booked
                         && trip.StartDate > today && trip.StartDate <= horizon && trip.ReminderSentUtc is null)
                {
                    trip.ReminderSentUtc = DateTime.UtcNow;
                    trip.Touch();
                    await notifications.NotifyAsync(trip.OwnerId,
                        new NotificationDto("trip.reminder", "Скоро в путь",
                            $"«{trip.Title}» начинается {trip.StartDate:dd.MM.yyyy}. Проверьте бронирования.", trip.Id, DateTime.UtcNow), ct);
                }
            }

            var changed = await db.SaveChangesAsync(ct);
            if (changed > 0) logger.LogInformation("Воркер обслуживания обновил записей: {Count}", changed);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Ошибка одной итерации не должна останавливать службу.
            logger.LogError(ex, "Ошибка итерации TripMaintenanceWorker");
        }
    }
}