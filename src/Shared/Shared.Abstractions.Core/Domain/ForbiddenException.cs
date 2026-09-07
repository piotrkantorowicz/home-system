namespace Shared.Abstractions.Core.Domain;

/// <summary>
/// The caller is authenticated but not allowed to perform this operation (e.g. lacks the
/// required role). Maps to HTTP 403 at the API boundary.
/// </summary>
public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}
