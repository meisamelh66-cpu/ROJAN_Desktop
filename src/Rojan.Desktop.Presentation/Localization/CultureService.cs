using System.Globalization;
using System.Windows;

namespace Rojan.Desktop.Presentation.Localization;

/// <summary>Default <see cref="ICultureService"/> implementation.</summary>
public sealed class CultureService : ICultureService
{
    public CultureInfo GetCultureInfo(string languageCode)
    {
        CultureInfo culture;
        try
        {
            culture = CultureInfo.GetCultureInfo(languageCode);
        }
#pragma warning disable CA1031 // An unrecognized/malformed pack-supplied culture code must fall back to invariant, not crash startup.
        catch (Exception)
#pragma warning restore CA1031
        {
            return CultureInfo.InvariantCulture;
        }

        // UI Polish Sprint: this app's business content is priced in Toman
        // (the everyday Iranian currency word, alongside Rial), never a
        // hardcoded "$" - every {0:C} binding across HR/Accounting/
        // Reporting relies on CurrentCulture.NumberFormat for its currency
        // symbol, so overriding it once here, for the app's default fa-IR
        // culture only, is what makes every one of those bindings render
        // consistently without touching each XAML file individually.
        if (string.Equals(languageCode, "fa-IR", StringComparison.OrdinalIgnoreCase))
        {
            culture = (CultureInfo)culture.Clone();
            culture.NumberFormat.CurrencySymbol = "تومان";
            culture.NumberFormat.CurrencyDecimalDigits = 0;
            culture.NumberFormat.CurrencyPositivePattern = 3;
            culture.NumberFormat.CurrencyNegativePattern = 8;
        }

        return culture;
    }

    public FlowDirection GetFlowDirection(bool isRightToLeft) =>
        isRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
}
