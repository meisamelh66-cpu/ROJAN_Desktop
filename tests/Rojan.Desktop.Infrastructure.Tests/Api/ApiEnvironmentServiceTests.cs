using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Infrastructure.Api;

namespace Rojan.Desktop.Infrastructure.Tests.Api;

/// <summary>
/// Exercises <see cref="ApiEnvironmentService"/> against a temp settings
/// file (never the real %LocalAppData%\RojanDesktop\api\environment.json)
/// via its internal path-overriding constructor - same shape
/// <c>Shell.Tests.Theming.ThemeServiceTests</c> already establishes.
///
/// Desktop Productionization Sprint 1: covers the new
/// <see cref="ApiEnvironmentService.ProductionUrlDefault"/> fallback - a
/// fresh install with no persisted override now resolves Production to a
/// real address instead of <see langword="null"/>, while an explicit
/// override (persisted or via <c>ROJAN_API_BASE_URL</c>) still wins, and
/// Development stays the untouched first-launch default.
/// </summary>
public sealed class ApiEnvironmentServiceTests : IDisposable
{
    private const string EnvironmentVariable = "ROJAN_API_BASE_URL";

    private readonly string _settingsFilePath;
    private readonly string? _originalEnvironmentVariable;

    public ApiEnvironmentServiceTests()
    {
        _settingsFilePath = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"), "api-environment.json");
        _originalEnvironmentVariable = Environment.GetEnvironmentVariable(EnvironmentVariable);
        Environment.SetEnvironmentVariable(EnvironmentVariable, null);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(EnvironmentVariable, _originalEnvironmentVariable);

        var directory = Path.GetDirectoryName(_settingsFilePath);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task InitializeAsync_WithNoSettingsFile_DefaultsToDevelopment()
    {
        var service = new ApiEnvironmentService(_settingsFilePath);

        await service.InitializeAsync();

        Assert.Equal(ApiEnvironment.Development, service.SelectedEnvironment);
        Assert.Equal(new Uri("http://localhost:8080"), service.ResolvedBaseAddress);
    }

    [Fact]
    public async Task SetEnvironmentAsync_ToProductionWithNoUrlEverConfigured_ResolvesToTheRealProductionDefault()
    {
        var service = new ApiEnvironmentService(_settingsFilePath);
        await service.InitializeAsync();

        await service.SetEnvironmentAsync(ApiEnvironment.Production, productionUrl: null);

        Assert.Null(service.ProductionUrl);
        Assert.Equal(new Uri(ApiEnvironmentService.ProductionUrlDefault), service.ResolvedBaseAddress);
    }

    [Fact]
    public async Task SetEnvironmentAsync_ToProductionWithAnExplicitUrl_TheExplicitUrlWinsOverTheDefault()
    {
        var service = new ApiEnvironmentService(_settingsFilePath);
        await service.InitializeAsync();

        await service.SetEnvironmentAsync(ApiEnvironment.Production, productionUrl: "https://staging.rojanai.ir");

        Assert.Equal(new Uri("https://staging.rojanai.ir"), service.ResolvedBaseAddress);
    }

    [Fact]
    public async Task InitializeAsync_ReloadingAPersistedProductionSelectionWithNoStoredUrl_StillResolvesToTheRealDefault()
    {
        var writer = new ApiEnvironmentService(_settingsFilePath);
        await writer.InitializeAsync();
        await writer.SetEnvironmentAsync(ApiEnvironment.Production, productionUrl: null);

        var reader = new ApiEnvironmentService(_settingsFilePath);
        await reader.InitializeAsync();

        Assert.Equal(ApiEnvironment.Production, reader.SelectedEnvironment);
        Assert.Equal(new Uri(ApiEnvironmentService.ProductionUrlDefault), reader.ResolvedBaseAddress);
    }

    [Fact]
    public async Task ResolvedBaseAddress_WithTheEnvironmentVariableSet_TheEnvironmentVariableWinsOverEverything()
    {
        Environment.SetEnvironmentVariable(EnvironmentVariable, "https://override.example.test");
        var service = new ApiEnvironmentService(_settingsFilePath);
        await service.InitializeAsync();
        await service.SetEnvironmentAsync(ApiEnvironment.Production, productionUrl: "https://staging.rojanai.ir");

        Assert.Equal(new Uri("https://override.example.test"), service.ResolvedBaseAddress);
    }
}
