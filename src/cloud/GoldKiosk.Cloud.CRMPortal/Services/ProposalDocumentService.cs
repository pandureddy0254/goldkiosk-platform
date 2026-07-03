using System.Globalization;
using GoldKiosk.Cloud.CRMPortal.Models.Domain;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GoldKiosk.Cloud.CRMPortal.Services;

/// <summary>
/// Generates a customer-ready proposal PDF for a lead using QuestPDF.
/// </summary>
public class ProposalDocumentService : IProposalDocumentService
{
    // Brand colours (QuestPDF takes RGB)
    private const string ColourGold = "#A07F3F";
    private const string ColourInkDark = "#1C1815";
    private const string ColourInkSoft = "#3F372F";
    private const string ColourInkFaint = "#6C6056";
    private const string ColourRule = "#ECE7DF";
    private const string ColourBg = "#FAF7F2";

    private readonly ProposalOptions _opts;
    private readonly IWebHostEnvironment _env;

    /// <summary>Initializes the service with proposal content options and the web root (for image assets).</summary>
    /// <param name="opts">Bound <see cref="ProposalOptions"/>.</param>
    /// <param name="env">Host environment used to resolve wwwroot image paths.</param>
    public ProposalDocumentService(
        IOptions<ProposalOptions> opts,
        IWebHostEnvironment env)
    {
        _opts = opts.Value;
        _env = env;
    }

    /// <inheritdoc/>
    public byte[] Render(Lead lead, ProposalRenderOptions opts)
    {
        ArgumentNullException.ThrowIfNull(lead);
        ArgumentNullException.ThrowIfNull(opts);

        var renderOpts = opts;
        var today = DateTime.UtcNow.Date;
        var validUntil = today.AddDays(renderOpts.ValidityDays);
        var shortId = lead.Id.ToString("N")[..8];
        var propNumber = $"GK-PROP-{shortId.ToUpperInvariant()}";
        var total = renderOpts.UnitPrice * renderOpts.KioskCount;

        // Format money per rules: {code} {amount:N0}
        string Money(decimal amount) =>
            $"{renderOpts.Currency} {amount.ToString("N0", CultureInfo.InvariantCulture)}";

        // Resolve asset paths
        var logoPath = Path.Combine(_env.WebRootPath, "img", "logo-mark-3d-opt.png");
        var sigPath = Path.Combine(_env.WebRootPath, "img", "nakia-signature.png");

        bool hasLogo = File.Exists(logoPath);
        bool hasSig = File.Exists(sigPath);

        // Default deliverables list when none provided
        var defaultDeliverables = new[]
        {
            "Hardware procurement & shipment",
            "On-site installation & calibration",
            "Staff training (8 hours)",
            "First 90 days of remote support",
        };

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(45, Unit.Point);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x
                    .FontFamily(Fonts.Arial)
                    .FontSize(10)
                    .FontColor(ColourInkSoft));

                // ── Watermark layer (behind content) ───────────────────────
                // 60pt fits comfortably in the 505pt A4 content width.
                page.Background()
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("GOLD KIOSK")
                    .FontSize(60)
                    .FontColor("#F0EDE8")
                    .Bold();

                // No page.Header(): the brand block + "PROPOSAL" title appear
                // ONCE only, not repeated on overflow pages. They are the first
                // items inside page.Content() instead. If body content ever
                // overflows, page 2 is a clean continuation.

