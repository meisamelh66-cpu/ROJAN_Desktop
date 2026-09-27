namespace Rojan.Desktop.Application.Banners;

/// <summary>The one admin-managed banner the Dashboard shows. <see cref="ImageBytes"/> is <see langword="null"/> when the image is unavailable.</summary>
public sealed record DashboardBannerDto(string Id, string? Title, string? Subtitle, string? Href, byte[]? ImageBytes);
