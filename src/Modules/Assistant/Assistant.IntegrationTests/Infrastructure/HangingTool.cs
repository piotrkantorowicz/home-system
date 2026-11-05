namespace Assistant.IntegrationTests.Infrastructure;

using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;

/// <summary>A read tool that signals it started, then waits until its call is cancelled.</summary>
public sealed class HangingTool
{
    private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Completes when the tool is running.</summary>
    public Task Started => _started.Task;

    /// <summary>Creates the tool, named <c>hang_tool</c>, that the assistant treats as a read tool.</summary>
    /// <returns>The registered form.</returns>
    public McpServerTool Create() => McpServerTool.Create(Hang, new McpServerToolCreateOptions { Name = "hang_tool", ReadOnly = true });

    [Authorize(Policy = "mcp:test:read")]
    private async Task<string> Hang(CancellationToken ct)
    {
        _started.SetResult();
        await Task.Delay(Timeout.Infinite, ct);
        return "unreachable";
    }
}
