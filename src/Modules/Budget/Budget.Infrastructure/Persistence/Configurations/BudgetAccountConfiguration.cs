namespace Budget.Infrastructure.Persistence.Configurations;

using Budget.Domain.Aggregates;
using Budget.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BudgetAggregate = Budget.Domain.Aggregates.Budget;

internal sealed class BudgetAccountConfiguration : IEntityTypeConfiguration<BudgetAccount>
{
    public void Configure(EntityTypeBuilder<BudgetAccount> builder)
    {
        builder.ToTable("budget_accounts", t => t.HasCheckConstraint(
            "ck_budget_accounts_owner_matches_visibility",
            "(visibility = 'Personal') = (owner_person_id IS NOT NULL)"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => BudgetAccountId.From(value))
            .HasColumnName("id");

        builder.Property(x => x.BudgetId)
            .HasConversion(id => id.Value, value => BudgetId.From(value))
            .HasColumnName("budget_id");
        builder.HasOne<BudgetAggregate>().WithMany().HasForeignKey(x => x.BudgetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.BudgetId, x.Visibility, x.OwnerPersonId });

        builder.Property(x => x.Name).IsRequired().HasMaxLength(BudgetAccount.MaxNameLength).HasColumnName("name");

        builder.Property(x => x.Visibility)
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasColumnName("visibility");

        builder.Property(x => x.OwnerPersonId).HasColumnName("owner_person_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.IsArchived).HasColumnName("is_archived").HasDefaultValue(false);
        builder.Property(x => x.Revision).HasColumnName("revision").HasDefaultValue(1);

        // Maps to PostgreSQL's xmin system column — no schema change. See BudgetAccount.Version.
        builder.Property(x => x.Version).IsRowVersion();
    }
}
