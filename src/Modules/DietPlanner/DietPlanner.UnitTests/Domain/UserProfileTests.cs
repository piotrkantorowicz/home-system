namespace DietPlanner.UnitTests.Domain;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;

public sealed class UserProfileTests
{
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

    [Fact]
    public void Create_WithNullUserId_ThrowsArgumentException()
    {
        var act = () => UserProfile.Create(UserProfileId.New(), null!, null, null, null, null, null, null);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentException()
    {
        var act = () => UserProfile.Create(UserProfileId.New(), string.Empty, null, null, null, null, null, null);

        act.ShouldThrow<ArgumentException>();
    }

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
