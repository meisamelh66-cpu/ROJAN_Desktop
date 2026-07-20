using Rojan.Desktop.Application.Help;
using Rojan.Desktop.Presentation.Help;

namespace Rojan.Desktop.Presentation.Tests.Help;

/// <summary>Exercises <see cref="HelpContentResolver"/>'s <c>KeyPrefix</c>-to-resx expansion and newline-splitting of the list-shaped sections (Steps/Tips/Warnings/BestPractices/Notes).</summary>
public sealed class HelpContentResolverTests
{
    private readonly HelpContentResolver _resolver = new();

    private static HelpTopicDto Topic(string keyPrefix, IReadOnlyList<HelpShortcutDto>? shortcuts = null) =>
        new("help-customers", "customers", null, keyPrefix, shortcuts ?? [], ["help-bookings"]);

    [Fact]
    public void Resolve_ExpandsKeyPrefixIntoScalarFields()
    {
        var resolved = _resolver.Resolve(Topic("Help_Customers"));

        Assert.NotEmpty(resolved.Title);
        Assert.NotEmpty(resolved.Description);
        Assert.NotEqual("Help_Customers_Title", resolved.Title);
    }

    [Fact]
    public void Resolve_PreservesTopicIdentityFields()
    {
        var resolved = _resolver.Resolve(Topic("Help_Customers"));

        Assert.Equal("help-customers", resolved.TopicId);
        Assert.Equal("customers", resolved.ModuleId);
        Assert.Equal(["help-bookings"], resolved.RelatedTopicIds);
    }

    [Fact]
    public void Resolve_SplitsListShapedSectionsIntoIndividualEntries()
    {
        var resolved = _resolver.Resolve(Topic("Help_Customers"));

        Assert.True(resolved.Steps.Count > 1, "Help_Customers_Steps is authored as multiple newline-separated steps.");
        Assert.All(resolved.Steps, step => Assert.False(string.IsNullOrWhiteSpace(step)));
    }

    [Fact]
    public void Resolve_ExpandsShortcutDescriptionKeyToLocalizedText()
    {
        var resolved = _resolver.Resolve(Topic("Help_Customers", [new HelpShortcutDto("Esc", "Help_Shortcut_CloseDialog")]));

        var shortcut = Assert.Single(resolved.Shortcuts);
        Assert.Equal("Esc", shortcut.KeysDisplay);
        Assert.NotEmpty(shortcut.Description);
        Assert.NotEqual("Help_Shortcut_CloseDialog", shortcut.Description);
    }

    [Fact]
    public void Resolve_UnknownKeyPrefix_FallsBackToTheRawKeyRatherThanThrowing()
    {
        // Mirrors Strings.Get's missing-resource fallback (ResourceManager.GetString(...) ?? key)
        // used throughout this codebase - a missing Help_* entry surfaces as the key itself,
        // not a crash or a silently empty dialog.
        var resolved = _resolver.Resolve(Topic("Help_DoesNotExist"));

        Assert.Equal("Help_DoesNotExist_Title", resolved.Title);
    }
}
