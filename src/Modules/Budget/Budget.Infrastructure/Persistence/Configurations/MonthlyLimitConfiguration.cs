namespace Budget.Infrastructure.Persistence.Configurations;

using Budget.Domain.Aggregates;
using Budget.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class MonthlyLimitConfiguration : IEntityTypeConfiguration<MonthlyLimit>
{
    public void Configure(EntityTypeBuilder<MonthlyLimit> builder)
    {
        builder.ToTable("monthly_limits", t =>
        {
            t.HasCheckConstraint("ck_monthly_limits_amount_range", "amount >= 0 AND amount <= 9999999999.99");
            t.HasCheckConstraint("ck_monthly_limits_month_start", "EXTRACT(DAY FROM month_start) = 1");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(id => id.Value, v => MonthlyLimitId.From(v)).HasColumnName("id");
        builder.Property(x => x.BudgetId).HasConversion(id => id.Value, v => BudgetId.From(v)).HasColumnName("budget_id");
        builder.Property(x => x.BudgetAccountId).HasConversion(id => id.Value, v => BudgetAccountId.From(v)).HasColumnName("budget_account_id");

        // Composite FK: a limit's envelope must belong to the same budget.
        builder.HasOne<BudgetAccount>().WithMany()
            .HasForeignKey(x => new { x.BudgetAccountId, x.BudgetId })
            .HasPrincipalKey(a => new { a.Id, a.BudgetId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.MonthStart).HasColumnName("month_start");
        builder.Property(x => x.Amount).HasColumnType("numeric(18,2)").HasColumnName("amount");
        builder.Property(x => x.Revision).HasColumnName("revision");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        // Maps to PostgreSQL's xmin system column — no schema change. See MonthlyLimit.Version.
        builder.Property(x => x.Version).IsRowVersion();

        builder.HasIndex(x => new { x.BudgetAccountId, x.MonthStart }).IsUnique()
            .HasDatabaseName("ux_monthly_limits_account_month");
    }
}
