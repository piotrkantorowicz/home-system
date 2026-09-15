namespace DietPlanner.UnitTests.Application.Commands;

#pragma warning disable IDE0005
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

    private static UserProfile CreateProfile(string userId, decimal? currentWeight = 80m)
        => UserProfile.Create(UserProfileId.New(), userId, null, null, null, currentWeight, null, null, TestClock.UtcNow);

    /// <summary>When no existing entry: <c>HandleAsync</c> creates new entry and updates profile.</summary>
    [Fact]
    public async Task HandleAsync_WhenNoExistingEntry_CreatesNewEntryAndUpdatesProfile()
    {
        var command = new LogWeightEntryCommand("user-1", Today, 75m);
        _weightRepo.GetByUserAndDateAsync("user-1", Today, Arg.Any<CancellationToken>())
            .Returns((WeightEntry?)null);
        var profile = CreateProfile("user-1");
        _profileRepo.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>()).Returns(profile);

        LogWeightEntryResult result = await _sut.HandleAsync(command, CancellationToken.None);

        result.Created.ShouldBeTrue();
        await _weightRepo.Received(1).AddAsync(
            Arg.Is<WeightEntry>(e => e.UserId == "user-1" && e.Date == Today && e.WeightKg == 75m),
            Arg.Any<CancellationToken>());
        profile.CurrentWeightKg.ShouldBe(75m);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When entry for date exists: <c>HandleAsync</c> updates existing entry.</summary>
    [Fact]
    public async Task HandleAsync_WhenEntryForDateExists_UpdatesExistingEntry()
    {
        var existing = WeightEntry.Create(WeightEntryId.New(), "user-1", Today, 80m, TestClock.UtcNow);
        var command = new LogWeightEntryCommand("user-1", Today, 79m);
        _weightRepo.GetByUserAndDateAsync("user-1", Today, Arg.Any<CancellationToken>())
            .Returns(existing);
        var profile = CreateProfile("user-1");
        _profileRepo.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>()).Returns(profile);

        LogWeightEntryResult result = await _sut.HandleAsync(command, CancellationToken.None);

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
        var command = new LogWeightEntryCommand("user-1", pastDate, 90m);
        _weightRepo.GetByUserAndDateAsync("user-1", pastDate, Arg.Any<CancellationToken>())
            .Returns((WeightEntry?)null);
        var profile = CreateProfile("user-1", currentWeight: 80m);
        _profileRepo.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>()).Returns(profile);

        await _sut.HandleAsync(command, CancellationToken.None);

        profile.CurrentWeightKg.ShouldBe(90m);
    }

    /// <summary>When profile missing: <c>HandleAsync</c> throws not found exception.</summary>
    [Fact]
    public async Task HandleAsync_WhenProfileMissing_ThrowsNotFoundException()
    {
        var command = new LogWeightEntryCommand("user-1", Today, 75m);
        _weightRepo.GetByUserAndDateAsync("user-1", Today, Arg.Any<CancellationToken>())
            .Returns((WeightEntry?)null);
        _profileRepo.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>())
            .Returns((UserProfile?)null);

        var act = () => _sut.HandleAsync(command, CancellationToken.None);

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
        var command = new LogWeightEntryCommand("user-1", Today, 80m);

        var errors = _sut.Validate(command).ToList();

        errors.ShouldBeEmpty();
    }

    /// <summary>With empty user id: <c>Validate</c> returns validation error.</summary>
    [Fact]
    public void Validate_WithEmptyUserId_ReturnsValidationError()
    {
        var command = new LogWeightEntryCommand("", Today, 80m);

        var errors = _sut.Validate(command).ToList();

        errors.ShouldContain(e => e.PropertyName == nameof(command.UserId));
    }

    /// <summary>With invalid weight: <c>Validate</c> returns validation error.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1000)]
    public void Validate_WithInvalidWeight_ReturnsValidationError(decimal weight)
    {
        var command = new LogWeightEntryCommand("user-1", Today, weight);

        var errors = _sut.Validate(command).ToList();

        errors.ShouldContain(e => e.PropertyName == nameof(command.WeightKg));
    }

    /// <summary>With future date: <c>Validate</c> returns validation error.</summary>
    [Fact]
    public void Validate_WithFutureDate_ReturnsValidationError()
    {
        var command = new LogWeightEntryCommand("user-1", Today.AddDays(1), 80m);

        var errors = _sut.Validate(command).ToList();

        errors.ShouldContain(e => e.PropertyName == nameof(command.Date));
    }
}
