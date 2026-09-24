namespace Rojan.Desktop.Domain.Media;

/// <summary>
/// Phase F (Salon Media Integration): one of a salon's logo/cover/gallery
/// images, as returned by <see cref="ISalonMediaRepository"/>. Read-only,
/// on purpose - upload/delete/reorder have no Desktop UI to drive them yet
/// (ROJAN_Backend's own <c>MediaController</c> supports all three, but
/// nothing in this app needs them today; see <see cref="ISalonMediaRepository"/>'s
/// own doc comment). Deliberately a small projection of ROJAN_Backend's
/// full <c>MediaAssetResponse</c> - <see cref="Id"/>/<see cref="Url"/>/
/// <see cref="OriginalName"/>/<see cref="DisplayOrder"/> are the only
/// fields a read-only branding display (an <c>&lt;img&gt;</c> source, its
/// alt text, and gallery order) actually needs; <c>MimeType</c>/
/// <c>FileSize</c>/<c>Status</c>/<c>CreatedAt</c>/<c>TargetId</c> exist on
/// the wire response but have no read-only consumer here, so they are not
/// carried into the Domain layer.
/// </summary>
public sealed record SalonMediaAsset(string Id, string Url, string OriginalName, int DisplayOrder);

/// <summary>
/// The subset of ROJAN_Backend's <c>MediaType</c> a salon's own profile/
/// branding area cares about - <c>PORTFOLIO</c>/<c>SERVICE_IMAGE</c> are
/// scoped to a specialist/service (need a <c>targetId</c>, not a salon-flat
/// read), <c>DOCUMENT</c> is unrelated to media/branding, and
/// <c>AVATAR</c>/<c>PROFILE_COVER</c> are user-owned, not salon-owned - see
/// ROJAN_Backend's own <c>MediaType</c> doc comment (<c>domain/media/MediaAsset.kt</c>).
/// </summary>
public enum SalonMediaCategory
{
    Logo,
    Cover,
    Gallery,
}
