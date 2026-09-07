namespace Household.UnitTests.Domain;

using global::Household.Domain.Exceptions;
using global::Household.Domain.ValueObjects;

public sealed class PersonEmailTests
{
    [Theory]
    [InlineData("  Ada@Example.COM ", "ada@example.com")]
    [InlineData("x@y.z", "x@y.z")]
    public void Create_NormalisesToTrimmedLowerCase(string input, string expected)
        => PersonEmail.Create(input).Value.ShouldBe(expected);

    [Theory]
    [InlineData("nope")]
    [InlineData("@nohost")]
    [InlineData("nolocal@")]
    [InlineData("two@@ats")]
    public void Create_WithInvalidAddress_Throws(string input)
        => Should.Throw<HouseholdDomainException>(() => PersonEmail.Create(input));

    [Fact]
    public void CreateOrNull_WithBlank_ReturnsNull()
        => PersonEmail.CreateOrNull("  ").ShouldBeNull();

    [Fact]
    public void Equality_IsByNormalisedValue()
        => PersonEmail.Create("A@B.com").ShouldBe(PersonEmail.Create("a@b.com"));
}
