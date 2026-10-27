namespace Shared.Infrastructure.Web;

using System.Security.Claims;
using ModelContextProtocol;

/// <summary>
/// The authenticated caller of the current tool call, one per call scope. The <c>/mcp</c> transport and
/// <see cref="McpToolInvoker"/> set it before the tool runs, so tools take it from DI instead of reading
/// the MCP request: the same tool method serves both, and it never appears in a tool's input schema.
/// </summary>
public sealed class McpToolUser
{
    private ClaimsPrincipal? _principal;

    /// <summary>The caller, or <see langword="null"/> when nothing authenticated this call.</summary>
    public ClaimsPrincipal? Principal => _principal;

    /// <summary>Binds the call to <paramref name="principal"/>; a call has exactly one caller.</summary>
    /// <param name="principal">The authenticated caller.</param>
    public void Set(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        if (_principal is not null && !ReferenceEquals(_principal, principal))
            throw new InvalidOperationException("The tool call already has a caller.");

        _principal = principal;
    }

    /// <summary>The caller's auth subject, as the claims pipeline sets it for both transports.</summary>
    /// <returns>The subject id.</returns>
    /// <exception cref="McpException">Nothing authenticated this call.</exception>
    public string AuthSubject()
        => _principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? _principal?.FindFirstValue("sub")
           ?? throw new McpException("The request is not authenticated.");
}
