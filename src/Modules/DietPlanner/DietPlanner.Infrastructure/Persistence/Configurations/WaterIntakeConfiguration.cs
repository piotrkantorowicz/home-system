namespace DietPlanner.Infrastructure.Persistence.Configurations;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class WaterIntakeConfiguration : IEntityTypeConfiguration<WaterIntake>
{
    public void Configure(EntityTypeBuilder<WaterIntake> builder)
    {
        builder.ToTable("water_intakes");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => WaterIntakeId.From(value))
            .HasColumnName("id");

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("user_id");

        builder.Property(x => x.Date)
            .HasColumnName("date");

        builder.Property(x => x.AmountMl)
            .HasColumnName("amount_ml");

        builder.Property(x => x.Timestamp)
            .HasColumnName("timestamp");

        builder.Property(x => x.Note)
            .HasMaxLength(500)
            .HasColumnName("note");

        builder.HasIndex(x => new { x.UserId, x.Date })
            .HasDatabaseName("idx_water_intakes_user_date");
    }
}
