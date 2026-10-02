namespace Budget.Domain.ValueObjects;

/// <summary>Stable reporting code attached to every expense. Labels are localised in the UI; no administration in v1.</summary>
public enum ExpenseCategory
{
    /// <summary>Food and household supplies.</summary>
    Groceries,

    /// <summary>Rent, mortgage, repairs.</summary>
    Housing,

    /// <summary>Energy, water, internet, phone.</summary>
    Utilities,

    /// <summary>Fuel, tickets, car costs.</summary>
    Transport,

    /// <summary>Medical and pharmacy.</summary>
    Health,

    /// <summary>Entertainment and hobbies.</summary>
    Leisure,

    /// <summary>Courses, school costs.</summary>
    Education,

    /// <summary>Anything else.</summary>
    Other,
}
