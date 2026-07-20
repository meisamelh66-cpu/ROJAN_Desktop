namespace Rojan.Desktop.Domain.Organizations;

/// <summary>A branch's POS receipt template settings.</summary>
public sealed record ReceiptSettings(string HeaderText, string FooterText, bool ShowLogo);
