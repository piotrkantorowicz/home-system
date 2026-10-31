namespace Assistant.IntegrationTests.Api;

using System.Net.ServerSentEvents;
using System.Text.Json;
using Assistant.IntegrationTests.Infrastructure;
using Microsoft.Extensions.AI;

/// <summary>#664 — a receipt photo becomes a held create_expense; nothing is recorded before Confirm.</summary>
public sealed class AssistantReceiptTests(AssistantDatabaseFixture databases) : IClassFixture<AssistantDatabaseFixture>, IDisposable
{
    // Smallest bytes that pass the JPEG signature check; the scripted model never decodes them.
    private static readonly byte[] Receipt = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, (byte)'J', (byte)'F', (byte)'I', (byte)'F'];

    private readonly AssistantApiFactory _factory = new(databases);
    private readonly string _tag = Guid.CreateVersion7().ToString("N")[^8..];

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>The model reads the context, the expense it proposes is held with the receipt's fields, and only Confirm records it.</summary>
    [Fact]
    public async Task SendMessage_ReceiptPhoto_HoldsExpenseUntilConfirm()
    {
        var me = await _factory.HouseholdOwnerAsync();
        Guid envelope = await AssistantApiFactory.CreateEnvelopeAsync(me);
        _factory.Model.CallsTool("get_budget_context");
        _factory.Model.CallsTool("create_expense", new()
        {
            ["accountId"] = envelope,
            ["amount"] = "42.50",
            ["occurredOn"] = "2026-10-27",
            ["category"] = "Groceries",
            ["fundingSource"] = "HouseholdFunds",
            ["description"] = $"Shop {_tag}",
        });

        List<SseItem<string>> held = await AssistantApiFactory.SendAsync(
            me, new { message = "Add this receipt, paid from household money", image = new { mediaType = "image/jpeg", data = Convert.ToBase64String(Receipt) } });

        _factory.Model.Calls.First().Messages.Last(m => m.Role == ChatRole.User).Contents.OfType<DataContent>().ShouldHaveSingleItem();
        held.Select(e => e.EventType).ShouldBe(["tool_call", "tool_result", "pending_action", "history", "usage", "done"]);
        Data(held, "tool_result").GetProperty("ok").GetBoolean().ShouldBeTrue();
        JsonElement action = Data(held, "pending_action");
        action.GetProperty("name").GetString().ShouldBe("create_expense");
        JsonElement arguments = action.GetProperty("arguments");
        arguments.GetProperty("accountId").GetGuid().ShouldBe(envelope);
        arguments.GetProperty("amount").GetString().ShouldBe("42.50");
        arguments.GetProperty("occurredOn").GetString().ShouldBe("2026-10-27");
        arguments.GetProperty("category").GetString().ShouldBe("Groceries");
        (await me.GetStringAsync("/api/budget/expenses", Ct)).ShouldNotContain($"Shop {_tag}");

        _factory.Model.Answers("Recorded.");
        List<SseItem<string>> confirmed = await AssistantApiFactory.SendAsync(
            me, new { confirm = action.GetProperty("token").GetString(), messages = Data(held, "history").GetProperty("messages") });

        Data(confirmed, "tool_result").GetProperty("ok").GetBoolean().ShouldBeTrue();
        string expenses = await me.GetStringAsync("/api/budget/expenses", Ct);
        expenses.ShouldContain($"Shop {_tag}");
        expenses.ShouldContain("42.5");
    }

    /// <summary>When the model cannot tell the category it asks; no expense is held or recorded.</summary>
    [Fact]
    public async Task SendMessage_ReceiptWithUnclearCategory_AsksInsteadOfHolding()
    {
        var me = await _factory.HouseholdOwnerAsync();
        await AssistantApiFactory.CreateEnvelopeAsync(me);
        _factory.Model.CallsTool("get_budget_context");
        _factory.Model.Answers("The total is 42.50 PLN from 27 October. Is it Groceries or Household?");

        List<SseItem<string>> events = await AssistantApiFactory.SendAsync(
            me, new { message = "Add this receipt", image = new { mediaType = "image/jpeg", data = Convert.ToBase64String(Receipt) } });

        events.Select(e => e.EventType).ShouldNotContain("pending_action");
        string.Concat(events.Where(e => e.EventType == "text").Select(e => JsonDocument.Parse(e.Data).RootElement.GetProperty("text").GetString()))
            .ShouldEndWith("?");
        (await me.GetStringAsync("/api/budget/expenses", Ct)).ShouldNotContain("42.5");
    }

    /// <inheritdoc />
    public void Dispose() => _factory.Dispose();

    private static JsonElement Data(List<SseItem<string>> events, string type)
        => JsonDocument.Parse(events.First(e => e.EventType == type).Data).RootElement;
}
