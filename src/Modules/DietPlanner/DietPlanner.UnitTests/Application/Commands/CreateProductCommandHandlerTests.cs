namespace DietPlanner.UnitTests.Application.Commands;

using DietPlanner.Application.Commands.CreateProduct;
using DietPlanner.Application.OperationReceipts;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.Repositories;
using Microsoft.Extensions.Time.Testing;
using Shared.Abstractions.Core.Domain;

/// <summary>Unit tests for <c>CreateProductCommandHandler</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class CreateProductCommandHandlerTests
{
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly IOperationReceiptRepository _receipts = Substitute.For<IOperationReceiptRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _clock = TestClock.Create();
    private readonly CreateProductCommandHandler _sut;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public CreateProductCommandHandlerTests()
    {
        _repository.GetByNameAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Product?)null);
        _receipts.TryClaimAsync(Arg.Any<OperationReceipt>(), Arg.Any<CancellationToken>()).Returns(true);
        _sut = new CreateProductCommandHandler(_repository, TestHouseholds.Solo(), new OperationReplay(_receipts), _unitOfWork, _clock);
    }

    /// <summary>With valid command: <c>HandleAsync</c> adds product and commits.</summary>
    [Fact]
    public async Task HandleAsync_WithValidCommand_AddsProductAndCommits()
    {
        var command = new CreateProductCommand("Chicken", 165m, 31m, 0m, 3.6m, 0m, "g", null, null, null, "user-1");

        var id = await _sut.HandleAsync(command, TestContext.Current.CancellationToken);

        id.ShouldNotBe(Guid.Empty);
        await _repository.Received(1).AddAsync(
            Arg.Is<Product>(p =>
                p.Name == "Chicken" &&
                p.CreatedByUserId == "user-1" &&
                p.CreatedAt == _clock.GetUtcNow().UtcDateTime),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary><c>HandleAsync</c> created product has correct nutrition.</summary>
    [Fact]
    public async Task HandleAsync_CreatedProductHasCorrectNutrition()
    {
        var command = new CreateProductCommand("Egg", 155m, 13m, 1.1m, 11m, 0m, "piece", null, 50m, null, "user-1");
        Product? captured = null;
        await _repository.AddAsync(Arg.Do<Product>(p => captured = p), Arg.Any<CancellationToken>());

        await _sut.HandleAsync(command, TestContext.Current.CancellationToken);

        captured.ShouldNotBeNull();
        captured.Nutrition.Calories.ShouldBe(155m);
        captured.Nutrition.Protein.ShouldBe(13m);
        captured.GramPerPiece.ShouldBe(50m);
    }

    /// <summary>With a new operation key: <c>HandleAsync</c> claims a receipt for the product it creates.</summary>
    [Fact]
    public async Task HandleAsync_WithNewOperationKey_ClaimsReceiptForCreatedProduct()
    {
        var id = await _sut.HandleAsync(Keyed("Rice"), TestContext.Current.CancellationToken);

        await _receipts.Received(1).TryClaimAsync(
            Arg.Is<OperationReceipt>(r => r.Key == "key-0001" && r.Operation == "create_product" && r.ActorSubject == "user-1" && r.ResultId == id),
            Arg.Any<CancellationToken>());
        await _repository.Received(1).AddAsync(Arg.Is<Product>(p => p.Id.Value == id), Arg.Any<CancellationToken>());
    }

    /// <summary>With a replayed key and the same fields: <c>HandleAsync</c> returns the original id and writes nothing.</summary>
    [Fact]
    public async Task HandleAsync_WithReplayedKeyAndSameFields_ReturnsOriginalWithoutWriting()
    {
        var original = Guid.CreateVersion7();
        OperationReceipt? claimed = null;
        _receipts.TryClaimAsync(Arg.Do<OperationReceipt>(r => claimed = r), Arg.Any<CancellationToken>()).Returns(false);
        _receipts.FindAsync("user-1", "create_product", "key-0001", Arg.Any<CancellationToken>())
            .Returns(_ => OperationReceipt.Create("user-1", "create_product", "key-0001", claimed!.RequestHash, original, TestClock.UtcNow));

        var id = await _sut.HandleAsync(Keyed("Rice"), TestContext.Current.CancellationToken);

        id.ShouldBe(original);
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, TestContext.Current.CancellationToken);
        await _unitOfWork.DidNotReceiveWithAnyArgs().CommitAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>With a replayed key and different fields: <c>HandleAsync</c> throws a conflict and writes nothing.</summary>
    [Fact]
    public async Task HandleAsync_WithReplayedKeyAndDifferentFields_ThrowsConflict()
    {
        _receipts.TryClaimAsync(Arg.Any<OperationReceipt>(), Arg.Any<CancellationToken>()).Returns(false);
        _receipts.FindAsync("user-1", "create_product", "key-0001", Arg.Any<CancellationToken>())
            .Returns(OperationReceipt.Create("user-1", "create_product", "key-0001", "other-hash", Guid.CreateVersion7(), TestClock.UtcNow));

        await Should.ThrowAsync<ConflictException>(() => _sut.HandleAsync(Keyed("Rice"), TestContext.Current.CancellationToken));

        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, TestContext.Current.CancellationToken);
    }

    /// <summary>Retries that differ only in decimal scale or an omitted default visibility hash as the same request.</summary>
    [Fact]
    public async Task HandleAsync_WithEquivalentFields_ClaimsTheSameHash()
    {
        var hashes = new List<string>();
        _receipts.TryClaimAsync(Arg.Do<OperationReceipt>(r => hashes.Add(r.RequestHash)), Arg.Any<CancellationToken>()).Returns(true);

        await _sut.HandleAsync(Keyed("Rice") with { Calories = 300m }, TestContext.Current.CancellationToken);
        await _sut.HandleAsync(Keyed("Rice") with { Calories = 300.00m, Visibility = "household" }, TestContext.Current.CancellationToken);

        hashes.Count.ShouldBe(2);
        hashes.Distinct().ShouldHaveSingleItem();
    }

    private static CreateProductCommand Keyed(string name)
        => new(name, null, null, null, null, null, "g", null, null, null, "user-1", "key-0001");
}
