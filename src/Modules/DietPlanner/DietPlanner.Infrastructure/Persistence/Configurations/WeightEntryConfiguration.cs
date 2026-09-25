namespace DietPlanner.Infrastructure.Persistence.Configurations;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class WeightEntryConfiguration : IEntityTypeConfiguration<WeightEntry>
{
    public void Configure(EntityTypeBuilder<WeightEntry> builder)
    {
        builder.ToTable("weight_entries");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => WeightEntryId.From(value))
            .HasColumnName("id");

        builder.Property(x => x.PersonId)
            .IsRequired()
            .HasColumnName("person_id");

        builder.Property(x => x.Date).HasColumnName("date");

        builder.Property(x => x.WeightKg)
            .HasPrecision(8, 2)
            .HasColumnName("weight_kg");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(x => new { x.PersonId, x.Date })
            .IsUnique()
            .HasDatabaseName("idx_weight_entries_person_date");
    }
}
