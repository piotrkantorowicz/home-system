namespace DietPlanner.Infrastructure.Persistence.Configurations;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Entities;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class MealEntryConfiguration : IEntityTypeConfiguration<MealEntry>
{
    public void Configure(EntityTypeBuilder<MealEntry> builder)
    {
        builder.ToTable("meal_entries");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => MealEntryId.From(value))
            .HasColumnName("id");

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("user_id");

        builder.Property(x => x.Date).HasColumnName("date");

        builder.Property(x => x.MealSlotId)
            .HasConversion(id => id.Value, value => MealSlotId.From(value))
            .HasColumnName("meal_slot_id");

        builder.Property(x => x.RecipeId)
            .HasConversion(id => id.Value, value => RecipeId.From(value))
            .HasColumnName("recipe_id");

        builder.Property(x => x.Servings)
            .HasPrecision(5, 2)
            .HasColumnName("servings")
            .HasDefaultValue(1m);

        builder.Property(x => x.Notes).HasColumnName("notes");
        builder.Property(x => x.MealTime).HasColumnName("meal_time");
        builder.Property(x => x.SequenceOrder).HasColumnName("sequence_order");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(32)
            .HasColumnName("status")
            .HasDefaultValue(MealEntryStatus.Planned);

        builder.Property(x => x.ActualRecipeId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value == null ? null : RecipeId.From(value.Value))
            .HasColumnName("actual_recipe_id");

        // FK relationship to Recipe — MealEntry has no navigation, Recipe has no back-collection
        builder.HasOne<Recipe>()
            .WithMany()
            .HasForeignKey(x => x.RecipeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Recipe>()
            .WithMany()
            .HasForeignKey(x => x.ActualRecipeId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK relationship to MealSlot — block slot deletion when entries exist
        builder.HasOne<MealSlot>()
            .WithMany()
            .HasForeignKey(x => x.MealSlotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.ActualProducts)
            .WithOne()
            .HasForeignKey("meal_entry_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.ActualProducts).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => new { x.UserId, x.Date }).HasDatabaseName("idx_meal_entries_user_date");
        builder.HasIndex(x => x.RecipeId).HasDatabaseName("idx_meal_entries_recipe");
        builder.HasIndex(x => x.MealSlotId).HasDatabaseName("idx_meal_entries_meal_slot");
        builder.HasIndex(x => x.ActualRecipeId).HasDatabaseName("idx_meal_entries_actual_recipe");
    }
}
