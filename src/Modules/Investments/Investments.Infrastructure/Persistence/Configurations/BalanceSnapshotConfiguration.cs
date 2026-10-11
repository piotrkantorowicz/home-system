namespace Investments.Infrastructure.Persistence.Configurations;

using Investments.Domain.Aggregates;
using Investments.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class BalanceSnapshotConfiguration : IEntityTypeConfiguration<BalanceSnapshot>
{
    public void Configure(EntityTypeBuilder<BalanceSnapshot> builder)
    {
        builder.ToTable("balance_snapshots", t =>
            t.HasCheckConstraint("ck_balance_snapshots_amount_range", "amount >= 0 AND amount <= 9999999999.99"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(id => id.Value, v => BalanceSnapshotId.From(v)).HasColumnName("id");
        builder.Property(x => x.WalletId).HasConversion(id => id.Value, v => WalletId.From(v)).HasColumnName("wallet_id");
        builder.Property(x => x.HouseholdId).HasColumnName("household_id");

        // Composite FK: a snapshot can never point at another household's wallet.
        builder.HasOne<Wallet>().WithMany()
            .HasForeignKey(x => new { x.WalletId, x.HouseholdId })
            .HasPrincipalKey(w => new { w.Id, w.HouseholdId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.AsOf).HasColumnName("as_of");
        builder.Property(x => x.Amount).HasColumnType("numeric(18,2)").HasColumnName("amount");
        builder.Property(x => x.Note).HasMaxLength(BalanceSnapshot.MaxNoteLength).HasColumnName("note");
        builder.Property(x => x.RecordedByPersonId).HasColumnName("recorded_by_person_id");
        builder.Property(x => x.RecordedByName).IsRequired().HasMaxLength(Wallet.MaxOwnerNameLength).HasColumnName("recorded_by_name");
        builder.Property(x => x.RecordedAt).HasColumnName("recorded_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        // One balance per wallet and date; descending so "latest on or before D" is an index seek.
        builder.HasIndex(x => new { x.WalletId, x.AsOf }).IsUnique().IsDescending(false, true)
            .HasDatabaseName("ux_balance_snapshots_wallet_as_of");
    }
}
