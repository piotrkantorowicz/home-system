namespace Shared.Infrastructure.Web;

using System.Diagnostics.Metrics;
using ModelContextProtocol.Server;

/// <summary>
/// MCP tool call durations (<c>HomeSystem.Mcp</c> meter), recorded on both paths: <c>/mcp</c> requests and
/// <see cref="McpToolInvoker"/>. Tags carry the tool name and the outcome only — never arguments or results.
/// Singleton, created through <see cref="IMeterFactory"/>.
/// </summary>
public sealed class McpToolMetrics
{
    /// <summary>Meter name; the host subscribes to <c>HomeSystem.*</c>.</summary>
    public const string MeterName = "HomeSystem.Mcp";

    /// <summary>Tag carrying the tool name.</summary>
    public const string ToolTag = "homesystem.mcp.tool";

    /// <summary>Tag carrying the call outcome.</summary>
    public const string OutcomeTag = "outcome";

    /// <summary>The tool returned a result.</summary>
    public const string Success = "success";

    /// <summary>The tool returned an error result (bad argument, unknown tool, expected failure).</summary>
    public const string Error = "error";

    /// <summary>The call threw.</summary>
    public const string Exception = "exception";

    /// <summary>The caller cancelled the call.</summary>
    public const string Cancelled = "cancelled";

    /// <summary>Tool name recorded for a name that is not in the registered catalog.</summary>
    public const string UnknownTool = "unknown";

    private readonly Histogram<double> _duration;
    private readonly HashSet<string> _tools;

    /// <summary>Creates the meter and its instrument.</summary>
    /// <param name="meters">Factory that owns the meter's lifetime.</param>
    /// <param name="tools">Every registered tool; any other name is recorded as <see cref="UnknownTool"/>.</param>
    public McpToolMetrics(IMeterFactory meters, IEnumerable<McpServerTool> tools)
    {
        ArgumentNullException.ThrowIfNull(meters);
        ArgumentNullException.ThrowIfNull(tools);

        _tools = tools.Select(t => t.ProtocolTool.Name).ToHashSet(StringComparer.Ordinal);
        _duration = meters.Create(MeterName).CreateHistogram<double>("homesystem.mcp.tool.duration", unit: "s");
    }

    /// <summary>Records one tool call.</summary>
    /// <param name="tool">Tool name as the caller sent it; names outside the catalog are recorded as <see cref="UnknownTool"/>.</param>
    /// <param name="outcome">One of <see cref="Success"/>, <see cref="Error"/>, <see cref="Exception"/>, <see cref="Cancelled"/>.</param>
    /// <param name="elapsed">Call duration.</param>
    public void RecordCall(string? tool, string outcome, TimeSpan elapsed)
        => _duration.Record(
            elapsed.TotalSeconds,
            new(ToolTag, tool is not null && _tools.Contains(tool) ? tool : UnknownTool),
            new(OutcomeTag, outcome));
}
