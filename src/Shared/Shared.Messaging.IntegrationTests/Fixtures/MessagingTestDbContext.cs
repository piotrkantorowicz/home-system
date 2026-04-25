namespace Shared.Messaging.IntegrationTests.Fixtures;

using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Messaging.Ef.Inbox;
using Shared.Infrastructure.Messaging.Ef.Outbox;

internal sealed class MessagingTestDbContext : DbContext
{
    public MessagingTestDbContext(DbContextOptions<MessagingTestDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OutboxMessageEntityConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageEntityConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
