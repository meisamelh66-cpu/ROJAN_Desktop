using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Rojan.Desktop.Presentation.Controls.Help;

/// <summary>Reusable compact Help entry point (Phase 26.3) - a thin wrapper passing <see cref="Command"/>/<see cref="CommandParameter"/> straight to an inner styled <see cref="Button"/>, so any page/dialog can drop one in and bind it to whatever "open Help for this context" command it already has, the same way a plain <see cref="Button"/> would be used.</summary>
public partial class HelpButton : UserControl
{
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(HelpButton), new PropertyMetadata(null));

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(HelpButton), new PropertyMetadata(null));

    public HelpButton()
    {
        InitializeComponent();
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>Typically the current module id (e.g. <c>"customers"</c>) - see <c>Application.Help.IHelpQueryService.GetTopicForContextAsync</c>'s context-resolution contract.</summary>
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }
}
