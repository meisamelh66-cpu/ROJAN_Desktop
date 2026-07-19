using System.Windows;
using Rojan.Desktop.Presentation.Localization;

namespace Rojan.Desktop.Presentation.Tests.Localization;

public sealed class CultureServiceTests
{
    private readonly CultureService _service = new();

    [Theory]
    [InlineData("fa-IR")]
    [InlineData("en-US")]
    [InlineData("ar-SA")]
    public void GetCultureInfo_WithKnownCode_ReturnsMatchingCulture(string code)
    {
        var culture = _service.GetCultureInfo(code);

        Assert.Equal(code, culture.Name);
    }

    [Fact]
    public void GetCultureInfo_WithMalformedCode_FallsBackToInvariantCulture()
    {
        var culture = _service.GetCultureInfo("123456");

        Assert.Equal(System.Globalization.CultureInfo.InvariantCulture, culture);
    }

    [Fact]
    public void GetFlowDirection_WhenRightToLeft_ReturnsRightToLeft()
    {
        Assert.Equal(FlowDirection.RightToLeft, _service.GetFlowDirection(isRightToLeft: true));
    }

    [Fact]
    public void GetFlowDirection_WhenLeftToRight_ReturnsLeftToRight()
    {
        Assert.Equal(FlowDirection.LeftToRight, _service.GetFlowDirection(isRightToLeft: false));
    }
}
