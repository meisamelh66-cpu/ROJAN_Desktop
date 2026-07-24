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

    // Sprint 5 Commit 4: premium specialist search and profile foundation.

    private static IReadOnlyList<DomainSpecialists.Specialist> MakeFilterFixture() =>
    [
        new("specialist-1", "Jordan Lee", "Senior Colour Specialist", "jordan.lee@rojan.example", "+1 555 020 1001",
            DomainSpecialists.SpecialistStatus.Active, "Specializes in balayage."),
        new("specialist-2", "Priya Nair", "Stylist & Consultant", "priya.nair@rojan.example", "+1 555 020 1002",
            DomainSpecialists.SpecialistStatus.Active, "Consults on new-client styling."),
        new("specialist-3", "Casey Morgan", "Spa Therapist", "casey.morgan@rojan.example", "+1 555 020 1003",
            DomainSpecialists.SpecialistStatus.OnLeave, "Massage and facial specialist."),
    ];

    [Fact]
    public async Task SearchSpecialistsAsync_EmptyFilter_ReturnsEverySpecialistSameAsGetSpecialistsAsync()
    {
        var repository = new StubSpecialistRepository(MakeFilterFixture());
        var sut = new SpecialistQueryService(repository);

        var filtered = await sut.SearchSpecialistsAsync(SpecialistSearchFilter.Empty);
        var unfiltered = await sut.GetSpecialistsAsync();

        Assert.Equal(unfiltered.Select(specialist => specialist.Id), filtered.Select(specialist => specialist.Id));
    }

    [Fact]
    public async Task SearchSpecialistsAsync_NullFilterFields_BehaveAsNoFilterApplied()
    {
        var repository = new StubSpecialistRepository(MakeFilterFixture());
        var sut = new SpecialistQueryService(repository);

        var result = await sut.SearchSpecialistsAsync(new SpecialistSearchFilter(SearchText: null, Status: null, Skill: null));

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task SearchSpecialistsAsync_FilterSearchText_MatchesBio()
    {
        var repository = new StubSpecialistRepository(MakeFilterFixture());
        var sut = new SpecialistQueryService(repository);

        var result = await sut.SearchSpecialistsAsync(new SpecialistSearchFilter(SearchText: "balayage"));

        Assert.Equal("specialist-1", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchSpecialistsAsync_FilterSearchText_MatchesName()
    {
        var repository = new StubSpecialistRepository(MakeFilterFixture());
        var sut = new SpecialistQueryService(repository);

        var result = await sut.SearchSpecialistsAsync(new SpecialistSearchFilter(SearchText: "Priya"));

        Assert.Equal("specialist-2", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchSpecialistsAsync_FilterSearchText_MatchesPhone()
    {
        var repository = new StubSpecialistRepository(MakeFilterFixture());
        var sut = new SpecialistQueryService(repository);

        var result = await sut.SearchSpecialistsAsync(new SpecialistSearchFilter(SearchText: "1003"));

        Assert.Equal("specialist-3", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchSpecialistsAsync_FilterSearchText_DoesNotMatchTitleOrEmail()
    {
        // Deliberate behavior difference from the legacy SearchSpecialistsAsync(string) overload
        // above: the new filter's SearchText covers Name/Bio/Phone only, per this commit's scope.
        var repository = new StubSpecialistRepository(MakeFilterFixture());
        var sut = new SpecialistQueryService(repository);

        var result = await sut.SearchSpecialistsAsync(new SpecialistSearchFilter(SearchText: "Therapist"));

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchSpecialistsAsync_FilterStatus_ReturnsOnlyThatStatus()
    {
        var repository = new StubSpecialistRepository(MakeFilterFixture());
        var sut = new SpecialistQueryService(repository);

        var result = await sut.SearchSpecialistsAsync(new SpecialistSearchFilter(Status: SpecialistStatus.OnLeave));

        Assert.Equal("specialist-3", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchSpecialistsAsync_FilterSkill_ReturnsOnlySpecialistsWithMatchingSkill()
    {
        var repository = new StubSpecialistRepository(MakeFilterFixture());
        repository.Skills.Add(new DomainSpecialists.SpecialistSkill("skill-1", "specialist-1", "Balayage"));
        repository.Skills.Add(new DomainSpecialists.SpecialistSkill("skill-2", "specialist-2", "Consultation"));
        var sut = new SpecialistQueryService(repository);

        var result = await sut.SearchSpecialistsAsync(new SpecialistSearchFilter(Skill: "balayage"));

        Assert.Equal("specialist-1", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchSpecialistsAsync_FilterSkill_NoSpecialistHasMatchingSkill_ReturnsEmptyList()
    {
        var repository = new StubSpecialistRepository(MakeFilterFixture());
        repository.Skills.Add(new DomainSpecialists.SpecialistSkill("skill-1", "specialist-1", "Balayage"));
        var sut = new SpecialistQueryService(repository);

        var result = await sut.SearchSpecialistsAsync(new SpecialistSearchFilter(Skill: "massage-therapy"));

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchSpecialistsAsync_CombinedFilters_AreAndedTogether()
    {
        var repository = new StubSpecialistRepository(MakeFilterFixture());
        var sut = new SpecialistQueryService(repository);

        // specialist-3 is OnLeave, not Active - an Active status plus a name match on Casey must
        // match nobody.
        var result = await sut.SearchSpecialistsAsync(new SpecialistSearchFilter(SearchText: "Casey", Status: SpecialistStatus.Active));

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchSpecialistsAsync_CombinedFilters_MatchWhenEveryCriterionAgrees()
    {
        var repository = new StubSpecialistRepository(MakeFilterFixture());
        repository.Skills.Add(new DomainSpecialists.SpecialistSkill("skill-1", "specialist-1", "Balayage"));
        var sut = new SpecialistQueryService(repository);

        var result = await sut.SearchSpecialistsAsync(new SpecialistSearchFilter(
            Status: SpecialistStatus.Active, Skill: "Balayage"));

        Assert.Equal("specialist-1", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchSpecialistsAsync_FilterMatchesNoSpecialist_ReturnsEmptyList()
    {
        var repository = new StubSpecialistRepository(MakeFilterFixture());
        var sut = new SpecialistQueryService(repository);

        var result = await sut.SearchSpecialistsAsync(new SpecialistSearchFilter(SearchText: "no-such-specialist"));

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchSpecialistsAsync_FilteredResults_PreserveRepositoryOrder()
    {
        // Deterministic, stable ordering: filtering must never reorder the underlying repository
        // sequence, only narrow it.
        var repository = new StubSpecialistRepository(MakeFilterFixture());
        var sut = new SpecialistQueryService(repository);

        var result = await sut.SearchSpecialistsAsync(new SpecialistSearchFilter());

        Assert.Equal(["specialist-1", "specialist-2", "specialist-3"], result.Select(specialist => specialist.Id));
    }
}