                // ── Content (single block, may flow across pages) ─────────
                page.Content().Column(body =>
                {
                    // ── One-time brand + office block (renders on page 1 only) ─────
                    body.Item().Row(row =>
                    {
                        // Left: logo + wordmark
                        row.RelativeItem(3).Column(left =>
                        {
                            left.Item().Row(logoRow =>
                            {
                                if (hasLogo)
                                {
                                    logoRow.ConstantItem(52).Height(52).Image(logoPath).FitArea();
                                    logoRow.ConstantItem(10); // spacer
                                }

                                logoRow.RelativeItem().Column(wordmark =>
                                {
                                    wordmark.Item()
                                        .Text("Gold Kiosk")
                                        .FontSize(18)
                                        .Bold()
                                        .FontColor(ColourInkDark);
                                    wordmark.Item()
                                        .Text("Self-service precious-metals terminals")
                                        .FontSize(8)
                                        .FontColor(ColourInkFaint);
                                });
                            });
                        });

                        // Right: office block
                        row.RelativeItem(2).AlignRight().Column(right =>
                        {
                            right.Item()
                                .Text("8 The Green, STE B")
                                .FontSize(8)
                                .FontColor(ColourInkFaint);
                            right.Item()
                                .Text("Dover, Delaware, 19901, USA")
                                .FontSize(8)
                                .FontColor(ColourInkFaint);
                            right.Item().Height(4);
                            right.Item()
                                .Text($"USA: {_opts.PhoneUsa}")
                                .FontSize(8)
                                .FontColor(ColourInkFaint);
                            right.Item()
                                .Text($"India: {_opts.PhoneIndia}")
                                .FontSize(8)
                                .FontColor(ColourInkFaint);
                        });
                    });

                    body.Item().Height(8);
                    body.Item().BorderBottom(1).BorderColor(ColourRule).Height(0);
                    body.Item().Height(12);

                    // ── One-time title row (renders on page 1 only) ─────
                    body.Item().Row(titleRow =>
                    {
                        titleRow.RelativeItem();
                        titleRow.RelativeItem(2).AlignRight().Column(t =>
                        {
                            t.Item()
                                .Text("PROPOSAL")
                                .FontSize(22)
                                .Bold()
                                .FontColor(ColourInkDark)
                                .LetterSpacing(0.08f);
                            t.Item()
                                .Text($"For {lead.CompanyName}")
                                .FontSize(10)
                                .FontColor(ColourGold);
                        });
                    });

                    body.Item().Height(10);

                    // Meta box
                    body.Item().Table(meta =>
                    {
                        meta.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        meta.Cell().Row(1).Column(1).Element(MetaCell)
                            .Column(c =>
                            {
                                c.Item().Text("PROPOSAL #").FontSize(7).FontColor(ColourInkFaint).Bold().LetterSpacing(0.12f);
                                c.Item().Text(propNumber).FontSize(10).FontColor(ColourInkDark).Bold();
                            });

                        meta.Cell().Row(1).Column(2).Element(MetaCell)
                            .Column(c =>
                            {
                                c.Item().Text("DATE").FontSize(7).FontColor(ColourInkFaint).Bold().LetterSpacing(0.12f);
                                c.Item().Text(today.ToString("d MMMM yyyy", CultureInfo.InvariantCulture)).FontSize(10).FontColor(ColourInkDark);
                            });

                        meta.Cell().Row(1).Column(3).Element(MetaCell)
                            .Column(c =>
                            {
                                c.Item().Text("VALID UNTIL").FontSize(7).FontColor(ColourInkFaint).Bold().LetterSpacing(0.12f);
                                c.Item().Text(validUntil.ToString("d MMMM yyyy", CultureInfo.InvariantCulture)).FontSize(10).FontColor(ColourInkDark);
                            });
                    });

                    body.Item().Height(8);

                    // Customer block
                    body.Item().Border(1).BorderColor(ColourRule).Padding(8).Column(cust =>
                    {
                        cust.Item().Text("PREPARED FOR").FontSize(7).Bold().FontColor(ColourInkFaint).LetterSpacing(0.12f);
                        cust.Item().Height(4);
                        cust.Item().Text(lead.CompanyName).FontSize(13).Bold().FontColor(ColourInkDark);
                        cust.Item().Text($"{lead.ContactName}  ({lead.ContactEmail ?? "—"})").FontSize(10).FontColor(ColourInkSoft);

                        var region = lead.Region;
                        var country = lead.Country;
                        if (!string.IsNullOrWhiteSpace(region) || !string.IsNullOrWhiteSpace(country))
                        {
                            var loc = string.Join(", ", new[] { region, country }.Where(x => !string.IsNullOrWhiteSpace(x)));
                            cust.Item().Text(loc).FontSize(9).FontColor(ColourInkFaint);
                        }
                    });

                    body.Item().Height(10);

                    // ── Section: Scope ─────────────────────────────────────
                    body.Item().Column(sec =>
                    {
                        sec.Item().Text("Scope").FontSize(12).Bold().FontColor(ColourInkDark);
                        sec.Item().Height(2).BorderBottom(1).BorderColor(ColourRule);
                        sec.Item().Height(8);

                        var scopeText = string.IsNullOrWhiteSpace(renderOpts.ScopeSummary)
                            ? "To be agreed in scope review."
                            : renderOpts.ScopeSummary;

                        sec.Item().Text(scopeText).FontSize(10).FontColor(ColourInkSoft).LineHeight(1.55f);
                    });

                    body.Item().Height(8);

                    // ── Section: Deliverables ──────────────────────────────
                    body.Item().Column(sec =>
                    {
                        sec.Item().Text("Deliverables").FontSize(12).Bold().FontColor(ColourInkDark);
                        sec.Item().Height(2).BorderBottom(1).BorderColor(ColourRule);
                        sec.Item().Height(8);

                        if (string.IsNullOrWhiteSpace(renderOpts.Deliverables))
                        {
                            foreach (var item in defaultDeliverables)
                            {
                                sec.Item().Row(r =>
                                {
                                    r.ConstantItem(12).Text("•").FontColor(ColourGold).FontSize(10);
                                    r.RelativeItem().Text(item).FontSize(10).FontColor(ColourInkSoft);
                                });
                                sec.Item().Height(3);
                            }
                        }
                        else
                        {
                            sec.Item().Text(renderOpts.Deliverables).FontSize(10).FontColor(ColourInkSoft).LineHeight(1.55f);
                        }
                    });

                    body.Item().Height(8);

                    // ── Section: Pricing ───────────────────────────────────
                    body.Item().Column(sec =>
                    {
                        sec.Item().Text("Pricing").FontSize(12).Bold().FontColor(ColourInkDark);
                        sec.Item().Height(2).BorderBottom(1).BorderColor(ColourRule);
                        sec.Item().Height(8);

                        sec.Item().Table(tbl =>
                        {
                            tbl.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(5);   // Description
                                c.RelativeColumn(1);   // Qty
                                c.RelativeColumn(2);   // Unit price
                                c.RelativeColumn(2);   // Total
                            });

                            // Header row
                            tbl.Header(h =>
                            {
                                static IContainer HeaderCell(IContainer c) =>
                                    c.Background(ColourBg).PaddingVertical(6).PaddingHorizontal(8);

                                h.Cell().Element(HeaderCell).Text("Description").FontSize(8).Bold().FontColor(ColourInkFaint).LetterSpacing(0.1f);
                                h.Cell().Element(HeaderCell).AlignRight().Text("Qty").FontSize(8).Bold().FontColor(ColourInkFaint).LetterSpacing(0.1f);
                                h.Cell().Element(HeaderCell).AlignRight().Text("Unit price").FontSize(8).Bold().FontColor(ColourInkFaint).LetterSpacing(0.1f);
                                h.Cell().Element(HeaderCell).AlignRight().Text("Total").FontSize(8).Bold().FontColor(ColourInkFaint).LetterSpacing(0.1f);
                            });

                            static IContainer BodyCell(IContainer c) =>
                                c.BorderBottom(1).BorderColor("#F0EDE8").PaddingVertical(8).PaddingHorizontal(8);

                            // Data row
                            tbl.Cell().Element(BodyCell)
                                .Text("Gold Kiosk terminal — annual licence + remote support")
                                .FontSize(10).FontColor(ColourInkSoft);
                            tbl.Cell().Element(BodyCell).AlignRight()
                                .Text(renderOpts.KioskCount.ToString(CultureInfo.InvariantCulture))
                                .FontSize(10).FontColor(ColourInkSoft);
                            tbl.Cell().Element(BodyCell).AlignRight()
                                .Text(Money(renderOpts.UnitPrice))
                                .FontSize(10).FontColor(ColourInkSoft);
                            tbl.Cell().Element(BodyCell).AlignRight()
                                .Text(Money(renderOpts.UnitPrice * renderOpts.KioskCount))
                                .FontSize(10).FontColor(ColourInkSoft);

                            // Total row
                            static IContainer TotalCell(IContainer c) =>
                                c.Background(ColourBg).PaddingVertical(8).PaddingHorizontal(8);

                            tbl.Cell().Element(TotalCell)
                                .Text("TOTAL").FontSize(10).Bold().FontColor(ColourInkDark).LetterSpacing(0.06f);
                            tbl.Cell().Element(TotalCell).Text("");
                            tbl.Cell().Element(TotalCell).Text("");
                            tbl.Cell().Element(TotalCell).AlignRight()
                                .Text(Money(total))
                                .FontSize(11).Bold().FontColor(ColourInkDark);
                        });
                    });

