namespace DietPlanner.UnitTests.Application.Commands;

using DietPlanner.Application.Commands.ExecuteImport;
using DietPlanner.Application.Commands.ValidateImport;
using DietPlanner.Application.ImportPlanning;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

/// <summary>Unit tests for <c>ExecuteImportCommandHandler</c> over the real <c>ImportPlanResolver</c>; repositories and unit of work are substituted with NSubstitute.</summary>
public sealed class ExecuteImportCommandHandlerTests
{
    private const string UserId = "user-1";
    private static readonly Guid PersonId = Guid.Parse("0199a1b2-0000-7000-8000-000000000002");

    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IRecipeRepository _recipes = Substitute.For<IRecipeRepository>();
    private readonly IMealEntryRepository _meals = Substitute.For<IMealEntryRepository>();
    private readonly IMealScheduleConfigRepository _schedules = Substitute.For<IMealScheduleConfigRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ExecuteImportCommandHandler _sut;

    /// <summary>Builds the system under test over an empty catalog and diary.</summary>
    public ExecuteImportCommandHandlerTests()
    {
        _products.GetByNamesAsync(Arg.Any<IReadOnlyCollection<string>>(), UserId, Arg.Any<CancellationToken>()).Returns([]);
        _recipes.GetByNamesAsync(Arg.Any<IReadOnlyCollection<string>>(), UserId, Arg.Any<CancellationToken>()).Returns([]);
        _meals.GetOccupiedSlotsAsync(PersonId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns([]);
        _sut = new ExecuteImportCommandHandler(
            new ImportPlanResolver(_products, _recipes, _meals, _schedules),
            _products, _recipes, _meals, _schedules, _unitOfWork, TestClock.Create());
    }

    /// <summary>With a resolvable file: <c>HandleAsync</c> writes products, recipes and planned meals in one commit.</summary>
    [Fact]
    public async Task HandleAsync_WithResolvableFile_WritesEverythingAndCommitsOnce()
    {
        var lunch = GivenSchedule("Lunch").Slots.Single();

        var result = await _sut.HandleAsync(Command(Import("Lunch", "Rice bowl"), strict: true), TestContext.Current.CancellationToken);

        result.Stats.ShouldBe(new ImportStatsDto(1, 0, 1, 0, 1));
        await _products.Received(1).AddAsync(Arg.Is<Product>(p => p.Name == "Rice"), Arg.Any<CancellationToken>());
        await _recipes.Received(1).AddAsync(Arg.Is<Recipe>(r => r.Ingredients.Count == 1), Arg.Any<CancellationToken>());
        await _meals.Received(1).AddAsync(
            Arg.Is<MealEntry>(m => m.MealSlotId == lunch.Id && m.Status == MealEntryStatus.Planned), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>With an unresolved meal recipe: <c>HandleAsync</c> rejects the file and writes nothing (it used to skip the meal).</summary>
    [Fact]
    public async Task HandleAsync_WhenMealRecipeUnresolved_ThrowsAndWritesNothing()
    {
        GivenSchedule("Lunch");

        var ex = await Should.ThrowAsync<CommandValidationException>(
            () => _sut.HandleAsync(Command(Import("Lunch", "Phantom"), strict: false), TestContext.Current.CancellationToken));

        ex.Errors.ShouldContain(e => e.PropertyName == "schedule[0].meals[0].recipe");
        await _products.DidNotReceiveWithAnyArgs().AddAsync(default!, TestContext.Current.CancellationToken);
        await _unitOfWork.DidNotReceiveWithAnyArgs().CommitAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>In legacy mode without a schedule: <c>HandleAsync</c> creates the default schedule plus an "Other" slot for unknown types.</summary>
    [Fact]
    public async Task HandleAsync_LegacyWithoutScheduleAndUnknownType_CreatesDefaultScheduleWithOther()
    {
        MealScheduleConfig? created = null;
        await _schedules.AddAsync(Arg.Do<MealScheduleConfig>(s => created = s), Arg.Any<CancellationToken>());

        await _sut.HandleAsync(Command(Import("brunch", "Rice bowl"), strict: false), TestContext.Current.CancellationToken);

        created.ShouldNotBeNull();
        created.Slots.Select(s => s.Name).ShouldBe(["Breakfast", "Lunch", "Dinner", "Snack", "Other"]);
        var other = created.Slots.Single(s => s.Name == "Other");
        await _meals.Received(1).AddAsync(Arg.Is<MealEntry>(m => m.MealSlotId == other.Id), Arg.Any<CancellationToken>());
    }

    /// <summary>In strict mode without a schedule: <c>HandleAsync</c> rejects instead of creating one.</summary>
    [Fact]
    public async Task HandleAsync_StrictWithoutSchedule_ThrowsAndCreatesNoSchedule()
    {
        await Should.ThrowAsync<CommandValidationException>(
            () => _sut.HandleAsync(Command(Import("Lunch", "Rice bowl"), strict: true), TestContext.Current.CancellationToken));

        await _schedules.DidNotReceiveWithAnyArgs().AddAsync(default!, TestContext.Current.CancellationToken);
    }

    private static ExecuteImportCommand Command(ImportDto import, bool strict) => new(import, UserId, PersonId, strict);

    private static ImportDto Import(string mealType, string mealRecipe)
        => new(
            [new ImportProductDto("Rice", 130m, 2.7m, 28m, 0.3m, null, "g", null, null)],
            [new ImportRecipeDto("Rice bowl", null, null, 1, null, [new ImportIngredientDto("Rice", 80m, "g")])],
            [new ImportScheduleDto("2026-10-01", [new ImportMealDto(mealType, mealRecipe, 1m, null)])]);

    private MealScheduleConfig GivenSchedule(params string[] slots)
    {
        var schedule = MealScheduleConfig.Create(
            MealScheduleConfigId.New(), PersonId, [.. slots.Select((s, i) => (s, new TimeOnly(7 + i, 0)))], TestClock.UtcNow);
        _schedules.GetByPersonIdAsync(PersonId, Arg.Any<CancellationToken>()).Returns(schedule);
        return schedule;
    }
}
