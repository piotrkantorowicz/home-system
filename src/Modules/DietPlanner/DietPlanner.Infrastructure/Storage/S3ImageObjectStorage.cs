namespace DietPlanner.Infrastructure.Storage;

using System.Net.Sockets;
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
        ImageObjectStorageValidation.ValidateKey(key);
        ImageObjectStorageValidation.ValidateContent(content, contentType);

        try
        {
            content.Position = 0;
            await client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = options.Value.Bucket,
                Key = key,
                InputStream = content,
                ContentType = contentType,
                AutoCloseStream = false,
            }, ct);
        }
        catch (Exception ex) when (ex is AmazonS3Exception or AmazonClientException or HttpRequestException or IOException or SocketException
            || ex is OperationCanceledException && !ct.IsCancellationRequested)
        {
            ct.ThrowIfCancellationRequested();
            throw new ImageStorageUnavailableException();
        }
    }

    /// <inheritdoc/>
    public async Task<ImageObjectRead?> OpenReadAsync(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ImageObjectStorageValidation.ValidateKey(key);
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
        catch (Exception ex) when (ex is AmazonS3Exception or AmazonClientException or HttpRequestException or IOException or SocketException
            || ex is OperationCanceledException && !ct.IsCancellationRequested)
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
        ImageObjectStorageValidation.ValidateKey(key);
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
        catch (Exception ex) when (ex is AmazonS3Exception or AmazonClientException or HttpRequestException or IOException or SocketException
            || ex is OperationCanceledException && !ct.IsCancellationRequested)
        {
            ct.ThrowIfCancellationRequested();
            throw new ImageStorageUnavailableException();
        }
    }
}
