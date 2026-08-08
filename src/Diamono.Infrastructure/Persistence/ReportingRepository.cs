using Diamono.Application.Reporting;
using Diamono.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace Diamono.Infrastructure.Persistence;

public sealed class ReportingRepository(DiamonoDbContext db) : IReportingRepository
{
    public async Task<ReportingData> GetReportingDataAsync(
        ReportingPeriod period,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var queryStart = Min(period.StartsOn, today);
        var queryEnd = Max(period.EndsOn, today);
        var startsAt = new DateTimeOffset(queryStart.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var endsAt = new DateTimeOffset(queryEnd.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var bookings = await db.Bookings
            .AsNoTracking()
            .Where(x => x.StartsAt < endsAt && startsAt < x.EndsAt)
            .Select(x => new ReportingBooking(
                x.Id,
                x.StartsAt,
                x.EndsAt,
                x.CustomerCategory,
                x.ActivityType,
                x.RentalAmount + x.LightingAmount + x.DepositAmount,
                x.Status,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        var blocks = await db.BookingBlocks
            .AsNoTracking()
            .Where(x => x.CancelledAt == null && x.StartsAt < endsAt && startsAt < x.EndsAt)
            .Select(x => new ReportingBlock(x.Id, x.StartsAt, x.EndsAt))
            .ToListAsync(cancellationToken);

        var payments = await db.Payments
            .AsNoTracking()
            .Where(x => x.Status == PaymentStatus.Paid && x.PaidAt >= startsAt && x.PaidAt < endsAt)
            .Select(x => new ReportingPayment(x.Id, x.BookingId, x.Amount, x.PaidAt))
            .ToListAsync(cancellationToken);

        var settings = await db.StadiumBookingSettings
            .AsNoTracking()
            .FirstAsync(cancellationToken);

        return new ReportingData(bookings, payments, blocks, settings);
    }

    private static DateOnly Min(DateOnly a, DateOnly b) => a <= b ? a : b;

    private static DateOnly Max(DateOnly a, DateOnly b) => a >= b ? a : b;
}
