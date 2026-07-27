using System.Globalization;

namespace Rojan.Desktop.Presentation.Localization;

/// <summary>Default <see cref="ICalendarService"/>. Resolves every registered <see cref="IDateProvider"/> as <see cref="IEnumerable{T}"/> (same "one interface, many registrations, keyed by an id on the implementation" pattern <c>Application.Automation.WorkflowExecutionEngine</c> already uses for <c>IWorkflowStepExecutor</c>/<c>WorkflowStepType</c>) and picks the one matching <see cref="ILocalizationService.CurrentLanguage"/>'s <see cref="LanguageInfo.DateProviderId"/>, falling back to Gregorian if the current language names a provider that isn't registered - a defensive default, never a thrown exception, since losing calendar formatting is not worth crashing a screen over.</summary>
public sealed class CalendarService : ICalendarService
{
    private const string GregorianProviderId = "Gregorian";

    private readonly ILocalizationService _localizationService;
    private readonly Dictionary<string, IDateProvider> _providersById;

    public CalendarService(ILocalizationService localizationService, IEnumerable<IDateProvider> dateProviders)
    {
        _localizationService = localizationService;
        _providersById = dateProviders.ToDictionary(provider => provider.ProviderId, StringComparer.OrdinalIgnoreCase);
    }

    public CultureInfo CurrentCulture => CultureInfo.CurrentCulture;

    public DateTime Today => ActiveProvider.Today;

    public string ToDisplayString(DateTime value) => ActiveProvider.ToDisplayString(value);

    private IDateProvider ActiveProvider =>
        _providersById.TryGetValue(_localizationService.CurrentLanguage.DateProviderId, out var provider)
            ? provider
            : _providersById[GregorianProviderId];
}
