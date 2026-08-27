using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Application.DependencyInjection;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Application.Specialists.Schedule;
using Rojan.Desktop.Domain.Specialists.Schedule;
using Rojan.Desktop.Infrastructure.DependencyInjection;
using Rojan.Desktop.Infrastructure.Specialists.Schedule;
using Rojan.Desktop.Presentation.DependencyInjection;
using Rojan.Desktop.Presentation.ViewModels.Specialists;

namespace Rojan.Desktop.Shell.Tests.DependencyInjection;

/// <summary>
/// Phase 7.4.8 Shift Engine DI Production Fix: proves the real composition root -
/// <c>AddApplication().AddInfrastructure().AddPresentation()</c>, the exact call order
/// <c>App.xaml.cs</c> itself uses - can actually resolve <see cref="SpecialistPageViewModel"/>
/// end to end. This lives in Shell.Tests specifically because it is the one test project with a
/// real (transitive) reference to all three layers at once; every other test project only reaches
/// one or two of them, which is exactly why this gap was invisible until now (see below).
///
/// Root cause (see ROJAN_PHASE7_4_7_AUTHORITY_RECONCILIATION_REPORT_v1.md and
/// ROJAN_PHASE7_4_8_SHIFT_ENGINE_DI_FIX_REPORT_v1.md for the full account): the Shift Engine
/// commit that introduced <see cref="ISpecialistScheduleQueryService"/>/
/// <see cref="ISpecialistScheduleCommandService"/>/<see cref="ISpecialistScheduleRepository"/> and
/// their real implementations also wired <see cref="SpecialistPageViewModel"/>/
/// <c>SpecialistProfileViewModel</c> to constructor-inject the first two - but never registered
/// any of the three with the container. Every existing <see cref="SpecialistPageViewModel"/>/
/// <c>SpecialistProfileViewModel</c> test constructs its subject directly via <c>new(...)</c>, so
/// the full test suite passing never proved the real container could do the same. Confirmed
/// directly against <c>HEAD</c> before this fix: <c>ISpecialistScheduleQueryService</c> was absent
/// from <c>AddApplication()</c>'s registrations entirely - this test would have thrown
/// <see cref="InvalidOperationException"/> ("Unable to resolve service for type
/// ISpecialistScheduleQueryService...") at the <c>GetRequiredService</c> call below, the same
/// exception the real running app would throw the moment a user opened the Specialists page.
/// </summary>
public sealed class ShiftEngineDependencyInjectionTests
{
    [Fact]
    public void RealCompositionRoot_ResolvesSpecialistPageViewModel()
    {
        var services = new ServiceCollection()
            .AddApplication()
            .AddInfrastructure()
            .AddPresentation();
        services.AddSingleton<IEnterpriseContext>(new StubEnterpriseContext());
        var provider = services.BuildServiceProvider();

        // Before this fix: throws InvalidOperationException here - ISpecialistScheduleQueryService/
        // ISpecialistScheduleCommandService have no registration to satisfy SpecialistPageViewModel's
        // own constructor. After: resolves successfully, proving the whole Shift Engine dependency
        // graph (query service -> repository, command service -> permission gate -> repository) is
        // now reachable from the real container, not just from tests that bypass it.
        var viewModel = provider.GetRequiredService<SpecialistPageViewModel>();

        Assert.NotNull(viewModel);
    }

    [Fact]
    public void AddInfrastructure_RegistersBackendSpecialistScheduleRepository()
    {
        // Same "prove the real composition root, not a reimplementation of it" shape as
        // Infrastructure.Tests' own PersistenceDependencyInjectionTests Backend*Repository
        // assertions (AddInfrastructure_RegistersBackendCustomerRepository, etc.).
        var services = new ServiceCollection().AddApplication().AddInfrastructure();
        services.AddSingleton<IEnterpriseContext>(new StubEnterpriseContext());
        var provider = services.BuildServiceProvider();

        var repository = provider.GetRequiredService<ISpecialistScheduleRepository>();

        Assert.IsType<BackendSpecialistScheduleRepository>(repository);
    }

    [Fact]
    public void AddApplication_RegistersScheduleCommandServiceBehindTheExistingBackendPermissionGate()
    {
        // No new permission model and no new authority - SpecialistScheduleCommandServicePermissionGate
        // itself was already committed (f691dea) and already uses the same IBackendPermissionGate
        // every other command-side gate in this codebase uses. This only proves the DI wiring now
        // reaches the already-existing gate, not that anything about authorization changed.
        var services = new ServiceCollection().AddApplication().AddInfrastructure();
        services.AddSingleton<IEnterpriseContext>(new StubEnterpriseContext());
        var provider = services.BuildServiceProvider();

        var commandService = provider.GetRequiredService<ISpecialistScheduleCommandService>();

        Assert.IsType<SpecialistScheduleCommandServicePermissionGate>(commandService);
    }

    private sealed class StubEnterpriseContext : IEnterpriseContext
    {
        public string? CurrentOrganizationId => "org-1";

        public string? CurrentBranchId => null;

        public WorkspaceRole CurrentRole => WorkspaceRole.OrganizationOwner;

        public IReadOnlySet<string> BackendPermissions => new HashSet<string>();
    }
}
