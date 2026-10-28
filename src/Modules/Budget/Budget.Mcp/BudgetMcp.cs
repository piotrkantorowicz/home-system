namespace Budget.Mcp;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Budget's MCP surface: the OAuth scopes its tools require and the tool registration the host
/// composes into its MCP server. Tools are transport endpoints; they call the dispatchers and never the
/// handlers or the database directly.
/// </summary>
public static class BudgetMcp
{
    /// <summary>Read envelopes, expenses and monthly summaries.</summary>
    public const string ReadScope = "mcp:budget:read";

    /// <summary>Record and correct expenses, manage envelopes, limits and repayments.</summary>
    public const string WriteScope = "mcp:budget:write";

    /// <summary>Every scope Budget tools use, for policy registration and resource metadata.</summary>
    public static IReadOnlyList<string> Scopes { get; } = [ReadScope, WriteScope];

    /// <summary>Registers Budget's tools on the host's MCP server.</summary>
    /// <param name="builder">The host's MCP server builder.</param>
    /// <returns><paramref name="builder"/> for chaining.</returns>
    public static IMcpServerBuilder AddBudgetMcpTools(this IMcpServerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .WithTools<BudgetContextTools>()
            .WithTools<ExpenseTools>()
            .WithTools<ExpenseWriteTools>();
    }
}
