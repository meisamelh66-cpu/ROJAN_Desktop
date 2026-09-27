using Rojan.Desktop.Presentation.Mvvm;

namespace Rojan.Desktop.Presentation.Modules;

/// <summary>
/// A module's metadata plus how to activate it. <c>CreateViewModel</c> is a
/// factory rather than a plain <see cref="Type"/> so navigation isn't
/// constrained to DI's generic-type resolution (<c>NavigateTo&lt;T&gt;()</c>) -
/// several modules can legitimately share one ViewModel type (see
/// <see cref="PlaceholderModule"/>) while still each carrying their own
/// construction data (their title). Real modules typically resolve their
/// ViewModel from the container inside this factory; placeholder modules
/// construct it directly.
///
/// Shell Navigation: <see cref="ViewModelType"/> optionally names the page
/// ViewModel type <see cref="CreateViewModel"/> produces, so the shell can map a
/// typed <c>NavigateTo&lt;T&gt;()</c> (e.g. a Dashboard shortcut) back to its
/// sidebar module without constructing anything. Left <see langword="null"/>
/// where one type is shared by several modules (<see cref="PlaceholderModule"/>),
/// since a type alone cannot identify the module there.
/// </summary>
public sealed record ModuleDescriptor(ModuleMetadata Metadata, Func<IServiceProvider, ViewModelBase> CreateViewModel, Type? ViewModelType = null);
