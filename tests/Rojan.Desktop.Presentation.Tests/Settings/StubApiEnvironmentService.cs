using Rojan.Desktop.Application.Api;

namespace Rojan.Desktop.Presentation.Tests.Settings;

/// <summary>Fakes <see cref="IApiEnvironmentService"/> for SettingsPageViewModel tests - no file-system access, unlike the real Infrastructure.Api.ApiEnvironmentService.</summary>
internal sealed class StubApiEnvironmentService(ApiEnvironment selectedEnvironment = ApiEnvironment.Development, string? productionUrl = null) : IApiEnvironmentService
{
    public ApiEnvironment SelectedEnvironment { get; private set; } = selectedEnvironment;

    public string? ProductionUrl { get; private set; } = productionUrl;

    public bool IsRestartRequired { get; private set; }

    public Uri? ResolvedBaseAddress => null;

    /// <summary>Optional failure hook - when set, <see cref="SetEnvironmentAsync"/> faults with this exception (no state change).</summary>
    public Exception? SetEnvironmentException { get; set; }

    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SetEnvironmentAsync(ApiEnvironment environment, string? newProductionUrl, CancellationToken cancellationToken = default)
    {
        if (SetEnvironmentException is not null)
        {
            return Task.FromException(SetEnvironmentException);
        }

        if (environment != SelectedEnvironment || (newProductionUrl is not null && newProductionUrl != ProductionUrl))
        {
            IsRestartRequired = true;
        }

        SelectedEnvironment = environment;
        ProductionUrl = newProductionUrl ?? ProductionUrl;
        return Task.CompletedTask;
    }
}
