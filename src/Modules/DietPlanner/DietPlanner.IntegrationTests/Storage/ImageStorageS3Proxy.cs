namespace DietPlanner.IntegrationTests.Storage;

using System.Reflection;
using Amazon.Runtime;

/// <summary>Intercepts only the SDK request boundary; inheritance is required by DispatchProxy.</summary>
public class ImageStorageS3Proxy : DispatchProxy
{
    /// <summary>Handles an SDK request and returns its response task.</summary>
    public Func<AmazonWebServiceRequest, CancellationToken, object> Operation { get; set; }
        = (_, _) => throw new InvalidOperationException("No SDK operation was configured.");

    /// <inheritdoc/>
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        => args is [AmazonWebServiceRequest request, CancellationToken ct]
            ? Operation(request, ct)
            : throw new InvalidOperationException("Unexpected SDK call.");
}
