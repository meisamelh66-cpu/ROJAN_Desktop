using System.Windows;
using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Controls.Shared;

/// <summary>Reusable vector flag chip, keyed by <see cref="LanguageCode"/> (e.g. <see cref="Localization.LanguageInfo.Code"/>) - see this control's own XAML doc comment for why the visuals are hand-authored vector rather than an external asset. Unrecognized codes render nothing (all four flags are <c>Collapsed</c> by default).</summary>
public partial class FlagIcon : UserControl
{
    public static readonly DependencyProperty LanguageCodeProperty =
        DependencyProperty.Register(nameof(LanguageCode), typeof(string), typeof(FlagIcon), new PropertyMetadata(string.Empty));

    public FlagIcon()
    {
        InitializeComponent();
    }

    public string LanguageCode
    {
        get => (string)GetValue(LanguageCodeProperty);
        set => SetValue(LanguageCodeProperty, value);
    }
}
