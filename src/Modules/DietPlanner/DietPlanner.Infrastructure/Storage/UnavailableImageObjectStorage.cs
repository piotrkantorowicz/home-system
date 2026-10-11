namespace DietPlanner.Infrastructure.Storage;

using DietPlanner.Application.Storage;

/// <summary>Reports sanitized storage failures while optional image storage is disabled.</summary>
public sealed class UnavailableImageObjectStorage : IImageObjectStorage
{
    /// <inheritdoc/>
    public Task PutAsync(string key, Stream content, string contentType, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        throw new ImageStorageUnavailableException();
    }

    /// <inheritdoc/>
    public Task<ImageObjectRead?> OpenReadAsync(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        throw new ImageStorageUnavailableException();
    }

    /// <inheritdoc/>
    public Task DeleteAsync(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        throw new ImageStorageUnavailableException();
    }
}
