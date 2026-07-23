using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Rojan.Desktop.Presentation.Controls.Shared;

/// <summary>
/// Circular name-initials badge with a deterministic accent color (see
/// AvatarColorResolver), or a real photo when ImageSource is set. The
/// four background colors it cycles through are the app's own existing
/// "emphasis only" brand accents (Colors.xaml's AccentPurple/RoseGold/
/// Lavender/Aqua) - no new raw color introduced.
/// </summary>
public partial class EntityAvatar : UserControl
{
    private static readonly string[] PaletteResourceKeys =
    [
        "Rojan.Brush.AccentPurple",
        "Rojan.Brush.AccentRoseGold",
        "Rojan.Brush.AccentLavender",
        "Rojan.Brush.AccentAqua",
    ];

    public static readonly DependencyProperty EntityNameProperty =
        DependencyProperty.Register(nameof(EntityName), typeof(string), typeof(EntityAvatar), new PropertyMetadata(string.Empty, OnEntityNameChanged));

    public static readonly DependencyProperty ImageSourceProperty =
        DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(EntityAvatar), new PropertyMetadata(null));

    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(nameof(Size), typeof(double), typeof(EntityAvatar), new PropertyMetadata(36.0));

    public static readonly DependencyProperty InitialsProperty =
        DependencyProperty.Register(nameof(Initials), typeof(string), typeof(EntityAvatar), new PropertyMetadata("?"));

    public static readonly DependencyProperty AvatarBrushProperty =
        DependencyProperty.Register(nameof(AvatarBrush), typeof(Brush), typeof(EntityAvatar), new PropertyMetadata(null));

    public EntityAvatar()
    {
        InitializeComponent();
    }

    /// <summary>The entity's display name (customer/specialist/employee/etc.) - named EntityName, not Name, so it does not shadow FrameworkElement.Name (the WPF element-naming mechanism x:Name sets).</summary>
    public string EntityName
    {
        get => (string)GetValue(EntityNameProperty);
        set => SetValue(EntityNameProperty, value);
    }

    public ImageSource? ImageSource
    {
        get => (ImageSource?)GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>Computed from Name; read-only from a consumer's perspective, the same "Root-bound derived DP" shape KPIValue.DisplayText already uses.</summary>
    public string Initials => (string)GetValue(InitialsProperty);

    /// <summary>Computed from Name; read-only from a consumer's perspective.</summary>
    public Brush? AvatarBrush => (Brush?)GetValue(AvatarBrushProperty);

    private static void OnEntityNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((EntityAvatar)d).UpdateDerivedValues((string?)e.NewValue);

    private void UpdateDerivedValues(string? name)
    {
        SetValue(InitialsProperty, AvatarColorResolver.ResolveInitials(name));

        var key = PaletteResourceKeys[AvatarColorResolver.ResolveIndex(name)];
        SetValue(AvatarBrushProperty, TryFindResource(key) as Brush);
    }
}
