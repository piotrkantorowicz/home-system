namespace Household.Application.Commands.ConvertManagedMemberToAccount;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Marks a managed household member as convertible to a real account: their next login
/// with <paramref name="Email"/> links the existing <c>Person</c> instead of creating a
/// new one, so all personal data (keyed on <c>PersonId</c>) is preserved.
/// </summary>
public sealed record ConvertManagedMemberToAccountCommand(
    string RequestingAuthSubject,
    Guid HouseholdId,
    Guid PersonId,
    string Email) : ICommand;
