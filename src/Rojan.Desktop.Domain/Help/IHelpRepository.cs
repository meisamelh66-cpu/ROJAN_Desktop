namespace Rojan.Desktop.Domain.Help;

/// <summary>Phase 26: Smart Context Help. Domain-defined repository abstraction - implemented by <c>Infrastructure.Help.HelpTopicRegistry</c> (Phase 26.8's "Help Registry"), same repository-pattern shape every other module already uses (e.g. <c>Dashboard.IDashboardRepository</c>).</summary>
public interface IHelpRepository
{
    public Task<IReadOnlyList<HelpTopic>> GetAllAsync(CancellationToken cancellationToken = default);

    public Task<HelpTopic?> GetByIdAsync(string topicId, CancellationToken cancellationToken = default);
}
