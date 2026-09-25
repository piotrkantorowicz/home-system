namespace Household.IntegrationTests.Persistence;

using Household.Domain.Aggregates;
using Household.Domain.ValueObjects;
using Household.Infrastructure.Persistence;
using Household.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using HouseholdAggregate = Household.Domain.Aggregates.Household;

/// <summary>
/// #371 review — accept / decline / revoke all read-then-write the same <c>HouseholdInvitation</c>
/// row with no other locking. These tests drive two independent <c>HouseholdDbContext</c> instances
/// directly (bypassing the API) to prove the EF concurrency token (mapped to PostgreSQL's <c>xmin</c>)
/// actually stops a stale write from silently overwriting a concurrent one, rather than relying on
/// timing through HTTP requests.
/// </summary>
public sealed class HouseholdInvitationConcurrencyTests : IClassFixture<HouseholdDatabaseFixture>, IDisposable
{
    private readonly string _connectionString;
    private readonly List<HouseholdDbContext> _contexts = [];

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared fixture for this collection.</param>
    public HouseholdInvitationConcurrencyTests(HouseholdDatabaseFixture fixture)
        => _connectionString = fixture.ConnectionString;

    /// <summary>Disposes every <c>HouseholdDbContext</c> created for this test instance.</summary>
    public void Dispose()
    {
        foreach (var context in _contexts)
            context.Dispose();
    }

    private HouseholdDbContext NewContext()
    {
        var context = new HouseholdDbContext(
            new DbContextOptionsBuilder<HouseholdDbContext>().UseNpgsql(_connectionString).Options);
        _contexts.Add(context);
        return context;
    }

    private async Task<HouseholdInvitationId> SeedPendingInvitationAsync()
    {
        var now = DateTime.UtcNow;
        var ownerId = PersonId.New();
        var household = HouseholdAggregate.Create(HouseholdId.New(), "Race House", ownerId, now);
        var invitation = HouseholdInvitation.Create(
            HouseholdInvitationId.New(), household.Id, PersonEmail.Create($"racer-{Guid.NewGuid():N}@example.com"),
            targetPersonId: null, HouseholdRole.Adult, ownerId, now);

        await using var seed = NewContext();
        seed.Households.Add(household);
        seed.HouseholdInvitations.Add(invitation);
        await seed.SaveChangesAsync(TestContext.Current.CancellationToken);

        return invitation.Id;
    }

    /// <summary>
    /// Two contexts load the same Pending invitation. One revokes and commits first; the other,
    /// still holding the pre-revoke row, tries to accept — its write must lose to a concurrency
    /// conflict instead of silently flipping Revoked back to Accepted.
    /// </summary>
    [Fact]
    public async Task ConcurrentAcceptAfterRevoke_LosesToConflict_InsteadOfOverwritingTheRevoke()
    {
        var id = await SeedPendingInvitationAsync();
        var now = DateTime.UtcNow;

        var contextA = NewContext();
        var contextB = NewContext();
        var invitationA = await contextA.HouseholdInvitations.SingleAsync(
            i => i.Id == id, TestContext.Current.CancellationToken);
        var invitationB = await contextB.HouseholdInvitations.SingleAsync(
            i => i.Id == id, TestContext.Current.CancellationToken);

        invitationA.Revoke(now.AddMinutes(1));
        await contextA.SaveChangesAsync(TestContext.Current.CancellationToken);

        invitationB.Accept(now.AddMinutes(2));
        await Should.ThrowAsync<DbUpdateConcurrencyException>(
            () => contextB.SaveChangesAsync(TestContext.Current.CancellationToken));

        await using var verify = NewContext();
        var final = await verify.HouseholdInvitations.SingleAsync(
            i => i.Id == id, TestContext.Current.CancellationToken);
        final.Status.ShouldBe(InvitationStatus.Revoked);
    }

    /// <summary>Same race, the other order: a concurrent decline loses to a prior accept instead of overwriting new membership.</summary>
    [Fact]
    public async Task ConcurrentDeclineAfterAccept_LosesToConflict_InsteadOfOverwritingTheAccept()
    {
        var id = await SeedPendingInvitationAsync();
        var now = DateTime.UtcNow;

        var contextA = NewContext();
        var contextB = NewContext();
        var invitationA = await contextA.HouseholdInvitations.SingleAsync(
            i => i.Id == id, TestContext.Current.CancellationToken);
        var invitationB = await contextB.HouseholdInvitations.SingleAsync(
            i => i.Id == id, TestContext.Current.CancellationToken);

        invitationA.Accept(now.AddMinutes(1));
        await contextA.SaveChangesAsync(TestContext.Current.CancellationToken);

        invitationB.Decline(now.AddMinutes(2));
        await Should.ThrowAsync<DbUpdateConcurrencyException>(
            () => contextB.SaveChangesAsync(TestContext.Current.CancellationToken));

        await using var verify = NewContext();
        var final = await verify.HouseholdInvitations.SingleAsync(
            i => i.Id == id, TestContext.Current.CancellationToken);
        final.Status.ShouldBe(InvitationStatus.Accepted);
    }
}
