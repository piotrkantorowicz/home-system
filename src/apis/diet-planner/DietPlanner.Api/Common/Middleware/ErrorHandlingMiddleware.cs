using DietPlanner.Api.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace DietPlanner.Api.Common.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, details) = exception switch
        {
            NotFoundException notFound => (
                HttpStatusCode.NotFound,
                notFound.Message,
                (object?)null
            ),

            ForbiddenException forbidden => (
                HttpStatusCode.Forbidden,
                forbidden.Message,
                (object?)null
            ),

            ValidationException validation => (
                HttpStatusCode.BadRequest,
                validation.Message,
                (object?)validation.Errors
            ),

            ImportException import => (
                HttpStatusCode.UnprocessableEntity,
                import.Message,
                (object?)null
            ),

            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                "Unauthorized access",
                (object?)null
            ),

            DbUpdateException dbUpdate => (
                HttpStatusCode.Conflict,
                "A database error occurred. The operation could not be completed.",
                _environment.IsDevelopment() ? (object?)dbUpdate.InnerException?.Message : null
            ),

            _ => (
                HttpStatusCode.InternalServerError,
                _environment.IsDevelopment()
                    ? exception.Message
                    : "An error occurred while processing your request",
                _environment.IsDevelopment() ? (object?)exception.StackTrace : null
            )
        };

        // Log the exception
        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception occurred: {Message}", exception.Message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            error = message,
            statusCode = (int)statusCode,
            traceId = context.TraceIdentifier,
            details
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}
