using Diamono.Application.Abstractions;
using Diamono.Domain.Payments;

namespace Diamono.Application.Tests;

internal sealed class FakePaymentRepository(params Payment[] payments) : IPaymentRepository
{
    private readonly List<Payment> payments = [.. payments];

    public int SaveChangesCallCount { get; private set; }

    public Task AddAsync(Payment payment, CancellationToken cancellationToken)
    {
        payments.Add(payment);
        return Task.CompletedTask;
    }

    public Task<Payment?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken)
        => Task.FromResult(payments.FirstOrDefault(x => x.Id == paymentId));

    public Task<IReadOnlyList<Payment>> GetByBookingAsync(Guid bookingId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Payment>>([.. payments.Where(x => x.BookingId == bookingId).OrderByDescending(x => x.CreatedAt)]);

    public Task<IReadOnlyList<Payment>> GetBackOfficeAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Payment>>([.. payments.OrderByDescending(x => x.CreatedAt)]);

    public Task<bool> HasPaidPaymentForBookingAsync(Guid bookingId, CancellationToken cancellationToken)
        => Task.FromResult(payments.Any(x => x.BookingId == bookingId && x.Status == PaymentStatus.Paid));

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }
}
