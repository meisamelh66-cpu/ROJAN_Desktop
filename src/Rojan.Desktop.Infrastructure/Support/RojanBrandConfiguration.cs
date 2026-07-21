using Rojan.Desktop.Application.Support;

namespace Rojan.Desktop.Infrastructure.Support;

/// <summary>
/// Default <see cref="IRojanBrandConfiguration"/>. These four values are
/// the one place this app's official website/phone/support-email/API
/// values live - every Support Center screen reads them from here rather
/// than hardcoding them, so changing a value means changing it once, here.
/// </summary>
public sealed class RojanBrandConfiguration : IRojanBrandConfiguration
{
    public string WebsiteUrl => "rojanai.ir";

    public string PhoneNumber => "09114050112";

    public string SupportEmail => "support@rojanai.ir";

    public string ApiBaseUrl => "api.rojanai.ir";
}
