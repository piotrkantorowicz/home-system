namespace Household.UnitTests.Domain;

using global::Household.Domain.Exceptions;
using global::Household.Domain.ValueObjects;

/// <summary>Unit tests for <c>PersonEmail</c> domain rules: in-memory only, no infrastructure and no mocks.</summary>
public sealed class PersonEmailTests
{
    /// <summary><c>Create</c> normalises to trimmed lower case.</summary>
    [Theory]
    [InlineData("  Ada@Example.COM ", "ada@example.com")]
    [InlineData("x@y.z", "x@y.z")]
    public void Create_NormalisesToTrimmedLowerCase(string input, string expected)
        => PersonEmail.Create(input).Value.ShouldBe(expected);

    /// <summary>With invalid address: <c>Create</c> throws.</summary>
    [Theory]
    [InlineData("nope")]
    [InlineData("@nohost")]
    [InlineData("nolocal@")]
    [InlineData("two@@ats")]
    public void Create_WithInvalidAddress_Throws(string input)
        => Should.Throw<HouseholdDomainException>(() => PersonEmail.Create(input));

    /// <summary>With blank: <c>CreateOrNull</c> returns null.</summary>
    [Fact]
    public void CreateOrNull_WithBlank_ReturnsNull()
        => PersonEmail.CreateOrNull("  ").ShouldBeNull();

    /// <summary><c>Equality</c> is by normalised value.</summary>
    [Fact]
    public void Equality_IsByNormalisedValue()
        => PersonEmail.Create("A@B.com").ShouldBe(PersonEmail.Create("a@b.com"));
}
