using Microsoft.Extensions.DependencyInjection;

namespace Rojan.Desktop.Application.DependencyInjection;

/// <summary>
/// Composition entry point for this layer. <c>Shell</c>'s composition root
/// calls this without knowing what, if anything, it registers - kept empty
/// until the first use case/CQRS handler lands (Phase 06+).
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
