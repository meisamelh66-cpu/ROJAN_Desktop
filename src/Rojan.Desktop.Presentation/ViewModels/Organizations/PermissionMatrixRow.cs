using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Presentation.ViewModels.Organizations;

/// <summary>One row of the read-only Permissions reference grid - a <see cref="WorkspaceRole"/> and the comma-joined names of every <see cref="Permission"/> <c>IPermissionEngine</c> grants it.</summary>
public sealed record PermissionMatrixRow(WorkspaceRole Role, string GrantedPermissions);
