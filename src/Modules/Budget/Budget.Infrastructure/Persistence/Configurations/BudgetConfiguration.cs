namespace Budget.Infrastructure.Persistence.Configurations;

using Budget.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BudgetAggregate = Budget.Domain.Aggregates.Budget;

internal sealed class BudgetConfiguration : IEntityTypeConfiguration<BudgetAggregate>
{
    public void Configure(EntityTypeBuilder<BudgetAggregate> builder)
    {
        builder.ToTable("budgets");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => BudgetId.From(value))
            .HasColumnName("id");

        builder.Property(x => x.HouseholdId).HasColumnName("household_id");
        builder.HasIndex(x => x.HouseholdId).IsUnique();

        builder.Property(x => x.Currency)
            .HasConversion<string>()
            .HasMaxLength(3)
            .HasColumnName("currency");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
    }
}
