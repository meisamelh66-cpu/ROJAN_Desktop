using System.Windows;
using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Controls;

/// <summary>Reusable section title with an accent underline. No logic beyond the one dependency property.</summary>
public partial class SectionHeader : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(SectionHeader),
            new PropertyMetadata(string.Empty));

    public SectionHeader()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
}
