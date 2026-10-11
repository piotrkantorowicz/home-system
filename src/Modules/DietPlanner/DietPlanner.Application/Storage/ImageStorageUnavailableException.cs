namespace DietPlanner.Application.Storage;

/// <summary>A sanitized storage failure that carries no provider details or object keys.</summary>
public sealed class ImageStorageUnavailableException()
    : Exception("Recipe image storage is unavailable.");
