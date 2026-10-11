namespace Investments.Infrastructure.Persistence.Configurations;

using Investments.Domain.Aggregates;
using Investments.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    private const int EnumLength = 16;

    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("wallets", t =>
        {
            t.HasCheckConstraint("ck_wallets_institution", InList("institution", Enum.GetNames<WalletInstitution>()));
            t.HasCheckConstraint("ck_wallets_kind", InList("kind", Enum.GetNames<WalletKind>()));
            t.HasCheckConstraint("ck_wallets_currency", InList("currency", Enum.GetNames<WalletCurrency>()));
            t.HasCheckConstraint("ck_wallets_visibility", InList("visibility", Enum.GetNames<WalletVisibility>()));
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => WalletId.From(value))
            .HasColumnName("id");

        builder.Property(x => x.HouseholdId).HasColumnName("household_id");
        builder.Property(x => x.OwnerPersonId).HasColumnName("owner_person_id");
        builder.HasIndex(x => new { x.HouseholdId, x.OwnerPersonId });

        builder.Property(x => x.OwnerName).IsRequired().HasMaxLength(Wallet.MaxOwnerNameLength).HasColumnName("owner_name");
        builder.Property(x => x.Name).IsRequired().HasMaxLength(Wallet.MaxNameLength).HasColumnName("name");
        builder.Property(x => x.Institution).HasConversion<string>().HasMaxLength(EnumLength).HasColumnName("institution");
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(EnumLength).HasColumnName("kind");
        builder.Property(x => x.Currency).HasConversion<string>().HasMaxLength(3).HasColumnName("currency");
        builder.Property(x => x.Visibility).HasConversion<string>().HasMaxLength(EnumLength).HasColumnName("visibility");
        builder.Property(x => x.IsArchived).HasColumnName("is_archived").HasDefaultValue(false);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        // Maps to PostgreSQL's xmin system column — no schema change. See Wallet.Version.
        builder.Property(x => x.Version).IsRowVersion();
    }

    private static string InList(string column, IEnumerable<string> values)
        => $"{column} IN ({string.Join(", ", values.Select(v => $"'{v}'"))})";
}
