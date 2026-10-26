namespace Budget.UnitTests.Application.StatementImports;

using global::Budget.Application.StatementImports;

/// <summary>#672 — per-row revision request IDs are RFC 9562 UUIDv5.</summary>
public sealed class StatementRowRequestIdTests
{
    /// <summary>The RFC 9562 example: <c>www.example.com</c> in the DNS namespace.</summary>
    [Fact]
    public void For_RfcVector_MatchesUuidV5()
        => StatementRowRequestId.For(Guid.Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8"), "www.example.com")
            .ShouldBe(Guid.Parse("2ed6657d-e927-568b-95e1-2665a8aea6a2"));

    /// <summary>Same import and key give the same ID; another key or import gives another.</summary>
    [Fact]
    public void For_IsDeterministicPerImportAndKey()
    {
        var import = Guid.CreateVersion7();

        StatementRowRequestId.For(import, "a").ShouldBe(StatementRowRequestId.For(import, "a"));
        StatementRowRequestId.For(import, "a").ShouldNotBe(StatementRowRequestId.For(import, "b"));
        StatementRowRequestId.For(import, "a").ShouldNotBe(StatementRowRequestId.For(Guid.CreateVersion7(), "a"));
    }
}
