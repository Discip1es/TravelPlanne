namespace TravelPlanner.Core.Domain;

/// <summary>Базовое доменное исключение. Middleware переводит наследников в HTTP-коды.</summary>
public abstract class DomainException(string message) : Exception(message);

public sealed class NotFoundException(string entity, object key)
    : DomainException($"{entity} «{key}» не найден.");

public sealed class ConflictException(string message) : DomainException(message);

public sealed class ForbiddenException(string message = "Доступ к ресурсу запрещён.") : DomainException(message);

public sealed class AuthenticationException(string message = "Неверные учётные данные.") : DomainException(message);

/// <summary>Ошибка валидации с картой «поле → сообщения» (совместимо с ValidationProblemDetails).</summary>
public sealed class ValidationException : DomainException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string field, string message) : base("Ошибка валидации данных.")
        => Errors = new Dictionary<string, string[]> { [field] = [message] };

    public ValidationException(IReadOnlyDictionary<string, string[]> errors) : base("Ошибка валидации данных.")
        => Errors = errors;
}