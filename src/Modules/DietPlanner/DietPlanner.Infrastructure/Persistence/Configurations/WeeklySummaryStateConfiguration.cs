namespace DietPlanner.Infrastructure.Persistence.Configurations;

using DietPlanner.Domain.Ledgers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class WeeklySummaryStateConfiguration : IEntityTypeConfiguration<WeeklySummaryState>
{
    public void Configure(EntityTypeBuilder<WeeklySummaryState> builder)
    {
        builder.ToTable("weekly_summary_state");

        builder.HasKey(x => x.PersonId);
        builder.Property(x => x.PersonId).HasColumnName("person_id");

        builder.Property(x => x.LastWeeklySummaryAt)
            .HasColumnName("last_weekly_summary_at");
    }
}
