using Microsoft.Extensions.DependencyInjection;

namespace Rojan.Desktop.Infrastructure.DependencyInjection;

/// <summary>
/// Composition entry point for this layer. <c>Shell</c>'s composition root
/// calls this without knowing what, if anything, it registers - kept empty
/// until the first persistence/file-system/external-I/O service lands
/// (Phase 03 logging/config decisions land here too, once approved).
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        return services;
    }
}
