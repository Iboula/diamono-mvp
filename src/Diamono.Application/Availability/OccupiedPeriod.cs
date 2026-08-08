namespace Diamono.Application.Availability;

public enum OccupancyKind
{
    /// <summary>Réservation en PendingApproval, AwaitingPayment ou Confirmed (BR-008).</summary>
    Booking = 1,

    /// <summary>Blocage maintenance / événement (BR-011).</summary>
    Block = 2
}

/// <summary>Période déjà occupée sur une ressource, projetée depuis la persistance.</summary>
public sealed record OccupiedPeriod(
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    OccupancyKind Kind,
    string Reason);
