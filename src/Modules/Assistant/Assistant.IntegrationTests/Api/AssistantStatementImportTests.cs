namespace Assistant.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Text;
using System.Text.Json;
using Assistant.IntegrationTests.Infrastructure;
using Microsoft.Extensions.AI;

/// <summary>#731 — a bank statement attached to a message is previewed by the server, never copied by the model, and imported only on Confirm.</summary>
public sealed class AssistantStatementImportTests(AssistantDatabaseFixture databases) : IClassFixture<AssistantDatabaseFixture>, IDisposable
{
    // A real mBank export in Windows-1250, decoded the way the panel decodes it.
    private static readonly string Statement = Decode(File.ReadAllBytes(Path.Join(AppContext.BaseDirectory, "Fixtures", "mbank.csv")));

    private readonly AssistantApiFactory _factory = new(databases, new Dictionary<string, string?> { ["Mcp:ImportsEnabled"] = "true" });

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static object Attached => new { name = "mbank.csv", text = Statement };

    /// <summary>The preview gets the file from the server, the import waits for Confirm, and a second preview reports the row as imported.</summary>
    [Fact]
    public async Task SendMessage_Statement_PreviewsThenImportsOnlyOnConfirm()
    {
        var me = await _factory.HouseholdOwnerAsync();
        Guid envelope = await AssistantApiFactory.CreateEnvelopeAsync(me);
        _factory.Model.CallsTool("preview_expense_import", Preview(envelope));
        _factory.Model.Answers("I found new rows. Import the shop one as Groceries?");

        List<SseItem<string>> previewed = await AssistantApiFactory.SendAsync(me, new { message = "Import this statement", document = Attached });

        Data(previewed, "tool_result").GetProperty("ok").GetBoolean().ShouldBeTrue();
        JsonElement row = Candidates(_factory.Model.Calls.Last()).First(c => c.GetProperty("status").GetString() == "New");
        JsonElement history = Data(previewed, "history").GetProperty("messages");
        history.GetRawText().ShouldNotContain("Saldo");

        _factory.Model.CallsTool("execute_expense_import", new()
        {
            ["accountId"] = envelope,
            ["rows"] = new[] { ImportRow(row) },
        });
        List<SseItem<string>> held = await AssistantApiFactory.SendAsync(
            me, new { message = "Yes, from household money", messages = history });

        JsonElement action = Data(held, "pending_action");
        action.GetProperty("name").GetString().ShouldBe("execute_expense_import");
        string sourceKey = row.GetProperty("sourceKey").GetString()!;
        (await ExpensesAsync(me)).ShouldBe(0);

        _factory.Model.Answers("Imported.");
        List<SseItem<string>> confirmed = await AssistantApiFactory.SendAsync(
            me, new { confirm = action.GetProperty("token").GetString(), messages = Data(held, "history").GetProperty("messages") });

        Data(confirmed, "tool_result").GetProperty("ok").GetBoolean().ShouldBeTrue();
        (await ExpensesAsync(me)).ShouldBe(1);

        _factory.Model.CallsTool("preview_expense_import", Preview(envelope));
        _factory.Model.Answers("That row is already imported.");
        await AssistantApiFactory.SendAsync(me, new { message = "And this one?", document = Attached });

        Candidates(_factory.Model.Calls.Last()).Single(c => c.GetProperty("sourceKey").GetString() == sourceKey)
            .GetProperty("status").GetString().ShouldBe("AlreadyImported");
    }

    /// <summary>When a category is unclear the model asks after the preview; nothing is held or imported.</summary>
    [Fact]
    public async Task SendMessage_StatementWithUnclearCategory_AsksInsteadOfHolding()
    {
        var me = await _factory.HouseholdOwnerAsync();
        Guid envelope = await AssistantApiFactory.CreateEnvelopeAsync(me);
        _factory.Model.CallsTool("preview_expense_import", Preview(envelope));
        _factory.Model.Answers("What category is MUZYKA ONLINE?");

        List<SseItem<string>> events = await AssistantApiFactory.SendAsync(me, new { message = "Import this statement", document = Attached });

        events.Select(e => e.EventType).ShouldNotContain("pending_action");
        Candidates(_factory.Model.Calls.Last()).ShouldNotBeEmpty();
        (await ExpensesAsync(me)).ShouldBe(0);
    }

    /// <summary>The model never receives the file: its request carries only a note naming it.</summary>
    [Fact]
    public async Task SendMessage_Statement_IsNotSentToTheModel()
    {
        var me = await _factory.HouseholdOwnerAsync();
        _factory.Model.Answers("Which envelope?");

        await AssistantApiFactory.SendAsync(me, new { message = "Import this statement", document = Attached });

        string asked = _factory.Model.Calls.Single().Messages.Last(m => m.Role == ChatRole.User).Text;
        asked.ShouldContain("mbank.csv");
        asked.ShouldNotContain("Saldo");
    }

    /// <summary>A file without a message, or longer than the cap, is refused before the model is called.</summary>
    [Fact]
    public async Task SendMessage_DocumentTooLong_Returns400()
    {
        using var small = new AssistantApiFactory(databases, new Dictionary<string, string?> { ["Assistant:MaxDocumentChars"] = "10" });
        var me = small.CreateClientFor($"auth|{Guid.CreateVersion7():N}");

        var response = await me.PostAsJsonAsync("/api/assistant/messages", new { message = "Import", document = Attached }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        small.Model.Calls.ShouldBeEmpty();
    }

    /// <inheritdoc />
    public void Dispose() => _factory.Dispose();

    private static Dictionary<string, object?> Preview(Guid envelope) => new() { ["accountId"] = envelope, ["statement"] = "attached" };

    private static Dictionary<string, object?> ImportRow(JsonElement candidate) => new()
    {
        ["sourceKey"] = candidate.GetProperty("sourceKey").GetString(),
        ["amount"] = candidate.GetProperty("amount").GetString(),
        ["occurredOn"] = candidate.GetProperty("occurredOn").GetString(),
        ["category"] = "Groceries",
        ["fundingSource"] = "HouseholdFunds",
        ["description"] = candidate.GetProperty("description").GetString(),
        ["merchantKey"] = candidate.GetProperty("merchantKey").GetString(),
    };

    // The preview result the model got back in its latest call.
    private static List<JsonElement> Candidates((IReadOnlyList<ChatMessage> Messages, ChatOptions? Options) call)
    {
        string result = call.Messages.SelectMany(m => m.Contents).OfType<FunctionResultContent>().Last().Result!.ToString()!;
        return [.. JsonDocument.Parse(result).RootElement.GetProperty("candidates").EnumerateArray()];
    }

    private static async Task<int> ExpensesAsync(HttpClient me)
        => (await me.GetFromJsonAsync<JsonElement>("/api/budget/expenses", Ct)).GetProperty("totalCount").GetInt32();

    private static string Decode(byte[] bytes)
    {
        try
        {
            return new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return CodePagesEncodingProvider.Instance.GetEncoding(1250)!.GetString(bytes);
        }
    }

    private static JsonElement Data(List<SseItem<string>> events, string type)
        => JsonDocument.Parse(events.First(e => e.EventType == type).Data).RootElement;
}
