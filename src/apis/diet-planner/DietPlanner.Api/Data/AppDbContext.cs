using DietPlanner.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DietPlanner.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<DietPlan> DietPlans => Set<DietPlan>();
    public DbSet<MealEntry> MealEntries => Set<MealEntry>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");

            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).HasColumnName("id");

            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("name");

            entity.HasIndex(p => p.Name)
                .IsUnique()
                .HasFilter("deleted_at IS NULL");

            entity.Property(p => p.CaloriesPer100g).HasColumnName("calories_per_100g");
            entity.Property(p => p.ProteinPer100g).HasColumnName("protein_per_100g");
            entity.Property(p => p.CarbsPer100g).HasColumnName("carbs_per_100g");
            entity.Property(p => p.FatPer100g).HasColumnName("fat_per_100g");
            entity.Property(p => p.FiberPer100g).HasColumnName("fiber_per_100g");
            entity.Property(p => p.DefaultUnit)
                .HasMaxLength(50)
                .HasColumnName("default_unit");
            entity.Property(p => p.DensityGramsPerMl)
                .HasPrecision(6, 3)
                .HasColumnName("density_grams_per_ml");
            entity.Property(p => p.GramPerPiece)
                .HasPrecision(10, 2)
                .HasColumnName("gram_per_piece");

            entity.Property(p => p.CreatedByUserId)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("created_by_user_id");

            entity.Property(p => p.CreatedAt).HasColumnName("created_at");
            entity.Property(p => p.UpdatedAt).HasColumnName("updated_at");
            entity.Property(p => p.DeletedAt).HasColumnName("deleted_at");

            // Global soft-delete filter — use .IgnoreQueryFilters() where deleted items are needed
            entity.HasQueryFilter(p => p.DeletedAt == null);

            // Indexes
            entity.HasIndex(p => p.CreatedByUserId).HasDatabaseName("idx_products_created_by");
            entity.HasIndex(p => p.Id)
                .HasDatabaseName("idx_products_active")
                .HasFilter("deleted_at IS NULL");
        });

        // Recipe configuration
        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.ToTable("recipes");

            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).HasColumnName("id");

            entity.Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("name");

            entity.HasIndex(r => r.Name)
                .IsUnique()
                .HasFilter("deleted_at IS NULL");

            entity.Property(r => r.Description).HasColumnName("description");
            entity.Property(r => r.Instructions).HasColumnName("instructions");
            entity.Property(r => r.Servings)
                .HasColumnName("servings")
                .HasDefaultValue(1);
            entity.Property(r => r.PrepTimeMinutes).HasColumnName("prep_time_minutes");

            entity.Property(r => r.CreatedByUserId)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("created_by_user_id");

            entity.Property(r => r.CreatedAt).HasColumnName("created_at");
            entity.Property(r => r.UpdatedAt).HasColumnName("updated_at");
            entity.Property(r => r.DeletedAt).HasColumnName("deleted_at");

            // Global soft-delete filter
            entity.HasQueryFilter(r => r.DeletedAt == null);

            // Indexes
            entity.HasIndex(r => r.CreatedByUserId).HasDatabaseName("idx_recipes_created_by");
            entity.HasIndex(r => r.Id)
                .HasDatabaseName("idx_recipes_active")
                .HasFilter("deleted_at IS NULL");
        });

        // RecipeIngredient configuration
        modelBuilder.Entity<RecipeIngredient>(entity =>
        {
            entity.ToTable("recipe_ingredients");

            entity.HasKey(ri => ri.Id);
            entity.Property(ri => ri.Id).HasColumnName("id");

            entity.Property(ri => ri.RecipeId).HasColumnName("recipe_id");
            entity.Property(ri => ri.ProductId).HasColumnName("product_id");
            entity.Property(ri => ri.Amount)
                .HasPrecision(10, 2)
                .HasColumnName("amount");
            entity.Property(ri => ri.Unit)
                .HasMaxLength(50)
                .HasColumnName("unit");

            // Relationships
            entity.HasOne(ri => ri.Recipe)
                .WithMany(r => r.Ingredients)
                .HasForeignKey(ri => ri.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ri => ri.Product)
                .WithMany(p => p.RecipeIngredients)
                .HasForeignKey(ri => ri.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(ri => new { ri.RecipeId, ri.ProductId })
                .IsUnique()
                .HasDatabaseName("unique_recipe_product");

            entity.HasIndex(ri => ri.RecipeId).HasDatabaseName("idx_recipe_ingredients_recipe");
            entity.HasIndex(ri => ri.ProductId).HasDatabaseName("idx_recipe_ingredients_product");
        });

        // DietPlan configuration
        modelBuilder.Entity<DietPlan>(entity =>
        {
            entity.ToTable("diet_plans");

            entity.HasKey(dp => dp.Id);
            entity.Property(dp => dp.Id).HasColumnName("id");

            entity.Property(dp => dp.UserId)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("user_id");

            entity.Property(dp => dp.Name)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("name");

            entity.Property(dp => dp.StartDate).HasColumnName("start_date");
            entity.Property(dp => dp.EndDate).HasColumnName("end_date");
            entity.Property(dp => dp.CreatedAt).HasColumnName("created_at");
            entity.Property(dp => dp.DeletedAt).HasColumnName("deleted_at");

            // Global soft-delete filter
            entity.HasQueryFilter(dp => dp.DeletedAt == null);

            // Indexes
            entity.HasIndex(dp => dp.UserId).HasDatabaseName("idx_diet_plans_user");
            entity.HasIndex(dp => new { dp.StartDate, dp.EndDate })
                .HasDatabaseName("idx_diet_plans_dates");
        });

        // MealEntry configuration
        modelBuilder.Entity<MealEntry>(entity =>
        {
            entity.ToTable("meal_entries");

            entity.HasKey(me => me.Id);
            entity.Property(me => me.Id).HasColumnName("id");

            entity.Property(me => me.DietPlanId).HasColumnName("diet_plan_id");
            entity.Property(me => me.Date).HasColumnName("date");
            entity.Property(me => me.MealType)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("meal_type");
            entity.Property(me => me.RecipeId).HasColumnName("recipe_id");
            entity.Property(me => me.Servings)
                .HasPrecision(5, 2)
                .HasColumnName("servings")
                .HasDefaultValue(1m);
            entity.Property(me => me.Notes).HasColumnName("notes");
            entity.Property(me => me.MealTime).HasColumnName("meal_time");
            entity.Property(me => me.SequenceOrder).HasColumnName("sequence_order");
            entity.Property(me => me.CreatedAt).HasColumnName("created_at");

            // Relationships
            entity.HasOne(me => me.DietPlan)
                .WithMany(dp => dp.MealEntries)
                .HasForeignKey(me => me.DietPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(me => me.Recipe)
                .WithMany(r => r.MealEntries)
                .HasForeignKey(me => me.RecipeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(me => new { me.DietPlanId, me.Date })
                .HasDatabaseName("idx_meal_entries_date");
            entity.HasIndex(me => me.RecipeId)
                .HasDatabaseName("idx_meal_entries_recipe");
        });

    }
}
