namespace Shared.Abstractions.Core.Domain;

/// <summary>
/// The request is valid but clashes with the current state of the resource (e.g. a different
/// currency for an already-initialised budget). Maps to HTTP 409 at the API boundary.
/// </summary>
public sealed class ConflictException : Exception
{
    /// <summary>Creates the exception with the conflict, phrased for the end user.</summary>
    /// <param name="message">What clashes with the current state.</param>
    public ConflictException(string message) : base(message) { }
}
