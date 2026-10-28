namespace Assistant.IntegrationTests.Api;

using Assistant.Api;
using Assistant.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

/// <summary>
/// Design §5 guard: every registered tool carries scopes the capability gate understands, and its
/// annotations agree with them. A new tool missing <c>[Authorize]</c>, using an unknown scope or
/// mis-annotated fails here instead of being silently hidden or running unconfirmed.
/// </summary>
public sealed class ToolCapabilityGuardTests(AssistantDatabaseFixture databases) : IClassFixture<AssistantDatabaseFixture>
{
    /// <summary>Every tool: at least one scope, all recognised, read-only scopes exactly when annotated read-only.</summary>
    [Fact]
    public void RegisteredTools_HaveRecognisedScopesMatchingAnnotations()
    {
        using var factory = new AssistantApiFactory(databases);
        List<McpServerTool> tools = [.. factory.Services.GetServices<McpServerTool>()];

        tools.ShouldNotBeEmpty();
        tools.ShouldAllBe(t => ToolCapabilities.Scopes(t).Count > 0, "a tool has no [Authorize] scope");
        tools.ShouldAllBe(t => ToolCapabilities.Scopes(t).All(ToolCapabilities.IsRecognised), "a tool has an unrecognised scope");
        tools.ShouldAllBe(
            t => ToolCapabilities.Scopes(t).All(ToolCapabilities.IsRead) == (t.ProtocolTool.Annotations!.ReadOnlyHint == true),
            "a tool's read-only annotation disagrees with its scopes");
    }
}
