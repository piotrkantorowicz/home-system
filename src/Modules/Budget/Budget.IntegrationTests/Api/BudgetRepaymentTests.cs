namespace Budget.IntegrationTests.Api;

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Budget.IntegrationTests.Infrastructure;

/// <summary>#455 — recording and voiding repayments: idempotency, authorisation, balances and races over HTTP to PostgreSQL.</summary>
public sealed class BudgetRepaymentTests : IClassFixture<BudgetDatabaseFixture>, IDisposable
{
    private readonly BudgetApiFactory _factory;
    private readonly BudgetScenario _scenario;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public BudgetRepaymentTests(BudgetDatabaseFixture fixture)
    {
        _factory = new BudgetApiFactory(fixture);
        _scenario = new BudgetScenario(_factory);
    }

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public void Dispose() => _factory.Dispose();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static object Body(Guid from, Guid to, string amount = "25.00", string date = "2026-10-02", string? note = null, Guid? requestId = null)
        => new { clientRequestId = requestId ?? Guid.NewGuid(), fromPersonId = from, toPersonId = to, amount, paidOn = date, note };

    private static Task<HttpResponseMessage> RecordAsync(HttpClient client, object body)
        => client.PostAsJsonAsync("/api/budget/settlements", body, Ct);

    private static async Task<ResultBody> RecordedAsync(HttpClient client, object body)
    {
        var response = await RecordAsync(client, body);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Ct));
        return (await response.Content.ReadFromJsonAsync<ResultBody>(Ct))!;
    }

    private static Task<HttpResponseMessage> VoidAsync(HttpClient client, Guid id, int expected = 1, string reason = "entered twice")
        => client.PostAsJsonAsync($"/api/budget/settlements/{id}/void", new { expectedRevision = expected, reason }, Ct);

    private static async Task<SettlementBody> BalancesAsync(HttpClient client)
        => (await client.GetFromJsonAsync<SettlementBody>("/api/budget/settlement", Ct))!;

    private static decimal Net(SettlementBody s, Guid person)
        => decimal.Parse(s.Balances.Single(b => b.PersonId == person).Net, CultureInfo.InvariantCulture);

    private static async Task<PageBody> ListAsync(HttpClient client, string query = "")
        => (await client.GetFromJsonAsync<PageBody>($"/api/budget/settlements{query}", Ct))!;

    /// <summary>Alex is owed 40.00 by Bea after the design example's two shared expenses.</summary>
    private async Task<(BudgetScenario.Household H, BudgetScenario.Member Bea, Guid Account)> AlexIsOwed40Async()
    {
        var h = await _scenario.NewHouseholdAsync();
        var bea = await _scenario.MemberAsync(h, "Adult");
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        Guid[] both = [h.OwnerPersonId, bea.PersonId];
        foreach (var (client, amount, payer) in new[] { (h.Owner, "120.00", h.OwnerPersonId), (bea.Client, "40.00", bea.PersonId) })
        {
            (await client.PostAsJsonAsync("/api/budget/expenses", new
            {
                clientRequestId = Guid.NewGuid(),
                accountId = account,
                amount,
                occurredOn = "2026-10-01",
                category = "Groceries",
                fundingSource = "Individual",
                paidByPersonId = payer,
                participantIds = both,
            }, Ct)).EnsureSuccessStatusCode();
        }

        return (h, bea, account);
    }

    /// <summary>The design's 25.00 payment leaves 15.00 owed; voiding restores 40.00; spending never changes.</summary>
    [Fact]
    public async Task DesignExample_PartialPaymentThenVoid_AndSpendingUnchanged()
    {
        var (h, bea, _) = await AlexIsOwed40Async();
        var spendingBefore = (await h.Owner.GetFromJsonAsync<SummaryBody>("/api/budget/summary?month=2026-10&scope=shared", Ct))!.TotalSpent;

        var repayment = await RecordedAsync(bea.Client, Body(bea.PersonId, h.OwnerPersonId));
        var after = await BalancesAsync(h.Owner);
        Net(after, h.OwnerPersonId).ShouldBe(15m);
        Net(after, bea.PersonId).ShouldBe(-15m);
        after.Suggestions.Single().Amount.ShouldBe("15.00");

        (await VoidAsync(h.Owner, repayment.RepaymentId)).EnsureSuccessStatusCode();
        var restored = await BalancesAsync(h.Owner);
        Net(restored, h.OwnerPersonId).ShouldBe(40m);

        (await h.Owner.GetFromJsonAsync<SummaryBody>("/api/budget/summary?month=2026-10&scope=shared", Ct))!.TotalSpent.ShouldBe(spendingBefore);
    }

    /// <summary>A payment larger than the debt is recorded as stated and shows up as a reverse credit.</summary>
    [Fact]
    public async Task Overpayment_CreatesVisibleReverseCredit()
    {
        var (h, bea, _) = await AlexIsOwed40Async();

        await RecordedAsync(bea.Client, Body(bea.PersonId, h.OwnerPersonId, "60.00"));
        var s = await BalancesAsync(h.Owner);

        Net(s, h.OwnerPersonId).ShouldBe(-20m);
        Net(s, bea.PersonId).ShouldBe(20m);
        var transfer = s.Suggestions.Single();
        (transfer.FromPersonId, transfer.ToPersonId, transfer.Amount).ShouldBe((h.OwnerPersonId, bea.PersonId, "20.00"));
    }

    /// <summary>An adult can record on behalf of either party; the recorder is always the caller.</summary>
    [Fact]
    public async Task AdultRecordsOnBehalfOfEitherParty_AttributedToTheRecorder()
    {
        var (h, bea, _) = await AlexIsOwed40Async();

        var repayment = await RecordedAsync(h.Owner, Body(bea.PersonId, h.OwnerPersonId, "10.00", note: "cash"));
        var listed = (await ListAsync(h.Owner)).Items.Single(i => i.Id == repayment.RepaymentId);

        listed.AddedByPersonId.ShouldBe(h.OwnerPersonId);
        (listed.FromPersonId, listed.ToPersonId, listed.Amount, listed.Note).ShouldBe((bea.PersonId, h.OwnerPersonId, "10.00", "cash"));
    }

    /// <summary>Same sender and recipient, bad amounts, bad dates and spoofed fields are rejected.</summary>
    [Fact]
    public async Task InvalidInput_Returns400()
    {
        var (h, bea, _) = await AlexIsOwed40Async();

        (await RecordAsync(h.Owner, Body(h.OwnerPersonId, h.OwnerPersonId))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        foreach (var amount in new[] { "0", "-5", "1.005", "1e2", "" })
            (await RecordAsync(h.Owner, Body(bea.PersonId, h.OwnerPersonId, amount))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await RecordAsync(h.Owner, Body(bea.PersonId, h.OwnerPersonId, requestId: Guid.Empty))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await RecordAsync(h.Owner, Body(bea.PersonId, h.OwnerPersonId, note: new string('x', 201)))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var spoofed = await h.Owner.PostAsJsonAsync("/api/budget/settlements", new
        {
            clientRequestId = Guid.NewGuid(),
            fromPersonId = bea.PersonId,
            toPersonId = h.OwnerPersonId,
            amount = "5.00",
            paidOn = "2026-10-02",
            addedByPersonId = bea.PersonId,
        }, Ct);
        spoofed.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary>Unrelated outsiders, children and unknown people cannot be party to a repayment.</summary>
    [Fact]
    public async Task OutsidersAndNonAdultsWithoutLedgerHistory_AreRejected()
    {
        var (h, bea, _) = await AlexIsOwed40Async();
        var child = await _scenario.MemberAsync(h, "Child");
        var other = await _scenario.NewHouseholdAsync();

        (await RecordAsync(h.Owner, Body(other.OwnerPersonId, h.OwnerPersonId))).StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await RecordAsync(h.Owner, Body(bea.PersonId, other.OwnerPersonId))).StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await RecordAsync(h.Owner, Body(child.PersonId, h.OwnerPersonId))).StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await RecordAsync(h.Owner, Body(Guid.NewGuid(), h.OwnerPersonId))).StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ListAsync(h.Owner)).TotalCount.ShouldBe(0);
    }

    /// <summary>Only owners and adults can record, list or void; children and guests are denied.</summary>
    [Fact]
    public async Task OnlyAdults_CanUseRepayments()
    {
        var (h, bea, _) = await AlexIsOwed40Async();
        var child = await _scenario.MemberAsync(h, "Child");
        var guest = await _scenario.MemberAsync(h, "Guest");
        var repayment = await RecordedAsync(bea.Client, Body(bea.PersonId, h.OwnerPersonId));

        foreach (var denied in new[] { child.Client, guest.Client })
        {
            (await RecordAsync(denied, Body(bea.PersonId, h.OwnerPersonId))).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            (await denied.GetAsync("/api/budget/settlements", Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            (await VoidAsync(denied, repayment.RepaymentId)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }
    }

    /// <summary>Another household can neither see nor void this household's repayments.</summary>
    [Fact]
    public async Task OtherHousehold_CannotSeeOrVoid()
    {
        var (h, bea, _) = await AlexIsOwed40Async();
        var other = await _scenario.NewHouseholdAsync();
        var repayment = await RecordedAsync(bea.Client, Body(bea.PersonId, h.OwnerPersonId));

        (await ListAsync(other.Owner)).TotalCount.ShouldBe(0);
        (await VoidAsync(other.Owner, repayment.RepaymentId)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ListAsync(h.Owner)).Items.Single().IsVoided.ShouldBeFalse();
    }

    /// <summary>A departed adult and a demoted adult can both still be settled.</summary>
    [Fact]
    public async Task DepartedAndDemotedLedgerParticipants_CanBeSettled()
    {
        var (h, bea, account) = await AlexIsOwed40Async();
        var cy = await _scenario.MemberAsync(h, "Adult");
        (await h.Owner.PostAsJsonAsync("/api/budget/expenses", new
        {
            clientRequestId = Guid.NewGuid(),
            accountId = account,
            amount = "30.00",
            occurredOn = "2026-10-01",
            category = "Groceries",
            fundingSource = "Individual",
            paidByPersonId = h.OwnerPersonId,
            participantIds = new[] { h.OwnerPersonId, cy.PersonId },
        }, Ct)).EnsureSuccessStatusCode();
        (await h.Owner.DeleteAsync($"/api/households/{h.Id}/members/{bea.PersonId}", Ct)).EnsureSuccessStatusCode();
        (await h.Owner.PutAsJsonAsync($"/api/households/{h.Id}/members/{cy.PersonId}/role", new { role = "Child" }, Ct)).EnsureSuccessStatusCode();

        await RecordedAsync(h.Owner, Body(bea.PersonId, h.OwnerPersonId, "40.00"));
        await RecordedAsync(h.Owner, Body(cy.PersonId, h.OwnerPersonId, "15.00"));

        var s = await BalancesAsync(h.Owner);
        s.IsSettled.ShouldBeTrue();
        s.Balances.Single(b => b.PersonId == bea.PersonId).IsFormerAdult.ShouldBeTrue();
        var listed = (await ListAsync(h.Owner)).Items;
        listed.ShouldContain(i => i.FromPersonId == bea.PersonId && i.FromDisplayName.Length > 0);
    }

    /// <summary>Unpaid debt stays outstanding: there is no write-off, and nothing is cleared when a member leaves.</summary>
    [Fact]
    public async Task UnpaidDebt_StaysOutstanding_AfterDeparture()
    {
        var (h, bea, _) = await AlexIsOwed40Async();
        (await h.Owner.DeleteAsync($"/api/households/{h.Id}/members/{bea.PersonId}", Ct)).EnsureSuccessStatusCode();

        var s = await BalancesAsync(h.Owner);

        Net(s, bea.PersonId).ShouldBe(-40m);
        s.IsSettled.ShouldBeFalse();
    }

    /// <summary>A retry returns the original result with 200 and records nothing twice.</summary>
    [Fact]
    public async Task Retry_ReturnsOriginal_AndDoesNotDoubleRecord()
    {
        var (h, bea, _) = await AlexIsOwed40Async();
        var body = Body(bea.PersonId, h.OwnerPersonId, "10.00");

        var first = await RecordedAsync(bea.Client, body);
        var retry = await RecordAsync(bea.Client, body);

        retry.StatusCode.ShouldBe(HttpStatusCode.OK);
        var replay = (await retry.Content.ReadFromJsonAsync<ResultBody>(Ct))!;
        replay.RepaymentId.ShouldBe(first.RepaymentId);
        replay.Revision.ShouldBe(1);
        (await ListAsync(h.Owner)).TotalCount.ShouldBe(1);
        Net(await BalancesAsync(h.Owner), h.OwnerPersonId).ShouldBe(30m);
    }

    /// <summary>A retry after the repayment was voided still returns the original creation result and does not re-record.</summary>
    [Fact]
    public async Task RetryAfterVoid_ReturnsOriginalResult()
    {
        var (h, bea, _) = await AlexIsOwed40Async();
        var body = Body(bea.PersonId, h.OwnerPersonId, "10.00");
        var first = await RecordedAsync(bea.Client, body);
        (await VoidAsync(h.Owner, first.RepaymentId)).EnsureSuccessStatusCode();

        var retry = await RecordAsync(bea.Client, body);

        retry.StatusCode.ShouldBe(HttpStatusCode.OK);
        var replay = (await retry.Content.ReadFromJsonAsync<ResultBody>(Ct))!;
        (replay.RepaymentId, replay.Revision).ShouldBe((first.RepaymentId, 1));
        (await ListAsync(h.Owner)).TotalCount.ShouldBe(1);
        Net(await BalancesAsync(h.Owner), h.OwnerPersonId).ShouldBe(40m);
    }

    /// <summary>A reused key with a different payload is a 409; another actor's key is independent.</summary>
    [Fact]
    public async Task ReusedKey_DifferentPayloadConflicts_OtherActorIndependent()
    {
        var (h, bea, _) = await AlexIsOwed40Async();
        var key = Guid.NewGuid();
        await RecordedAsync(bea.Client, Body(bea.PersonId, h.OwnerPersonId, "10.00", requestId: key));

        (await RecordAsync(bea.Client, Body(bea.PersonId, h.OwnerPersonId, "11.00", requestId: key))).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await RecordAsync(bea.Client, Body(bea.PersonId, h.OwnerPersonId, "10.00", date: "2026-10-03", requestId: key))).StatusCode
            .ShouldBe(HttpStatusCode.Conflict);
        (await RecordAsync(h.Owner, Body(bea.PersonId, h.OwnerPersonId, "11.00", requestId: key))).StatusCode.ShouldBe(HttpStatusCode.Created);
        (await ListAsync(h.Owner)).TotalCount.ShouldBe(2);
    }

    /// <summary>Simultaneous identical submissions record exactly once.</summary>
    [Fact]
    public async Task ConcurrentRetries_RecordOnce()
    {
        var (h, bea, _) = await AlexIsOwed40Async();
        var body = Body(bea.PersonId, h.OwnerPersonId, "10.00");

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => RecordAsync(bea.Client, body)));

        responses.ShouldAllBe(r => r.StatusCode == HttpStatusCode.Created || r.StatusCode == HttpStatusCode.OK);
        responses.Count(r => r.StatusCode == HttpStatusCode.Created).ShouldBe(1);
        (await ListAsync(h.Owner)).TotalCount.ShouldBe(1);
    }

    /// <summary>Void needs a reason and the current revision; repeating a completed void is a no-op with no extra revision.</summary>
    [Fact]
    public async Task Void_Rules_AndIdempotence()
    {
        var (h, bea, _) = await AlexIsOwed40Async();
        var repayment = await RecordedAsync(bea.Client, Body(bea.PersonId, h.OwnerPersonId));

        (await VoidAsync(h.Owner, repayment.RepaymentId, reason: " ")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await VoidAsync(h.Owner, repayment.RepaymentId, expected: 7)).StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var voided = await VoidAsync(h.Owner, repayment.RepaymentId);
        voided.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await voided.Content.ReadFromJsonAsync<ResultBody>(Ct))!.Revision.ShouldBe(2);
        var again = await VoidAsync(h.Owner, repayment.RepaymentId, expected: 1, reason: "again");
        again.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await again.Content.ReadFromJsonAsync<ResultBody>(Ct))!.Revision.ShouldBe(2);

        var listed = (await ListAsync(h.Owner)).Items.Single();
        (listed.IsVoided, listed.Revision, listed.VoidReason).ShouldBe((true, 2, "entered twice"));
        (await VoidAsync(h.Owner, Guid.NewGuid())).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>Simultaneous voids end with one voided repayment at revision 2 and every call succeeding.</summary>
    [Fact]
    public async Task ConcurrentVoids_VoidOnce()
    {
        var (h, bea, _) = await AlexIsOwed40Async();
        var repayment = await RecordedAsync(bea.Client, Body(bea.PersonId, h.OwnerPersonId));

        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => VoidAsync(h.Owner, repayment.RepaymentId)));

        responses.ShouldAllBe(r => r.StatusCode == HttpStatusCode.OK);
        var listed = (await ListAsync(h.Owner)).Items.Single();
        (listed.IsVoided, listed.Revision).ShouldBe((true, 2));
        Net(await BalancesAsync(h.Owner), h.OwnerPersonId).ShouldBe(40m);
    }

    /// <summary>The list is newest payment first, paged and capped, and keeps voided entries marked.</summary>
    [Fact]
    public async Task List_IsOrderedPagedAndKeepsVoided()
    {
        var (h, bea, _) = await AlexIsOwed40Async();
        var oldest = await RecordedAsync(bea.Client, Body(bea.PersonId, h.OwnerPersonId, "1.00", "2026-09-01"));
        await RecordedAsync(bea.Client, Body(bea.PersonId, h.OwnerPersonId, "2.00", "2026-09-03"));
        await RecordedAsync(bea.Client, Body(bea.PersonId, h.OwnerPersonId, "3.00", "2026-09-02"));
        (await VoidAsync(h.Owner, oldest.RepaymentId)).EnsureSuccessStatusCode();

        var first = await ListAsync(h.Owner, "?page=1&pageSize=2");
        var second = await ListAsync(h.Owner, "?page=2&pageSize=2");
        var huge = await ListAsync(h.Owner, "?pageSize=9999");

        first.Items.Select(i => i.Amount).ShouldBe(["2.00", "3.00"]);
        second.Items.Select(i => (i.Amount, i.IsVoided)).ShouldBe([("1.00", true)]);
        first.TotalCount.ShouldBe(3);
        huge.PageSize.ShouldBe(100);
    }

    private sealed record ResultBody(Guid RepaymentId, int Revision);
    private sealed record BalanceBody(Guid PersonId, bool IsFormerAdult, string Net);
    private sealed record TransferBody(Guid FromPersonId, Guid ToPersonId, string Amount);
    private sealed record SettlementBody(bool IsSettled, List<BalanceBody> Balances, List<TransferBody> Suggestions);
    private sealed record SummaryBody(string TotalSpent);
    private sealed record RepaymentBody(
        Guid Id, Guid FromPersonId, string FromDisplayName, Guid ToPersonId, string Amount, string? Note, Guid AddedByPersonId,
        int Revision, bool IsVoided, string? VoidReason);
    private sealed record PageBody(List<RepaymentBody> Items, int TotalCount, int Page, int PageSize);
}
