using System.Collections.ObjectModel;
using System.Windows.Input;
using Rojan.Desktop.Application.Workspaces;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Modules;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.Workspaces;

namespace Rojan.Desktop.Presentation.ViewModels.Workspaces;

/// <summary>
/// Phase 29: Enterprise Workspace &amp; Window Management. The orchestrator for
/// everything layered additively around the pre-existing, sidebar-driven
/// primary content region: the secondary-pane tree (splits/tabs), docked
/// panels, floating windows, and named/saved workspaces. Deliberately does
/// not own the primary pane itself - <c>Shell.MainWindowViewModel</c>'s
/// existing <c>SelectedNavigationItem</c>/<c>INavigationService</c>
/// machinery (unchanged since Phase 07) keeps driving that, calling
/// <see cref="SetPrimaryModuleId"/> to keep this ViewModel's own record of
/// "what the primary pane shows" in sync. Constructed via <c>new</c> by
/// <c>MainWindowViewModel</c> (not DI-registered) - the same
/// "constructed by its opener, lives for the app's lifetime" shape
/// <c>NotificationCenterViewModel</c>/<c>ToastHostViewModel</c> already
/// establish - since it needs <see cref="IServiceProvider"/> to resolve
/// tab content, which only the composition root should hand out.
///
/// All structural pane operations (split/open tab/close tab/resize) run
/// synchronously in memory via <see cref="PaneTreeRules"/> (itself a thin
/// wrapper around <c>Domain.Workspaces.WorkspaceRules</c>) - persistence is
/// a separate, best-effort <see cref="SaveAsync"/> fired after each one, so
/// the UI never waits on disk I/O for a click or a drag. A leaf/tab
/// instance cache (<see cref="_leafCache"/>/<see cref="_tabCache"/>) means a
/// structural change elsewhere in the tree never discards or recreates an
/// unrelated tab's live content ViewModel - state (scroll position, an
/// in-progress form) survives.
/// </summary>
public sealed class WorkspaceHostViewModel : ViewModelBase
{
    private const string OutlinePanelKey = "outline";
    private const string OutlineIconGlyph = "";
    private const double DefaultOutlinePanelSize = 280;
    private const double DefaultFloatingWidth = 900;
    private const double DefaultFloatingHeight = 640;

    private readonly IWorkspaceService _workspaceService;
    private readonly IModuleRegistry _moduleRegistry;
    private readonly IServiceProvider _serviceProvider;
    private readonly IFloatingWindowManager _floatingWindowManager;

    private readonly Dictionary<string, PaneLeafViewModel> _leafCache = [];
    private readonly Dictionary<string, TabViewModel> _tabCache = [];

    private string _workspaceId = string.Empty;
    private string _workspaceName = string.Empty;
    private string _primaryModuleId = string.Empty;
    private PaneNodeDto? _secondaryRootDto;
    private object? _secondaryRoot;
    private string? _focusedLeafId;
    private DateTimeOffset _createdAt;
    private bool _isDefaultWorkspace;

    private bool _isSwitcherOpen;
    private bool _isRenaming;
    private string? _renameTargetId;
    private string _newWorkspaceNameText = string.Empty;

