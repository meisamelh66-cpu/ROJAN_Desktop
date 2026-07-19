namespace Rojan.Desktop.Presentation.Localization;

/// <summary>
/// Everything the localization platform knows about one language - the
/// in-memory shape a Language Pack manifest (see
/// <c>Shell.Localization.LanguagePackManager</c>) deserializes into,
/// covering every field Phase 19A's "LANGUAGE PACK FORMAT" requirement
/// lists: strings (resolved separately through <see cref="Strings"/>'s
/// satellite-resource mechanism, not carried on this record), fonts,
/// RTL/LTR, number format, currency, date provider, regional settings,
/// and pack/compatibility versioning. <see cref="IsBuiltIn"/> is true for
/// Persian/English/Arabic (compiled into this assembly's own satellite
/// resources); every other language arrives as an installed
/// <c>Languages/*.pack</c> file discovered by <c>LanguagePackManager</c> -
/// the architecture supports either without any application code change.
/// </summary>
public sealed record LanguageInfo(
    string Code,
    string NativeName,
    string EnglishName,
    bool IsRightToLeft,
    string FontFamily,
    NumberDigits NumberDigits,
    string DefaultCurrencyCode,
    string DateProviderId,
    string PackVersion,
    string CompatibilityVersion,
    bool IsBuiltIn);
