namespace Diamono.Application.Availability;

/// <summary>
/// Créneau affiché sur le calendrier public. Une valeur <c>IsAvailable = true</c>
/// est une indication de lecture : le contrôle d'overlap définitif reste effectué
/// par <c>BookingApplicationService.CreateAsync</c> au moment de la création.
/// </summary>
public sealed record AvailabilitySlot(
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    bool IsAvailable,
    string? UnavailableReason);
