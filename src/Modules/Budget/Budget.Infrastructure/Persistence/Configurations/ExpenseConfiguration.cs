namespace Budget.Infrastructure.Persistence.Configurations;

using Budget.Domain.Aggregates;
using Budget.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("expenses", t =>
        {
            t.HasCheckConstraint("ck_expenses_amount_range", "amount > 0 AND amount <= 9999999999.99");
            t.HasCheckConstraint("ck_expenses_payer_matches_funding", "(funding_source = 'HouseholdFunds') = (paid_by_person_id IS NULL)");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(id => id.Value, v => ExpenseId.From(v)).HasColumnName("id");

        builder.Property(x => x.BudgetId).HasConversion(id => id.Value, v => BudgetId.From(v)).HasColumnName("budget_id");
        builder.Property(x => x.BudgetAccountId).HasConversion(id => id.Value, v => BudgetAccountId.From(v)).HasColumnName("budget_account_id");

        // Composite FK: an expense's envelope must belong to the same budget.
        builder.HasOne<BudgetAccount>().WithMany()
            .HasForeignKey(x => new { x.BudgetAccountId, x.BudgetId })
            .HasPrincipalKey(a => new { a.Id, a.BudgetId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Amount).HasColumnType("numeric(18,2)").HasColumnName("amount");
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(16).HasColumnName("category");
        builder.Property(x => x.OccurredOn).HasColumnName("occurred_on");
        builder.Property(x => x.FundingSource).HasConversion<string>().HasMaxLength(16).HasColumnName("funding_source");
        builder.Property(x => x.PaidByPersonId).HasColumnName("paid_by_person_id");
        builder.Property(x => x.PaidByDisplayName).HasMaxLength(200).HasColumnName("paid_by_display_name");
        builder.Property(x => x.AddedByPersonId).HasColumnName("added_by_person_id");
        builder.Property(x => x.AddedByDisplayName).IsRequired().HasMaxLength(200).HasColumnName("added_by_display_name");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.Revision).HasColumnName("revision");
        builder.Property(x => x.IsVoided).HasColumnName("is_voided").HasDefaultValue(false);
        builder.Property(x => x.VoidedAt).HasColumnName("voided_at");

        // Maps to PostgreSQL's xmin system column — no schema change. See Expense.Version.
        builder.Property(x => x.Version).IsRowVersion();

        builder.HasIndex(x => new { x.BudgetId, x.OccurredOn, x.Id }).IsDescending(false, true, true)
            .HasDatabaseName("ix_expenses_budget_date_id");
        builder.HasIndex(x => new { x.BudgetId, x.Category, x.Amount, x.OccurredOn }).HasDatabaseName("ix_expenses_duplicate_lookup");

        builder.OwnsMany(x => x.Shares, share =>
        {
            share.ToTable("expense_shares", t => t.HasCheckConstraint("ck_expense_shares_amount", "amount >= 0"));
            share.WithOwner().HasForeignKey(s => s.ExpenseId);
            share.Property(s => s.ExpenseId).HasConversion(id => id.Value, v => ExpenseId.From(v)).HasColumnName("expense_id");
            share.HasKey(s => new { s.ExpenseId, s.PersonId });
            share.Property(s => s.PersonId).ValueGeneratedNever().HasColumnName("person_id"); // client-set: a new share must be INSERTed, not treated as modified
            share.Property(s => s.PersonDisplayName).IsRequired().HasMaxLength(200).HasColumnName("person_display_name");
            share.Property(s => s.Amount).HasColumnType("numeric(18,2)").HasColumnName("amount");
            share.HasIndex(s => s.PersonId);
        });
        builder.Navigation(x => x.Shares).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Revisions).WithOne().HasForeignKey(r => r.ExpenseId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Revisions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
