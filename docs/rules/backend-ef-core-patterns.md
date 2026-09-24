# Backend — EF Core Patterns

> Applies to **Style-1** modules. For Dapper modules see `backend-dapper-module-structure.md`.

## DbContext Per Module

Each module has its **own** DbContext. Never share a DbContext across modules.

```csharp
// BudgetPlan.Infrastructure/Persistence/BudgetPlanDbContext.cs
internal sealed class BudgetPlanDbContext : DbContext, IUnitOfWork
{
    public BudgetPlanDbContext(DbContextOptions<BudgetPlanDbContext> options) : base(options) { }

    public DbSet<BudgetPlan> BudgetPlans => Set<BudgetPlan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all IEntityTypeConfiguration<T> from this assembly automatically
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BudgetPlanDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public async Task CommitAsync(CancellationToken ct = default)
        => await SaveChangesAsync(ct);
}
```

## Entity Type Configuration

Never configure entities in `OnModelCreating` directly — always use `IEntityTypeConfiguration<T>`.

```csharp
// BudgetPlan.Infrastructure/Persistence/Configurations/BudgetPlanConfiguration.cs
internal sealed class BudgetPlanConfiguration : IEntityTypeConfiguration<BudgetPlan>
{
    public void Configure(EntityTypeBuilder<BudgetPlan> builder)
    {
        builder.ToTable("budget_plans");

        // Typed ID — convert to/from Guid for the DB column
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
               .HasConversion(id => id.Value, value => BudgetPlanId.From(value))
               .HasColumnName("id");

        // Value object stored as owned type (two columns in the same table)
        builder.OwnsOne(x => x.Limit, money =>
        {
            money.Property(m => m.Value)
                 .HasColumnName("limit_value")
                 .HasPrecision(18, 4)
                 .IsRequired();
            money.Property(m => m.Currency)
                 .HasColumnName("limit_currency")
                 .HasMaxLength(3)
                 .IsRequired();
        });

        builder.OwnsOne(x => x.Period, period =>
        {
            period.Property(p => p.Start).HasColumnName("period_start").IsRequired();
            period.Property(p => p.End).HasColumnName("period_end").IsRequired();
        });

        builder.Property(x => x.Status)
               .HasConversion<string>()
               .HasColumnName("status")
               .HasMaxLength(32)
               .IsRequired();

        // Private backing field for the collection
        builder.HasMany<BudgetEntry>("_entries")
               .WithOne()
               .HasForeignKey("budget_plan_id")
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation("_entries").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
```

```csharp
// BudgetPlan.Infrastructure/Persistence/Configurations/BudgetEntryConfiguration.cs
internal sealed class BudgetEntryConfiguration : IEntityTypeConfiguration<BudgetEntry>
{
    public void Configure(EntityTypeBuilder<BudgetEntry> builder)
    {
        builder.ToTable("budget_entries");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
               .HasConversion(id => id.Value, value => BudgetEntryId.From(value))
               .HasColumnName("id");

        builder.OwnsOne(x => x.Amount, money =>
        {
            money.Property(m => m.Value).HasColumnName("amount_value").HasPrecision(18, 4);
            money.Property(m => m.Currency).HasColumnName("amount_currency").HasMaxLength(3);
        });

        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(512);
    }
}
```

## Column Naming Convention

Use `snake_case` for all column names. Configure a global naming convention:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // snake_case all columns automatically
    foreach (var entity in modelBuilder.Model.GetEntityTypes())
    {
        entity.SetTableName(entity.GetTableName()?.ToSnakeCase());
        foreach (var property in entity.GetProperties())
            property.SetColumnName(property.GetColumnName().ToSnakeCase());
    }

    modelBuilder.ApplyConfigurationsFromAssembly(typeof(BudgetPlanDbContext).Assembly);
    base.OnModelCreating(modelBuilder);
}
```

## Repository Implementation

```csharp
// BudgetPlan.Infrastructure/Persistence/Repositories/BudgetPlanRepository.cs
internal sealed class BudgetPlanRepository(BudgetPlanDbContext dbContext) : IBudgetPlanRepository
{
    public async Task<BudgetPlan?> GetByIdAsync(BudgetPlanId id, CancellationToken ct = default)
        => await dbContext.BudgetPlans
            .Include("_entries")   // load private backing field
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(BudgetPlan plan, CancellationToken ct = default)
        => await dbContext.BudgetPlans.AddAsync(plan, ct);

    public void Update(BudgetPlan plan)
        => dbContext.BudgetPlans.Update(plan);

    public void Delete(BudgetPlan plan)
        => dbContext.BudgetPlans.Remove(plan);
}
```

## Migrations

- Each module runs its own migrations in its own schema or table prefix.
- Create migration: `dotnet ef migrations add <Name> --project BudgetPlan.Infrastructure --startup-project Api`
- Apply migration: `dotnet ef database update --project BudgetPlan.Infrastructure --startup-project Api`
- Migration files are **committed to source control** — never auto-migrate in production startup.
- Migrations are generated code: `.editorconfig` marks `**/Migrations/*.cs` as such, analyzers skip them.
- Bulk updates that need no aggregate logic: `ExecuteUpdateAsync` / `ExecuteDeleteAsync` (EF 7+),
  never load-modify-save loops.
- Never edit a migration that has been deployed. Add a new one instead.

## Outbox / Inbox Table Configuration

The shared messaging library ships ready-made `IEntityTypeConfiguration<T>` for the
`outbox_messages` and `inbox_messages` tables. Apply them in the module's DbContext:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(BudgetPlanDbContext).Assembly);

    // Add the shared outbox + inbox tables to this module's schema:
    modelBuilder.ApplyConfiguration(new OutboxMessageEntityConfiguration());  // Shared.Infrastructure.Messaging.Ef.Outbox
    modelBuilder.ApplyConfiguration(new InboxMessageEntityConfiguration());    // Shared.Infrastructure.Messaging.Ef.Inbox

    base.OnModelCreating(modelBuilder);
}
```

See `backend-integration-patterns.md` for the publishing/consuming flow and
`.claude/skills/backend-messaging.md` for the full schema (`event_id`, `event_type`,
`payload jsonb`, `occurred_at`, `processed_at`, `attempt_count`, `last_error`).

## DI Registration

```csharp
// BudgetPlan.Infrastructure/DependencyInjection.cs
internal static class InfrastructureDependencyInjection
{
    internal static IServiceCollection AddBudgetPlanInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<BudgetPlanDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("BudgetPlan")));

        services.AddScoped<IBudgetPlanRepository, BudgetPlanRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<BudgetPlanDbContext>());

        return services;
    }
}
```
