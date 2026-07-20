namespace Rojan.Desktop.Domain.Help;

/// <summary>Phase 26: Smart Context Help. Pure content-resolution and version-compatibility logic - no I/O, no localization awareness, matches the "value/data in, decision out" shape every other <c>*Rules</c> class in this codebase already uses (e.g. <see cref="Security.SessionRules"/>).</summary>
public static class HelpContentRules
{
    /// <summary>
    /// Picks the best-matching topic for the given context: an exact
    /// (<paramref name="moduleId"/>, <paramref name="pageId"/>) match
    /// first, then a module-level topic (one whose own <see cref="HelpTopic.PageId"/>
    /// is <see langword="null"/>) for the same module, otherwise
    /// <see langword="null"/> - callers decide what "no topic found"
    /// falls back to (a generic default topic id is an Application-level
    /// policy, not a Domain rule).
    /// </summary>
    public static HelpTopic? ResolveContext(IReadOnlyList<HelpTopic> topics, string moduleId, string? pageId)
    {
        if (pageId is not null)
        {
            var exactMatch = topics.FirstOrDefault(topic => topic.ModuleId == moduleId && topic.PageId == pageId);
            if (exactMatch is not null)
            {
                return exactMatch;
            }
        }

        return topics.FirstOrDefault(topic => topic.ModuleId == moduleId && topic.PageId is null);
    }

    /// <summary>
    /// A topic is compatible with the running app when its recorded major
    /// version is less than or equal to the app's major version - content
    /// written for an earlier major version is assumed still broadly
    /// accurate (Phase 26.1's "version compatibility"); a topic authored
    /// for a *later* major version than the app currently running is not
    /// (it may describe features/flows this build does not have yet).
    /// Unparseable versions on either side are treated as compatible
    /// (fail open - a formatting slip in a version string should not hide
    /// otherwise-valid help content from the user).
    /// </summary>
    public static bool IsVersionCompatible(string topicVersion, string appVersion)
    {
        var topicMajor = ParseMajorVersion(topicVersion);
        var appMajor = ParseMajorVersion(appVersion);
        if (topicMajor is null || appMajor is null)
        {
            return true;
        }

        return topicMajor <= appMajor;
    }

    private static int? ParseMajorVersion(string version)
    {
        var firstSegment = version.Split('.', '-')[0];
        return int.TryParse(firstSegment, out var major) ? major : null;
    }
}
