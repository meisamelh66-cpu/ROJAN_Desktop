namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>
/// One entry of ROJAN_Backend's <c>GET .../timeline</c> merged feed -
/// matches that project's <c>CustomerTimelineEntryResponse</c> field-for-field
/// (see <c>api/customer/CustomerDtos.kt</c>). <see cref="Type"/> is one of
/// <c>STATUS_CHANGED | TAG_ADDED | TAG_REMOVED | NOTE | BOOKING_CREATED |
/// BOOKING_CONFIRMED | BOOKING_COMPLETED | BOOKING_CANCELLED</c> - already
/// sorted newest-first server-side, matching this app's own
/// <c>ICustomerRepository.GetActivityAsync</c> ordering convention, so no
/// re-sorting is needed on this side. Carries no id of its own (it is a
/// read-time merge, not a stored row) - the consuming repository
/// synthesizes one, since this is display-only data.
/// </summary>
public sealed record CustomerTimelineEntryResponse(string Type, string Description, DateTimeOffset OccurredAt);
