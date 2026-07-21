using System.Windows.Input;
using Rojan.Desktop.Presentation.Modules;
using Rojan.Desktop.Presentation.Mvvm;

namespace Rojan.Desktop.Presentation.ViewModels.Workspaces;

/// <summary>
/// One open module inside a <see cref="PaneLeafViewModel"/>'s tab strip.
/// <see cref="Content"/> is resolved once, when the tab is created, and kept
/// alive for as long as the tab stays open - switching tabs preserves
/// whatever state the page's own ViewModel holds, the standard tab UX.
/// <see cref="ActivateCommand"/>/<see cref="CloseCommand"/>/<see cref="FloatOutCommand"/>
/// are supplied by <c>WorkspaceHostViewModel</c> at construction, each
/// closing over this tab's (leaf id, module id) - keeps every tab-strip
/// button a simple parameterless <c>Command="{Binding ...}"</c> in XAML
/// rather than needing multi-value command parameters.
/// </summary>
public sealed class TabViewModel : ViewModelBase
{
    private bool _isActive;

    public TabViewModel(ModuleDescriptor descriptor, ViewModelBase content, ICommand activateCommand, ICommand closeCommand, ICommand floatOutCommand)
    {
        Descriptor = descriptor;
        Content = content;
        ActivateCommand = activateCommand;
        CloseCommand = closeCommand;
        FloatOutCommand = floatOutCommand;
    }

    public ModuleDescriptor Descriptor { get; }

    public string ModuleId => Descriptor.Metadata.Id;

    public string Title => Descriptor.Metadata.Title;

    public ViewModelBase Content { get; }

    /// <summary>Whether this is the tab strip's currently selected tab - kept in sync by <see cref="PaneLeafViewModel.ActiveTab"/>'s setter, purely for the tab strip's own selection highlight (the tab's <see cref="Content"/> renders regardless, only when it's the pane's <c>ActiveTab</c>).</summary>
    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public ICommand ActivateCommand { get; }

    public ICommand CloseCommand { get; }

    public ICommand FloatOutCommand { get; }
}
