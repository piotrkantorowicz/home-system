namespace DietPlanner.Domain.ValueObjects;

/// <summary>
/// How physically active the user is day to day. Selects the multiplier applied to basal metabolic
/// rate when estimating total daily energy expenditure (1.2 for <see cref="Sedentary"/> up to 1.9
/// for <see cref="ExtraActive"/>).
/// </summary>
public enum ActivityLevel
{
    /// <summary>Little or no exercise; desk job.</summary>
    Sedentary,

    /// <summary>Light exercise one to three days a week.</summary>
    LightlyActive,

    /// <summary>Moderate exercise three to five days a week.</summary>
    ModeratelyActive,

    /// <summary>Hard exercise six to seven days a week.</summary>
    VeryActive,

    /// <summary>Very hard exercise, physical job or training twice a day.</summary>
    ExtraActive
}
