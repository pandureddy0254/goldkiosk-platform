namespace GoldKiosk.Cloud.CRMPortal.Models.Domain;

/// <summary>Mirrors <c>crm.contracts.contract_type</c> (text column).</summary>
public static class ContractType
{
    /// <summary>Master services agreement.</summary>
    public const string Msa = "msa";

    /// <summary>Statement of work.</summary>
    public const string Sow = "sow";

    /// <summary>Data processing agreement.</summary>
    public const string Dpa = "dpa";

    /// <summary>Service level agreement.</summary>
    public const string Sla = "sla";

    /// <summary>Non-disclosure agreement.</summary>
    public const string Nda = "nda";

    /// <summary>Addendum to an existing contract.</summary>
    public const string Addendum = "addendum";
}

/// <summary>Mirrors <c>crm.contracts.status</c> (text column).</summary>
public static class ContractStatus
{
    /// <summary>Draft — not yet sent.</summary>
    public const string Draft = "draft";

    /// <summary>Sent and awaiting signature.</summary>
    public const string OutForSignature = "out_for_signature";

    /// <summary>Fully signed (DB value stays <c>signed</c>; the identifier avoids the CA1720 type-name clash).</summary>
    public const string FullySigned = "signed";

    /// <summary>Terminated.</summary>
    public const string Terminated = "terminated";
}

/// <summary>Maps to <c>crm.contracts</c> — a legal document recorded against a partner.</summary>
public class Contract
{
    /// <summary>Primary key (server-generated uuid).</summary>
    public Guid Id { get; set; }

    /// <summary>The partner the contract belongs to.</summary>
    public Guid PartnerId { get; set; }

    /// <summary>Contract kind — one of <see cref="ContractType"/>. Column name is <c>contract_type</c>.</summary>
    public string ContractTypeCode { get; set; } = ContractType.Msa;

    /// <summary>Lifecycle status — one of <see cref="ContractStatus"/>.</summary>
    public string Status { get; set; } = ContractStatus.Draft;

    /// <summary>When the contract was signed, if it has been.</summary>
    public DateTime? SignedAt { get; set; }

    /// <summary>Contract term length in months, when fixed-term.</summary>
    public int? TermMonths { get; set; }

    /// <summary>Total contract value in <see cref="CurrencyCode"/>.</summary>
    public decimal? ValueAmount { get; set; }

    /// <summary>ISO currency code for <see cref="ValueAmount"/>.</summary>
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>Link to the signed/draft document, when stored externally.</summary>
    public string? DocumentUrl { get; set; }

    /// <summary>Name of the counterparty signatory.</summary>
    public string? SignedByName { get; set; }

    /// <summary>Email of the counterparty signatory (citext column).</summary>
    public string? SignedByEmail { get; set; }

    /// <summary>Row creation timestamp (DB-managed).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Row last-update timestamp (DB trigger-managed).</summary>
    public DateTime UpdatedAt { get; set; }
}
