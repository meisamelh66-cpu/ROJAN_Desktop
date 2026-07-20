using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Rojan.Desktop.Presentation.Views.Help;

/// <summary>
/// Phase 26.4/26.11: Context Help Dialog. Self-contained focus trapping -
/// Tab/Shift+Tab cycles only among this dialog's own focusable
/// descendants (there is no WPF built-in for this), and initial focus
/// lands on the search box as soon as the dialog opens. ESC-to-close and
/// outside-click-to-close are handled one level up, in
/// <c>Shell.MainWindow</c>, scoped specifically to this dialog's
/// ViewModel type so no other existing dialog's behavior changes.
/// </summary>
public partial class ContextHelpDialogView : UserControl
{
    public ContextHelpDialogView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SearchBox.Focus();
        Keyboard.Focus(SearchBox);
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Tab)
        {
            return;
        }

        var direction = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)
            ? FocusNavigationDirection.Previous
            : FocusNavigationDirection.Next;

        if (Keyboard.FocusedElement is not UIElement focused)
        {
            return;
        }

        var next = focused.PredictFocus(direction) as DependencyObject;
        if (next is null || !IsDescendantOf(next, RootBorder))
        {
            // Wrap: predicted next focus would leave the dialog - cycle back
            // to the first/last focusable element inside it instead.
            e.Handled = true;
            var wrapTarget = direction == FocusNavigationDirection.Next ? (Control)SearchBox : CloseButton;
            wrapTarget.Focus();
            Keyboard.Focus(wrapTarget);
        }
    }

    private static bool IsDescendantOf(DependencyObject element, DependencyObject ancestor)
    {
        var current = element;
        while (current is not null)
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }

            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }

        return false;
    }
}
