using System.Globalization;
using System.Text.RegularExpressions;
using Rojan.Desktop.Application.Dashboard;

namespace Rojan.Desktop.Presentation.Controls.Dashboard;

/// <summary>
/// Shared "parse the real bound KPI Value string, derive the real
/// previous-period value implied by TrendDirection/TrendPercentage" logic
/// used by both KPIValue's count-up reveal and KpiChart's mini charts -
/// kept in one place so the two consumers can't drift. Everything here is a
/// deterministic function of the DTO's own already-bound fields - it never
/// invents a number that isn't derivable from real, existing data.
/// </summary>
internal static class KpiNumberParsing
{
    private static readonly Regex LeadingNumberPattern =
        new(@"^(?<prefix>[^\d]*)(?<number>[\d,]+(?:\.[\d]+)?)(?<suffix>.*)$", RegexOptions.Compiled);

    /// <summary>Parses the leading numeric run out of a formatted KPI Value string (e.g. "124,000,000 تومان"), keeping enough shape info (prefix/suffix/thousands-separator/decimal places) to re-render the exact same format around an animated number.</summary>
    public static bool TryParse(string? value, out double number, out string prefix, out string suffix, out bool hasThousandsSeparator, out int decimalPlaces)
    {
        number = 0;
        prefix = string.Empty;
        suffix = string.Empty;
        hasThousandsSeparator = false;
        decimalPlaces = 0;

        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        var match = LeadingNumberPattern.Match(value);
        if (!match.Success)
        {
            return false;
        }

        var numberText = match.Groups["number"].Value;
        if (!double.TryParse(numberText, NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out number))
        {
            return false;
        }

        prefix = match.Groups["prefix"].Value;
        suffix = match.Groups["suffix"].Value;
        hasThousandsSeparator = numberText.Contains(',');
        decimalPlaces = numberText.Contains('.') ? numberText[(numberText.IndexOf('.') + 1)..].Length : 0;
        return true;
    }

    /// <summary>
    /// Derives the real previous-period value implied by the current value and
    /// its already-bound trend - e.g. TrendDirection.Down at 2.1% on a current
    /// value of 124,000,000 means the previous period was
    /// 124,000,000 / (1 - 0.021). This is arithmetic on two real bound fields
    /// (Value, TrendPercentage), not a fabricated number - it is the same
    /// "previous period" fact TrendIndicator already displays as a percentage,
    /// just expressed as an absolute value so it can anchor a two-point chart.
    /// </summary>
    public static double ComputePrevious(double current, TrendDirection direction, double percentageMagnitude)
    {
        if (percentageMagnitude <= 0 || direction == TrendDirection.Flat)
        {
            return current;
        }

        var fraction = percentageMagnitude / 100.0;
        return direction switch
        {
            TrendDirection.Up => current / (1 + fraction),
            TrendDirection.Down when fraction < 1 => current / (1 - fraction),
            _ => current,
        };
    }
}
