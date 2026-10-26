namespace Budget.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using Budget.IntegrationTests.Infrastructure;

/// <summary>#673 — the preview suggests the category last imported for a merchant, per budget and scope.</summary>
public sealed class BudgetCategorySuggestionTests : IClassFixture<BudgetDatabaseFixture>, IDisposable
{
    private static readonly byte[] Mbank = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "mbank.csv"));

    private readonly BudgetApiFactory _factory;
    private readonly BudgetScenario _scenario;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public BudgetCategorySuggestionTests(BudgetDatabaseFixture fixture)
    {
        _factory = new BudgetApiFactory(fixture);
        _scenario = new BudgetScenario(_factory);
    }

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public void Dispose() => _factory.Dispose();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<List<CandidateBody>> PreviewAsync(HttpClient client, Guid accountId)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(Mbank), "file", "statement.csv");
        form.Add(new StringContent(accountId.ToString()), "budgetAccountId");
        var response = await client.PostAsync("/api/budget/imports/preview", form, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Ct));
        return (await response.Content.ReadFromJsonAsync<PreviewBody>(Ct))!.Candidates;
    }

    private static Task ImportAsync(HttpClient client, Guid accountId, params object[] rows)
        => ImportAsync(client, Guid.NewGuid(), accountId, HttpStatusCode.Created, rows);

    private static async Task ImportAsync(HttpClient client, Guid requestId, Guid accountId, HttpStatusCode expected, params object[] rows)
    {
        var response = await client.PostAsJsonAsync("/api/budget/imports", new { clientRequestId = requestId, accountId, rows }, Ct);
        response.StatusCode.ShouldBe(expected, await response.Content.ReadAsStringAsync(Ct));
    }

    private static object Row(CandidateBody c, string category, string? sourceKey = null, string? funding = "HouseholdFunds")
        => new
        {
            sourceKey = sourceKey ?? c.SourceKey,
            c.Amount,
            c.OccurredOn,
            category,
            fundingSource = funding,
            c.Description,
            c.MerchantKey,
        };

    /// <summary>The next preview suggests the imported category, and a later override replaces it.</summary>
    [Fact]
    public async Task Preview_AfterImport_SuggestsLatestCategoryForMerchant()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var first = await PreviewAsync(h.Owner, account);
        first.ShouldAllBe(c => c.MerchantKey != null && c.SuggestedCategory == null);

        await ImportAsync(h.Owner, account, [.. first.Select(c => Row(c, "Health"))]);
        (await PreviewAsync(h.Owner, account)).Select(c => c.SuggestedCategory).ShouldAllBe(s => s == "Health");

        await ImportAsync(h.Owner, account, Row(first[0], "Leisure", sourceKey: new string('e', 64)));
        (await PreviewAsync(h.Owner, account)).Select(c => c.SuggestedCategory)
            .ShouldBe([.. first.Select(c => c.MerchantKey == first[0].MerchantKey ? "Leisure" : "Health")]);
    }

    /// <summary>Rules stay in their budget and scope: another household and a personal envelope get no suggestion.</summary>
    [Fact]
    public async Task Preview_OtherHouseholdOrPersonalScope_GetsNoSuggestion()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        await ImportAsync(h.Owner, account, [.. (await PreviewAsync(h.Owner, account)).Select(c => Row(c, "Health"))]);

        var other = await _scenario.NewHouseholdAsync();
        (await PreviewAsync(other.Owner, await BudgetScenario.DefaultEnvelopeAsync(other.Owner)))
            .ShouldAllBe(c => c.SuggestedCategory == null);

        var personal = (await (await h.Owner.PostAsJsonAsync("/api/budget/accounts", new { name = "Mine", visibility = "Personal" }, Ct))
            .Content.ReadFromJsonAsync<AccountBody>(Ct))!.Id;
        (await PreviewAsync(h.Owner, personal)).ShouldAllBe(c => c.SuggestedCategory == null);
    }

    /// <summary>Within one import the last row for a merchant wins; replaying an older import relearns nothing.</summary>
    [Fact]
    public async Task Import_SameMerchantTwiceAndReplay_LastChoiceStands()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var c = (await PreviewAsync(h.Owner, account))[0];
        var first = Guid.NewGuid();
        object[] rows = [Row(c, "Health"), Row(c, "Leisure", sourceKey: new string('e', 64))];

        await ImportAsync(h.Owner, first, account, HttpStatusCode.Created, rows);
        (await PreviewAsync(h.Owner, account))[0].SuggestedCategory.ShouldBe("Leisure");

        await ImportAsync(h.Owner, account, Row(c, "Transport", sourceKey: new string('d', 64)));
        await ImportAsync(h.Owner, first, account, HttpStatusCode.OK, rows);
        (await PreviewAsync(h.Owner, account))[0].SuggestedCategory.ShouldBe("Transport");
    }

    /// <summary>A rule learned in a personal envelope is suggested to its owner there, never in the household envelope or to another adult.</summary>
    [Fact]
    public async Task Preview_PersonalRule_StaysWithItsOwner()
    {
        var h = await _scenario.NewHouseholdAsync();
        var adult = await _scenario.MemberAsync(h, "Adult");
        var personal = (await (await h.Owner.PostAsJsonAsync("/api/budget/accounts", new { name = "Mine", visibility = "Personal" }, Ct))
            .Content.ReadFromJsonAsync<AccountBody>(Ct))!.Id;
        var theirs = (await (await adult.Client.PostAsJsonAsync("/api/budget/accounts", new { name = "Theirs", visibility = "Personal" }, Ct))
            .Content.ReadFromJsonAsync<AccountBody>(Ct))!.Id;
        var household = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);

        await ImportAsync(h.Owner, personal, [.. (await PreviewAsync(h.Owner, personal)).Select(c => Row(c, "Health", funding: null))]);

        (await PreviewAsync(h.Owner, personal)).ShouldAllBe(c => c.SuggestedCategory == "Health");
        (await PreviewAsync(h.Owner, household)).ShouldAllBe(c => c.SuggestedCategory == null);
        (await PreviewAsync(adult.Client, household)).ShouldAllBe(c => c.SuggestedCategory == null);
        (await PreviewAsync(adult.Client, theirs)).ShouldAllBe(c => c.SuggestedCategory == null);
    }

    /// <summary>A blank or oversized merchant key rejects the import with the row's position.</summary>
    [Fact]
    public async Task Import_InvalidMerchantKey_IsRejected()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var c = (await PreviewAsync(h.Owner, account))[0];

        var response = await h.Owner.PostAsJsonAsync("/api/budget/imports", new
        {
            clientRequestId = Guid.NewGuid(),
            accountId = account,
            rows = new[] { new { c.SourceKey, c.Amount, c.OccurredOn, category = "Other", fundingSource = "HouseholdFunds", merchantKey = new string('x', 101) } },
        }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync(Ct)).ShouldContain("Rows[0].MerchantKey");
    }

    private sealed record AccountBody(Guid Id);
    private sealed record CandidateBody(string SourceKey, DateOnly OccurredOn, string Amount, string Description, string? MerchantKey, string? SuggestedCategory);
    private sealed record PreviewBody(List<CandidateBody> Candidates);
}
