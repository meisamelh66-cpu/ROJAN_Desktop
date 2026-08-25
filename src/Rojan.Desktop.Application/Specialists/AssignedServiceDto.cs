namespace Rojan.Desktop.Application.Specialists;

/// <summary>
/// Specialist-Service Assignment: one service a specialist is eligible to
/// perform, from the specialist's own point of view - the counterpart to
/// <c>Services.AssignedSpecialistDto</c>, but keyed on the real
/// <c>(SpecialistId, ServiceId)</c> pair ROJAN_Backend itself uses, with no
/// synthetic assignment id. <see cref="ServiceName"/> is resolved by
/// <see cref="SpecialistProfileQueryService"/> against the service
/// catalog - ROJAN_Backend's own <c>GET /specialists/{id}/services</c>
/// returns ids only.
/// </summary>
public sealed record AssignedServiceDto(string ServiceId, string ServiceName);
