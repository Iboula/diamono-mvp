using Diamono.Domain.Bookings;
using Xunit;

namespace Diamono.Domain.Tests;

public sealed class BookingRulesTests
{
    private static DateTimeOffset At(int hour) => new(2026, 8, 15, hour, 0, 0, TimeSpan.Zero);

    [Theory]
    // Périodes disjointes.
    [InlineData(18, 20, 16, 18, false)] // fin existante == début demandé
    [InlineData(18, 20, 20, 22, false)] // fin demandée == début existant
    [InlineData(18, 20, 8, 10, false)]
    // Chevauchements.
    [InlineData(18, 20, 18, 20, true)]  // identiques
    [InlineData(17, 19, 18, 20, true)]  // partiel par la gauche
    [InlineData(19, 21, 18, 20, true)]  // partiel par la droite
    [InlineData(17, 23, 18, 20, true)]  // demandé inclus dans existant
    [InlineData(18, 19, 17, 22, true)]  // existant inclus dans demandé
    public void Overlaps_applique_la_regle_start_inferieur_end(
        int existingStart, int existingEnd, int requestedStart, int requestedEnd, bool expected)
    {
        var overlaps = BookingRules.Overlaps(
            At(existingStart), At(existingEnd), At(requestedStart), At(requestedEnd));

        Assert.Equal(expected, overlaps);
    }

    [Fact]
    public void Les_statuts_bloquants_sont_ceux_de_BR_008()
    {
        Assert.Equal(
            [BookingStatus.PendingApproval, BookingStatus.AwaitingPayment, BookingStatus.Confirmed],
            BookingRules.BlockingStatuses);
    }
}
