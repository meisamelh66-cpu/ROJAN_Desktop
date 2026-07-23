using System.Windows;
using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Controls.Shared;

/// <summary>Compact per-widget title. No logic beyond the one dependency property.</summary>
public partial class PremiumCardHeader : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(PremiumCardHeader),
            new PropertyMetadata(string.Empty));

    public PremiumCardHeader()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
}
