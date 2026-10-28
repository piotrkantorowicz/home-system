namespace Assistant.Api;

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;

/// <summary>
/// The capability gate: derives what the assistant may do with a tool from the scopes on its
/// <c>[Authorize]</c> attributes and its annotations. It fails closed: no scope, or any scope it does
/// not recognise, means the tool is not offered. Design: <c>docs/design/assistant/README.md</c> §5.
/// </summary>
public static partial class ToolCapabilities
{
    /// <summary>The scopes on the tool, i.e. the policies of its <c>[Authorize]</c> attributes.</summary>
    /// <param name="tool">A registered tool.</param>
    /// <returns>The policy names; null for an attribute without a policy.</returns>
    public static IReadOnlyList<string?> Scopes(McpServerTool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        return [.. tool.Metadata.OfType<IAuthorizeData>().Select(a => a.Policy)];
    }

    /// <summary>True for <c>mcp:&lt;module&gt;:read|write|import</c>.</summary>
    /// <param name="scope">A scope from <see cref="Scopes"/>.</param>
    /// <returns>Whether the gate knows what the scope allows.</returns>
    public static bool IsRecognised(string? scope) => scope is not null && ScopePattern().IsMatch(scope);

    /// <summary>True for a recognised <c>*:read</c> scope.</summary>
    /// <param name="scope">A scope from <see cref="Scopes"/>.</param>
    /// <returns>Whether the scope only reads.</returns>
    public static bool IsRead(string? scope) => IsRecognised(scope) && scope!.EndsWith(":read", StringComparison.Ordinal);

    /// <summary>Classifies one tool.</summary>
    /// <param name="tool">A registered tool.</param>
    /// <returns>What the assistant may do with it.</returns>
    public static ToolCapability Of(McpServerTool tool)
    {
        IReadOnlyList<string?> scopes = Scopes(tool);
        if (scopes.Count == 0 || !scopes.All(IsRecognised))
            return ToolCapability.NotOffered;

        return scopes.All(IsRead) && tool.ProtocolTool.Annotations?.ReadOnlyHint == true
            ? ToolCapability.RunNow
            : ToolCapability.Hold;
    }

    [GeneratedRegex("^mcp:[a-z]+:(read|write|import)$")]
    private static partial Regex ScopePattern();
}
