namespace HomeSystem.REST.Mcp;

/// <summary>
/// Operational settings of the MCP endpoint (<c>Mcp</c> section). See <c>docs/design/mcp/OPERATIONS.md</c>.
/// </summary>
internal sealed class McpOptions
{
    public const string SectionName = "Mcp";

    /// <summary>Maps <c>/mcp</c> and its metadata at all. Off by default.</summary>
    public bool Enabled { get; set; }

    /// <summary>Public HTTPS origin clients use, e.g. <c>https://home.example.com</c>. Resource metadata and challenges are built from it, never from request or forwarded headers.</summary>
    public Uri? PublicBaseUrl { get; set; }

    /// <summary>Browser origins allowed to call <c>/mcp</c>. Requests without an <c>Origin</c> header (native clients) are allowed; any other origin is rejected.</summary>
    public IReadOnlyList<string> AllowedOrigins { get; set; } = [];

    /// <summary>Allows tools that need the write grant. Turning it off also turns off imports.</summary>
    public bool WritesEnabled { get; set; } = true;

    /// <summary>Allows <c>execute_plan_import</c>. Off by default until imports are released.</summary>
    public bool ImportsEnabled { get; set; }

    /// <summary>MCP requests one caller may send per minute.</summary>
    public int RequestsPerMinute { get; set; } = 120;

    /// <summary>Largest accepted MCP request body. A whole-month plan is about 16 KB.</summary>
    public long MaxRequestBytes { get; set; } = 1024 * 1024;

    /// <summary>Directory for the Data Protection key ring that signs import preview tokens; in-memory when unset, so tokens die with the process.</summary>
    public string? DataProtectionKeysPath { get; set; }
}
