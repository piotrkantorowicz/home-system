namespace HomeSystem.REST;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

/// <summary>
/// Healthy when the JWT handler holds Authentik's signing keys. Reads the handler's cached OIDC metadata, so it
/// only calls Authentik when nothing is cached yet: an Authentik outage after startup does not stop token checks.
/// </summary>
/// <param name="jwtOptions">The JWT bearer options whose configuration manager fetches the metadata.</param>
internal sealed class AuthentikHealthCheck(IOptionsMonitor<JwtBearerOptions> jwtOptions) : IHealthCheck
{
    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var manager = jwtOptions.Get(JwtBearerDefaults.AuthenticationScheme).ConfigurationManager
            ?? throw new InvalidOperationException("JWT bearer authentication has no OIDC configuration manager.");
        await manager.GetConfigurationAsync(cancellationToken);
        return HealthCheckResult.Healthy();
    }
}
