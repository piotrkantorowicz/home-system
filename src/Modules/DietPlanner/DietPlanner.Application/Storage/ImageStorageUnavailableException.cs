namespace DietPlanner.Application.Storage;

using Shared.Abstractions.Core.Domain;

/// <summary>A sanitized storage failure that carries no provider details or object keys.</summary>
public sealed class ImageStorageUnavailableException()
    : ServiceUnavailableException("Recipe image storage is unavailable.");
