namespace Rojan.Desktop.Domain.Security;

/// <summary>
/// Sprint 7 Commit 4: resolution lifecycle of a recorded
/// <see cref="SyncConflict"/>. Detection/recording is all that exists so
/// far (see <see cref="SyncConflict"/>'s own doc comment) - no service
/// method transitions a conflict away from <see cref="Unresolved"/> yet,
/// since an actual resolution workflow/UI is explicitly a later phase's
/// scope, not this one's. The field exists now, real rather than a
/// placeholder, so a future resolution workflow only has to set it via
/// <c>SyncConflict.with</c>, not add it.
/// </summary>
public enum SyncConflictResolutionStatus
{
    Unresolved,
    Resolved,
}
