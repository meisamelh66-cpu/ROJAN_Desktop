using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Rojan.Desktop.Infrastructure.Persistence;
using Rojan.Desktop.Infrastructure.Persistence.Specialists;
using DomainSpecialists = Rojan.Desktop.Domain.Specialists;

namespace Rojan.Desktop.Infrastructure.Tests.Persistence.Specialists;

/// <summary>
/// Exercises <see cref="EfSpecialistRepository"/> against a real, migrated,
/// temp-file SQLite database - never the production
/// <see cref="SqlitePersistenceOptions.Default"/> path. Same shape as
/// <c>Customers.EfCustomerRepositoryTests</c>: every test resolves a fresh
/// <see cref="RojanDbContext"/> per operation through the same
/// <see cref="IDbContextFactory{TContext}"/> the repository itself uses,
/// so a "does it actually persist" assertion means what it says. No
/// Organization/Branch tests here - unlike Customers,
/// <see cref="DomainSpecialists.Specialist"/> has no such fields at all
/// (confirmed by reading the Domain record and both Application services
/// before writing any code - Specialists is not an Organization/Branch-
/// scoped module).
/// </summary>
public sealed class EfSpecialistRepositoryTests : IDisposable
{
    private readonly string _testRoot;
    private readonly EfSpecialistRepository _sut;

    public EfSpecialistRepositoryTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"));
        var options = new SqlitePersistenceOptions(Path.Combine(_testRoot, "rojan.db"));
        var optionsBuilder = new DbContextOptionsBuilder<RojanDbContext>().UseSqlite(options.ConnectionString);
        var contextFactory = new TestDbContextFactory(optionsBuilder.Options);

        // Applies the real Sprint 6 Commit 3 migration (not EnsureCreated,
        // which would create the schema straight from the model and never
        // actually exercise the migration file itself).
        using var context = contextFactory.CreateDbContext();
        context.Database.Migrate();

        _sut = new EfSpecialistRepository(contextFactory);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private static DomainSpecialists.Specialist MakeSpecialist(
        string id = "specialist-1",
        DomainSpecialists.SpecialistStatus status = DomainSpecialists.SpecialistStatus.Active) =>
        new(id, "Alex Stylist", "Senior Colour Specialist", "alex.stylist@rojan.example", "+1 555 010 2231", status, "Specializes in balayage.");

    [Fact]
    public async Task CreateSpecialistAsync_ThenGetSpecialistByIdAsync_ReturnsThePersistedSpecialist()
    {
        var specialist = MakeSpecialist();

        await _sut.CreateSpecialistAsync(specialist);
        var found = await _sut.GetSpecialistByIdAsync("specialist-1");

        Assert.NotNull(found);
        Assert.Equal(specialist, found);
    }

    [Fact]
    public async Task GetSpecialistByIdAsync_NoMatchingSpecialist_ReturnsNull()
    {
        var found = await _sut.GetSpecialistByIdAsync("missing-specialist");

        Assert.Null(found);
    }

    [Fact]
    public async Task GetSpecialistsAsync_NoSpecialists_ReturnsEmptyList()
    {
        var specialists = await _sut.GetSpecialistsAsync();

        Assert.Empty(specialists);
    }

    [Fact]
    public async Task GetSpecialistsAsync_ReturnsEveryPersistedSpecialist()
    {
        await _sut.CreateSpecialistAsync(MakeSpecialist("specialist-1"));
        await _sut.CreateSpecialistAsync(MakeSpecialist("specialist-2"));

        var specialists = await _sut.GetSpecialistsAsync();

        Assert.Equal(2, specialists.Count);
        Assert.Contains(specialists, specialist => specialist.Id == "specialist-1");
        Assert.Contains(specialists, specialist => specialist.Id == "specialist-2");
    }

    [Fact]
    public async Task UpdateSpecialistAsync_PersistsEveryChangedField()
    {
        await _sut.CreateSpecialistAsync(MakeSpecialist());
        var updated = MakeSpecialist() with
        {
            FullName = "Alex Stylist-Ross",
            Title = "Principal Colourist",
            Email = "alex.ross@rojan.example",
            Phone = "+1 555 010 9999",
            Bio = "Updated bio.",
        };

        await _sut.UpdateSpecialistAsync(updated);
        var found = await _sut.GetSpecialistByIdAsync("specialist-1");

        Assert.Equal(updated, found);
    }

