namespace Rojan.Desktop.Application.Organizations;

/// <summary>
/// Phase 22A: Enterprise Context Migration. The Application layer's own
/// read-only view of "what is the current session scoped to" - Id-only
/// (never a full <see cref="OrganizationDto"/>/<see cref="BranchDto"/>),
/// so this stays a tiny, dependency-free abstraction every module's
/// query/command service can depend on without pulling in the richer
/// session-management surface (<c>Presentation.Organizations.ICurrentSessionService</c>
/// - which the Application layer must never reference, since Application
/// cannot depend on Presentation). The concrete implementation is the
/// same Shell-owned <c>CurrentSessionService</c> that already backs
/// <c>ICurrentSessionService</c> - one object, two interfaces, same
/// "interface split by consuming layer" reasoning already used
/// throughout this codebase.
/// </summary>
public interface IEnterpriseContext
{
    /// <summary>Null only in the moment before the very first <c>InitializeAsync</c> completes at startup - every module's filtering treats null as "match nothing" rather than "match everything," never a data-leakage fallback.</summary>
    public string? CurrentOrganizationId { get; }

    public string? CurrentBranchId { get; }

    public WorkspaceRole CurrentRole { get; }
}
