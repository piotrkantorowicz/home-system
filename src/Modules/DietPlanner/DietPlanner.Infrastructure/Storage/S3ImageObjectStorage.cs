namespace DietPlanner.Infrastructure.Storage;

using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DietPlanner.Application.Storage;
using Microsoft.Extensions.Options;

/// <summary>Streams private image objects through the module's configured S3-compatible bucket.</summary>
/// <param name="client">The DI-owned SDK client using the standard credentials chain.</param>
/// <param name="options">The enabled image storage configuration.</param>
public sealed class S3ImageObjectStorage(IAmazonS3 client, IOptions<ImageStorageOptions> options) : IImageObjectStorage
{
    /// <inheritdoc/>
    public async Task PutAsync(string key, Stream content, string contentType, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ValidateKey(key);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        if (!content.CanRead)
        {
            throw new ArgumentException("Image content must be readable.", nameof(content));
        }

        try
        {
            await client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = options.Value.Bucket,
                Key = key,
                InputStream = content,
                ContentType = contentType,
                AutoCloseStream = false,
            }, ct);
        }
        catch (Exception ex) when (ex is AmazonS3Exception or AmazonClientException or HttpRequestException)
        {
            ct.ThrowIfCancellationRequested();
            throw new ImageStorageUnavailableException();
        }
    }

    /// <inheritdoc/>
    public async Task<ImageObjectRead?> OpenReadAsync(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ValidateKey(key);
        GetObjectResponse? response = null;
        try
        {
            response = await client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = options.Value.Bucket,
                Key = key,
            }, ct);
            ct.ThrowIfCancellationRequested();
            var read = new ImageObjectRead(response.ResponseStream, response);
            response = null;
            return read;
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchKey")
        {
            ct.ThrowIfCancellationRequested();
            return null;
        }
        catch (Exception ex) when (ex is AmazonS3Exception or AmazonClientException or HttpRequestException)
        {
            ct.ThrowIfCancellationRequested();
            throw new ImageStorageUnavailableException();
        }
        finally
        {
            response?.Dispose();
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ValidateKey(key);
        try
        {
            await client.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = options.Value.Bucket,
                Key = key,
            }, ct);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchKey")
        {
            ct.ThrowIfCancellationRequested();
        }
        catch (Exception ex) when (ex is AmazonS3Exception or AmazonClientException or HttpRequestException)
        {
            ct.ThrowIfCancellationRequested();
            throw new ImageStorageUnavailableException();
        }
    }

    private static void ValidateKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (key.StartsWith('/') || Uri.TryCreate(key, UriKind.Absolute, out _)
            || key.Split('/').Any(segment => segment is "." or ".."))
        {
            throw new ArgumentException("Image keys must be relative internal object names without dot segments.", nameof(key));
        }
    }
}
