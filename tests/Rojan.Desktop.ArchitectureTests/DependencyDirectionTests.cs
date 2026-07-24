using NetArchTest.Rules;

namespace Rojan.Desktop.ArchitectureTests;

/// <summary>
/// Enforces the dependency rule stated in docs/architecture/00-overview.md
/// §2/§3 as an executable check rather than a convention everyone happens
/// to follow: Domain and Application never reference outer layers, and
/// Presentation never reaches past Application into Domain/Infrastructure
/// directly - both explicitly called out as intentional in the Dashboard/
/// Customer query service doc comments ("nothing Domain-shaped ever
/// crosses into Presentation").
///
/// Sprint 6 Commit 1: each of the three checks below also forbids
/// <c>Microsoft.EntityFrameworkCore</c> directly, not just the
/// "Rojan.Desktop.Infrastructure" assembly name. The Infrastructure-name
/// check alone would already catch EF Core reached indirectly (through
/// <c>RojanDbContext</c> or any repository); this catches the separate,
/// real possibility of a project adding a stray direct
/// <c>PackageReference</c> to an EF Core package and using its types
/// without ever going through Infrastructure at all - see
/// <c>RojanDbContext</c>'s own doc comment for why EF Core must stay
/// confined to Infrastructure.
/// </summary>
public sealed class DependencyDirectionTests
{
    [Fact]
    public void Domain_ShouldNotDependOnOuterLayers()
    {
        var result = Types.InAssembly(typeof(Domain.Customers.Customer).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Rojan.Desktop.Application",
                "Rojan.Desktop.Infrastructure",
                "Rojan.Desktop.Presentation",
                "Rojan.Desktop.Shell",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void Application_ShouldNotDependOnOuterLayers()
    {
        var result = Types.InAssembly(typeof(Application.Customers.CustomerDto).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Rojan.Desktop.Infrastructure",
                "Rojan.Desktop.Presentation",
                "Rojan.Desktop.Shell",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    [Fact]
    public void Presentation_ShouldNotDependOnDomainInfrastructureOrShell()
    {
        var result = Types.InAssembly(typeof(Presentation.Mvvm.ViewModelBase).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Rojan.Desktop.Domain",
                "Rojan.Desktop.Infrastructure",
                "Rojan.Desktop.Shell",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    private static string FailureMessage(TestResult result) =>
        "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []);
}
