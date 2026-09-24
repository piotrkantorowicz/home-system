namespace Shared.Infrastructure.Web;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Maps the exceptions defined in <c>Shared.Abstractions</c> to <c>application/problem+json</c> responses in one
/// place: <see cref="CommandValidationException"/> → 400 (with an <c>errors</c> extension grouped by property),
/// <see cref="ForbiddenException"/> → 403, <see cref="NotFoundException"/> → 404, <see cref="DomainException"/> → 422,
/// <see cref="OperationCanceledException"/> → 499 (empty body), anything else → 500 with no exception detail.
/// Endpoints and handlers never build problem details themselves.
/// </summary>
/// <remarks>
/// Registered through <c>AddExceptionHandler&lt;ApplicationExceptionHandler&gt;()</c> and invoked by the framework's
/// exception handler middleware, which also owns logging: pass <see cref="ShouldSuppressDiagnostics"/> as the
/// <see cref="Microsoft.AspNetCore.Builder.ExceptionHandlerOptions.SuppressDiagnosticsCallback"/> so expected exceptions stay quiet and unhandled
/// ones are logged exactly once.
/// </remarks>
/// <param name="problemDetails">Writes the response body, adding the <c>traceId</c> and RFC 9457 defaults.</param>
public sealed class ApplicationExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    /// <summary>Client Closed Request (nginx convention) — the client is gone, nobody reads a body.</summary>
    public const int ClientClosedRequestStatusCode = 499;

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is OperationCanceledException)
        {
            // The middleware already answers 499 when the request itself was aborted; this covers a cancellation
            // raised while the client is still connected (a linked token, an internal timeout) the same way.
            httpContext.Response.StatusCode = ClientClosedRequestStatusCode;
            return true;
        }

        var problem = Map(exception);
        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        // The status code is the contract. If no writer accepts the request's Accept header the body is skipped,
        // but the exception is still handled — falling through would turn a mapped 4xx into a rethrown 500.
        await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem,
        });

        return true;
    }

    /// <summary>
    /// Tells the exception handler middleware which exceptions to keep out of the log and the error metrics:
    /// every exception this handler maps to a 4xx/499 is an expected outcome, not a fault.
    /// </summary>
    /// <param name="context">The middleware's view of the handled exception.</param>
    /// <returns><see langword="true"/> to suppress diagnostics for an expected exception.</returns>
    public static bool ShouldSuppressDiagnostics(ExceptionHandlerSuppressDiagnosticsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return IsExpected(context.Exception);
    }

    private static bool IsExpected(Exception exception)
        => exception is CommandValidationException
            or ForbiddenException
            or NotFoundException
            or DomainException
            or OperationCanceledException;

    private static ProblemDetails Map(Exception exception)
        => exception switch
        {
            CommandValidationException validation => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Extensions = { ["errors"] = GroupErrors(validation) },
            },
            ForbiddenException => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = exception.Message,
            },
            NotFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not found",
                Detail = exception.Message,
            },
            DomainException => new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Business rule violation",
                Detail = exception.Message,
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal server error",
            },
        };

    /// <summary>Same shape as <c>ValidationProblemDetails.Errors</c>: property name → messages.</summary>
    private static Dictionary<string, string[]> GroupErrors(CommandValidationException exception)
        => exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
}
