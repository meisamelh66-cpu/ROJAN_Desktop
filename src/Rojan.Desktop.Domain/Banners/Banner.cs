namespace Rojan.Desktop.Domain.Banners;

/// <summary>
/// A platform banner published for the Desktop app from the ROJAN Admin Panel
/// (<c>/rojan-admin/banners</c>, target "دسکتاپ"). Content is owned by the backend - never
/// hard-coded here. <see cref="Href"/> is the admin-configured call-to-action target, if any.
/// </summary>
public sealed record Banner(string Id, string? Title, string? Subtitle, string? Href, string ImageUrl, int DisplayOrder);
