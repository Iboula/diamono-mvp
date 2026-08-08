using Diamono.Application.Abstractions;
using Diamono.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace Diamono.Infrastructure.Persistence;

public sealed class PaymentRepository(DiamonoDbContext db) : IPaymentRepository
{
    public Task AddAsync(Payment payment, CancellationToken cancellationToken)
        => db.Payments.AddAsync(payment, cancellationToken).AsTask();

    public Task<Payment?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken)
        => db.Payments.FirstOrDefaultAsync(x => x.Id == paymentId, cancellationToken);

    public async Task<IReadOnlyList<Payment>> GetByBookingAsync(Guid bookingId, CancellationToken cancellationToken)
        => await db.Payments.AsNoTracking()
            .Where(x => x.BookingId == bookingId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Payment>> GetBackOfficeAsync(CancellationToken cancellationToken)
        => await db.Payments.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<bool> HasPaidPaymentForBookingAsync(Guid bookingId, CancellationToken cancellationToken)
        => db.Payments.AnyAsync(x => x.BookingId == bookingId && x.Status == PaymentStatus.Paid, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => db.SaveChangesAsync(cancellationToken);
}
