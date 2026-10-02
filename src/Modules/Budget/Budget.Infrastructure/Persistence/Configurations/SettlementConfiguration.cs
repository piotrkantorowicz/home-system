namespace Budget.Infrastructure.Persistence.Configurations;

using Budget.Domain.Aggregates;
using Budget.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BudgetAggregate = Budget.Domain.Aggregates.Budget;

internal sealed class SettlementConfiguration : IEntityTypeConfiguration<Settlement>
{
    public void Configure(EntityTypeBuilder<Settlement> builder)
    {
        builder.ToTable("settlements", t =>
        {
            t.HasCheckConstraint("ck_settlements_amount_range", "amount > 0 AND amount <= 9999999999.99");
            t.HasCheckConstraint("ck_settlements_distinct_people", "from_person_id <> to_person_id");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(id => id.Value, v => SettlementId.From(v)).HasColumnName("id");
        builder.Property(x => x.BudgetId).HasConversion(id => id.Value, v => BudgetId.From(v)).HasColumnName("budget_id");
        builder.HasOne<BudgetAggregate>().WithMany().HasForeignKey(x => x.BudgetId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.FromPersonId).HasColumnName("from_person_id");
        builder.Property(x => x.FromDisplayName).IsRequired().HasMaxLength(200).HasColumnName("from_display_name");
        builder.Property(x => x.ToPersonId).HasColumnName("to_person_id");
        builder.Property(x => x.ToDisplayName).IsRequired().HasMaxLength(200).HasColumnName("to_display_name");
        builder.Property(x => x.Amount).HasColumnType("numeric(18,2)").HasColumnName("amount");
        builder.Property(x => x.PaidOn).HasColumnName("paid_on");
        builder.Property(x => x.Note).HasMaxLength(Settlement.MaxNoteLength).HasColumnName("note");
        builder.Property(x => x.AddedByPersonId).HasColumnName("added_by_person_id");
        builder.Property(x => x.AddedByDisplayName).IsRequired().HasMaxLength(200).HasColumnName("added_by_display_name");
        builder.Property(x => x.ClientRequestId).HasColumnName("client_request_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.Revision).HasColumnName("revision");
        builder.Property(x => x.IsVoided).HasColumnName("is_voided").HasDefaultValue(false);
        builder.Property(x => x.VoidedAt).HasColumnName("voided_at");
        builder.Property(x => x.VoidReason).HasMaxLength(Settlement.MaxReasonLength).HasColumnName("void_reason");
        builder.Property(x => x.VoidedByPersonId).HasColumnName("voided_by_person_id");
        builder.Property(x => x.VoidedByDisplayName).HasMaxLength(200).HasColumnName("voided_by_display_name");

        // Maps to PostgreSQL's xmin system column — no schema change. See Settlement.Version.
        builder.Property(x => x.Version).IsRowVersion();

        builder.HasIndex(x => new { x.BudgetId, x.AddedByPersonId, x.ClientRequestId }).IsUnique()
            .HasDatabaseName("ux_settlements_request_identity");
        builder.HasIndex(x => new { x.BudgetId, x.PaidOn }).HasDatabaseName("ix_settlements_budget_date");
    }
}
