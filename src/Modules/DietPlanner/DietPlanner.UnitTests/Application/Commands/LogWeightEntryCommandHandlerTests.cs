namespace DietPlanner.UnitTests.Application.Commands;

#pragma warning disable IDE0005 // REASON: InternalsVisibleTo prevents Roslyn from resolving internal test types.
using DietPlanner.Application.Commands.LogWeightEntry;
#pragma warning restore IDE0005
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Microsoft.Extensions.Time.Testing;
using Shared.Abstractions.Core.Domain;

/// <summary>Unit tests for <c>LogWeightEntryCommandHandler</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class LogWeightEntryCommandHandlerTests
{
    private static readonly DateOnly Today = TestClock.Today;

    private readonly IWeightEntryRepository _weightRepo = Substitute.For<IWeightEntryRepository>();
    private readonly IUserProfileRepository _profileRepo = Substitute.For<IUserProfileRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _clock = TestClock.Create();
    private readonly LogWeightEntryCommandHandler _sut;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public LogWeightEntryCommandHandlerTests()
        => _sut = new LogWeightEntryCommandHandler(_weightRepo, _profileRepo, _unitOfWork, _clock);

    private static UserProfile CreateProfile(Guid personId, decimal? currentWeight = 80m)
        => UserProfile.Create(UserProfileId.New(), personId, null, null, null, currentWeight, null, null, TestClock.UtcNow);

    /// <summary>When no existing entry: <c>HandleAsync</c> creates new entry and updates profile.</summary>
    [Fact]
    public async Task HandleAsync_WhenNoExistingEntry_CreatesNewEntryAndUpdatesProfile()
    {
        var command = new LogWeightEntryCommand(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Today, 75m);
        _weightRepo.GetByPersonAndDateAsync(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Today, Arg.Any<CancellationToken>())
            .Returns((WeightEntry?)null);
        var profile = CreateProfile(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"));
        _profileRepo.GetByPersonIdAsync(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Arg.Any<CancellationToken>()).Returns(profile);

        LogWeightEntryResult result = await _sut.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Created.ShouldBeTrue();
        await _weightRepo.Received(1).AddAsync(
            Arg.Is<WeightEntry>(e => e.PersonId == Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb") && e.Date == Today && e.WeightKg == 75m),
            Arg.Any<CancellationToken>());
        profile.CurrentWeightKg.ShouldBe(75m);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When entry for date exists: <c>HandleAsync</c> updates existing entry.</summary>
    [Fact]
    public async Task HandleAsync_WhenEntryForDateExists_UpdatesExistingEntry()
    {
        var existing = WeightEntry.Create(WeightEntryId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Today, 80m, TestClock.UtcNow);
        var command = new LogWeightEntryCommand(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Today, 79m);
        _weightRepo.GetByPersonAndDateAsync(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Today, Arg.Any<CancellationToken>())
            .Returns(existing);
        var profile = CreateProfile(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"));
        _profileRepo.GetByPersonIdAsync(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Arg.Any<CancellationToken>()).Returns(profile);

        LogWeightEntryResult result = await _sut.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Created.ShouldBeFalse();
        result.Id.ShouldBe(existing.Id.Value);
        existing.WeightKg.ShouldBe(79m);
        await _weightRepo.DidNotReceive().AddAsync(Arg.Any<WeightEntry>(), Arg.Any<CancellationToken>());
        profile.CurrentWeightKg.ShouldBe(79m);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Always updates profile current weight: <c>HandleAsync</c> even for retroactive entries.</summary>
    [Fact]
    public async Task HandleAsync_AlwaysUpdatesProfileCurrentWeight_EvenForRetroactiveEntries()
    {
        var pastDate = Today.AddDays(-30);
        var command = new LogWeightEntryCommand(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), pastDate, 90m);
        _weightRepo.GetByPersonAndDateAsync(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), pastDate, Arg.Any<CancellationToken>())
            .Returns((WeightEntry?)null);
        var profile = CreateProfile(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), currentWeight: 80m);
        _profileRepo.GetByPersonIdAsync(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Arg.Any<CancellationToken>()).Returns(profile);

        await _sut.HandleAsync(command, TestContext.Current.CancellationToken);

        profile.CurrentWeightKg.ShouldBe(90m);
    }

    /// <summary>When profile missing: <c>HandleAsync</c> throws not found exception.</summary>
    [Fact]
    public async Task HandleAsync_WhenProfileMissing_ThrowsNotFoundException()
    {
        var command = new LogWeightEntryCommand(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Today, 75m);
        _weightRepo.GetByPersonAndDateAsync(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Today, Arg.Any<CancellationToken>())
            .Returns((WeightEntry?)null);
        _profileRepo.GetByPersonIdAsync(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Arg.Any<CancellationToken>())
            .Returns((UserProfile?)null);

        var act = () => _sut.HandleAsync(command, TestContext.Current.CancellationToken);

        await act.ShouldThrowAsync<NotFoundException>();
    }
}

/// <summary>Unit tests for <c>LogWeightEntryCommandValidator</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class LogWeightEntryCommandValidatorTests
{
    private static readonly DateOnly Today = TestClock.Today;

    private readonly LogWeightEntryCommandValidator _sut = new(TestClock.Create());

    /// <summary>With valid command: <c>Validate</c> returns no errors.</summary>
    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var command = new LogWeightEntryCommand(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Today, 80m);

        var errors = _sut.Validate(command).ToList();

        errors.ShouldBeEmpty();
    }

    /// <summary>With empty user id: <c>Validate</c> returns validation error.</summary>
    [Fact]
    public void Validate_WithEmptyPersonId_ReturnsValidationError()
    {
        var command = new LogWeightEntryCommand(Guid.Empty, Today, 80m);

        var errors = _sut.Validate(command).ToList();

        errors.ShouldContain(e => e.PropertyName == nameof(command.PersonId));
    }

    /// <summary>With invalid weight: <c>Validate</c> returns validation error.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1000)]
    public void Validate_WithInvalidWeight_ReturnsValidationError(decimal weight)
    {
        var command = new LogWeightEntryCommand(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Today, weight);

        var errors = _sut.Validate(command).ToList();

        errors.ShouldContain(e => e.PropertyName == nameof(command.WeightKg));
    }

    /// <summary>With future date: <c>Validate</c> returns validation error.</summary>
    [Fact]
    public void Validate_WithFutureDate_ReturnsValidationError()
    {
        var command = new LogWeightEntryCommand(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Today.AddDays(1), 80m);

        var errors = _sut.Validate(command).ToList();

        errors.ShouldContain(e => e.PropertyName == nameof(command.Date));
    }
}
