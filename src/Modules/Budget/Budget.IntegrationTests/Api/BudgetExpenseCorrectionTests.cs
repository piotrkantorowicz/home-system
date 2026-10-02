namespace Budget.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using Budget.IntegrationTests.Infrastructure;
using global::Budget.Domain.ValueObjects;
using global::Budget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>#449 — correcting and voiding expenses: history, concurrency, replay and privacy over HTTP to PostgreSQL.</summary>
public sealed class BudgetExpenseCorrectionTests : IClassFixture<BudgetDatabaseFixture>, IDisposable
{
    private readonly BudgetApiFactory _factory;
    private readonly BudgetScenario _scenario;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public BudgetExpenseCorrectionTests(BudgetDatabaseFixture fixture)
    {
        _factory = new BudgetApiFactory(fixture);
        _scenario = new BudgetScenario(_factory);
    }

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public void Dispose() => _factory.Dispose();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static object CreateBody(Guid accountId, Guid? requestId = null, string amount = "100.00", Guid? paidBy = null, Guid[]? participants = null, string? funding = null)
        => new
        {
            clientRequestId = requestId ?? Guid.NewGuid(),
            accountId,
            amount,
            occurredOn = "2026-10-01",
            category = "Groceries",
            fundingSource = funding,
            paidByPersonId = paidBy,
            participantIds = participants,
        };

    private static object EditBody(int expected, Guid? requestId = null, string amount = "60.00", Guid? paidBy = null, Guid[]? participants = null,
        string reason = "typo", string category = "Groceries", string? funding = null)
        => new
        {
            clientRequestId = requestId ?? Guid.NewGuid(),
            expectedRevision = expected,
            reason,
            amount,
            occurredOn = "2026-10-01",
            category,
            fundingSource = funding,
            paidByPersonId = paidBy,
            participantIds = participants,
        };