                    body.Item().Height(8);

                    // ── Section: Payment terms ─────────────────────────────
                    body.Item().Column(sec =>
                    {
                        sec.Item().Text("Payment terms").FontSize(12).Bold().FontColor(ColourInkDark);
                        sec.Item().Height(2).BorderBottom(1).BorderColor(ColourRule);
                        sec.Item().Height(8);

                        var terms = string.IsNullOrWhiteSpace(renderOpts.PaymentTerms)
                            ? "50% on signature, 50% on delivery. Net 30."
                            : renderOpts.PaymentTerms;

                        sec.Item().Text(terms).FontSize(10).FontColor(ColourInkSoft).LineHeight(1.55f);
                    });

                    body.Item().Height(8);

                    // ── Section: Validity ──────────────────────────────────
                    body.Item().Column(sec =>
                    {
                        sec.Item().Text("Validity").FontSize(12).Bold().FontColor(ColourInkDark);
                        sec.Item().Height(2).BorderBottom(1).BorderColor(ColourRule);
                        sec.Item().Height(8);

                        sec.Item()
                            .Text($"This proposal is valid for {renderOpts.ValidityDays} days from the date above.")
                            .FontSize(10)
                            .FontColor(ColourInkSoft)
                            .LineHeight(1.55f);
                    });

