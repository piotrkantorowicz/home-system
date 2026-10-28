namespace Assistant.Api;

/// <summary>
/// Settings of the in-app assistant (<c>Assistant</c> section). See <c>docs/design/assistant/README.md</c> §7.
/// </summary>
public sealed class AssistantOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Assistant";

    /// <summary>Maps the endpoint and registers the tools at all. Off by default: the endpoint answers 404.</summary>
    public bool Enabled { get; set; }

    /// <summary>Model provider for the whole deployment. Only <c>Anthropic</c> today.</summary>
    public string Provider { get; set; } = "Anthropic";

    /// <summary>Provider model id.</summary>
    public string Model { get; set; } = "claude-haiku-5-5";

    /// <summary>Provider API key; secret store or environment only, never logged or sent to the browser.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Model ↔ tool round trips one request may make.</summary>
    public int MaxToolRounds { get; set; } = 10;

    /// <summary>Output tokens per model call.</summary>
    public int MaxOutputTokens { get; set; } = 4096;

    /// <summary>Input plus output tokens one user may use per server day; the in-app fairness cap (the provider console spend limit is the bill cap).</summary>
    public long DailyTokenBudget { get; set; } = 500_000;

    /// <summary>Assistant requests one user may start per minute.</summary>
    public int RequestsPerMinute { get; set; } = 10;

    /// <summary>Rough token allowance for the browser history; older turns beyond it are dropped before the model call.</summary>
    public int MaxHistoryTokens { get; set; } = 20_000;
}
