namespace Shared.Infrastructure.Messaging.Ef.Inbox;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class InboxMessageEntityConfiguration : IEntityTypeConfiguration<InboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<InboxMessageEntity> builder)
    {
        builder.ToTable("inbox_messages");
        builder.HasKey(x => x.EventId);

        builder.Property(x => x.EventId).HasColumnName("event_id");
        builder.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(1024).IsRequired();
        builder.Property(x => x.ConsumedAt).HasColumnName("consumed_at");
    }
}
