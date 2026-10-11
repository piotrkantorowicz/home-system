namespace Investments.IntegrationTests.Api;

using System.Net;
using Investments.IntegrationTests.Infrastructure;

/// <summary>The module composes into the host and its route group is authentication-protected.</summary>
public sealed class InvestmentsRouteGroupTests : IClassFixture<InvestmentsDatabaseFixture>, IDisposable
{
    private readonly InvestmentsApiFactory _factory;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared database for this class.</param>
    public InvestmentsRouteGroupTests(InvestmentsDatabaseFixture fixture) => _factory = new InvestmentsApiFactory(fixture);

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public void Dispose() => _factory.Dispose();

    /// <summary>Anonymous callers are rejected anywhere under the prefix.</summary>
    [Theory]
    [InlineData("/api/investments")]
    [InlineData("/api/investments/wallets")]
    public async Task AnonymousRequest_Returns401(string path)
    {
        var response = await _factory.CreateClient().GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>Signed-in callers get 404 for routes that do not exist yet.</summary>
    [Fact]
    public async Task AuthenticatedRequest_ToUnknownRoute_Returns404()
    {
        var response = await _factory.CreateClientFor("investor-1")
            .GetAsync("/api/investments", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
