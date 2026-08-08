using Diamono.Application.Bookings;
using Diamono.Domain.Bookings;
using Xunit;

namespace Diamono.Application.Tests;

public sealed class BookingBlockApplicationServiceTests
{
    private static readonly Guid ResourceId = Guid.Parse("7f099973-c811-4afe-beca-c9a1b8fcd001");

    private static DateTimeOffset At(int hour) => new(2026, 8, 15, hour, 0, 0, TimeSpan.Zero);

    private static CreateBookingBlockRequest Request(int startsAt, int endsAt) =>
        new(ResourceId, At(startsAt), At(endsAt), BookingBlockType.Maintenance, "Entretien pelouse");

    private static Booking Booking(int startsAt, int endsAt)
        => new(
            ResourceId,
            At(startsAt),
            At(endsAt),
            "Awa Diop",
            "+221 77 000 00 00",
            CustomerCategory.Individual,
            "Football",
            rentalAmount: 50_000m,
            lightingAmount: 5_000m,
            depositAmount: 25_000m);

    private static BookingBlockApplicationService Service(
        FakeBookingBlockRepository blockRepository,
        FakeBookingRepository bookingRepository)
        // Tests de regles de blocage : garde permissif, l'autorisation est couverte
        // par UseCasePermissionTests.
        => new(blockRepository, bookingRepository, FakePermissionGuard.AllowAll(), new FakeAuditWriter());

    [Fact]
    public async Task CreateBookingBlockAsync_cree_un_blocage_valide()
    {
        var blockRepository = new FakeBookingBlockRepository();

        var block = await Service(blockRepository, new FakeBookingRepository())
            .CreateBookingBlockAsync(Request(8, 10));

        Assert.Equal(BookingBlockType.Maintenance, block.Type);
        Assert.Equal("Maintenance pelouse", block.Reason);
        Assert.True(block.IsActive);
        Assert.Equal(1, blockRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CreateBookingBlockAsync_refuse_une_fin_avant_ou_egale_au_debut()
    {
        var service = Service(new FakeBookingBlockRepository(), new FakeBookingRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateBookingBlockAsync(Request(10, 10)));
    }

    [Fact]
    public async Task CreateBookingBlockAsync_refuse_un_chevauchement_pending_approval()
    {
        await AssertBlockConflictAsync(Booking(9, 11));
    }

    [Fact]
    public async Task CreateBookingBlockAsync_refuse_un_chevauchement_awaiting_payment()
    {
        var booking = Booking(9, 11);
        booking.Approve();

        await AssertBlockConflictAsync(booking);
    }

    [Fact]
    public async Task CreateBookingBlockAsync_refuse_un_chevauchement_confirmed()
    {
        var booking = Booking(9, 11);
        booking.Approve();
        booking.ConfirmPayment();

        await AssertBlockConflictAsync(booking);
    }

    [Fact]
    public async Task CreateBookingBlockAsync_accepte_un_blocage_hors_reservation()
    {
        var blockRepository = new FakeBookingBlockRepository();
        var service = Service(blockRepository, new FakeBookingRepository(Booking(12, 14)));

        var block = await service.CreateBookingBlockAsync(Request(8, 10));

        Assert.True(block.IsActive);
        Assert.Equal(1, blockRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CancelBookingBlockAsync_retire_le_blocage_des_blocages_actifs()
    {
        var block = new BookingBlock(ResourceId, At(8), At(10), BookingBlockType.Cleaning, "Nettoyage");
        var blockRepository = new FakeBookingBlockRepository(block);
        var service = Service(blockRepository, new FakeBookingRepository());

        await service.CancelBookingBlockAsync(block.Id);
        var activeBlocks = await service.GetBookingBlocksAsync();

        Assert.False(block.IsActive);
        Assert.NotNull(block.CancelledAt);
        Assert.Empty(activeBlocks);
        Assert.Equal(1, blockRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CancelBookingBlockAsync_refuse_un_blocage_deja_annule()
    {
        var block = new BookingBlock(ResourceId, At(8), At(10), BookingBlockType.Cleaning, "Nettoyage");
        block.Cancel();
        var service = Service(new FakeBookingBlockRepository(block), new FakeBookingRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CancelBookingBlockAsync(block.Id));
    }

    private static async Task AssertBlockConflictAsync(Booking booking)
    {
        var service = Service(new FakeBookingBlockRepository(), new FakeBookingRepository(booking));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateBookingBlockAsync(Request(8, 10)));
    }
}
