namespace Budget.Domain.Aggregates;

using global::Budget.Domain.Exceptions;
using global::Budget.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>A named spending bucket ("envelope") with household or personal visibility.</summary>
public sealed class BudgetAccount : AggregateRoot<BudgetAccountId>
{
    /// <summary>Maximum length of an envelope name.</summary>
    public const int MaxNameLength = 80;

    private BudgetAccount() { }

    /// <summary>Creates an envelope.</summary>
    /// <param name="id">Identifier for the new envelope.</param>
    /// <param name="budgetId">The budget it belongs to.</param>
    /// <param name="name">Display name; trimmed, 1–80 characters.</param>
    /// <param name="visibility">Who can see it.</param>
    /// <param name="ownerPersonId">The owner of a <see cref="AccountVisibility.Personal"/> envelope; must be <see langword="null"/> for a household one.</param>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    /// <exception cref="BudgetDomainException">The name is blank/too long, or the owner does not match the visibility.</exception>
    public static BudgetAccount Create(
        BudgetAccountId id,
        BudgetId budgetId,
        string name,
        AccountVisibility visibility,
        Guid? ownerPersonId,
        DateTime now)
    {
        var trimmed = NormalizeName(name);

        if ((visibility == AccountVisibility.Personal) != ownerPersonId.HasValue)
            throw new BudgetDomainException("A personal envelope needs an owner; a household envelope has none.");

        return new BudgetAccount
        {
            Id = id,
            BudgetId = budgetId,
            Name = trimmed,
            Visibility = visibility,
            OwnerPersonId = ownerPersonId,
            CreatedAt = now,
            Revision = 1,
        };
    }

    /// <summary>Renames the envelope.</summary>
    /// <param name="name">New display name; trimmed, 1–80 characters.</param>
    /// <param name="expectedRevision">The revision the caller last saw.</param>
    /// <exception cref="ConflictException">The envelope changed since <paramref name="expectedRevision"/>.</exception>
    /// <exception cref="BudgetDomainException">The name is blank or too long.</exception>
    public void Rename(string name, int expectedRevision)
    {
        EnsureRevision(expectedRevision);
        var trimmed = NormalizeName(name);
        if (trimmed == Name)
            return;

        Name = trimmed;
        Revision++;
    }

    /// <summary>Hides the envelope from new-entry choices; history stays. No-op when already archived.</summary>
    /// <param name="expectedRevision">The revision the caller last saw.</param>
    /// <exception cref="ConflictException">The envelope changed since <paramref name="expectedRevision"/>.</exception>
    public void Archive(int expectedRevision)
    {
        EnsureRevision(expectedRevision);
        if (IsArchived)
            return;

        IsArchived = true;
        Revision++;
    }

    /// <summary>Makes an archived envelope available for new entries again. No-op when not archived.</summary>
    /// <param name="expectedRevision">The revision the caller last saw.</param>
    /// <exception cref="ConflictException">The envelope changed since <paramref name="expectedRevision"/>.</exception>
    public void Restore(int expectedRevision)
    {
        EnsureRevision(expectedRevision);
        if (!IsArchived)
            return;

        IsArchived = false;
        Revision++;
    }

    private void EnsureRevision(int expectedRevision)
    {
        if (Revision != expectedRevision)
            throw new ConflictException("This envelope was changed by someone else. Reload and try again.");
    }

    private static string NormalizeName(string? name)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxNameLength)
            throw new BudgetDomainException($"An envelope name must be 1–{MaxNameLength} characters.");

        return trimmed;
    }

    /// <summary>The budget this envelope belongs to.</summary>
    public BudgetId BudgetId { get; private set; } = default!;

    /// <summary>Display name.</summary>
    public string Name { get; private set; } = default!;

    /// <summary>Who can see the envelope.</summary>
    public AccountVisibility Visibility { get; private set; }

    /// <summary>The owner of a personal envelope; <see langword="null"/> for a household one.</summary>
    public Guid? OwnerPersonId { get; private set; }

    /// <summary>Creation time, UTC.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Whether the envelope is hidden from new-entry choices.</summary>
    public bool IsArchived { get; private set; }

    /// <summary>Increases by one on every change; callers send it back to detect stale edits.</summary>
    public int Revision { get; private set; }

    /// <summary>
    /// Concurrency token mapped to PostgreSQL's <c>xmin</c> (see <c>BudgetAccountConfiguration</c>);
    /// never set by domain logic. Catches two writers racing past the <see cref="Revision"/> check.
    /// </summary>
    public uint Version { get; private set; }
}
