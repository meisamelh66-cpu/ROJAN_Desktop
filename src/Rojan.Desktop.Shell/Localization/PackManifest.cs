namespace Rojan.Desktop.Shell.Localization;

/// <summary>
/// The on-disk shape of a <c>Languages/*.pack</c> file - deserialized via
/// <see cref="System.Text.Json.JsonSerializer"/> (already part of the
/// .NET runtime, no new package needed). <see cref="Strings"/> is
/// optional: built-in languages (fa-IR/en-US/ar-SA) omit it because their
/// strings come from this assembly's compiled satellite resources; a
/// third-party pack supplies whichever keys it wants to override there -
/// see <c>Rojan.Desktop.Presentation.Localization.Strings.SetPackOverrides</c>.
/// Every field maps directly to Phase 19A's "LANGUAGE PACK FORMAT"
/// requirement list.
/// </summary>
public sealed class PackManifest
{
    public string Code { get; set; } = string.Empty;

    public string NativeName { get; set; } = string.Empty;

    public string EnglishName { get; set; } = string.Empty;

    public bool IsRightToLeft { get; set; }

    public string FontFamily { get; set; } = string.Empty;

    public string NumberDigits { get; set; } = "Latin";

    public string DefaultCurrencyCode { get; set; } = "Usd";

    public string DateProviderId { get; set; } = "Gregorian";

    public string PackVersion { get; set; } = "1.0.0";

    public string CompatibilityVersion { get; set; } = "1.0";

    public bool IsBuiltIn { get; set; }

    public Dictionary<string, string>? Strings { get; set; }
}
