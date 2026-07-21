using Rojan.Desktop.Application.Support;

namespace Rojan.Desktop.Application.Tests.Support;

public sealed class DevelopmentApplicationServiceTests
{
    [Fact]
    public async Task SubmitAsync_ValidApplication_PersistsAndReturnsIt()
    {
        var repository = new FakeDevelopmentApplicationRepository();
        var service = new DevelopmentApplicationService(repository);

        var application = await service.SubmitAsync(
            "Sara", "Ahmadi", "0912-000-0000", "sara@example.com", "Tehran", "Backend",
            "https://github.com/sara", "https://linkedin.com/in/sara", "https://sara.dev", "https://sara.dev/resume.pdf", "I would like to help.");

        Assert.Equal("Sara", application.FirstName);
        Assert.Single(await repository.GetAllAsync());
    }

    [Fact]
    public async Task SubmitAsync_MissingCollaborationArea_Throws()
    {
        var service = new DevelopmentApplicationService(new FakeDevelopmentApplicationRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitAsync(
            "Sara", "Ahmadi", "0912-000-0000", "sara@example.com", "Tehran", "",
            "", "", "", "", ""));
    }

    [Fact]
    public async Task SubmitAsync_OptionalLinksOmitted_StillSucceeds()
    {
        var repository = new FakeDevelopmentApplicationRepository();
        var service = new DevelopmentApplicationService(repository);

        var application = await service.SubmitAsync(
            "Sara", "Ahmadi", "0912-000-0000", "", "Tehran", "Design",
            "", "", "", "", "");

        Assert.Equal("Design", application.CollaborationArea);
    }
}
