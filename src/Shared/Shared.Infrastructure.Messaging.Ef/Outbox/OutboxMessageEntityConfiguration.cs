namespace Shared.Infrastructure.Messaging.Ef.Outbox;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Maps <see cref="OutboxMessageEntity"/> to the <c>outbox_messages</c> table, including the partial
/// index the worker polls on. A publishing Style-1 module applies it in <c>OnModelCreating</c> so the
/// table lives in that module's schema and migrations.
/// </summary>
public sealed class OutboxMessageEntityConfiguration : IEntityTypeConfiguration<OutboxMessageEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.EventId).HasColumnName("event_id");
        builder.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(1024).IsRequired();
        builder.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at");
        builder.Property(x => x.ProcessedAt).HasColumnName("processed_at");
        builder.Property(x => x.AttemptCount).HasColumnName("attempt_count").HasDefaultValue(0);
        builder.Property(x => x.LastError).HasColumnName("last_error");

        builder.HasIndex(x => x.ProcessedAt)
               .HasFilter("\"processed_at\" IS NULL")
               .HasDatabaseName("ix_outbox_messages_unprocessed");
    }
}
