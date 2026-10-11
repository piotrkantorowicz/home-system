namespace DietPlanner.IntegrationTests.Images;

using DietPlanner.Application.Storage;

internal sealed class PhotoTestStorage(IImageObjectStorage inner) : IImageObjectStorage
{
    internal Func<string, CancellationToken, Task>? BeforePut { get; set; }
    internal bool FailDelete { get; set; }

    public async Task PutAsync(string key, Stream content, string contentType, CancellationToken ct)
    {
        if (BeforePut is not null) await BeforePut(key, ct);
        await inner.PutAsync(key, content, contentType, ct);
    }

    public Task<ImageObjectRead?> OpenReadAsync(string key, CancellationToken ct) => inner.OpenReadAsync(key, ct);

    public Task DeleteAsync(string key, CancellationToken ct)
        => FailDelete ? throw new ImageStorageUnavailableException() : inner.DeleteAsync(key, ct);
}
