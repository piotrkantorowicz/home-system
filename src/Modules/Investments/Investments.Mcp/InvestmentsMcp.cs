namespace Investments.Mcp;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Investments' MCP surface: the OAuth scopes its tools require and the tool registration the host
/// composes into its MCP server. Tools are transport endpoints; they call the dispatchers and never the
/// handlers or the database directly.
/// </summary>
public static class InvestmentsMcp
{
    /// <summary>Read wallets, balances and net worth.</summary>
    public const string ReadScope = "mcp:investments:read";

    /// <summary>Manage wallets and record or delete balances.</summary>
    public const string WriteScope = "mcp:investments:write";

    /// <summary>Every scope Investments tools use, for policy registration and resource metadata.</summary>
    public static IReadOnlyList<string> Scopes { get; } = [ReadScope, WriteScope];

    /// <summary>Registers Investments' tools on the host's MCP server.</summary>
    /// <param name="builder">The host's MCP server builder.</param>
    /// <returns><paramref name="builder"/> for chaining.</returns>
    public static IMcpServerBuilder AddInvestmentsMcpTools(this IMcpServerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .WithTools<WalletTools>()
            .WithTools<NetWorthTools>()
            .WithTools<SnapshotWriteTools>()
            .WithTools<WalletWriteTools>();
    }
}
