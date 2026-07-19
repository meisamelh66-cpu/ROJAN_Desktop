using System.Globalization;
using System.Windows;

namespace Rojan.Desktop.Presentation.Localization;

/// <summary>Default <see cref="ICultureService"/> implementation.</summary>
public sealed class CultureService : ICultureService
{
    public CultureInfo GetCultureInfo(string languageCode)
    {
        try
        {
            return CultureInfo.GetCultureInfo(languageCode);
        }
#pragma warning disable CA1031 // An unrecognized/malformed pack-supplied culture code must fall back to invariant, not crash startup.
        catch (Exception)
#pragma warning restore CA1031
        {
            return CultureInfo.InvariantCulture;
        }
    }

    public FlowDirection GetFlowDirection(bool isRightToLeft) =>
        isRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
}
