using System.Windows.Input;
using Rojan.Desktop.Application.Workspaces;
using Rojan.Desktop.Presentation.Mvvm;

namespace Rojan.Desktop.Presentation.ViewModels.Workspaces;

/// <summary>A pinned side panel's live arrangement plus its resolved content ViewModel.</summary>
public sealed class DockedPanelViewModel : ViewModelBase
{
    private DockSide _side;
    private double _size;
    private bool _isVisible;

    public DockedPanelViewModel(string panelKey, string title, string iconGlyph, DockSide side, double size, bool isVisible, ViewModelBase content, ICommand toggleVisibilityCommand)
    {
        PanelKey = panelKey;
        Title = title;
        IconGlyph = iconGlyph;
        _side = side;
        _size = size;
        _isVisible = isVisible;
        Content = content;
        ToggleVisibilityCommand = toggleVisibilityCommand;
    }

    public string PanelKey { get; }

    public string Title { get; }

    public string IconGlyph { get; }

    public DockSide Side
    {
        get => _side;
        set => SetProperty(ref _side, value);
    }

    public double Size
    {
        get => _size;
        set
        {
            if (SetProperty(ref _size, value))
            {
                OnPropertyChanged(nameof(EffectiveSize));
            }
        }
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (SetProperty(ref _isVisible, value))
            {
                OnPropertyChanged(nameof(EffectiveSize));
            }
        }
    }

    /// <summary><see cref="Size"/> when visible, otherwise 0 - what the dock zone's <c>ColumnDefinition</c>/<c>RowDefinition</c> actually binds to (via <c>Converters.DoubleToGridLengthConverter</c>), so hiding a panel needs no separate visibility trigger.</summary>
    public double EffectiveSize => IsVisible ? Size : 0;

    public ViewModelBase Content { get; }

    public ICommand ToggleVisibilityCommand { get; }
}
