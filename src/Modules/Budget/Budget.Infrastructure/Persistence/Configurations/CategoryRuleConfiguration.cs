namespace Budget.Infrastructure.Persistence.Configurations;

using Budget.Domain.Aggregates;
using Budget.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BudgetAggregate = Budget.Domain.Aggregates.Budget;

internal sealed class CategoryRuleConfiguration : IEntityTypeConfiguration<CategoryRule>
{
    public void Configure(EntityTypeBuilder<CategoryRule> builder)
    {
        builder.ToTable("category_rules");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(id => id.Value, v => CategoryRuleId.From(v)).HasColumnName("id");
        builder.Property(x => x.BudgetId).HasConversion(id => id.Value, v => BudgetId.From(v)).HasColumnName("budget_id");
        builder.HasOne<BudgetAggregate>().WithMany().HasForeignKey(x => x.BudgetId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.ImportScope).HasMaxLength(36).HasColumnName("import_scope");
        builder.Property(x => x.MerchantKey).HasMaxLength(CategoryRule.MaxMerchantKeyLength).HasColumnName("merchant_key");
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(16).HasColumnName("category");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(x => new { x.BudgetId, x.ImportScope, x.MerchantKey }).IsUnique()
            .HasDatabaseName("ux_category_rules_merchant");
    }
}
