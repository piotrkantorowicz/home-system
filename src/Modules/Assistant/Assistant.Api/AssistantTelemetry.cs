namespace Assistant.Api;

using System.Diagnostics;
using System.Diagnostics.Metrics;

/// <summary>
/// The assistant's own telemetry (<c>HomeSystem.Assistant</c>): an <c>execute_tool</c> span and a counter per
/// tool call, a counter per turn. Tags carry the tool name and the outcome only, never a prompt, a reply,
/// tool arguments or results. Model calls are traced by the chat client's OpenTelemetry wrapper.
/// </summary>
internal sealed class AssistantTelemetry
{
    /// <summary>Name of the <see cref="ActivitySource"/> and the <see cref="Meter"/>.</summary>
    public const string Name = "HomeSystem.Assistant";

    /// <summary>The tool ran and reported success.</summary>
    public const string ToolOk = "ok";

    /// <summary>The tool ran and reported an error.</summary>
    public const string ToolError = "error";

    /// <summary>The tool is not offered or not allowed to run, so it did not run.</summary>
    public const string ToolRefused = "refused";

    /// <summary>The turn ended normally, including one that waits for the user to confirm a write.</summary>
    public const string TurnCompleted = "completed";

    /// <summary>A provider failure ended the turn.</summary>
    public const string TurnFailed = "failed";

    /// <summary>The turn stopped at the tool-round cap or the token budget.</summary>
    public const string TurnLimitReached = "limit_reached";

    private static readonly ActivitySource Source = new(Name);

    private readonly Counter<long> _toolCalls;
    private readonly Counter<long> _turns;

    public AssistantTelemetry(IMeterFactory meters)
    {
        Meter meter = meters.Create(Name);
        _toolCalls = meter.CreateCounter<long>("homesystem.assistant.tool_calls", "{call}");
        _turns = meter.CreateCounter<long>("homesystem.assistant.turns", "{turn}");
    }

    /// <summary>Starts the span of one tool call; null when nothing listens.</summary>
    /// <param name="tool">Tool name.</param>
    /// <returns>The span, to be passed to <see cref="EndToolCall"/>.</returns>
    public static Activity? StartToolCall(string tool)
        => Source.StartActivity($"execute_tool {tool}")
            ?.SetTag("gen_ai.operation.name", "execute_tool")
            .SetTag("gen_ai.tool.name", tool);

    /// <summary>Ends the span and counts the call.</summary>
    /// <param name="span">From <see cref="StartToolCall"/>.</param>
    /// <param name="tool">Tool name.</param>
    /// <param name="outcome">One of the <c>Tool*</c> constants.</param>
    public void EndToolCall(Activity? span, string tool, string outcome)
    {
        _toolCalls.Add(1, new("tool", tool), new("outcome", outcome));
        if (span is null)
            return;
        span.SetTag("homesystem.assistant.outcome", outcome);
        if (outcome == ToolError)
            span.SetStatus(ActivityStatusCode.Error);
        span.Dispose();
    }

    /// <summary>Counts a finished turn.</summary>
    /// <param name="outcome">One of the <c>Turn*</c> constants.</param>
    public void RecordTurn(string outcome) => _turns.Add(1, new KeyValuePair<string, object?>("outcome", outcome));
}
