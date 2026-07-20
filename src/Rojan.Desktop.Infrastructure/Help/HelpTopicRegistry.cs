using Rojan.Desktop.Domain.Help;

namespace Rojan.Desktop.Infrastructure.Help;

/// <summary>
/// Phase 26.8: Help Registry. Real, authored help topics - not fake/demo
/// data (unlike the <c>Fake*Repository</c> classes elsewhere in this
/// project, which explicitly are placeholder business data) - for the six
/// flagship modules (Dashboard, Customers, Bookings, Inventory,
/// Accounting, Services), plus one generic <see cref="Application.Help.HelpQueryService.DefaultTopicId"/>
/// topic every other module resolves to today. Every field a user reads
/// lives in <c>Strings.resx</c>/<c>Strings.en.resx</c>/<c>Strings.ar.resx</c>
/// under each topic's <see cref="HelpTopic.KeyPrefix"/> - this class only
/// holds structure (which module/page a topic belongs to, its shortcuts,
/// its related topics), never literal text, per Phase 26.10's "no
/// hardcoded text" (see <see cref="HelpTopic.KeyPrefix"/>'s own doc
/// comment for the key-expansion convention).
///
/// The remaining eight modules (Organizations, Calendar, Specialists, HR,
/// Reports, Analytics, AI Center, Settings) fall back to the default
/// topic - a documented scope boundary, the same "flagship subset now,
/// the rest later" shape Phase 22A/23/24 already established repeatedly
/// in this codebase, rather than authoring eight more full topics' worth
/// of content (~90 more localized fields) speculatively.
/// </summary>
public sealed class HelpTopicRegistry : IHelpRepository
{
    private static readonly IReadOnlyList<HelpTopic> Topics =
    [
        new HelpTopic(
            Id: "help-dashboard",
            ModuleId: "dashboard",
            PageId: null,
            KeyPrefix: "Help_Dashboard",
            Shortcuts: [new HelpShortcut("Esc", "Help_Shortcut_CloseDialog")],
            RelatedTopicIds: ["help-customers", "help-bookings"],
            Version: "1.0.0"),

        new HelpTopic(
            Id: "help-customers",
            ModuleId: "customers",
            PageId: null,
            KeyPrefix: "Help_Customers",
            Shortcuts: [new HelpShortcut("Esc", "Help_Shortcut_CloseDialog")],
            RelatedTopicIds: ["help-bookings", "help-dashboard"],
            Version: "1.0.0"),

        new HelpTopic(
            Id: "help-bookings",
            ModuleId: "bookings",
            PageId: null,
            KeyPrefix: "Help_Bookings",
            Shortcuts: [new HelpShortcut("Esc", "Help_Shortcut_CloseDialog")],
            RelatedTopicIds: ["help-customers", "help-services"],
            Version: "1.0.0"),

        new HelpTopic(
            Id: "help-inventory",
            ModuleId: "inventory",
            PageId: null,
            KeyPrefix: "Help_Inventory",
            Shortcuts: [new HelpShortcut("Esc", "Help_Shortcut_CloseDialog")],
            RelatedTopicIds: ["help-accounting", "help-services"],
            Version: "1.0.0"),

        new HelpTopic(
            Id: "help-accounting",
            ModuleId: "accounting",
            PageId: null,
            KeyPrefix: "Help_Accounting",
            Shortcuts: [new HelpShortcut("Esc", "Help_Shortcut_CloseDialog")],
            RelatedTopicIds: ["help-inventory", "help-bookings"],
            Version: "1.0.0"),

        new HelpTopic(
            Id: "help-services",
            ModuleId: "services",
            PageId: null,
            KeyPrefix: "Help_Services",
            Shortcuts: [new HelpShortcut("Esc", "Help_Shortcut_CloseDialog")],
            RelatedTopicIds: ["help-bookings", "help-inventory"],
            Version: "1.0.0"),

        new HelpTopic(
            Id: "help-default",
            ModuleId: "default",
            PageId: null,
            KeyPrefix: "Help_Default",
            Shortcuts: [new HelpShortcut("Esc", "Help_Shortcut_CloseDialog")],
            RelatedTopicIds: [],
            Version: "1.0.0"),
    ];

    public Task<IReadOnlyList<HelpTopic>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Topics);

    public Task<HelpTopic?> GetByIdAsync(string topicId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Topics.FirstOrDefault(topic => topic.Id == topicId));
}
