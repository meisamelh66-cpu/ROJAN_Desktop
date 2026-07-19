using NetArchTest.Rules;
using Rojan.Desktop.Presentation.ViewModels.Customers;

namespace Rojan.Desktop.ArchitectureTests;

/// <summary>
/// Enforces the "testable without a running Dispatcher/Application" goal
/// stated in docs/architecture/00-overview.md §2 ("ViewModels must be
/// testable without a running Application/Dispatcher") as an executable
/// check: no type in the ViewModels namespace may depend on WPF's
/// threading/dispatcher machinery or take a hard dependency on element
/// base types that assume a live visual tree.
/// </summary>
public sealed class ViewModelTestabilityTests
{
    [Fact]
    public void ViewModels_ShouldNotDependOnWpfDispatcherOrControls()
    {
        var result = Types.InAssembly(typeof(CustomerPageViewModel).Assembly)
            .That()
            .ResideInNamespace("Rojan.Desktop.Presentation.ViewModels")
            .ShouldNot()
            .HaveDependencyOnAny("System.Windows.Threading", "System.Windows.Controls")
            .GetResult();

        Assert.True(result.IsSuccessful, "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }
}
