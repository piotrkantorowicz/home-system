namespace Shared.Abstractions.Core.Domain;

/// <summary>
/// The caller is authenticated but not allowed to perform this operation (e.g. lacks the
/// required role). Maps to HTTP 403 at the API boundary.
/// </summary>
public sealed class ForbiddenException : Exception
{
    /// <summary>Creates the exception with the reason the caller is not allowed to proceed.</summary>
    /// <param name="message">Why the operation is forbidden, phrased for the end user.</param>
    public ForbiddenException(string message) : base(message) { }
}
