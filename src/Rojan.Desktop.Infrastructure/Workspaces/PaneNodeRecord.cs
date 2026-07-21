using System.Text.Json.Serialization;
using DomainWorkspaces = Rojan.Desktop.Domain.Workspaces;

namespace Rojan.Desktop.Infrastructure.Workspaces;

/// <summary>
/// JSON-serializable mirror of <c>Domain.Workspaces.PaneNode</c>, private to
/// this Infrastructure vertical slice. Domain stays free of any
/// <see cref="System.Text.Json"/> attribute/converter concern (this
/// codebase's "Domain is just data + repository contract" discipline) -
/// <see cref="System.Text.Json.Serialization.JsonPolymorphicAttribute"/>/
/// <see cref="System.Text.Json.Serialization.JsonDerivedTypeAttribute"/>
/// live on this wire-only copy instead, and <see cref="LocalWorkspaceStore"/>
/// maps between the two at the persistence boundary - the same "mirror type
/// at each layer boundary" pattern <c>Application.Workspaces.PaneNodeDto</c>
/// already establishes one layer up.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(PaneLeafRecord), "leaf")]
[JsonDerivedType(typeof(PaneSplitRecord), "split")]
internal abstract record PaneNodeRecord(string Id);

internal sealed record PaneLeafRecord(string Id, IReadOnlyList<string> ModuleIds, string? ActiveModuleId) : PaneNodeRecord(Id);

internal sealed record PaneSplitRecord(string Id, DomainWorkspaces.PaneOrientation Orientation, double Ratio, PaneNodeRecord First, PaneNodeRecord Second) : PaneNodeRecord(Id);
