namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>ROJAN_Backend's <c>BannerResponse</c> (<c>GET /api/v1/public/banners?target=DESKTOP</c>).</summary>
public sealed record BannerResponse(
    string Id,
    string Target,
    string? Title,
    string? Subtitle,
    string? Href,
    string ImageUrl,
    bool IsActive,
    int DisplayOrder,
    DateTimeOffset CreatedAt);
