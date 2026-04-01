namespace DietPlanner.Infrastructure.Persistence.Configurations;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => UserProfileId.From(value))
            .HasColumnName("id");

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("user_id");

        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasDatabaseName("idx_user_profiles_user");

        builder.Property(x => x.DateOfBirth).HasColumnName("date_of_birth");

        builder.Property(x => x.Gender)
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasColumnName("gender");

        builder.Property(x => x.HeightCm)
            .HasPrecision(8, 2)
            .HasColumnName("height_cm");

        builder.Property(x => x.CurrentWeightKg)
            .HasPrecision(8, 2)
            .HasColumnName("current_weight_kg");

        builder.Property(x => x.TargetWeightKg)
            .HasPrecision(8, 2)
            .HasColumnName("target_weight_kg");

        builder.Property(x => x.ActivityLevel)
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasColumnName("activity_level");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}
