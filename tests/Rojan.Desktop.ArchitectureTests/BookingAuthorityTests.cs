using NetArchTest.Rules;

namespace Rojan.Desktop.ArchitectureTests;

/// <summary>
/// P1-1 - Guard local-release calls. R1 (<c>refactor(desktop): remove client
/// booking conflict authority</c>) removed <see cref="Application.BookingWorkflow.BookingWorkflowService"/>'s
/// dependency on <c>Application.Calendar.ICalendarCommandService</c> entirely -
/// Backend is the only Booking Authority, and the local
/// reserve/release-then-check-conflict flow that dependency enabled is
/// exactly the pattern this project's governance work exists to keep out.
///
/// <c>ICalendarCommandService</c>/<c>CalendarCommandService</c>/
/// <c>EfCalendarRepository</c> are deliberately retired in place, not
/// deleted, per this codebase's own established Fake/Ef-&gt;Backend
/// convention - which means nothing currently stops a future change from
/// quietly re-wiring that dependency back into <c>BookingWorkflowService</c>
/// and silently reintroducing the same P0. This test is that guard: an
/// executable rule, not a convention someone has to remember, in the same
/// style <see cref="DependencyDirectionTests"/> already established for
/// this project's layering rules.
/// </summary>
public sealed class BookingAuthorityTests
{
    [Fact]
    public void BookingWorkflow_ShouldNotDependOnCalendarCommandService()
    {
        var result = Types.InAssembly(typeof(Application.BookingWorkflow.BookingWorkflowService).Assembly)
            .That().ResideInNamespace("Rojan.Desktop.Application.BookingWorkflow")
            .ShouldNot()
            .HaveDependencyOn("Rojan.Desktop.Application.Calendar.ICalendarCommandService")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(result));
    }

    private static string FailureMessage(TestResult result) =>
        "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []);
}
