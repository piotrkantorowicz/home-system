namespace Investments.Infrastructure.Persistence.Configurations;

using Investments.Domain.Aggregates;
using Investments.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class ContributionConfiguration : IEntityTypeConfiguration<Contribution>
{
    public void Configure(EntityTypeBuilder<Contribution> builder)
    {
        builder.ToTable("contributions", t =>
        {
            t.HasCheckConstraint("ck_contributions_amount_range", "amount > 0 AND amount <= 9999999999.99");
            t.HasCheckConstraint("ck_contributions_direction", "direction IN ('Deposit', 'Withdrawal')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(id => id.Value, v => ContributionId.From(v)).HasColumnName("id");
        builder.Property(x => x.WalletId).HasConversion(id => id.Value, v => WalletId.From(v)).HasColumnName("wallet_id");
        builder.Property(x => x.HouseholdId).HasColumnName("household_id");

        // Composite FK: a contribution can never point at another household's wallet.
        builder.HasOne<Wallet>().WithMany()
            .HasForeignKey(x => new { x.WalletId, x.HouseholdId })
            .HasPrincipalKey(w => new { w.Id, w.HouseholdId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Date).HasColumnName("date");
        builder.Property(x => x.Direction).HasConversion<string>().HasMaxLength(16).HasColumnName("direction");
        builder.Property(x => x.Amount).HasColumnType("numeric(18,2)").HasColumnName("amount");
        builder.Property(x => x.Note).HasMaxLength(Contribution.MaxNoteLength).HasColumnName("note");
        builder.Property(x => x.RecordedByPersonId).HasColumnName("recorded_by_person_id");
        builder.Property(x => x.RecordedByName).IsRequired().HasMaxLength(Wallet.MaxOwnerNameLength).HasColumnName("recorded_by_name");
        builder.Property(x => x.RecordedAt).HasColumnName("recorded_at");

        builder.HasIndex(x => new { x.WalletId, x.Date }).HasDatabaseName("ix_contributions_wallet_date");
    }
}