    private static async Task<ResultBody> CreatedAsync(HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync("/api/budget/expenses", body, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Ct));
        return (await response.Content.ReadFromJsonAsync<ResultBody>(Ct))!;
    }

    private static Task<HttpResponseMessage> PutAsync(HttpClient client, Guid id, object body)
        => client.PutAsJsonAsync($"/api/budget/expenses/{id}", body, Ct);

    private static Task<HttpResponseMessage> VoidAsync(HttpClient client, Guid id, int expected, Guid? requestId = null, string reason = "oops")
        => client.PostAsJsonAsync($"/api/budget/expenses/{id}/void", new { clientRequestId = requestId ?? Guid.NewGuid(), expectedRevision = expected, reason }, Ct);

    private static async Task<ExpenseBody> GetAsync(HttpClient client, Guid id)
        => (await client.GetFromJsonAsync<ExpenseBody>($"/api/budget/expenses/{id}", Ct))!;

    private static async Task<ResultBody> OkAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Ct));
        return (await response.Content.ReadFromJsonAsync<ResultBody>(Ct))!;
    }

    private async Task<(BudgetScenario.Household H, BudgetScenario.Member Bea, Guid Account, ResultBody Expense)> SharedExpenseAsync()
    {
        var h = await _scenario.NewHouseholdAsync();
        var bea = await _scenario.MemberAsync(h, "Adult");
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var expense = await CreatedAsync(h.Owner, CreateBody(account, paidBy: bea.PersonId, participants: [h.OwnerPersonId, bea.PersonId]));
        return (h, bea, account, expense);
    }

    /// <summary>An edit changes the live row, bumps the revision, keeps the recorder, and history shows previous values and shares.</summary>
    [Fact]
    public async Task Edit_AppendsRevision_KeepsRecorder_AndHistoryHasPreviousValues()
    {
        var (h, bea, _, expense) = await SharedExpenseAsync();

        var edited = await OkAsync(await PutAsync(bea.Client, expense.ExpenseId, EditBody(1, amount: "60.01", paidBy: bea.PersonId, participants: [h.OwnerPersonId, bea.PersonId])));
        var detail = await GetAsync(h.Owner, expense.ExpenseId);

        edited.Revision.ShouldBe(2);
        detail.Amount.ShouldBe("60.01");
        detail.Revision.ShouldBe(2);
        detail.AddedByPersonId.ShouldBe(h.OwnerPersonId);
        detail.Shares.Sum(s => decimal.Parse(s.Amount, System.Globalization.CultureInfo.InvariantCulture)).ShouldBe(60.01m);
        detail.History!.Select(r => (r.RevisionNumber, r.Operation)).ShouldBe([(1, "Create"), (2, "Update")]);
        detail.History![0].Snapshot.Amount.ShouldBe("100.00");
        detail.History![0].Snapshot.Shares.Select(s => s.Amount).Order().ShouldBe(["50.00", "50.00"]);
        detail.History![1].Snapshot.Amount.ShouldBe("60.01");
        detail.History![1].ActorPersonId.ShouldBe(bea.PersonId);
        detail.History![1].Reason.ShouldBe("typo");
    }

    /// <summary>A share-only edit (participants change, amount same) still enforces parent concurrency.</summary>
    [Fact]
    public async Task ShareOnlyEdit_EnforcesParentConcurrency()
    {
        var (h, bea, _, expense) = await SharedExpenseAsync();
        await OkAsync(await PutAsync(h.Owner, expense.ExpenseId, EditBody(1, amount: "100.00", paidBy: bea.PersonId, participants: [bea.PersonId])));

        var stale = await PutAsync(h.Owner, expense.ExpenseId, EditBody(1, amount: "100.00", paidBy: bea.PersonId, participants: [h.OwnerPersonId]));

        stale.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await GetAsync(h.Owner, expense.ExpenseId)).Shares.Select(s => s.PersonId).ShouldBe([bea.PersonId]);
    }

    /// <summary>Two fresh edits on one expected revision: one wins, the rest get 409, and history has no gaps or duplicates.</summary>
    [Fact]
    public async Task ConcurrentEdits_OneSucceeds()
    {
        var (h, bea, _, expense) = await SharedExpenseAsync();

        var responses = await Task.WhenAll(Enumerable.Range(1, 6).Select(i =>
            PutAsync(h.Owner, expense.ExpenseId, EditBody(1, amount: $"{i}.00", paidBy: bea.PersonId, participants: [h.OwnerPersonId, bea.PersonId]))));

        responses.Count(r => r.StatusCode == HttpStatusCode.OK).ShouldBe(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).ShouldBe(5);
        var detail = await GetAsync(h.Owner, expense.ExpenseId);
        detail.Revision.ShouldBe(2);
        detail.History!.Select(r => r.RevisionNumber).ShouldBe([1, 2]);
    }

    /// <summary>The same edit submitted concurrently under one request ID is applied once.</summary>
    [Fact]
    public async Task ConcurrentSameOperation_AppliesOnce()
    {
        var (h, bea, _, expense) = await SharedExpenseAsync();
        var body = EditBody(1, amount: "70.00", paidBy: bea.PersonId, participants: [h.OwnerPersonId, bea.PersonId]);

        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => PutAsync(h.Owner, expense.ExpenseId, body)));

        responses.ShouldAllBe(r => r.StatusCode == HttpStatusCode.OK);
        (await GetAsync(h.Owner, expense.ExpenseId)).Revision.ShouldBe(2);
    }

    /// <summary>create → edit → retry create returns the creation result without a duplicate.</summary>
    [Fact]
    public async Task RetryCreateAfterEdit_ReturnsCreationResult()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var createRequest = Guid.NewGuid();
        var body = CreateBody(account, createRequest, funding: "HouseholdFunds");
        var created = await CreatedAsync(h.Owner, body);
        await OkAsync(await PutAsync(h.Owner, created.ExpenseId, EditBody(1, amount: "5.00", funding: "HouseholdFunds")));

        var retry = await h.Owner.PostAsJsonAsync("/api/budget/expenses", body, Ct);

        retry.StatusCode.ShouldBe(HttpStatusCode.OK);
        var replay = (await retry.Content.ReadFromJsonAsync<ResultBody>(Ct))!;
        replay.ExpenseId.ShouldBe(created.ExpenseId);
        replay.Revision.ShouldBe(1);
        (await h.Owner.GetFromJsonAsync<PageBody>("/api/budget/expenses", Ct))!.TotalCount.ShouldBe(1);
    }

    /// <summary>edit → another edit → retry the first edit returns its original result and reverts nothing.</summary>
    [Fact]
    public async Task RetryFirstEditAfterSecond_DoesNotRevert()
    {
        var (h, bea, _, expense) = await SharedExpenseAsync();
        var firstId = Guid.NewGuid();
        var first = EditBody(1, firstId, "60.00", bea.PersonId, [h.OwnerPersonId, bea.PersonId]);
        var firstResult = await OkAsync(await PutAsync(h.Owner, expense.ExpenseId, first));
        await OkAsync(await PutAsync(h.Owner, expense.ExpenseId, EditBody(2, amount: "70.00", paidBy: bea.PersonId, participants: [h.OwnerPersonId, bea.PersonId])));

        var retry = await OkAsync(await PutAsync(h.Owner, expense.ExpenseId, first));

        retry.ShouldBe(firstResult);
        retry.Revision.ShouldBe(2);
        var detail = await GetAsync(h.Owner, expense.ExpenseId);
        detail.Amount.ShouldBe("70.00");
        detail.Revision.ShouldBe(3);
    }

    /// <summary>A reused key with a different payload or a different target is a 409.</summary>
    [Fact]
    public async Task ReusedKey_WithDifferentPayloadOrTarget_Returns409()
    {
        var (h, bea, account, expense) = await SharedExpenseAsync();
        var other = await CreatedAsync(h.Owner, CreateBody(account, funding: "HouseholdFunds"));
        var key = Guid.NewGuid();
        await OkAsync(await PutAsync(h.Owner, expense.ExpenseId, EditBody(1, key, "60.00", bea.PersonId, [h.OwnerPersonId])));

        (await PutAsync(h.Owner, expense.ExpenseId, EditBody(1, key, "61.00", bea.PersonId, [h.OwnerPersonId]))).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await PutAsync(h.Owner, other.ExpenseId, EditBody(1, key, "60.00", funding: "HouseholdFunds"))).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await VoidAsync(h.Owner, expense.ExpenseId, 2, key)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    /// <summary>Void marks the expense, appends one revision, and a retry — same key or fresh — adds none.</summary>
    [Fact]
    public async Task Void_IsIdempotent_AndAddsOneRevision()
    {
        var (h, _, _, expense) = await SharedExpenseAsync();
        var key = Guid.NewGuid();

        var voided = await OkAsync(await VoidAsync(h.Owner, expense.ExpenseId, 1, key));
        var sameKey = await OkAsync(await VoidAsync(h.Owner, expense.ExpenseId, 1, key));
        var freshKey = await OkAsync(await VoidAsync(h.Owner, expense.ExpenseId, 1));
        var detail = await GetAsync(h.Owner, expense.ExpenseId);

        voided.Revision.ShouldBe(2);
        sameKey.ShouldBe(voided);
        freshKey.Revision.ShouldBe(2);
        detail.IsVoided.ShouldBeTrue();
        detail.History!.Select(r => r.Operation).ShouldBe(["Create", "Void"]);
        detail.History![1].Snapshot.Shares.Count.ShouldBe(2);
    }

    /// <summary>Voided expenses cannot be edited, leave the default list (and duplicate hints), and stay visible by ID and with includeVoided.</summary>
    [Fact]
    public async Task VoidedExpense_RejectsEdits_AndLeavesActiveLists()
    {
        var (h, bea, _, expense) = await SharedExpenseAsync();
        await OkAsync(await VoidAsync(h.Owner, expense.ExpenseId, 1));

        (await PutAsync(h.Owner, expense.ExpenseId, EditBody(2, paidBy: bea.PersonId, participants: [bea.PersonId]))).StatusCode
            .ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await h.Owner.GetFromJsonAsync<PageBody>("/api/budget/expenses", Ct))!.TotalCount.ShouldBe(0);
        (await h.Owner.GetFromJsonAsync<PageBody>("/api/budget/expenses?amount=100.00&category=Groceries", Ct))!.TotalCount.ShouldBe(0);
        (await h.Owner.GetFromJsonAsync<PageBody>("/api/budget/expenses?includeVoided=true", Ct))!.TotalCount.ShouldBe(1);
    }

    /// <summary>A retry after a void still returns the original result (create and edit alike).</summary>
    [Fact]
    public async Task RetryAfterVoid_ReturnsOriginalResults()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var createBody = CreateBody(account, funding: "HouseholdFunds");
        var created = await CreatedAsync(h.Owner, createBody);
        var edit = EditBody(1, amount: "9.00", funding: "HouseholdFunds");
        var edited = await OkAsync(await PutAsync(h.Owner, created.ExpenseId, edit));
        await OkAsync(await VoidAsync(h.Owner, created.ExpenseId, 2));

        (await h.Owner.PostAsJsonAsync("/api/budget/expenses", createBody, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await OkAsync(await PutAsync(h.Owner, created.ExpenseId, edit))).ShouldBe(edited);
    }

    /// <summary>Another actor's key is independent: it neither reuses nor exposes the first actor's result.</summary>
    [Fact]
    public async Task SameKeyFromAnotherActor_IsIndependent()
    {
        var (h, bea, _, expense) = await SharedExpenseAsync();
        var key = Guid.NewGuid();
        var body = EditBody(1, key, "60.00", bea.PersonId, [h.OwnerPersonId, bea.PersonId]);
        var byOwner = await OkAsync(await PutAsync(h.Owner, expense.ExpenseId, body));

        var byBea = await PutAsync(bea.Client, expense.ExpenseId, body);

        byBea.StatusCode.ShouldBe(HttpStatusCode.Conflict); // stale revision for a fresh request, not a replay of the owner's
        byOwner.Revision.ShouldBe(2);
    }

    /// <summary>Retained historical participants survive departure; newly introduced ineligible ones are rejected.</summary>
    [Fact]
    public async Task EditAfterDeparture_KeepsHistoricalParticipantsButRejectsNewOnes()
    {
        var (h, bea, _, expense) = await SharedExpenseAsync();
        var cy = await _scenario.MemberAsync(h, "Adult");
        (await h.Owner.DeleteAsync($"/api/households/{h.Id}/members/{bea.PersonId}", Ct)).EnsureSuccessStatusCode();
        var shareBefore = (await GetAsync(h.Owner, expense.ExpenseId)).Shares.Single(s => s.PersonId == bea.PersonId);

        // Retaining the departed payer/participant is fine; the amount changes so shares are recomputed.
        var kept = await OkAsync(await PutAsync(h.Owner, expense.ExpenseId, EditBody(1, amount: "80.00", paidBy: bea.PersonId, participants: [h.OwnerPersonId, bea.PersonId])));
        var detail = await GetAsync(h.Owner, expense.ExpenseId);

        kept.Revision.ShouldBe(2);
        detail.Shares.Single(s => s.PersonId == bea.PersonId).Amount.ShouldBe("40.00");
        detail.Shares.Single(s => s.PersonId == bea.PersonId).PersonDisplayName.ShouldBe(shareBefore.PersonDisplayName);

        // Introducing someone else ineligible is not: a departed person who was never on it, or a non-adult.
        var stranger = await _scenario.MemberAsync(h, "Child");
        (await PutAsync(h.Owner, expense.ExpenseId, EditBody(2, amount: "80.00", paidBy: bea.PersonId, participants: [h.OwnerPersonId, stranger.PersonId]))).StatusCode
            .ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await PutAsync(h.Owner, expense.ExpenseId, EditBody(2, amount: "80.00", paidBy: stranger.PersonId, participants: [h.OwnerPersonId]))).StatusCode
            .ShouldBe(HttpStatusCode.UnprocessableEntity);
        // A current adult can be newly introduced.
        (await PutAsync(h.Owner, expense.ExpenseId, EditBody(2, amount: "80.00", paidBy: cy.PersonId, participants: [h.OwnerPersonId, cy.PersonId, bea.PersonId]))).StatusCode
            .ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>Archived envelopes still allow historical corrections and voids.</summary>
    [Fact]
    public async Task ArchivedEnvelope_AllowsHistoricalCorrections()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var created = await CreatedAsync(h.Owner, CreateBody(account, funding: "HouseholdFunds"));
        (await h.Owner.PostAsJsonAsync($"/api/budget/accounts/{account}/archive", new { expectedRevision = 1 }, Ct)).EnsureSuccessStatusCode();

        await OkAsync(await PutAsync(h.Owner, created.ExpenseId, EditBody(1, amount: "8.00", funding: "HouseholdFunds")));
        await OkAsync(await VoidAsync(h.Owner, created.ExpenseId, 2));
    }

    /// <summary>Private expenses and their history are invisible to other adults and other households; edits are 404.</summary>
    [Fact]
    public async Task PrivateExpense_HistoryAndMutationsAreHidden()
    {
        var h = await _scenario.NewHouseholdAsync();
        var bea = await _scenario.MemberAsync(h, "Adult");
        var other = await _scenario.NewHouseholdAsync();
        var personal = (await (await bea.Client.PostAsJsonAsync("/api/budget/accounts", new { name = "Mine", visibility = "Personal" }, Ct))
            .Content.ReadFromJsonAsync<IdBody>(Ct))!.Id;
        var secret = await CreatedAsync(bea.Client, CreateBody(personal));

        foreach (var stranger in new[] { h.Owner, other.Owner })
        {
            (await stranger.GetAsync($"/api/budget/expenses/{secret.ExpenseId}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
            (await PutAsync(stranger, secret.ExpenseId, EditBody(1))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
            (await VoidAsync(stranger, secret.ExpenseId, 1)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        await OkAsync(await PutAsync(bea.Client, secret.ExpenseId, EditBody(1, amount: "9.00")));
        (await GetAsync(bea.Client, secret.ExpenseId)).History!.Count.ShouldBe(2);
    }

    /// <summary>Bad edit input is 400; the stored row and history are unchanged.</summary>
    [Fact]
    public async Task InvalidEdit_Returns400_AndChangesNothing()
    {
        var (h, bea, _, expense) = await SharedExpenseAsync();

        (await PutAsync(h.Owner, expense.ExpenseId, EditBody(1, reason: " ", paidBy: bea.PersonId, participants: [bea.PersonId]))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await PutAsync(h.Owner, expense.ExpenseId, EditBody(1, amount: "1.005", paidBy: bea.PersonId, participants: [bea.PersonId]))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await PutAsync(h.Owner, expense.ExpenseId, EditBody(0, paidBy: bea.PersonId, participants: [bea.PersonId]))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await VoidAsync(h.Owner, expense.ExpenseId, 1, reason: "")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        (await GetAsync(h.Owner, expense.ExpenseId)).Revision.ShouldBe(1);
    }

    /// <summary>A domain failure mid-edit rolls the whole change back: no new revision and the live row is unchanged.</summary>
    [Fact]
    public async Task RejectedEdit_RollsBackLiveRowAndHistory()
    {
        var (h, bea, _, expense) = await SharedExpenseAsync();

        (await PutAsync(h.Owner, expense.ExpenseId, EditBody(1, amount: "5.00", paidBy: bea.PersonId, participants: [Guid.NewGuid()]))).StatusCode
            .ShouldBe(HttpStatusCode.UnprocessableEntity);

        var detail = await GetAsync(h.Owner, expense.ExpenseId);
        detail.Amount.ShouldBe("100.00");
        detail.History!.Count.ShouldBe(1);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
        var id = ExpenseId.From(expense.ExpenseId);
        (await db.ExpenseRevisions.CountAsync(r => r.ExpenseId == id, Ct)).ShouldBe(1);
    }

    private sealed record IdBody(Guid Id);
    private sealed record ResultBody(Guid ExpenseId, int Revision);
    private sealed record ShareBody(Guid PersonId, string PersonDisplayName, string Amount);
    private sealed record SnapshotBody(string Amount, bool IsVoided, List<ShareBody> Shares);
    private sealed record HistoryBody(int RevisionNumber, string Operation, Guid ActorPersonId, string? Reason, SnapshotBody Snapshot);
    private sealed record ExpenseBody(string Amount, int Revision, Guid AddedByPersonId, bool IsVoided, List<ShareBody> Shares, List<HistoryBody>? History);
    private sealed record PageBody(int TotalCount);
}
