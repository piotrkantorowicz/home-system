namespace DietPlanner.Infrastructure.Storage;

using DietPlanner.Application.Storage;

/// <summary>Reports sanitized storage failures while optional image storage is disabled.</summary>
public sealed class UnavailableImageObjectStorage : IImageObjectStorage
{
    /// <inheritdoc/>
    public async Task PutAsync(string key, Stream content, string contentType, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ImageObjectStorageValidation.ValidateKey(key);
        ImageObjectStorageValidation.ValidateContent(content, contentType);
        await Task.FromException(new ImageStorageUnavailableException());
    }

    /// <inheritdoc/>
    public async Task<ImageObjectRead?> OpenReadAsync(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ImageObjectStorageValidation.ValidateKey(key);
        return await Task.FromException<ImageObjectRead?>(new ImageStorageUnavailableException());
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ImageObjectStorageValidation.ValidateKey(key);
        await Task.FromException(new ImageStorageUnavailableException());
    }
}
