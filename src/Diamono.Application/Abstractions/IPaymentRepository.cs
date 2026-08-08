using Diamono.Domain.Payments;

namespace Diamono.Application.Abstractions;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment, CancellationToken cancellationToken);
    Task<Payment?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Payment>> GetByBookingAsync(Guid bookingId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Payment>> GetBackOfficeAsync(CancellationToken cancellationToken);
    Task<bool> HasPaidPaymentForBookingAsync(Guid bookingId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
