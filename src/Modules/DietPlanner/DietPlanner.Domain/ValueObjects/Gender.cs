namespace DietPlanner.Domain.ValueObjects;

/// <summary>
/// Gender as used by the Mifflin-St Jeor basal metabolic rate formula. <see cref="Other"/> averages
/// the male and female constants.
/// </summary>
public enum Gender
{
    /// <summary>Uses the male BMR constant (+5).</summary>
    Male,

    /// <summary>Uses the female BMR constant (−161).</summary>
    Female,

    /// <summary>Uses the average of the male and female constants.</summary>
    Other
}
