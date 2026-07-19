using Rojan.Desktop.Presentation.Localization;

namespace Rojan.Desktop.Presentation.Tests.Localization;

/// <summary>
/// Proves the "future packs load their own resources" mechanism -
/// <see cref="Strings.SetPackOverrides"/> lets a non-built-in pack supply a
/// partial string table that wins over the compiled resx, with undefined
/// keys honestly falling through to the resx rather than the pack having to
/// cover every key. Resets the shared static state after every test since
/// <see cref="Strings"/> is process-wide (mirrors production: it's set once
/// at startup for the session).
/// </summary>
public sealed class StringsTests : IDisposable
{
    public void Dispose() => Strings.SetPackOverrides(null);

    [Fact]
    public void Get_WithNoPackOverrides_ReturnsCompiledResxValue()
    {
        Strings.SetPackOverrides(null);

        var value = Strings.Common_Save;

        Assert.NotEqual(nameof(Strings.Common_Save), value);
        Assert.NotEmpty(value);
    }

    [Fact]
    public void Get_WithPackOverrideForKey_ReturnsOverrideInsteadOfResx()
    {
        Strings.SetPackOverrides(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [nameof(Strings.Common_Save)] = "GESPEICHERT",
        });

        Assert.Equal("GESPEICHERT", Strings.Common_Save);
    }

    [Fact]
    public void Get_WithPartialPackOverride_FallsThroughToResxForUndefinedKeys()
    {
        Strings.SetPackOverrides(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [nameof(Strings.Common_Save)] = "GESPEICHERT",
        });

        var cancelValue = Strings.Common_Cancel;

        Assert.NotEqual(nameof(Strings.Common_Cancel), cancelValue);
        Assert.NotEqual("GESPEICHERT", cancelValue);
    }

    [Fact]
    public void SetPackOverrides_WithNull_ClearsPreviousOverride()
    {
        Strings.SetPackOverrides(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [nameof(Strings.Common_Save)] = "GESPEICHERT",
        });
        Strings.SetPackOverrides(null);

        Assert.NotEqual("GESPEICHERT", Strings.Common_Save);
    }
}
