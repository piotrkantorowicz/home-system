namespace DietPlanner.Infrastructure.Persistence.Configurations;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class HydrationConfigConfiguration : IEntityTypeConfiguration<HydrationConfig>
{
    public void Configure(EntityTypeBuilder<HydrationConfig> builder)
    {
        builder.ToTable("hydration_configs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => HydrationConfigId.From(value))
            .HasColumnName("id");

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("user_id");

        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasDatabaseName("idx_hydration_configs_user");

        builder.Property(x => x.DailyWaterTargetMl).HasColumnName("daily_water_target_ml");
        builder.Property(x => x.GlassSizeMl).HasColumnName("glass_size_ml");
        builder.Property(x => x.TrackWaterIntake).HasColumnName("track_water_intake");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}