                    body.Item().Height(12);

                    // ── Sign-off ───────────────────────────────────────────
                    // Right-aligned column anchored to a 220pt width: "Sincerely,"
                    // → signature image → gold rule → name line, all flush right.
                    // Signature renders at its natural aspect (158x70) pushed to the
                    // right of a 220pt row so its right edge lines up with the rule.
                    body.Item().AlignRight().Column(sign =>
                    {
                        sign.Item().Width(220).AlignRight().Text("Sincerely,").FontSize(10).FontColor(ColourInkFaint);
                        sign.Item().Height(6);

                        // Render the signature only when a real (processed) PNG is present.
                        // The 1x1 placeholder is ~70 bytes; the real Nakia PNG is >100 KB.
                        if (hasSig && new FileInfo(sigPath).Length > 5000)
                        {
                            sign.Item().Width(220).Row(r =>
                            {
                                r.RelativeItem();                                  // flexible left spacer pushes image to right edge
                                r.ConstantItem(161).Height(70).Image(sigPath).FitArea();
                            });
                        }

                        // Thin gold rule under the signature, then the name line.
                        sign.Item().Width(220).BorderBottom(0.5f).BorderColor(ColourGold).Height(2);
                        sign.Item().Height(4);
                        sign.Item().Width(220).AlignRight()
                            .Text($"{_opts.SigneeName} — {_opts.SigneeTitle}")
                            .FontSize(10)
                            .Bold()
                            .FontColor(ColourInkDark);
                    });
                });

                // ── Footer (every page) ───────────────────────────────────
                page.Footer()
                    .AlignCenter()
                    .Text(txt =>
                    {
                        txt.Span("Gold Kiosk · 8 The Green, STE B, Dover, Delaware, 19901, USA · ")
                            .FontSize(7)
                            .FontColor(ColourInkFaint);
                        txt.Span(_opts.PhoneUsa)
                            .FontSize(7)
                            .FontColor(ColourInkFaint);
                        txt.Span("   |   Page ")
                            .FontSize(7)
                            .FontColor(ColourInkFaint);
                        txt.CurrentPageNumber()
                            .FontSize(7)
                            .FontColor(ColourInkFaint);
                        txt.Span(" of ")
                            .FontSize(7)
                            .FontColor(ColourInkFaint);
                        txt.TotalPages()
                            .FontSize(7)
                            .FontColor(ColourInkFaint);
                    });
            });
        });

        return doc.GeneratePdf();
    }

    // Helper: styles a meta header cell with a light background + padding
    private static IContainer MetaCell(IContainer container) =>
        container
            .Background(ColourBg)
            .Border(1)
            .BorderColor(ColourRule)
            .Padding(10);
}
