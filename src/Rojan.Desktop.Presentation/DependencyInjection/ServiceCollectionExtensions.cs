using Microsoft.Extensions.DependencyInjection;

namespace Rojan.Desktop.Presentation.DependencyInjection;

/// <summary>
/// Composition entry point for this layer. <c>Shell</c>'s composition root
/// calls this without knowing what, if anything, it registers - kept empty
/// until the first ViewModel/View pair lands. Note: the concrete
/// <see cref="Navigation.INavigationService"/> implementation is registered
/// by <c>Shell</c>, not here - this layer only owns the abstraction.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        return services;
    }
}
