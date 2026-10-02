namespace Budget.Infrastructure.Persistence.Configurations;

using Budget.Domain.Entities;
using Budget.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class ExpenseRevisionConfiguration : IEntityTypeConfiguration<ExpenseRevision>
{
    public void Configure(EntityTypeBuilder<ExpenseRevision> builder)
    {
        builder.ToTable("expense_revisions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ExpenseId).HasConversion(id => id.Value, v => ExpenseId.From(v)).HasColumnName("expense_id");
        builder.Property(x => x.BudgetId).HasConversion(id => id.Value, v => BudgetId.From(v)).HasColumnName("budget_id");
        builder.Property(x => x.RevisionNumber).HasColumnName("revision_number");
        builder.Property(x => x.Operation).IsRequired().HasMaxLength(16).HasColumnName("operation");
        builder.Property(x => x.ActorPersonId).HasColumnName("actor_person_id");
        builder.Property(x => x.ClientRequestId).HasColumnName("client_request_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(x => new { x.ExpenseId, x.RevisionNumber }).IsUnique();
        builder.HasIndex(x => new { x.BudgetId, x.ActorPersonId, x.ClientRequestId }).IsUnique()
            .HasDatabaseName("ux_expense_revisions_request_identity");

        builder.ComplexProperty(x => x.Request, b => b.ToJson("request"));
        builder.ComplexProperty(x => x.Snapshot, b =>
        {
            b.ToJson("snapshot");
            b.ComplexCollection(s => s.Shares);
        });
    }
}
