namespace DietPlanner.IntegrationTests.Mcp;

using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using DietPlanner.IntegrationTests.Infrastructure;
using DietPlanner.Mcp;

/// <summary>
/// Pins what agents see in <c>tools/list</c>: names, descriptions, annotations and input/output schemas
/// against the checked-in <c>tool-schemas.json</c>. Refactors of how tools are hosted must not change it.
/// Run with <c>MCP_SCHEMA_UPDATE=1</c> to rewrite the file after an intended change.
/// </summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class McpToolSchemaTests(DatabaseFixture db) : IDisposable
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private readonly McpTestHost _mcp = new(db.ConnectionString);

    /// <summary>The listed tools match the checked-in snapshot exactly.</summary>
    [Fact]
    public async Task ListTools_MatchesCheckedInSchemas()
    {
        await using var client = await _mcp.ConnectAsync(
            $"schema-{Guid.CreateVersion7()}", string.Join(' ', DietPlannerMcp.Scopes));

        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var actual = new JsonArray([.. tools.OrderBy(t => t.Name, StringComparer.Ordinal)
            .Select(t => JsonSerializer.SerializeToNode(t.ProtocolTool, JsonSerializerOptions.Web))]).ToJsonString(Indented);

        var path = SnapshotPath();
        if (Environment.GetEnvironmentVariable("MCP_SCHEMA_UPDATE") == "1")
            await File.WriteAllTextAsync(path, actual + "\n", TestContext.Current.CancellationToken);

        actual.ShouldBe((await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken)).TrimEnd());
    }

    /// <inheritdoc />
    public void Dispose() => _mcp.Dispose();

    private static string SnapshotPath([CallerFilePath] string source = "")
        => Path.Combine(Path.GetDirectoryName(source)!, "tool-schemas.json");
}
