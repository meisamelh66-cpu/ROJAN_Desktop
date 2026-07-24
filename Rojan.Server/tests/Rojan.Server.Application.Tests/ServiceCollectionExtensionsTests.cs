using Microsoft.Extensions.DependencyInjection;
using Rojan.Server.Application.DependencyInjection;

namespace Rojan.Server.Application.Tests;

/// <summary>Sprint 8 Commit 1: Backend Foundation. Foundation-only test - <see cref="ServiceCollectionExtensions.AddApplication"/> registers nothing yet (no business orchestration exists), so this only proves the composition-root seam itself is callable and wired correctly.</summary>
public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddApplication_ReturnsTheSameServiceCollectionForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddApplication();

        Assert.Same(services, result);
    }

    [Fact]
    public void AddApplication_ServiceProviderBuildsWithoutThrowing()
    {
        var services = new ServiceCollection();
        services.AddApplication();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider);
    }
}
