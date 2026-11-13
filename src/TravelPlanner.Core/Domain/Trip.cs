namespace TravelPlanner.Core.Domain;

/// <summary>
/// Агрегат «Поездка». Все бизнес-правила сосредоточены здесь,
/// поэтому они проверяются одинаково и из HTTP-запроса, и из фонового воркера.
/// </summary>
public class Trip : EntityBase
{
    public Guid OwnerId { get; set; }
    public AppUser? Owner { get; set; }

    public string Title { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public TripStatus Status { get; set; } = TripStatus.Draft;
    public decimal Budget { get; set; }
    public string Currency { get; set; } = "RUB";

    /// <summary>Попутчики. EF Core 8+ хранит коллекцию примитивов в JSON-колонке.</summary>
    public List<string> Travelers { get; set; } = [];

    public List<ActivityItem> Activities { get; set; } = [];

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReminderSentUtc { get; set; }

    /// <summary>Токен оптимистичной конкуренции: меняется при каждой записи.</summary>
    public Guid Version { get; set; } = Guid.NewGuid();

    // ---------- Вычисляемые показатели (в БД не хранятся) ----------
    public int DurationDays => EndDate.DayNumber - StartDate.DayNumber + 1;
    public decimal PlannedCost => Activities.Where(a => a.Status != ActivityStatus.Cancelled).Sum(a => a.Cost);
    public decimal BookedCost => Activities.Where(a => a.Status is ActivityStatus.Booked or ActivityStatus.Done).Sum(a => a.Cost);
    public decimal Remaining => Budget - PlannedCost;
    public decimal BudgetUsagePercent => Budget <= 0 ? 0 : Math.Round(PlannedCost / Budget * 100, 1);

    // ---------- Разрешённые переходы статусов ----------
    private static readonly Dictionary<TripStatus, TripStatus[]> Transitions = new()
    {
        [TripStatus.Draft] = [TripStatus.Planned, TripStatus.Cancelled],
        [TripStatus.Planned] = [TripStatus.Booked, TripStatus.InProgress, TripStatus.Draft, TripStatus.Cancelled],
        [TripStatus.Booked] = [TripStatus.InProgress, TripStatus.Planned, TripStatus.Cancelled],
        [TripStatus.InProgress] = [TripStatus.Completed, TripStatus.Cancelled],
        [TripStatus.Completed] = [],
        [TripStatus.Cancelled] = [TripStatus.Draft]
    };

    public void Rename(string title)
    {
        title = (title ?? string.Empty).Trim();
        if (title.Length is < 3 or > 120)
            throw new ValidationException(nameof(Title), "Название поездки: от 3 до 120 символов.");
        Title = title;
    }

    public void SetDates(DateOnly start, DateOnly end)
    {
        if (end < start)
            throw new ValidationException(nameof(EndDate), "Дата окончания не может быть раньше даты начала.");
        if (end.DayNumber - start.DayNumber > 365)
            throw new ValidationException(nameof(EndDate), "Максимальная длительность поездки — 366 дней.");
        if (Activities.Any(a => a.Date < start || a.Date > end))
            throw new ValidationException(nameof(StartDate),
                "В программе есть активности вне нового диапазона дат. Сначала перенесите их.");

        StartDate = start;
        EndDate = end;
    }

    public void SetBudget(decimal budget)
    {
        if (budget < 0)
            throw new ValidationException(nameof(Budget), "Бюджет не может быть отрицательным.");
        Budget = decimal.Round(budget, 2);
    }

    public void SetCurrency(string currency)
    {
        currency = (currency ?? string.Empty).Trim().ToUpperInvariant();
        if (currency.Length != 3 || currency.Any(c => c is < 'A' or > 'Z'))
            throw new ValidationException(nameof(Currency), "Валюта задаётся трёхбуквенным кодом ISO 4217 (например, RUB).");
        Currency = currency;
    }

    public void SetTravelers(IEnumerable<string>? travelers)
    {
        var list = (travelers ?? [])
            .Select(t => (t ?? string.Empty).Trim())
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(13)
            .ToList();

        if (list.Any(t => t.Length > 64))
            throw new ValidationException(nameof(Travelers), "Имя попутчика не длиннее 64 символов.");
        if (list.Count > 12)
            throw new ValidationException(nameof(Travelers), "Не более 12 попутчиков.");

        Travelers = list;
    }

    public void ChangeStatus(TripStatus next)
    {
        if (Status == next) return;
        if (!Transitions[Status].Contains(next))
            throw new ConflictException($"Недопустимый переход статуса: {Status} → {next}.");
        Status = next;
    }

    /// <summary>Завершённые и отменённые поездки доступны только для чтения.</summary>
    public void EnsureEditable()
    {
        if (Status is TripStatus.Completed or TripStatus.Cancelled)
            throw new ConflictException("Программа завершённой или отменённой поездки не редактируется.");
    }

    public void EnsureActivityDate(DateOnly date)
    {
        if (date < StartDate || date > EndDate)
            throw new ValidationException("date", "Дата активности должна попадать в диапазон дат поездки.");
    }

    public ActivityItem AddActivity(ActivityItem item)
    {
        EnsureEditable();
        EnsureActivityDate(item.Date);
        if (item.Order <= 0)
            item.Order = Activities.Count == 0 ? 1 : Activities.Max(a => a.Order) + 1;
        item.TripId = Id;
        Activities.Add(item);
        Touch();
        return item;
    }

    public ActivityItem GetActivity(Guid activityId) =>
        Activities.FirstOrDefault(a => a.Id == activityId)
        ?? throw new NotFoundException("Активность", activityId);

    public void RemoveActivity(Guid activityId)
    {
        EnsureEditable();
        Activities.Remove(GetActivity(activityId));
        Touch();
    }

    /// <summary>Фиксация изменения агрегата: время обновления + новая версия для concurrency-контроля.</summary>
    public void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        Version = Guid.NewGuid();
    }
}