namespace Investments.Domain.ValueObjects;

/// <summary>Whether money went into a wallet or came out of it.</summary>
public enum ContributionDirection
{
    /// <summary>Money put into the wallet.</summary>
    Deposit,

    /// <summary>Money taken out of the wallet.</summary>
    Withdrawal,
}
