using Rojan.Desktop.Domain.Support;

namespace Rojan.Desktop.Application.Tests.Support;

internal sealed class FakeSupportMessageRepository : ISupportMessageRepository
{
    private readonly List<SupportMessage> _messages = [];

    public Task<IReadOnlyList<SupportMessage>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SupportMessage>>(_messages.ToList());

    public Task SaveAsync(SupportMessage message, CancellationToken cancellationToken = default)
    {
        _messages.Add(message);
        return Task.CompletedTask;
    }
}

internal sealed class FakeDevelopmentApplicationRepository : IDevelopmentApplicationRepository
{
    private readonly List<DevelopmentApplication> _applications = [];

    public Task<IReadOnlyList<DevelopmentApplication>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DevelopmentApplication>>(_applications.ToList());

    public Task SaveAsync(DevelopmentApplication application, CancellationToken cancellationToken = default)
    {
        _applications.Add(application);
        return Task.CompletedTask;
    }
}
