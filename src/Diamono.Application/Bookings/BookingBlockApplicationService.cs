using Diamono.Application.Abstractions;
using Diamono.Application.Audit;
using Diamono.Domain.Audit;
using Diamono.Domain.Bookings;
using Diamono.Domain.Security;

namespace Diamono.Application.Bookings;

public sealed class BookingBlockApplicationService(
    IBookingBlockRepository blockRepository,
    IBookingRepository bookingRepository,
    IPermissionGuard permissionGuard,
    IAuditWriter auditWriter)
{
    public async Task<BookingBlock> CreateBookingBlockAsync(
        CreateBookingBlockRequest request,
        CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.BookingBlocksManage, cancellationToken);

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
        await auditWriter.WriteAsync(new AuditWriteRequest(
            AuditActions.BookingBlockCreated,
            "BookingBlock",
            block.Id.ToString(),
            "Blocage operationnel cree.",
            NewValues: new
            {
                block.ResourceId,
                block.StartsAt,
                block.EndsAt,
                type = block.Type.ToString(),
                block.Reason,
                block.Description
            }),
            cancellationToken);
        await blockRepository.SaveChangesAsync(cancellationToken);
        return block;
    }

    public async Task CancelBookingBlockAsync(Guid blockId, CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.BookingBlocksManage, cancellationToken);

        var block = await blockRepository.GetByIdAsync(blockId, cancellationToken)
            ?? throw new KeyNotFoundException("Blocage introuvable.");

        var oldValues = new { isActive = block.IsActive, block.CancelledAt };
        block.Cancel();
        await auditWriter.WriteAsync(new AuditWriteRequest(
            AuditActions.BookingBlockCancelled,
            "BookingBlock",
            block.Id.ToString(),
            "Blocage operationnel annule.",
            OldValues: oldValues,
            NewValues: new { isActive = block.IsActive, block.CancelledAt }),
            cancellationToken);
        await blockRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BookingBlock>> GetBookingBlocksAsync(CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.BookingBlocksView, cancellationToken);
        return await blockRepository.GetActiveAsync(cancellationToken);
    }
}
