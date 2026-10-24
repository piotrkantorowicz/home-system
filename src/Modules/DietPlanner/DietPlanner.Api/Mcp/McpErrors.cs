namespace DietPlanner.Api.Mcp;

using ModelContextProtocol;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Turns expected application failures into tool errors the agent can act on. Anything else stays
/// an unexpected error, which the SDK reports without its message.
/// </summary>
internal static class McpErrors
{
    public static async Task<T> AsToolErrorsAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (CommandValidationException ex)
        {
            throw new McpException(string.Join(" ", ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));
        }
        catch (Exception ex) when (ex is DomainException or ConflictException or ForbiddenException or NotFoundException)
        {
            throw new McpException(ex.Message);
        }
    }
}
