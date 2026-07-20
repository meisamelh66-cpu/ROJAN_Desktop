using Rojan.Desktop.Application.Notifications;

namespace Rojan.Desktop.Application.Tests.Notifications;

/// <summary>In-memory <see cref="ISilentModePreferenceStore"/> test double.</summary>
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
