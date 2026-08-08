namespace Diamono.Domain.Bookings;

/// <summary>
/// Règles temporelles partagées du MVP (BR-002, BR-008, BR-011).
/// Centralisées ici pour que la réservation et la lecture de disponibilité
/// appliquent exactement la même définition.
/// </summary>
public static class BookingRules
{
    /// <summary>BR-002 — durée minimale d'une réservation.</summary>
    public const int MinimumHours = 2;

    /// <summary>BR-002 — durée maximale d'une réservation.</summary>
    public const int MaximumHours = 6;

    public static readonly TimeSpan MinimumDuration = TimeSpan.FromHours(MinimumHours);
    public static readonly TimeSpan MaximumDuration = TimeSpan.FromHours(MaximumHours);

    /// <summary>BR-008 — statuts qui bloquent un créneau.</summary>
    public static readonly BookingStatus[] BlockingStatuses =
    [
        BookingStatus.PendingApproval,
        BookingStatus.AwaitingPayment,
        BookingStatus.Confirmed
    ];

    /// <summary>
    /// Deux périodes se chevauchent lorsque existing.Start &lt; requested.End
    /// et existing.End &gt; requested.Start.
    /// </summary>
    public static bool Overlaps(
        DateTimeOffset existingStart, DateTimeOffset existingEnd,
        DateTimeOffset requestedStart, DateTimeOffset requestedEnd)
        => existingStart < requestedEnd && existingEnd > requestedStart;
}
