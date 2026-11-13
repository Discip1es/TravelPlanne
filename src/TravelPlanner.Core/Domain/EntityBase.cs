namespace TravelPlanner.Core.Domain;

/// <summary>Базовая сущность: идентификатор и момент создания (UTC).</summary>
public abstract class EntityBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}