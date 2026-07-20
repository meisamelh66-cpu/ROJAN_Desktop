using Rojan.Desktop.Application.Notifications;

namespace Rojan.Desktop.Shell.Tests.Navigation;

/// <summary>In-memory <see cref="ISilentModePreferenceStore"/> test double - avoids touching the real %LocalAppData% JSON file <c>LocalSilentModePreferenceStore</c> persists to.</summary>
internal sealed class StubSilentModePreferenceStore : ISilentModePreferenceStore
{
    private bool _isEnabled;

    public Task<bool> GetIsEnabledAsync(CancellationToken cancellationToken = default) => Task.FromResult(_isEnabled);

    public Task SetIsEnabledAsync(bool isEnabled, CancellationToken cancellationToken = default)
    {
        _isEnabled = isEnabled;
        return Task.CompletedTask;
    }
}
