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

    /// <summary>Model provider for the whole deployment: <c>Anthropic</c>, <c>OpenAI</c> or <c>XAI</c>.</summary>
    public string Provider { get; set; } = "Anthropic";

    /// <summary>Provider model id. Defaults to <c>claude-haiku-5-5</c> for Anthropic; required for OpenAI and XAI.</summary>
    public string? Model { get; set; }

    /// <summary>Provider API key; secret store or environment only, never logged or sent to the browser.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Endpoint override for the OpenAI-compatible providers; XAI defaults to <c>https://api.x.ai/v1</c>.</summary>
    public Uri? BaseUrl { get; set; }

    /// <summary>Model ↔ tool round trips one request may make.</summary>
    public int MaxToolRounds { get; set; } = 10;

    /// <summary>Output tokens per model call.</summary>
    public int MaxOutputTokens { get; set; } = 4096;

    /// <summary>Input plus output tokens one user may use per server day; the in-app fairness cap (the provider console spend limit is the bill cap).</summary>
    public long DailyTokenBudget { get; set; } = 500_000;

    /// <summary>Assistant requests one user may start per minute.</summary>
    public int RequestsPerMinute { get; set; } = 10;

    /// <summary>Copied from <c>Mcp:WritesEnabled</c>: the switch acts for every client, the assistant included.</summary>
    internal bool WritesEnabled { get; set; } = true;

    /// <summary>Copied from <c>Mcp:ImportsEnabled</c>; imports also need <see cref="WritesEnabled"/>.</summary>
    internal bool ImportsEnabled { get; set; }

    /// <summary>The configured model accepts images. Turn off for a text-only model; attachments are then refused.</summary>
    public bool ImagesEnabled { get; set; } = true;

    /// <summary>Largest accepted image after decoding; the panel downscales before sending.</summary>
    public int MaxImageBytes { get; set; } = 2_000_000;

    /// <summary>Rough token allowance for the browser history; older turns beyond it are dropped before the model call.</summary>
    public int MaxHistoryTokens { get; set; } = 20_000;
}
