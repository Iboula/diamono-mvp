using Diamono.Application.Availability;
using Diamono.Domain.Facilities;

namespace Diamono.Application.Abstractions;

/// <summary>
/// Abstraction de lecture dédiée au calendrier de disponibilités.
/// Volontairement séparée de <see cref="IBookingRepository"/> (écriture) afin de
/// garder un contrat de lecture qui charge une journée entière en un nombre fixe
/// de requêtes, sans appel DB dans une boucle.
/// </summary>
public interface IAvailabilityRepository
{
    Task<Resource?> GetResourceAsync(Guid resourceId, CancellationToken cancellationToken);

    /// <summary>
    /// Toutes les périodes bloquantes (réservations actives + blocages) qui chevauchent
    /// la fenêtre [from, to[, en une seule lecture.
    /// </summary>
    Task<IReadOnlyList<OccupiedPeriod>> GetOccupiedPeriodsAsync(
        Guid resourceId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);
}
