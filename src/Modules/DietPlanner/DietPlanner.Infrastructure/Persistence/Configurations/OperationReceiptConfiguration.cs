namespace DietPlanner.Infrastructure.Persistence.Configurations;

using DietPlanner.Domain.Ledgers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class OperationReceiptConfiguration : IEntityTypeConfiguration<OperationReceipt>
{
    public void Configure(EntityTypeBuilder<OperationReceipt> builder)
    {
        builder.ToTable("operation_receipts");

        // The key is the uniqueness guarantee: one receipt per caller, operation and idempotency key.
        builder.HasKey(x => new { x.ActorSubject, x.Operation, x.Key });

        builder.Property(x => x.ActorSubject).HasMaxLength(255).HasColumnName("actor_subject");
        builder.Property(x => x.Operation).HasMaxLength(64).HasColumnName("operation");
        builder.Property(x => x.Key).HasMaxLength(128).HasColumnName("key");
        builder.Property(x => x.RequestHash).HasMaxLength(64).HasColumnName("request_hash");
        builder.Property(x => x.ResultId).HasColumnName("result_id");
        builder.Property(x => x.Result).HasColumnType("jsonb").HasColumnName("result");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
    }
}
