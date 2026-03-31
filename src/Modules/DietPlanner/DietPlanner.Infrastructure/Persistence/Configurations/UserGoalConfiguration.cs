namespace DietPlanner.Infrastructure.Persistence.Configurations;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class UserGoalConfiguration : IEntityTypeConfiguration<UserGoal>
{
    public void Configure(EntityTypeBuilder<UserGoal> builder)
    {
        builder.ToTable("user_goals");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => UserGoalId.From(value))
            .HasColumnName("id");

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("user_id");

        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasDatabaseName("idx_user_goals_user");

        builder.Property(x => x.DailyCalorieTarget).HasColumnName("daily_calorie_target");
        builder.Property(x => x.ProteinGrams).HasColumnName("protein_grams");
        builder.Property(x => x.CarbsGrams).HasColumnName("carbs_grams");
        builder.Property(x => x.FatGrams).HasColumnName("fat_grams");
        builder.Property(x => x.FiberGrams).HasColumnName("fiber_grams");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}
