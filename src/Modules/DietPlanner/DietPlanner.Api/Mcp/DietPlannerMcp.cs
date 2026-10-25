namespace DietPlanner.Api.Mcp;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// DietPlanner's MCP surface: the OAuth scopes its tools require and the tool registration the host
/// composes into its MCP server. Tools are transport endpoints; they call the dispatchers and never the
/// handlers or the database directly.
/// </summary>
public static class DietPlannerMcp
{
    /// <summary>Read planning context, catalog and diary.</summary>
    public const string ReadScope = "mcp:diet:read";

    /// <summary>Create catalog items and plan meals.</summary>
    public const string WriteScope = "mcp:diet:write";

    /// <summary>Execute a previewed bulk import; requires <see cref="WriteScope"/> as well.</summary>
    public const string ImportScope = "mcp:diet:import";

    /// <summary>Every scope DietPlanner tools use, for policy registration and resource metadata.</summary>
    public static IReadOnlyList<string> Scopes { get; } = [ReadScope, WriteScope, ImportScope];

    /// <summary>Registers DietPlanner's tools on the host's MCP server.</summary>
    /// <param name="builder">The host's MCP server builder.</param>
    /// <returns><paramref name="builder"/> for chaining.</returns>
    public static IMcpServerBuilder AddDietPlannerMcpTools(this IMcpServerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddDataProtection();
        builder.Services.AddSingleton<ImportPreviewTokens>();
        builder.Services.AddScoped<McpCaller>();

        return builder
            .WithTools<DietContextTools>()
            .WithTools<CatalogTools>()
            .WithTools<DiaryTools>()
            .WithTools<CatalogWriteTools>()
            .WithTools<ImportTools>();
    }
}
