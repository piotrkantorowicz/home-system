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

        // FK relationship to Recipe — MealEntry has no navigation, Recipe has no back-collection
        builder.HasOne<Recipe>()
            .WithMany()
            .HasForeignKey(x => x.RecipeId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK relationship to MealSlot — block slot deletion when entries exist
        builder.HasOne<MealSlot>()
            .WithMany()
            .HasForeignKey(x => x.MealSlotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.UserId, x.Date }).HasDatabaseName("idx_meal_entries_user_date");
        builder.HasIndex(x => x.RecipeId).HasDatabaseName("idx_meal_entries_recipe");
        builder.HasIndex(x => x.MealSlotId).HasDatabaseName("idx_meal_entries_meal_slot");
    }
}
