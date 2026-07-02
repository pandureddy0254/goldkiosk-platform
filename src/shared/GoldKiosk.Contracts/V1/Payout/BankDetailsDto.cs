namespace GoldKiosk.Contracts.V1.Payout;

/// <summary>
/// Bank account details for a bank-transfer payout. Restricted data: never logged,
/// masked in any UI, encrypted at rest.
/// </summary>
/// <param name="AccountHolder">The account holder's name.</param>
/// <param name="RoutingNumber">The bank routing number.</param>
/// <param name="AccountNumber">The bank account number.</param>
/// <param name="AccountType">The account type, e.g. <c>checking</c> or <c>savings</c>.</param>
public sealed record BankDetailsDto(
    string AccountHolder,
    string RoutingNumber,
    string AccountNumber,
    string AccountType);
