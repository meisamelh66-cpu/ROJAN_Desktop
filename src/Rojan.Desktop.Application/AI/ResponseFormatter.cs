using System.Text.RegularExpressions;

namespace Rojan.Desktop.Application.AI;

public sealed partial class ResponseFormatter : IResponseFormatter
{
    private const int MaxLength = 4000;

    public string Format(string rawResponse)
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            return string.Empty;
        }

        var trimmed = rawResponse.Trim();
        var collapsed = ExcessiveBlankLines().Replace(trimmed, "\n\n");
        return collapsed.Length <= MaxLength ? collapsed : string.Concat(collapsed.AsSpan(0, MaxLength), "...");
    }

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ExcessiveBlankLines();
}
