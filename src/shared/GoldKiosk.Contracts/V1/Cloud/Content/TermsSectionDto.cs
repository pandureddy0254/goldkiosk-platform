namespace GoldKiosk.Contracts.V1.Cloud.Content;

/// <summary>
/// One section of the terms &amp; conditions document.
/// </summary>
/// <param name="Heading">The section heading.</param>
/// <param name="Body">The section body text.</param>
public sealed record TermsSectionDto(string Heading, string Body);
