namespace DietPlanner.IntegrationTests.Mcp;

using DietPlanner.Api.Mcp;
using DietPlanner.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.DataProtection;

/// <summary>Tests for <c>ImportPreviewTokens</c>: a token only validates for its own caller, options, fingerprint and lifetime.</summary>
public sealed class ImportPreviewTokensTests
{
    private static readonly Guid PersonId = Guid.CreateVersion7();
    private readonly Microsoft.Extensions.Time.Testing.FakeTimeProvider _clock = TestClock.Create();
    private readonly ImportPreviewTokens _sut;

    /// <summary>Builds the system under test with an in-memory key ring and a fake clock.</summary>
    public ImportPreviewTokensTests() => _sut = new ImportPreviewTokens(new EphemeralDataProtectionProvider(), _clock);

    /// <summary>A fresh token validates for the same caller, options and fingerprint.</summary>
    [Fact]
    public void Check_WithMatchingRequest_Accepts()
    {
        var (token, _) = _sut.Issue("user-1", PersonId, false, "fp-1");

        var (fingerprint, error) = _sut.Read(token, "user-1", PersonId, false);

        error.ShouldBeNull();
        fingerprint.ShouldBe("fp-1");
    }

    /// <summary>A token is rejected for another caller, other options, tampering or after expiry.</summary>
    [Theory]
    [InlineData("subject")]
    [InlineData("person")]
    [InlineData("append")]
    [InlineData("tampered")]
    [InlineData("expired")]
    public void Check_WithMismatch_Rejects(string mismatch)
    {
        var (token, _) = _sut.Issue("user-1", PersonId, false, "fp-1");
        if (mismatch == "tampered")
            token = token[..^4] + (token[^4] == 'A' ? "B" : "A") + token[^3..];
        if (mismatch == "expired")
            _clock.Advance(ImportPreviewTokens.Lifetime);

        var (fingerprint, error) = _sut.Read(
            token,
            mismatch == "subject" ? "user-2" : "user-1",
            mismatch == "person" ? Guid.CreateVersion7() : PersonId,
            mismatch == "append");

        fingerprint.ShouldBeNull();
        error.ShouldNotBeNull();
    }
}
