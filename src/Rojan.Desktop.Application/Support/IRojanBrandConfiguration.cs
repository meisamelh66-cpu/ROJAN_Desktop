namespace Rojan.Desktop.Application.Support;

/// <summary>
/// The organization-wide contact/brand values the Support Center displays
/// (About/Contact Us/Version Info) - a single injected seam so none of
/// them are hardcoded into a View or ViewModel. The concrete
/// implementation (Infrastructure) is the one place these values actually
/// live; changing them means changing that one registration, not hunting
/// through XAML.
/// </summary>
public interface IRojanBrandConfiguration
{
    public string WebsiteUrl { get; }

    public string PhoneNumber { get; }

    public string SupportEmail { get; }

    public string ApiBaseUrl { get; }
}
