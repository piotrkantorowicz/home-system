namespace DietPlanner.Infrastructure.Storage;

internal static class ImageObjectStorageValidation
{
    internal static void ValidateKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (key.StartsWith('/') || Uri.TryCreate(key, UriKind.Absolute, out _)
            || key.Split('/').Any(segment => segment is "." or ".."))
        {
            throw new ArgumentException("Image keys must be relative internal object names without dot segments.", nameof(key));
        }
    }

    internal static void ValidateContent(Stream content, string contentType)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        if (!content.CanRead || !content.CanSeek)
        {
            throw new ArgumentException("Image content must be readable and seekable for upload retries.", nameof(content));
        }

        if (content.Length == 0)
        {
            throw new ArgumentException("Image content must not be empty.", nameof(content));
        }
    }
}
