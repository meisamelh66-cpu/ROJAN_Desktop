using Rojan.Desktop.Application.HR;
using DomainHr = Rojan.Desktop.Domain.HR;

namespace Rojan.Desktop.Application.Tests.HR;

public sealed class ShiftServiceTests
{
    [Fact]
    public async Task GetShiftAssignmentsForEmployeeAsync_ReturnsOnlyThatEmployeesAssignments()
    {
        var repository = new StubHrRepository();
        repository.ShiftAssignments.Add(new DomainHr.ShiftAssignment("assignment-1", "shift-1", "employee-1", "Jordan Lee", new DateOnly(2026, 7, 19)));
        repository.ShiftAssignments.Add(new DomainHr.ShiftAssignment("assignment-2", "shift-1", "employee-2", "Priya Nair", new DateOnly(2026, 7, 19)));
        var sut = new ShiftQueryService(repository);

        var result = await sut.GetShiftAssignmentsForEmployeeAsync("employee-1");

        var assignment = Assert.Single(result);
        Assert.Equal("assignment-1", assignment.Id);
    }

    [Fact]
    public async Task CreateShiftAsync_ValidRequest_AddsShift()
    {
        var repository = new StubHrRepository();
        var sut = new ShiftCommandService(repository);
        var request = new CreateShiftRequest("Morning - Hair", Department.Hair, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0));

        var created = await sut.CreateShiftAsync(request);

        Assert.Equal("Morning - Hair", created.Label);
        Assert.Single(repository.Shifts);
    }

    [Fact]
    public async Task AssignShiftAsync_UnknownEmployee_ThrowsInvalidOperationException()
    {
        var repository = new StubHrRepository();
        var sut = new ShiftCommandService(repository);
        var request = new AssignShiftRequest("shift-1", "no-such-employee", new DateOnly(2026, 7, 19));

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.AssignShiftAsync(request));
    }

    [Fact]
    public async Task AssignShiftAsync_KnownEmployee_CreatesAssignmentWithEmployeeName()
    {
        var repository = new StubHrRepository();
        repository.Employees.Add(new DomainHr.Employee("employee-1", string.Empty, "Jordan Lee", "jordan@rojan.example", "+1 555", DomainHr.EmployeeRole.Colorist, DomainHr.Department.Hair, DomainHr.EmploymentType.FullTime, DomainHr.EmployeeStatus.Active, new DateOnly(2022, 1, 1), 3000m));
        var sut = new ShiftCommandService(repository);
        var request = new AssignShiftRequest("shift-1", "employee-1", new DateOnly(2026, 7, 19));

        var created = await sut.AssignShiftAsync(request);

        Assert.Equal("Jordan Lee", created.EmployeeName);
        Assert.Single(repository.ShiftAssignments);
    }
}