    [Fact]
    public async Task UpdateSpecialistAsync_SpecialistDoesNotExist_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.UpdateSpecialistAsync(MakeSpecialist("missing-specialist")));
    }

    [Theory]
    [InlineData(DomainSpecialists.SpecialistStatus.Active, DomainSpecialists.SpecialistStatus.OnLeave)]
    [InlineData(DomainSpecialists.SpecialistStatus.OnLeave, DomainSpecialists.SpecialistStatus.Active)]
    [InlineData(DomainSpecialists.SpecialistStatus.Active, DomainSpecialists.SpecialistStatus.Inactive)]
    [InlineData(DomainSpecialists.SpecialistStatus.Inactive, DomainSpecialists.SpecialistStatus.Active)]
    public async Task UpdateSpecialistAsync_StatusChange_PersistsTheNewStatus(
        DomainSpecialists.SpecialistStatus initialStatus, DomainSpecialists.SpecialistStatus newStatus)
    {
        await _sut.CreateSpecialistAsync(MakeSpecialist(status: initialStatus));

        await _sut.UpdateSpecialistAsync(MakeSpecialist(status: newStatus));
        var found = await _sut.GetSpecialistByIdAsync("specialist-1");

        Assert.Equal(newStatus, found?.Status);
    }

    [Fact]
    public async Task AddSkillAsync_ThenGetSkillsAsync_ReturnsThePersistedSkill()
    {
        await _sut.CreateSpecialistAsync(MakeSpecialist());
        var skill = new DomainSpecialists.SpecialistSkill("skill-1", "specialist-1", "Colour");

        await _sut.AddSkillAsync(skill);
        var skills = await _sut.GetSkillsAsync("specialist-1");

        Assert.Equal(skill, Assert.Single(skills));
    }

    [Fact]
    public async Task GetSkillsAsync_MultipleSkills_ReturnsEveryPersistedSkill()
    {
        await _sut.CreateSpecialistAsync(MakeSpecialist());
        await _sut.AddSkillAsync(new DomainSpecialists.SpecialistSkill("skill-1", "specialist-1", "Colour"));
        await _sut.AddSkillAsync(new DomainSpecialists.SpecialistSkill("skill-2", "specialist-1", "Balayage"));

        var skills = await _sut.GetSkillsAsync("specialist-1");

        Assert.Equal(2, skills.Count);
        Assert.Contains(skills, skill => skill.Id == "skill-1" && skill.Name == "Colour");
        Assert.Contains(skills, skill => skill.Id == "skill-2" && skill.Name == "Balayage");
    }

    [Fact]
    public async Task GetSkillsAsync_OnlyMatchingSpecialistIdSkillsAreReturned()
    {
        await _sut.CreateSpecialistAsync(MakeSpecialist("specialist-1"));
        await _sut.CreateSpecialistAsync(MakeSpecialist("specialist-2"));
        await _sut.AddSkillAsync(new DomainSpecialists.SpecialistSkill("skill-1", "specialist-1", "For specialist 1"));
        await _sut.AddSkillAsync(new DomainSpecialists.SpecialistSkill("skill-2", "specialist-2", "For specialist 2"));

        var skills = await _sut.GetSkillsAsync("specialist-1");

        Assert.Equal("skill-1", Assert.Single(skills).Id);
    }

    [Fact]
    public async Task RemoveSkillAsync_RemovesThePersistedSkill()
    {
        await _sut.CreateSpecialistAsync(MakeSpecialist());
        await _sut.AddSkillAsync(new DomainSpecialists.SpecialistSkill("skill-1", "specialist-1", "Colour"));

        await _sut.RemoveSkillAsync("specialist-1", "skill-1");
        var skills = await _sut.GetSkillsAsync("specialist-1");

        Assert.Empty(skills);
    }

    [Fact]
    public async Task RemoveSkillAsync_OnlyRemovesTheMatchingSkillNeverAffectsOtherSpecialists()
    {
        await _sut.CreateSpecialistAsync(MakeSpecialist("specialist-1"));
        await _sut.CreateSpecialistAsync(MakeSpecialist("specialist-2"));
        await _sut.AddSkillAsync(new DomainSpecialists.SpecialistSkill("skill-1", "specialist-1", "Colour"));
        await _sut.AddSkillAsync(new DomainSpecialists.SpecialistSkill("skill-2", "specialist-2", "Colour"));

        await _sut.RemoveSkillAsync("specialist-1", "skill-1");

        Assert.Empty(await _sut.GetSkillsAsync("specialist-1"));
        Assert.Single(await _sut.GetSkillsAsync("specialist-2"));
    }

    [Fact]
    public async Task RemoveSkillAsync_SkillDoesNotExist_DoesNotThrow()
    {
        await _sut.CreateSpecialistAsync(MakeSpecialist());

        var exception = await Record.ExceptionAsync(() => _sut.RemoveSkillAsync("specialist-1", "missing-skill"));

        Assert.Null(exception);
    }

    /// <summary>Minimal <see cref="IDbContextFactory{TContext}"/> for tests - hands out a fresh <see cref="RojanDbContext"/> per call against the same temp-file connection string, same shape <see cref="Rojan.Desktop.Infrastructure.DependencyInjection.ServiceCollectionExtensions.AddInfrastructure"/> registers in the running app.</summary>
    private sealed class TestDbContextFactory : IDbContextFactory<RojanDbContext>
    {
        private readonly DbContextOptions<RojanDbContext> _options;

        public TestDbContextFactory(DbContextOptions<RojanDbContext> options)
        {
            _options = options;
        }

        public RojanDbContext CreateDbContext() => new(_options);
    }
}
