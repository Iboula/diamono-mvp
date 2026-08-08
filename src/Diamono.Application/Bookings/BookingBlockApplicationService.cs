using Diamono.Application.Abstractions;
using Diamono.Domain.Bookings;

namespace Diamono.Application.Bookings;

public sealed class BookingBlockApplicationService(
    IBookingBlockRepository blockRepository,
    IBookingRepository bookingRepository)
{
    public async Task<BookingBlock> CreateBookingBlockAsync(
        CreateBookingBlockRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.EndsAt <= request.StartsAt)
            throw new ArgumentException("La date de fin doit etre apres la date de debut.", nameof(request));

        if (await bookingRepository.HasConflictAsync(
            request.ResourceId, request.StartsAt, request.EndsAt, cancellationToken))
        {
            throw new InvalidOperationException(
                "Impossible de bloquer : une reservation existe sur cette periode.");
        }

        var block = new BookingBlock(
            request.ResourceId,
            request.StartsAt,
            request.EndsAt,
            request.Type,
            request.Description);

        await blockRepository.AddAsync(block, cancellationToken);
        await blockRepository.SaveChangesAsync(cancellationToken);
        return block;
    }

    public async Task CancelBookingBlockAsync(Guid blockId, CancellationToken cancellationToken = default)
    {
        var block = await blockRepository.GetByIdAsync(blockId, cancellationToken)
            ?? throw new KeyNotFoundException("Blocage introuvable.");

        block.Cancel();
        await blockRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BookingBlock>> GetBookingBlocksAsync(CancellationToken cancellationToken = default)
        => await blockRepository.GetActiveAsync(cancellationToken);
}
