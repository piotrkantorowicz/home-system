namespace Investments.UnitTests.Domain;

using Investments.Domain.Aggregates;
using Investments.Domain.Exceptions;
using Investments.Domain.ValueObjects;

/// <summary>Invariants of the <c>Wallet</c> aggregate.</summary>
public sealed class WalletTests
{
    private static Wallet NewWallet(string name = "XTB main", WalletVisibility visibility = WalletVisibility.Shared)
        => Wallet.Create(
            WalletId.New(), Guid.CreateVersion7(), Guid.CreateVersion7(), "Anna", name,
            WalletInstitution.XTB, WalletKind.IKE, WalletCurrency.EUR, visibility, TestClock.Now);

    /// <summary>Creation keeps every field and trims the name.</summary>
    [Fact]
    public void Create_Valid_KeepsFieldsAndTrimsName()
    {
        var wallet = NewWallet("  XTB main ");

        wallet.Name.ShouldBe("XTB main");
        wallet.OwnerName.ShouldBe("Anna");
        wallet.Kind.ShouldBe(WalletKind.IKE);
        wallet.Currency.ShouldBe(WalletCurrency.EUR);
        wallet.IsArchived.ShouldBeFalse();
        wallet.CreatedAt.ShouldBe(TestClock.Now);
    }

    /// <summary>Blank and over-long names are rejected on create and rename.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankName_Throws(string name)
        => Should.Throw<InvestmentsDomainException>(() => NewWallet(name));

    /// <summary>A name over the cap is rejected.</summary>
    [Fact]
    public void Rename_TooLong_Throws()
        => Should.Throw<InvestmentsDomainException>(() => NewWallet().Rename(new string('x', Wallet.MaxNameLength + 1)));

    /// <summary>Rename and visibility change touch only those fields.</summary>
    [Fact]
    public void RenameAndChangeVisibility_KeepImmutableFields()
    {
        var wallet = NewWallet();
        var owner = wallet.OwnerPersonId;

        wallet.Rename(" Retirement ");
        wallet.ChangeVisibility(WalletVisibility.Private);

        wallet.Name.ShouldBe("Retirement");
        wallet.Visibility.ShouldBe(WalletVisibility.Private);
        wallet.OwnerPersonId.ShouldBe(owner);
        wallet.Kind.ShouldBe(WalletKind.IKE);
        wallet.Currency.ShouldBe(WalletCurrency.EUR);
    }

    /// <summary>Archive and restore are idempotent toggles.</summary>
    [Fact]
    public void ArchiveRestore_Toggle()
    {
        var wallet = NewWallet();

        wallet.Archive(new DateOnly(2026, 9, 1));
        wallet.Archive(new DateOnly(2026, 9, 2));
        wallet.IsArchived.ShouldBeTrue();
        wallet.ArchivedOn.ShouldBe(new DateOnly(2026, 9, 1));

        wallet.Restore();
        wallet.IsArchived.ShouldBeFalse();
        wallet.ArchivedOn.ShouldBeNull();
    }
}
