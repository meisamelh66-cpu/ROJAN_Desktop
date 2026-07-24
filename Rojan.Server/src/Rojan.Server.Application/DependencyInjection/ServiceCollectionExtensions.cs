using Microsoft.Extensions.DependencyInjection;

namespace Rojan.Server.Application.DependencyInjection;

/// <summary>
/// Composition entry point for this layer, same shape as the desktop
/// solution's own <c>Application.DependencyInjection.ServiceCollectionExtensions.AddApplication</c> -
/// <c>Rojan.Server.Api</c>'s composition root calls this without knowing
/// what, if anything, it registers.
///
/// Sprint 8 Commit 1: Backend Foundation. Empty today - no business
/// orchestration exists yet (explicitly out of scope for this commit).
/// Exists now so a future commit that adds the first real use
/// case/handler only has to add a registration line here, not invent this
/// seam.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services) => services;
}
