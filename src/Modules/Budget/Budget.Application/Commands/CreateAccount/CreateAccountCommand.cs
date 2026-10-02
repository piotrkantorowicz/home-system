namespace Budget.Application.Commands.CreateAccount;

using Budget.Application.Queries.GetAccount;
using Shared.Abstractions.Cqrs;

/// <summary>Creates an envelope in the caller's household budget.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="Name">Display name, 1–80 characters.</param>
/// <param name="Visibility"><c>Household</c> (Owner/Adult only) or <c>Personal</c>.</param>
/// <param name="OwnerPersonId">
/// For a personal envelope: the caller (default) or, for an adult, a current managed member.
/// Must be <see langword="null"/> for a household envelope.
/// </param>
public sealed record CreateAccountCommand(string AuthSubject, string Name, string Visibility, Guid? OwnerPersonId)
    : ICommand<AccountDto>;
