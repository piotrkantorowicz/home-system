namespace Budget.IntegrationTests.Mcp;

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Budget.IntegrationTests.Infrastructure;
using global::Budget.Mcp;
using ModelContextProtocol.Server;

/// <summary>
/// Pins what agents see in <c>tools/list</c> for Budget: names, descriptions, annotations and
/// input/output schemas against the checked-in <c>tool-schemas.json</c>.
/// Run with <c>MCP_SCHEMA_UPDATE=1</c> to rewrite the file after an intended change.
/// </summary>
public sealed class BudgetToolSchemaTests : IClassFixture<BudgetDatabaseFixture>, IDisposable
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private readonly BudgetApiFactory _factory;
    private readonly BudgetMcpHost _mcp;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public BudgetToolSchemaTests(BudgetDatabaseFixture fixture)
    {
        _factory = new BudgetApiFactory(fixture);
        _mcp = new BudgetMcpHost(_factory);
    }

    /// <summary>The listed tools match the checked-in snapshot exactly.</summary>
    [Fact]
    public async Task ListTools_MatchesCheckedInSchemas()
    {
        await using var client = await _mcp.ConnectAsync($"schema-{Guid.CreateVersion7()}", string.Join(' ', BudgetMcp.Scopes));

        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var actual = new JsonArray([.. tools.Where(t => BudgetToolNames.Contains(t.Name)).OrderBy(t => t.Name, StringComparer.Ordinal)
            .Select(t => JsonSerializer.SerializeToNode(t.ProtocolTool, JsonSerializerOptions.Web))]).ToJsonString(Indented);

        var path = SnapshotPath();
        if (Environment.GetEnvironmentVariable("MCP_SCHEMA_UPDATE") == "1")
            await File.WriteAllTextAsync(path, actual + "\n", TestContext.Current.CancellationToken);

        actual.ShouldBe((await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken)).TrimEnd());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _mcp.Dispose();
        _factory.Dispose();
    }

    // Derived from the assembly so a new Budget tool cannot be listed to agents without being pinned.
    private static readonly HashSet<string> BudgetToolNames = [.. typeof(BudgetMcp).Assembly.GetTypes()
        .SelectMany(t => t.GetMethods())
        .Select(m => m.GetCustomAttribute<McpServerToolAttribute>()?.Name)
        .OfType<string>()];

    private static string SnapshotPath([CallerFilePath] string source = "")
        => Path.Join(Path.GetDirectoryName(source)!, "tool-schemas.json");
}
