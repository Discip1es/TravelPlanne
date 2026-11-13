using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Core.Domain;

namespace TravelPlanner.Api.Middleware;

/// <summary>Единая точка перевода исключений в RFC 7807 (application/problem+json).</summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        // Если ответ уже начал отправляться (например, стриминг), менять статус нельзя.
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            await WriteProblemAsync(context, exception);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var (status, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Ошибка валидации"),
            AuthenticationException => (StatusCodes.Status401Unauthorized, "Ошибка аутентификации"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Доступ запрещён"),
            NotFoundException => (StatusCodes.Status404NotFound, "Ресурс не найден"),
            ConflictException => (StatusCodes.Status409Conflict, "Конфликт данных"),
            _ => (StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Необработанное исключение на {Path}", context.Request.Path);
        else
            logger.LogWarning("Бизнес-ошибка {Status} на {Path}: {Message}", status, context.Request.Path, exception.Message);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        if (exception is ValidationException validation)
            problem.Extensions["errors"] = validation.Errors;

        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    }
}