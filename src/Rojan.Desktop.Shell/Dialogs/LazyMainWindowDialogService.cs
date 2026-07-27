using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Presentation.Dialogs;

namespace Rojan.Desktop.Shell.Dialogs;

/// <summary>
/// Deadlock fix: <see cref="IDialogService"/> was previously registered as
/// <c>sp.GetRequiredService&lt;MainWindowViewModel&gt;()</c> directly -
/// fine on its own, but a circular resolution the instant
/// <see cref="MainWindowViewModel"/>'s own constructor needs it
/// transitively. That constructor navigates to whatever module the
/// persisted workspace restores (e.g. Bookings) before it returns, and
/// that module's ViewModel (e.g. <c>BookingPageViewModel</c>) takes
/// <see cref="IDialogService"/> as a constructor dependency - resolving
/// it eagerly re-enters resolution of the very
/// <see cref="MainWindowViewModel"/> singleton still under construction,
/// which deadlocks inside <c>Microsoft.Extensions.DependencyInjection</c>'s
/// own stack-guard/re-entrancy handling (confirmed via memory dump: the UI
/// thread parked in <c>ServiceProvider.GetService</c> resolving
/// <c>BookingPageViewModel</c>, forever) - no exception, no window, the
/// process just sits there.
///
/// This proxy breaks the cycle by deferring the real resolution: injecting
/// <see cref="IServiceProvider"/> costs nothing at construction time (it is
/// always already available, never itself re-entrant), so any ViewModel
/// built as part of <see cref="MainWindowViewModel"/>'s own construction
/// can depend on <see cref="IDialogService"/> without forcing
/// <see cref="MainWindowViewModel"/> to resolve before it exists.
/// <see cref="MainWindowViewModel"/> is only actually resolved inside
/// <see cref="ShowDialog"/>/<see cref="CloseDialog"/> - by the time either
/// is ever called, construction has long since finished and the singleton
/// is simply returned from the container's cache, same as any other
/// singleton lookup.
/// </summary>
internal sealed class LazyMainWindowDialogService : IDialogService
{
    private readonly IServiceProvider _serviceProvider;

    public LazyMainWindowDialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void ShowDialog(object viewModel) =>
        _serviceProvider.GetRequiredService<MainWindowViewModel>().ShowDialog(viewModel);

    public void CloseDialog() =>
        _serviceProvider.GetRequiredService<MainWindowViewModel>().CloseDialog();
}
