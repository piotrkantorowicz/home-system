namespace DietPlanner.Infrastructure.Storage;

using DietPlanner.Application.Storage;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

/// <summary>
/// Healthy when image storage is disabled, or when the bucket answers a read of a key that is never written.
/// A missing key is the expected answer; S3 only reports it (rather than access denied) to credentials that may
/// list the bucket.
/// </summary>
/// <param name="storage">The configured image storage.</param>
/// <param name="options">Image storage settings; only <see cref="ImageStorageOptions.Enabled"/> is read.</param>
public sealed class ImageStorageHealthCheck(IImageObjectStorage storage, IOptions<ImageStorageOptions> options) : IHealthCheck
{
    private const string ProbeKey = "health-check";

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled)
        {
            return HealthCheckResult.Healthy("Image storage is disabled.");
        }

        using var read = await storage.OpenReadAsync(ProbeKey, cancellationToken);
        return HealthCheckResult.Healthy();
    }
}
