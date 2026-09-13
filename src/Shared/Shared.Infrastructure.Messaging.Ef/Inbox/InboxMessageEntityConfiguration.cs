namespace Shared.Infrastructure.Messaging.Ef.Inbox;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Maps <see cref="InboxMessageEntity"/> to the <c>inbox_messages</c> table. A consuming Style-1
/// module applies it in <c>OnModelCreating</c> so the table lives in that module's schema and
/// migrations.
/// </summary>
public sealed class InboxMessageEntityConfiguration : IEntityTypeConfiguration<InboxMessageEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<InboxMessageEntity> builder)
    {
        builder.ToTable("inbox_messages");
        builder.HasKey(x => x.EventId);

        builder.Property(x => x.EventId).HasColumnName("event_id");
        builder.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(1024).IsRequired();
        builder.Property(x => x.ConsumedAt).HasColumnName("consumed_at");
    }
}
