namespace Rojan.Desktop.Application.Notifications;

/// <summary>Phase 27: Notification Search. Enterprise keyword search over an already-resolved candidate set - see <see cref="NotificationSearchCandidate"/>'s own doc comment for why candidates carry plain text rather than localization keys.</summary>
public interface INotificationSearchService
{
    /// <summary>Ranked matches for <paramref name="query"/> among <paramref name="candidates"/>, highest score first. An empty or whitespace-only query returns every candidate unranked (score 0, no highlights) - unlike Help Search, an empty Notification Search query means "show the full, unfiltered list", the natural default for a list the user is Browse-ing rather than actively searching.</summary>
    public IReadOnlyList<NotificationSearchResultDto> Search(IReadOnlyList<NotificationSearchCandidate> candidates, string query);
}
