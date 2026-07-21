using System.Windows.Input;
using Rojan.Desktop.Application.Workspaces;
using Rojan.Desktop.Presentation.Mvvm;

namespace Rojan.Desktop.Presentation.ViewModels.Workspaces;

/// <summary>
/// Two child panes divided by a resizable gutter - rebuilt fresh on every
/// structural change (unlike <see cref="PaneLeafViewModel"/>, it holds no
/// state worth preserving across a rebuild). <see cref="ResizeCommand"/> is
/// supplied by <c>WorkspaceHostViewModel</c> at construction, closing over
/// this split's id - <c>Views.Workspaces.PaneSplitView</c>'s code-behind
/// invokes it (parameter: the new ratio, a <see cref="double"/>) once a
/// <c>GridSplitter</c> drag completes.
/// </summary>
public sealed class PaneSplitViewModel : ViewModelBase
{
    private double _ratio;

    public PaneSplitViewModel(string id, PaneOrientation orientation, double ratio, object first, object second, ICommand resizeCommand)
    {
        Id = id;
        Orientation = orientation;
        _ratio = ratio;
        First = first;
        Second = second;
        ResizeCommand = resizeCommand;
    }

    public string Id { get; }

    public PaneOrientation Orientation { get; }

    public double Ratio
    {
        get => _ratio;
        set => SetProperty(ref _ratio, value);
    }

    /// <summary>Either a <see cref="PaneLeafViewModel"/> or another <see cref="PaneSplitViewModel"/> - a recursive tree, rendered by whichever implicit DataTemplate matches this object's runtime type.</summary>
    public object First { get; }

    public object Second { get; }

    /// <summary>Parameter: the new ratio (<see cref="double"/>).</summary>
    public ICommand ResizeCommand { get; }
}
