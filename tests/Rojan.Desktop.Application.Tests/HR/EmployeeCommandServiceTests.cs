using Rojan.Desktop.Application.HR;
using DomainHr = Rojan.Desktop.Domain.HR;

namespace Rojan.Desktop.Application.Tests.HR;

public sealed class EmployeeCommandServiceTests
{
    private static EmployeeCommandService MakeSut(StubHrRepository? repository = null) => new(repository ?? new StubHrRepository());

    private static DomainHr.Employee MakeEmployee(string id, DomainHr.EmployeeStatus status) =>
        new(id, string.Empty, "Test Employee", "test@rojan.example", "+1 555", DomainHr.EmployeeRole.Stylist, DomainHr.Department.Hair, DomainHr.EmploymentType.FullTime, status, new DateOnly(2022, 1, 1), 2500m);

    [Fact]
    public async Task CreateEmployeeAsync_ValidRequest_CreatesActiveEmployee()
    {
        var repository = new StubHrRepository();
        var sut = MakeSut(repository);
        var request = new CreateEmployeeRequest(string.Empty, "New Hire", "new.hire@rojan.example", "+1 555", EmployeeRole.Stylist, Department.Hair, EmploymentType.FullTime, new DateOnly(2026, 1, 1), 2200m);

        var created = await sut.CreateEmployeeAsync(request);

        Assert.Equal("New Hire", created.FullName);
        Assert.Equal(EmployeeStatus.Active, created.Status);
        Assert.Single(repository.Employees);
    }

    [Fact]
    public async Task ActivateEmployeeAsync_AlreadyActive_ThrowsInvalidOperationException()
    {
        var repository = new StubHrRepository();
        repository.Employees.Add(MakeEmployee("employee-1", DomainHr.EmployeeStatus.Active));
        var sut = MakeSut(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ActivateEmployeeAsync("employee-1"));
    }

    [Fact]
    public async Task ActivateEmployeeAsync_Inactive_SetsStatusToActive()
    {
        var repository = new StubHrRepository();
        repository.Employees.Add(MakeEmployee("employee-1", DomainHr.EmployeeStatus.Inactive));
        var sut = MakeSut(repository);

        var result = await sut.ActivateEmployeeAsync("employee-1");

        Assert.Equal(EmployeeStatus.Active, result.Status);
    }

    [Fact]
    public async Task DeactivateEmployeeAsync_Active_SetsStatusToInactive()
    {
        var repository = new StubHrRepository();
        repository.Employees.Add(MakeEmployee("employee-1", DomainHr.EmployeeStatus.Active));
        var sut = MakeSut(repository);

        var result = await sut.DeactivateEmployeeAsync("employee-1");

        Assert.Equal(EmployeeStatus.Inactive, result.Status);
    }

    [Fact]
    public async Task SuspendEmployeeAsync_Inactive_ThrowsInvalidOperationException()
    {
        var repository = new StubHrRepository();
        repository.Employees.Add(MakeEmployee("employee-1", DomainHr.EmployeeStatus.Inactive));
        var sut = MakeSut(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SuspendEmployeeAsync("employee-1"));
    }

    [Fact]
    public async Task SuspendEmployeeAsync_Active_SetsStatusToSuspended()
    {
        var repository = new StubHrRepository();
        repository.Employees.Add(MakeEmployee("employee-1", DomainHr.EmployeeStatus.Active));
        var sut = MakeSut(repository);

        var result = await sut.SuspendEmployeeAsync("employee-1");

        Assert.Equal(EmployeeStatus.Suspended, result.Status);
    }

    [Fact]
    public async Task AssignDepartmentAsync_ExistingEmployee_ChangesDepartment()
    {
        var repository = new StubHrRepository();
        repository.Employees.Add(MakeEmployee("employee-1", DomainHr.EmployeeStatus.Active));
        var sut = MakeSut(repository);

        var result = await sut.AssignDepartmentAsync("employee-1", Department.Nails);

        Assert.Equal(Department.Nails, result.Department);
    }
}
