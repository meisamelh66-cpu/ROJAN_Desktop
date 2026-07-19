using System.Globalization;
using System.Windows;

namespace Rojan.Desktop.Presentation.Localization;

/// <summary>Culture/flow-direction resolution for a language code - pure computation, no file/network access, so (unlike <see cref="ILocalizationService"/>/<see cref="ILanguagePackManager"/>) its implementation lives directly in Presentation rather than Shell.</summary>
public interface ICultureService
{
    public CultureInfo GetCultureInfo(string languageCode);

    public FlowDirection GetFlowDirection(bool isRightToLeft);
}
