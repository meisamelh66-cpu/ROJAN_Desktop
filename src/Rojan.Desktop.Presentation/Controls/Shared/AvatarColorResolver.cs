namespace Rojan.Desktop.Presentation.Controls.Shared;

/// <summary>
/// Pure name-to-avatar-color/initials logic, deliberately separated from
/// EntityAvatar's WPF-dependent code-behind so it's unit-testable without
/// a running Dispatcher/Application - the same "extract pure logic out of
/// code-behind" reasoning KpiNumberParsing/KpiPrivacy already establish
/// for Controls/Dashboard/KPIValue.xaml.cs.
/// </summary>
public static class AvatarColorResolver
{
    private const int PaletteSize = 4;

    /// <summary>Deterministic palette index (0-3) for a given name - the same name always yields the same index.</summary>
    public static int ResolveIndex(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return 0;
        }

        var hash = 0;
        foreach (var character in name.Trim())
        {
            hash = (hash * 31 + character) & 0x7FFFFFFF;
        }

        return hash % PaletteSize;
    }

    /// <summary>First letter of the first word, plus first letter of the last word when there is more than one word. "?" for an empty/whitespace-only name.</summary>
    public static string ResolveInitials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "?";
        }

        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..1].ToUpperInvariant(),
            _ => (parts[0][..1] + parts[^1][..1]).ToUpperInvariant(),
        };
    }
}
