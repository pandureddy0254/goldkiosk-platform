namespace GoldKiosk.Cloud.CRMPortal.Services;

/// <summary>Binds the <c>Proposal</c> configuration section — static content for proposal PDFs.</summary>
public class ProposalOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Proposal";

    /// <summary>Default per-kiosk annual price (USD) when a lead carries no explicit pricing.</summary>
    public decimal DefaultUnitPriceUsd { get; set; } = 12500m;

    /// <summary>Registered office address printed on the PDF.</summary>
    public string OfficeAddress { get; set; } = "8 The Green, STE B, Dover, Delaware, 19901, USA";

    /// <summary>USA contact phone number printed on the PDF.</summary>
    public string PhoneUsa { get; set; } = "+1 (800) 969-0506";

    /// <summary>India contact phone number printed on the PDF.</summary>
    public string PhoneIndia { get; set; } = "+91 (800) 202-2939";

    /// <summary>Name of the signing officer.</summary>
    public string SigneeName { get; set; } = "Nakia Geller";

    /// <summary>Title of the signing officer.</summary>
    public string SigneeTitle { get; set; } = "CEO";
}
