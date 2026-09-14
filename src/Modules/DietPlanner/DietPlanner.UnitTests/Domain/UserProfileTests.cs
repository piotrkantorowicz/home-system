namespace DietPlanner.UnitTests.Domain;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;

/// <summary>Unit tests for <c>UserProfile</c> domain rules: in-memory only, no infrastructure and no mocks.</summary>
public sealed class UserProfileTests
{
    /// <summary>With valid data: <c>Create</c> creates profile.</summary>
    [Fact]
    public void Create_WithValidData_CreatesProfile()
    {
        var id = UserProfileId.New();

        UserProfile profile = UserProfile.Create(
            id, "user-1",
            new DateOnly(1990, 5, 15), Gender.Male,
            180m, 80m, 75m, ActivityLevel.ModeratelyActive);

        profile.Id.ShouldBe(id);
        profile.UserId.ShouldBe("user-1");
        profile.DateOfBirth.ShouldBe(new DateOnly(1990, 5, 15));
        profile.Gender.ShouldBe(Gender.Male);
        profile.HeightCm.ShouldBe(180m);
        profile.CurrentWeightKg.ShouldBe(80m);
        profile.TargetWeightKg.ShouldBe(75m);
        profile.ActivityLevel.ShouldBe(ActivityLevel.ModeratelyActive);
        profile.UpdatedAt.ShouldBeNull();
    }

    /// <summary>With all nullable fields null: <c>Create</c> creates profile.</summary>
    [Fact]
    public void Create_WithAllNullableFieldsNull_CreatesProfile()
    {
        var id = UserProfileId.New();

        UserProfile profile = UserProfile.Create(id, "user-1", null, null, null, null, null, null);

        profile.Id.ShouldBe(id);
        profile.UserId.ShouldBe("user-1");
        profile.DateOfBirth.ShouldBeNull();
        profile.Gender.ShouldBeNull();
        profile.HeightCm.ShouldBeNull();
        profile.CurrentWeightKg.ShouldBeNull();
        profile.TargetWeightKg.ShouldBeNull();
        profile.ActivityLevel.ShouldBeNull();
    }

    /// <summary>With null user id: <c>Create</c> throws argument exception.</summary>
    [Fact]
    public void Create_WithNullUserId_ThrowsArgumentException()
    {
        var act = () => UserProfile.Create(UserProfileId.New(), null!, null, null, null, null, null, null);

        act.ShouldThrow<ArgumentException>();
    }

    /// <summary>With empty user id: <c>Create</c> throws argument exception.</summary>
    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentException()
    {
        var act = () => UserProfile.Create(UserProfileId.New(), string.Empty, null, null, null, null, null, null);

        act.ShouldThrow<ArgumentException>();
    }

    /// <summary>With new values: <c>Update</c> updates profile.</summary>
    [Fact]
    public void Update_WithNewValues_UpdatesProfile()
    {
        UserProfile profile = UserProfile.Create(
            UserProfileId.New(), "user-1",
            new DateOnly(1990, 5, 15), Gender.Male,
            180m, 80m, 75m, ActivityLevel.Sedentary);

        profile.Update(
            new DateOnly(1990, 5, 15), Gender.Female,
            175m, 70m, 65m, ActivityLevel.VeryActive);

        profile.Gender.ShouldBe(Gender.Female);
        profile.HeightCm.ShouldBe(175m);
        profile.CurrentWeightKg.ShouldBe(70m);
        profile.TargetWeightKg.ShouldBe(65m);
        profile.ActivityLevel.ShouldBe(ActivityLevel.VeryActive);
        profile.UpdatedAt.ShouldNotBeNull();
    }

    /// <summary>With value: <c>UpdateCurrentWeight</c> sets weight and stamps updated at.</summary>
    [Fact]
    public void UpdateCurrentWeight_WithValue_SetsWeightAndStampsUpdatedAt()
    {
        UserProfile profile = UserProfile.Create(
            UserProfileId.New(), "user-1", null, null, null, 80m, null, null);

        profile.UpdateCurrentWeight(82.5m);

        profile.CurrentWeightKg.ShouldBe(82.5m);
        profile.UpdatedAt.ShouldNotBeNull();
    }

    /// <summary>With null: <c>UpdateCurrentWeight</c> clears weight.</summary>
    [Fact]
    public void UpdateCurrentWeight_WithNull_ClearsWeight()
    {
        UserProfile profile = UserProfile.Create(
            UserProfileId.New(), "user-1", null, null, null, 80m, null, null);

        profile.UpdateCurrentWeight(null);

        profile.CurrentWeightKg.ShouldBeNull();
        profile.UpdatedAt.ShouldNotBeNull();
    }

    /// <summary>With null values: <c>Update</c> clears fields.</summary>
    [Fact]
    public void Update_WithNullValues_ClearsFields()
    {
        UserProfile profile = UserProfile.Create(
            UserProfileId.New(), "user-1",
            new DateOnly(1990, 5, 15), Gender.Male,
            180m, 80m, 75m, ActivityLevel.LightlyActive);

        profile.Update(null, null, null, null, null, null);

        profile.DateOfBirth.ShouldBeNull();
        profile.Gender.ShouldBeNull();
        profile.HeightCm.ShouldBeNull();
        profile.CurrentWeightKg.ShouldBeNull();
        profile.TargetWeightKg.ShouldBeNull();
        profile.ActivityLevel.ShouldBeNull();
        profile.UpdatedAt.ShouldNotBeNull();
    }
}
