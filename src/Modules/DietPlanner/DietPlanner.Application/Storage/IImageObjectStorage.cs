namespace DietPlanner.Application.Storage;

/// <summary>Stores private image objects under caller-provided opaque revision keys.</summary>
/// <remarks>
/// Keys are internal names, never filenames, identities, or URLs. The caller must persist a
/// new revision key before writing it; overwriting an existing key is outside this contract.
/// </remarks>
public interface IImageObjectStorage
{
    /// <summary>Writes an object without closing the caller's input stream.</summary>
    /// <param name="key">The opaque, previously persisted revision key.</param>
    /// <param name="content">The caller-owned readable content stream.</param>
    /// <param name="contentType">The stored media type.</param>
    /// <param name="ct">Cancels the storage operation.</param>
    /// <returns>A task that completes when the write succeeds.</returns>
    /// <exception cref="ImageStorageUnavailableException">Storage cannot complete the operation.</exception>
    Task PutAsync(string key, Stream content, string contentType, CancellationToken ct);

    /// <summary>Opens a streaming read, transferring ownership of the successful result to the caller.</summary>
    /// <param name="key">The opaque revision key.</param>
    /// <param name="ct">Cancels the storage operation.</param>
    /// <returns>An owned read handle, or <see langword="null"/> when the object is absent.</returns>
    /// <exception cref="ImageStorageUnavailableException">Storage cannot complete the operation.</exception>
    Task<ImageObjectRead?> OpenReadAsync(string key, CancellationToken ct);

    /// <summary>Deletes an object; an absent object is a successful no-op.</summary>
    /// <param name="key">The opaque revision key.</param>
    /// <param name="ct">Cancels the storage operation.</param>
    /// <returns>A task that completes when deletion succeeds or the object is absent.</returns>
    /// <exception cref="ImageStorageUnavailableException">Storage cannot complete the operation.</exception>
    Task DeleteAsync(string key, CancellationToken ct);
}