    public WorkspaceHostViewModel(
        IWorkspaceService workspaceService,
        IModuleRegistry moduleRegistry,
        IServiceProvider serviceProvider,
        IFloatingWindowManager floatingWindowManager)
    {
        _workspaceService = workspaceService;
        _moduleRegistry = moduleRegistry;
        _serviceProvider = serviceProvider;
        _floatingWindowManager = floatingWindowManager;

        _floatingWindowManager.WindowClosed += (_, floatingWindowId) => OnFloatingWindowClosed(floatingWindowId);

        Outline = new WorkspaceOutlineViewModel(FocusOutlineEntry, entry => _ = CloseOutlineEntryAsync(entry));
        FloatingWindows = [];
        Workspaces = [];
        RecentWorkspaces = [];

        // Constructed immediately (not lazily on first ApplyLayout) so
        // XAML can bind straight through OutlinePanel without a null
        // check - only its Side/Size/IsVisible change once a workspace
        // actually loads (see SyncDockedPanels).
        OutlinePanel = new DockedPanelViewModel(
            OutlinePanelKey, Strings.Workspace_Panel_Outline, OutlineIconGlyph, DockSide.Right, DefaultOutlinePanelSize, false, Outline,
            toggleVisibilityCommand: new AsyncRelayCommand(_ => ToggleDockPanelAsync(OutlinePanelKey)));
        DockedPanels = [OutlinePanel];

        SplitRightCommand = new AsyncRelayCommand(parameter => SplitAsync(parameter as string, PaneOrientation.Horizontal));
        SplitDownCommand = new AsyncRelayCommand(parameter => SplitAsync(parameter as string, PaneOrientation.Vertical));
        CloseFocusedTabCommand = new AsyncRelayCommand(_ => CloseFocusedTabAsync());
        FloatOutFocusedTabCommand = new AsyncRelayCommand(_ => FloatOutFocusedTabAsync());
        CycleTabNextCommand = new RelayCommand(_ => CycleTab(1));
        CycleTabPreviousCommand = new RelayCommand(_ => CycleTab(-1));
        ToggleDockPanelCommand = new AsyncRelayCommand(parameter => ToggleDockPanelAsync((string)parameter!));

        ToggleSwitcherCommand = new RelayCommand(_ => IsSwitcherOpen = !IsSwitcherOpen);
        StartCreateCommand = new RelayCommand(_ => BeginCreate());
        StartRenameCommand = new RelayCommand(parameter => BeginRename((WorkspaceSummaryDto)parameter!));
        CancelNameEditCommand = new RelayCommand(_ => CancelNameEdit());
        ConfirmNameCommand = new AsyncRelayCommand(_ => ConfirmNameAsync(), _ => !string.IsNullOrWhiteSpace(NewWorkspaceNameText));
        SwitchWorkspaceItemCommand = new AsyncRelayCommand(parameter => SwitchWorkspaceAsync(((WorkspaceSummaryDto)parameter!).Id));
        DuplicateWorkspaceCommand = new AsyncRelayCommand(parameter => DuplicateWorkspaceAsync((WorkspaceSummaryDto)parameter!));
        DeleteWorkspaceCommand = new AsyncRelayCommand(parameter => DeleteWorkspaceAsync((WorkspaceSummaryDto)parameter!), _ => Workspaces.Count > 1);
        ResetWorkspaceCommand = new AsyncRelayCommand(_ => ResetWorkspaceAsync());
    }

    /// <summary>Fires when the active workspace's primary module differs from what the sidebar currently shows (on restore/switch/delete-fallback) - <c>MainWindowViewModel</c> subscribes and sets <c>SelectedNavigationItem</c> accordingly.</summary>
    public event EventHandler<string>? PrimaryModuleChangeRequested;

    public string WorkspaceName
    {
        get => _workspaceName;
        private set => SetProperty(ref _workspaceName, value);
    }

    public string PrimaryModuleId
    {
        get => _primaryModuleId;
        private set => SetProperty(ref _primaryModuleId, value);
    }

    /// <summary>Either <see langword="null"/> (no extra panes - the default, unchanged-since-Phase-07 state), a <see cref="PaneLeafViewModel"/>, or a <see cref="PaneSplitViewModel"/>.</summary>
    public object? SecondaryRoot
    {
        get => _secondaryRoot;
        private set
        {
            if (SetProperty(ref _secondaryRoot, value))
            {
                OnPropertyChanged(nameof(HasSecondaryPane));
            }
        }
    }

    public bool HasSecondaryPane => _secondaryRoot is not null;

    public string? FocusedLeafId
    {
        get => _focusedLeafId;
        set => SetProperty(ref _focusedLeafId, value);
    }

