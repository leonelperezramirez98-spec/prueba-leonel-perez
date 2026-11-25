using System.Text.Json;
using MaintenanceSystem.Domain.Exceptions;

namespace MaintenanceSystem.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Regla de negocio incumplida ({StatusCode}): {Message}", ex.StatusCode, ex.Message);
            await WriteErrorAsync(context, ex.StatusCode, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado procesando la solicitud.");
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "Ocurrió un error interno en el servidor.");
        }
    }

    private static Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var payload = JsonSerializer.Serialize(new
        {
            status = statusCode,
            error = message,
            timestamp = DateTime.UtcNow
        });

        return context.Response.WriteAsync(payload);
    }
}