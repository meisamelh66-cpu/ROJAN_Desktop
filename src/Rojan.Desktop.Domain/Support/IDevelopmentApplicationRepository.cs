namespace Rojan.Desktop.Domain.Support;

public interface IDevelopmentApplicationRepository
{
    public Task<IReadOnlyList<DevelopmentApplication>> GetAllAsync(CancellationToken cancellationToken = default);

    public Task SaveAsync(DevelopmentApplication application, CancellationToken cancellationToken = default);
}