    public ObservableCollection<DockedPanelViewModel> DockedPanels { get; }

    public ObservableCollection<FloatingWindowHandleViewModel> FloatingWindows { get; }

    public WorkspaceOutlineViewModel Outline { get; }

    /// <summary>The one docked panel that exists today (the Workspace Outline) - always non-null, constructed in the constructor so XAML never needs a null check; its Side/Size/IsVisible are updated by <see cref="SyncDockedPanels"/> once a workspace loads. Also present in <see cref="DockedPanels"/> for whatever future consumer wants to enumerate every docked panel generically.</summary>
    public DockedPanelViewModel OutlinePanel { get; }

    public bool IsSwitcherOpen
    {
        get => _isSwitcherOpen;
        set => SetProperty(ref _isSwitcherOpen, value);
    }

    public bool IsRenaming
    {
        get => _isRenaming;
        private set => SetProperty(ref _isRenaming, value);
    }

    public string NewWorkspaceNameText
    {
        get => _newWorkspaceNameText;
        set
        {
            if (SetProperty(ref _newWorkspaceNameText, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public ObservableCollection<WorkspaceSummaryDto> Workspaces { get; }

    public ObservableCollection<WorkspaceSummaryDto> RecentWorkspaces { get; }

    public IReadOnlyList<ModuleDescriptor> AvailableModules => _moduleRegistry.Modules;

    /// <summary>Splits the pane identified by the command parameter (falls back to <see cref="FocusedLeafId"/>) - used by the header/keyboard-shortcut entry point; each pane's own toolbar instead uses its dedicated, id-bound <see cref="PaneLeafViewModel.SplitRightCommand"/>.</summary>
    public ICommand SplitRightCommand { get; }

    public ICommand SplitDownCommand { get; }

    public ICommand CloseFocusedTabCommand { get; }

    public ICommand FloatOutFocusedTabCommand { get; }

    public ICommand CycleTabNextCommand { get; }

    public ICommand CycleTabPreviousCommand { get; }

    /// <summary>Parameter: the panel key (only <c>"outline"</c> exists today).</summary>
    public ICommand ToggleDockPanelCommand { get; }

    public ICommand ToggleSwitcherCommand { get; }

    public ICommand StartCreateCommand { get; }

    public ICommand StartRenameCommand { get; }

    public ICommand CancelNameEditCommand { get; }

    public ICommand ConfirmNameCommand { get; }

    public ICommand SwitchWorkspaceItemCommand { get; }

    public ICommand DuplicateWorkspaceCommand { get; }

    public ICommand DeleteWorkspaceCommand { get; }

    public ICommand ResetWorkspaceCommand { get; }

    /// <summary>First-launch bootstrap plus "Restore last workspace on startup" - call once, after the primary pane has already navigated to <paramref name="defaultPrimaryModuleId"/> via the pre-existing Phase 07 mechanism.</summary>
    public async Task InitializeAsync(string defaultPrimaryModuleId)
    {
        var layout = await _workspaceService.EnsureInitializedAsync(defaultPrimaryModuleId).ConfigureAwait(true);
        ApplyLayout(layout);
        await RefreshWorkspaceListsAsync().ConfigureAwait(true);

        if (!string.Equals(layout.PrimaryModuleId, defaultPrimaryModuleId, StringComparison.Ordinal))
        {
            PrimaryModuleChangeRequested?.Invoke(this, layout.PrimaryModuleId);
        }
    }

    /// <summary>
    /// Called by <c>MainWindowViewModel</c> whenever sidebar navigation
    /// changes the primary module - keeps this workspace's own record of
    /// "what the primary pane shows" in sync, then persists it. A no-op
    /// save (update the property, skip <see cref="SaveAsync"/>) before
    /// <see cref="InitializeAsync"/> has run: <c>MainWindowViewModel</c>'s
    /// constructor sets the initial <c>SelectedNavigationItem</c> (which
    /// calls this) before it kicks off <see cref="InitializeAsync"/>, and
    /// there is no workspace id yet to save against.
    /// </summary>
    public void SetPrimaryModuleId(string moduleId)
    {
        if (string.Equals(PrimaryModuleId, moduleId, StringComparison.Ordinal))
        {
            return;
        }

        PrimaryModuleId = moduleId;
        if (_workspaceId.Length > 0)
        {
            _ = SaveAsync();
        }
    }

    private async Task SplitAsync(string? targetLeafId, PaneOrientation orientation)
    {
        var effectiveTarget = targetLeafId ?? FocusedLeafId;
        var sourceModuleId = ResolveSourceModuleId(effectiveTarget);

        _secondaryRootDto = PaneTreeRules.Split(_secondaryRootDto, effectiveTarget, PrimaryModuleId, sourceModuleId, orientation, NewId);
        RebuildTree();
        await SaveAsync().ConfigureAwait(true);
    }

    private string ResolveSourceModuleId(string? leafId)
    {
        if (leafId is not null && _leafCache.TryGetValue(leafId, out var leaf) && leaf.ActiveTab is not null)
        {
            return leaf.ActiveTab.ModuleId;
        }

        return PrimaryModuleId;
    }

    private async Task CloseTabAsync(string leafId, string moduleId)
    {
        if (_secondaryRootDto is null)
        {
            return;
        }

        _secondaryRootDto = PaneTreeRules.CloseTab(_secondaryRootDto, leafId, moduleId);
        RebuildTree();
        await SaveAsync().ConfigureAwait(true);
    }

    private async Task ClosePaneAsync(string leafId)
    {
        if (_secondaryRootDto is null || !_leafCache.TryGetValue(leafId, out var leaf))
        {
            return;
        }

        PaneNodeDto? current = _secondaryRootDto;
        foreach (var moduleId in leaf.Tabs.Select(t => t.ModuleId).ToList())
        {
            if (current is null)
            {
                break;
            }

            current = PaneTreeRules.CloseTab(current, leafId, moduleId);
        }

        _secondaryRootDto = current;
        RebuildTree();
        await SaveAsync().ConfigureAwait(true);
    }

    private async Task FloatOutAsync(string leafId, string moduleId)
    {
        var descriptor = ResolveModule(moduleId);
        if (descriptor is null)
        {
            return;
        }

        if (_secondaryRootDto is not null)
        {
            _secondaryRootDto = PaneTreeRules.CloseTab(_secondaryRootDto, leafId, moduleId);
            RebuildTree();
        }

        var windowId = NewId();
        _floatingWindowManager.Open(windowId, descriptor, 120, 120, DefaultFloatingWidth, DefaultFloatingHeight, false);
        FloatingWindows.Add(new FloatingWindowHandleViewModel(windowId, moduleId, descriptor.Metadata.Title));
        RefreshOutline();
        await SaveAsync().ConfigureAwait(true);
    }

    private async Task CloseFocusedTabAsync()
    {
        if (FocusedLeafId is not null && _leafCache.TryGetValue(FocusedLeafId, out var leaf) && leaf.ActiveTab is not null)
        {
            await CloseTabAsync(FocusedLeafId, leaf.ActiveTab.ModuleId).ConfigureAwait(true);
        }
    }

    private async Task FloatOutFocusedTabAsync()
    {
        if (FocusedLeafId is not null && _leafCache.TryGetValue(FocusedLeafId, out var leaf) && leaf.ActiveTab is not null)
        {
            await FloatOutAsync(FocusedLeafId, leaf.ActiveTab.ModuleId).ConfigureAwait(true);
        }
    }

    private void FocusTab(string leafId, string moduleId)
    {
        if (!_leafCache.TryGetValue(leafId, out var leaf))
        {
            return;
        }

        var tab = leaf.Tabs.FirstOrDefault(t => t.ModuleId == moduleId);
        if (tab is null)
        {
            return;
        }

        leaf.ActiveTab = tab;
        FocusedLeafId = leafId;

        if (_secondaryRootDto is not null)
        {
            _secondaryRootDto = PaneTreeRules.SetActiveTab(_secondaryRootDto, leafId, moduleId);
            _ = SaveAsync();
        }
    }

    private void CycleTab(int direction)
    {
        if (FocusedLeafId is null || !_leafCache.TryGetValue(FocusedLeafId, out var leaf) || leaf.Tabs.Count == 0)
        {
            return;
        }

        var currentIndex = leaf.ActiveTab is null ? 0 : leaf.Tabs.IndexOf(leaf.ActiveTab);
        var nextIndex = ((currentIndex + direction) % leaf.Tabs.Count + leaf.Tabs.Count) % leaf.Tabs.Count;
        FocusTab(leaf.Id, leaf.Tabs[nextIndex].ModuleId);
    }

    private async Task ToggleDockPanelAsync(string panelKey)
    {
        var panel = DockedPanels.FirstOrDefault(p => p.PanelKey == panelKey);
        if (panel is null)
        {
            return;
        }

        panel.IsVisible = !panel.IsVisible;
        await SaveAsync().ConfigureAwait(true);
    }

    private async Task OpenModuleInPaneAsync(string leafId, ModuleDescriptor descriptor)
    {
        if (_secondaryRootDto is null)
        {
            return;
        }

        _secondaryRootDto = PaneTreeRules.OpenTab(_secondaryRootDto, leafId, descriptor.Metadata.Id);
        RebuildTree();
        FocusTab(leafId, descriptor.Metadata.Id);
        await SaveAsync().ConfigureAwait(true);
    }

    private void RebuildTree()
    {
        PruneCaches(_secondaryRootDto);
        SecondaryRoot = BuildNode(_secondaryRootDto);
        RefreshOutline();
    }

    private object? BuildNode(PaneNodeDto? dto) => dto switch
    {
        null => null,
        PaneLeafDto leafDto => BuildLeaf(leafDto),
        PaneSplitDto splitDto => new PaneSplitViewModel(
            splitDto.Id,
            splitDto.Orientation,
            splitDto.Ratio,
            BuildNode(splitDto.First)!,
            BuildNode(splitDto.Second)!,
            new AsyncRelayCommand(parameter => ResizeSplitAsync(splitDto.Id, (double)parameter!))),
        _ => throw new InvalidOperationException($"Unknown pane node dto type '{dto.GetType()}'."),
    };

    private async Task ResizeSplitAsync(string splitId, double ratio)
    {
        if (_secondaryRootDto is null)
        {
            return;
        }

        _secondaryRootDto = PaneTreeRules.Resize(_secondaryRootDto, splitId, ratio);
        RebuildTree();
        await SaveAsync().ConfigureAwait(true);
    }

    private PaneLeafViewModel BuildLeaf(PaneLeafDto dto)
    {
        if (!_leafCache.TryGetValue(dto.Id, out var leaf))
        {
            var leafId = dto.Id;
            leaf = new PaneLeafViewModel(
                leafId,
                splitRightCommand: new AsyncRelayCommand(_ => SplitAsync(leafId, PaneOrientation.Horizontal)),
                splitDownCommand: new AsyncRelayCommand(_ => SplitAsync(leafId, PaneOrientation.Vertical)),
                closePaneCommand: new AsyncRelayCommand(_ => ClosePaneAsync(leafId)),
                focusCommand: new RelayCommand(_ => FocusedLeafId = leafId),
                addTabCommand: new AsyncRelayCommand(parameter => OpenModuleInPaneAsync(leafId, (ModuleDescriptor)parameter!)));

            _leafCache[dto.Id] = leaf;
        }

        SyncLeafTabs(leaf, dto);
        return leaf;
    }

    private void SyncLeafTabs(PaneLeafViewModel leaf, PaneLeafDto dto)
    {
        for (var i = leaf.Tabs.Count - 1; i >= 0; i--)
        {
            if (!dto.ModuleIds.Contains(leaf.Tabs[i].ModuleId))
            {
                _tabCache.Remove(TabKey(leaf.Id, leaf.Tabs[i].ModuleId));
                leaf.Tabs.RemoveAt(i);
            }
        }

        foreach (var moduleId in dto.ModuleIds)
        {
            if (leaf.Tabs.Any(t => t.ModuleId == moduleId))
            {
                continue;
            }

            var tab = GetOrCreateTab(leaf.Id, moduleId);
            if (tab is not null)
            {
                leaf.Tabs.Add(tab);
            }
        }

        leaf.ActiveTab = leaf.Tabs.FirstOrDefault(t => t.ModuleId == dto.ActiveModuleId) ?? leaf.Tabs.FirstOrDefault();
    }

    private TabViewModel? GetOrCreateTab(string leafId, string moduleId)
    {
        var key = TabKey(leafId, moduleId);
        if (_tabCache.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var descriptor = ResolveModule(moduleId);
        if (descriptor is null)
        {
            return null;
        }

        var tab = new TabViewModel(
            descriptor,
            descriptor.CreateViewModel(_serviceProvider),
            activateCommand: new RelayCommand(_ => FocusTab(leafId, moduleId)),
            closeCommand: new AsyncRelayCommand(_ => CloseTabAsync(leafId, moduleId)),
            floatOutCommand: new AsyncRelayCommand(_ => FloatOutAsync(leafId, moduleId)));

        _tabCache[key] = tab;
        return tab;
    }

    private static string TabKey(string leafId, string moduleId) => $"{leafId}::{moduleId}";

    private void PruneCaches(PaneNodeDto? root)
    {
        var liveLeafIds = new HashSet<string>(StringComparer.Ordinal);
        var liveTabKeys = new HashSet<string>(StringComparer.Ordinal);
        CollectIds(root, liveLeafIds, liveTabKeys);

        foreach (var staleLeafId in _leafCache.Keys.Where(id => !liveLeafIds.Contains(id)).ToList())
        {
            _leafCache.Remove(staleLeafId);
        }

        foreach (var staleKey in _tabCache.Keys.Where(key => !liveTabKeys.Contains(key)).ToList())
        {
            _tabCache.Remove(staleKey);
        }
    }

    private static void CollectIds(PaneNodeDto? node, HashSet<string> leafIds, HashSet<string> tabKeys)
    {
        switch (node)
        {
            case null:
                return;
            case PaneLeafDto leaf:
                leafIds.Add(leaf.Id);
                foreach (var moduleId in leaf.ModuleIds)
                {
                    tabKeys.Add(TabKey(leaf.Id, moduleId));
                }

                return;
            case PaneSplitDto split:
                CollectIds(split.First, leafIds, tabKeys);
                CollectIds(split.Second, leafIds, tabKeys);
                return;
        }
    }

    private void ApplyLayout(WorkspaceLayoutDto layout)
    {
        _workspaceId = layout.Id;
        _createdAt = layout.CreatedAt;
        _isDefaultWorkspace = layout.IsDefault;
        WorkspaceName = layout.Name;
        PrimaryModuleId = layout.PrimaryModuleId;
        _secondaryRootDto = layout.SecondaryRoot;

        RebuildTree();
        SyncDockedPanels(layout.DockedPanels);
        SyncFloatingWindows(layout.FloatingWindows);
    }

    private void SyncDockedPanels(IReadOnlyList<DockedPanelDto> panels)
    {
        var state = panels.FirstOrDefault(p => p.PanelKey == OutlinePanelKey)
            ?? new DockedPanelDto(OutlinePanelKey, DockSide.Right, DefaultOutlinePanelSize, false);

        OutlinePanel.Side = state.Side;
        OutlinePanel.Size = state.Size;
        OutlinePanel.IsVisible = state.IsVisible;
    }

    private void SyncFloatingWindows(IReadOnlyList<FloatingWindowDto> windows)
    {
        _floatingWindowManager.CloseAll();
        FloatingWindows.Clear();

        foreach (var window in windows)
        {
            var descriptor = ResolveModule(window.ModuleId);
            if (descriptor is null)
            {
                continue;
            }

            _floatingWindowManager.Open(window.Id, descriptor, window.X, window.Y, window.Width, window.Height, window.IsMaximized);
            FloatingWindows.Add(new FloatingWindowHandleViewModel(window.Id, window.ModuleId, descriptor.Metadata.Title));
        }

        RefreshOutline();
    }

    private void RefreshOutline()
    {
        var entries = new List<WorkspaceOutlineEntry>();
        CollectOutlineEntries(SecondaryRoot, entries);
        foreach (var window in FloatingWindows)
        {
            entries.Add(new WorkspaceOutlineEntry(null, window.ModuleId, window.Title, true));
        }

        Outline.Refresh(entries);
    }

    private static void CollectOutlineEntries(object? node, List<WorkspaceOutlineEntry> entries)
    {
        switch (node)
        {
            case PaneLeafViewModel leaf:
                foreach (var tab in leaf.Tabs)
                {
                    entries.Add(new WorkspaceOutlineEntry(leaf.Id, tab.ModuleId, tab.Title, false));
                }

                return;
            case PaneSplitViewModel split:
                CollectOutlineEntries(split.First, entries);
                CollectOutlineEntries(split.Second, entries);
                return;
        }
    }

    private void FocusOutlineEntry(WorkspaceOutlineEntry entry)
    {
        if (entry.IsFloating)
        {
            var handle = FloatingWindows.FirstOrDefault(w => w.ModuleId == entry.ModuleId);
            if (handle is not null)
            {
                _floatingWindowManager.Focus(handle.Id);
            }
        }
        else if (entry.LeafId is not null)
        {
            FocusTab(entry.LeafId, entry.ModuleId);
        }
    }

    private async Task CloseOutlineEntryAsync(WorkspaceOutlineEntry entry)
    {
        if (entry.IsFloating)
        {
            var handle = FloatingWindows.FirstOrDefault(w => w.ModuleId == entry.ModuleId);
            if (handle is null)
            {
                return;
            }

            _floatingWindowManager.Close(handle.Id);
            FloatingWindows.Remove(handle);
            RefreshOutline();
            await SaveAsync().ConfigureAwait(true);
        }
        else if (entry.LeafId is not null)
        {
            await CloseTabAsync(entry.LeafId, entry.ModuleId).ConfigureAwait(true);
        }
    }

    private void OnFloatingWindowClosed(string floatingWindowId)
    {
        var handle = FloatingWindows.FirstOrDefault(w => w.Id == floatingWindowId);
        if (handle is null)
        {
            return;
        }

        FloatingWindows.Remove(handle);
        RefreshOutline();
        _ = SaveAsync();
    }

    private void BeginCreate()
    {
        IsRenaming = false;
        _renameTargetId = null;
        NewWorkspaceNameText = string.Empty;
        IsSwitcherOpen = true;
    }

    private void BeginRename(WorkspaceSummaryDto summary)
    {
        IsRenaming = true;
        _renameTargetId = summary.Id;
        NewWorkspaceNameText = summary.Name;
    }

    private void CancelNameEdit()
    {
        IsRenaming = false;
        _renameTargetId = null;
        NewWorkspaceNameText = string.Empty;
    }

    private async Task ConfirmNameAsync()
    {
        var name = NewWorkspaceNameText.Trim();
        if (name.Length == 0)
        {
            return;
        }

        if (IsRenaming && _renameTargetId is not null)
        {
            await _workspaceService.RenameWorkspaceAsync(_renameTargetId, name).ConfigureAwait(true);
            if (_renameTargetId == _workspaceId)
            {
                WorkspaceName = name;
            }
        }
        else
        {
            var created = await _workspaceService.CreateWorkspaceAsync(name, PrimaryModuleId).ConfigureAwait(true);
            await SwitchWorkspaceAsync(created.Id).ConfigureAwait(true);
        }

        CancelNameEdit();
        await RefreshWorkspaceListsAsync().ConfigureAwait(true);
    }

    private async Task SwitchWorkspaceAsync(string workspaceId)
    {
        if (string.Equals(workspaceId, _workspaceId, StringComparison.Ordinal))
        {
            IsSwitcherOpen = false;
            return;
        }

        var layout = await _workspaceService.SwitchWorkspaceAsync(workspaceId).ConfigureAwait(true);
        ApplyLayout(layout);
        await RefreshWorkspaceListsAsync().ConfigureAwait(true);
        IsSwitcherOpen = false;
        PrimaryModuleChangeRequested?.Invoke(this, layout.PrimaryModuleId);
    }

    private async Task DuplicateWorkspaceAsync(WorkspaceSummaryDto summary)
    {
        await _workspaceService.DuplicateWorkspaceAsync(summary.Id, summary.Name).ConfigureAwait(true);
        await RefreshWorkspaceListsAsync().ConfigureAwait(true);
    }

    private async Task DeleteWorkspaceAsync(WorkspaceSummaryDto summary)
    {
        await _workspaceService.DeleteWorkspaceAsync(summary.Id).ConfigureAwait(true);

        if (summary.IsActive)
        {
            var active = await _workspaceService.GetActiveWorkspaceAsync().ConfigureAwait(true);
            ApplyLayout(active);
            PrimaryModuleChangeRequested?.Invoke(this, active.PrimaryModuleId);
        }

        await RefreshWorkspaceListsAsync().ConfigureAwait(true);
    }

    private async Task ResetWorkspaceAsync()
    {
        var reset = await _workspaceService.ResetWorkspaceAsync(_workspaceId).ConfigureAwait(true);
        ApplyLayout(reset);
        await RefreshWorkspaceListsAsync().ConfigureAwait(true);
    }

    private async Task RefreshWorkspaceListsAsync()
    {
        var all = await _workspaceService.GetWorkspacesAsync().ConfigureAwait(true);
        Workspaces.Clear();
        foreach (var workspace in all)
        {
            Workspaces.Add(workspace);
        }

        var recent = await _workspaceService.GetRecentWorkspacesAsync().ConfigureAwait(true);
        RecentWorkspaces.Clear();
        foreach (var workspace in recent)
        {
            RecentWorkspaces.Add(workspace);
        }

        CommandManager.InvalidateRequerySuggested();
    }

    private async Task SaveAsync()
    {
        var floatingDtos = new List<FloatingWindowDto>();
        foreach (var handle in FloatingWindows)
        {
            var geometry = _floatingWindowManager.GetGeometry(handle.Id);
            floatingDtos.Add(geometry is not null
                ? new FloatingWindowDto(handle.Id, handle.ModuleId, geometry.X, geometry.Y, geometry.Width, geometry.Height, geometry.IsMaximized)
                : new FloatingWindowDto(handle.Id, handle.ModuleId, 120, 120, DefaultFloatingWidth, DefaultFloatingHeight, false));
        }

        var layout = new WorkspaceLayoutDto(
            _workspaceId,
            WorkspaceName,
            PrimaryModuleId,
            _secondaryRootDto,
            DockedPanels.Select(p => new DockedPanelDto(p.PanelKey, p.Side, p.Size, p.IsVisible)).ToList(),
            floatingDtos,
            _createdAt,
            DateTimeOffset.UtcNow,
            _isDefaultWorkspace);

        await _workspaceService.SaveLayoutAsync(layout).ConfigureAwait(true);
    }

    private ModuleDescriptor? ResolveModule(string moduleId) =>
        _moduleRegistry.Modules.FirstOrDefault(m => m.Metadata.Id == moduleId);

    private static string NewId() => Guid.NewGuid().ToString("N");
}
