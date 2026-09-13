namespace Shared.Abstractions.Core.Domain;

/// <summary>
/// The resource a command or query addressed does not exist (or is not visible to the caller).
/// Thrown from application handlers; the API boundary maps it to HTTP 404.
/// </summary>
public sealed class NotFoundException : Exception
{
    /// <summary>Creates the exception with the standard "&lt;entity&gt; with id '&lt;id&gt;' was not found." message.</summary>
    /// <param name="entityName">The business name of the missing resource, e.g. <c>"Product"</c>.</param>
    /// <param name="id">The identifier that was looked up.</param>
    public NotFoundException(string entityName, object id)
        : base($"{entityName} with id '{id}' was not found.") { }

    /// <summary>Creates the exception with a custom message for lookups that are not by id.</summary>
    /// <param name="message">What was looked up and not found, phrased for the end user.</param>
    public NotFoundException(string message)
        : base(message) { }
}
