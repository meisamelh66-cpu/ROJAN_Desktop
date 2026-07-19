using Rojan.Desktop.Application.Specialists;
using DomainSpecialists = Rojan.Desktop.Domain.Specialists;

namespace Rojan.Desktop.Application.Tests.Specialists;

public sealed class SpecialistQueryServiceTests
{
    [Fact]
    public async Task GetSpecialistsAsync_RepositoryReturnsSpecialists_MapsEveryFieldToDto()
    {
        var domainSpecialist = new DomainSpecialists.Specialist(
            "specialist-1", "Jordan Lee", "Senior Colour Specialist", "jordan.lee@rojan.example",
            "+1 555 020 1001", DomainSpecialists.SpecialistStatus.Active, "Specializes in balayage.");
        var repository = new StubSpecialistRepository([domainSpecialist]);
        var sut = new SpecialistQueryService(repository);

        var result = await sut.GetSpecialistsAsync();

        var dto = Assert.Single(result);
        Assert.Equal(domainSpecialist.Id, dto.Id);
        Assert.Equal(domainSpecialist.FullName, dto.FullName);
        Assert.Equal(domainSpecialist.Title, dto.Title);
        Assert.Equal(domainSpecialist.Email, dto.Email);
        Assert.Equal(domainSpecialist.Phone, dto.Phone);
        Assert.Equal(SpecialistStatus.Active, dto.Status);
        Assert.Equal(domainSpecialist.Bio, dto.Bio);
    }

    [Fact]
    public async Task GetSpecialistsAsync_RepositoryReturnsEmptyList_ReturnsEmptyList()
    {
        var repository = new StubSpecialistRepository([]);
        var sut = new SpecialistQueryService(repository);

        var result = await sut.GetSpecialistsAsync();

        Assert.Empty(result);
    }

    [Theory]
    [InlineData(DomainSpecialists.SpecialistStatus.Active, SpecialistStatus.Active)]
    [InlineData(DomainSpecialists.SpecialistStatus.OnLeave, SpecialistStatus.OnLeave)]
    [InlineData(DomainSpecialists.SpecialistStatus.Inactive, SpecialistStatus.Inactive)]
    public async Task GetSpecialistsAsync_EachDomainStatus_MapsToMatchingApplicationStatus(
        DomainSpecialists.SpecialistStatus domainStatus, SpecialistStatus expectedStatus)
    {
        var domainSpecialist = new DomainSpecialists.Specialist(
            "specialist-1", "Test Specialist", string.Empty, "test@example.com", string.Empty,
            domainStatus, string.Empty);
        var repository = new StubSpecialistRepository([domainSpecialist]);
        var sut = new SpecialistQueryService(repository);

        var result = await sut.GetSpecialistsAsync();

        Assert.Equal(expectedStatus, Assert.Single(result).Status);
    }

    private static IReadOnlyList<DomainSpecialists.Specialist> MakeSearchFixture() =>
    [
        new("specialist-1", "Jordan Lee", "Senior Colour Specialist", "jordan.lee@rojan.example", string.Empty,
            DomainSpecialists.SpecialistStatus.Active, string.Empty),
        new("specialist-2", "Priya Nair", "Stylist & Consultant", "priya.nair@rojan.example", string.Empty,
            DomainSpecialists.SpecialistStatus.Active, string.Empty),
        new("specialist-3", "Casey Morgan", "Spa Therapist", "casey.morgan@rojan.example", string.Empty,
            DomainSpecialists.SpecialistStatus.Active, string.Empty),
    ];

    [Fact]
    public async Task SearchSpecialistsAsync_TextMatchesTitle_ReturnsOnlyThatSpecialist()
    {
        var repository = new StubSpecialistRepository(MakeSearchFixture());
        var sut = new SpecialistQueryService(repository);

        var result = await sut.SearchSpecialistsAsync("therapist");

        Assert.Equal("specialist-3", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchSpecialistsAsync_TextMatchesEmail_ReturnsOnlyThatSpecialist()
    {
        var repository = new StubSpecialistRepository(MakeSearchFixture());
        var sut = new SpecialistQueryService(repository);

        var result = await sut.SearchSpecialistsAsync("priya.nair");

        Assert.Equal("specialist-2", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchSpecialistsAsync_EmptySearchText_ReturnsEverySpecialist()
    {
        var repository = new StubSpecialistRepository(MakeSearchFixture());
        var sut = new SpecialistQueryService(repository);

        var result = await sut.SearchSpecialistsAsync(string.Empty);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task SearchSpecialistsAsync_NoMatch_ReturnsEmptyList()
    {
        var repository = new StubSpecialistRepository(MakeSearchFixture());
        var sut = new SpecialistQueryService(repository);

        var result = await sut.SearchSpecialistsAsync("no-such-specialist");

        Assert.Empty(result);
    }
}
