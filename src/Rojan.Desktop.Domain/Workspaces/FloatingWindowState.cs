namespace Rojan.Desktop.Domain.Workspaces;

/// <summary>A module detached into its own OS window, as returned by <see cref="IWorkspaceRepository"/>. <see cref="ModuleId"/> is a free-form, unvalidated reference - same "Domain stays a dumb data shape" reasoning as <c>Bookings.Booking.SpecialistId</c>.</summary>
public sealed record FloatingWindowState(string Id, string ModuleId, double X, double Y, double Width, double Height, bool IsMaximized);
