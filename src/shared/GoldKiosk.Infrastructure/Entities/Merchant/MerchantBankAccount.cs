namespace GoldKiosk.Infrastructure.Entities.Merchant;

/// <summary>Maps onto <c>merchant.merchant_bank_accounts</c>.</summary>
public sealed class MerchantBankAccount
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the merchant id.</summary>
    public Guid MerchantId { get; set; }

    /// <summary>Gets or sets the holder name enc.</summary>
    public byte[] HolderNameEnc { get; set; } = Array.Empty<byte>();
    /// <summary>Gets or sets the iban enc.</summary>
    public byte[]? IbanEnc { get; set; }
    /// <summary>Gets or sets the account number enc.</summary>
    public byte[]? AccountNumberEnc { get; set; }

    /// <summary>Gets or sets the swift bic.</summary>
    public string? SwiftBic { get; set; }
    /// <summary>Gets or sets the bank name.</summary>
    public string? BankName { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether is verified.</summary>
    public bool IsVerified { get; set; }
}
