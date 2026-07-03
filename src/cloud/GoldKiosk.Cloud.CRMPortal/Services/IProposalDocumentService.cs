using GoldKiosk.Cloud.CRMPortal.Models.Domain;

namespace GoldKiosk.Cloud.CRMPortal.Services;

/// <summary>Renders customer-ready proposal PDFs for leads.</summary>
public interface IProposalDocumentService
{
    /// <summary>Renders the proposal PDF and returns the document bytes.</summary>
    /// <param name="lead">The lead the proposal is for.</param>
    /// <param name="opts">Pricing and content inputs for this render.</param>
    byte[] Render(Lead lead, ProposalRenderOptions opts);
}
