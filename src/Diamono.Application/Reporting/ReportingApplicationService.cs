using Diamono.Application.Abstractions;
using Diamono.Domain.Bookings;
using Diamono.Domain.Security;

namespace Diamono.Application.Reporting;

public sealed class ReportingApplicationService(
    IReportingRepository repository,
    IPermissionGuard permissionGuard)
{
    private static readonly BookingStatus[] BlockingStatuses =
    [
        BookingStatus.PendingApproval,
        BookingStatus.AwaitingPayment,
        BookingStatus.Confirmed
    ];

    public async Task<ReportingDashboard> GetDashboardAsync(
        ReportingPeriod period,
        CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.BookingsView, cancellationToken);

        if (period.EndsOn < period.StartsOn)
            throw new ArgumentException("La date de fin doit etre apres la date de debut.", nameof(period));

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var data = await repository.GetReportingDataAsync(period, today, cancellationToken);
        var todayPeriod = new ReportingPeriod(today, today);

        var bookingsInPeriod = data.Bookings
            .Where(x => IntersectsDateRange(x.StartsAt, x.EndsAt, period))
            .ToList();
        var blocksInPeriod = data.Blocks
            .Where(x => IntersectsDateRange(x.StartsAt, x.EndsAt, period))
            .ToList();
        var todayBookings = data.Bookings
            .Where(x => IntersectsDateRange(x.StartsAt, x.EndsAt, todayPeriod))
            .ToList();
        var todayBlocks = data.Blocks
            .Where(x => IntersectsDateRange(x.StartsAt, x.EndsAt, todayPeriod))
            .ToList();

        var occupationIntervals = BuildOccupationIntervals(
            bookingsInPeriod.Where(x => BlockingStatuses.Contains(x.Status)).Select(x => (x.StartsAt, x.EndsAt)),
            blocksInPeriod.Select(x => (x.StartsAt, x.EndsAt)),
            period,
            data.Settings.OpensAt,
            data.Settings.ClosesAt);

        var todayIntervals = BuildOccupationIntervals(
            todayBookings.Where(x => BlockingStatuses.Contains(x.Status)).Select(x => (x.StartsAt, x.EndsAt)),
            todayBlocks.Select(x => (x.StartsAt, x.EndsAt)),
            todayPeriod,
            data.Settings.OpensAt,
            data.Settings.ClosesAt);

        var theoreticalHours = TheoreticalOpenHours(period, data.Settings.OpensAt, data.Settings.ClosesAt);
        var occupiedHours = occupationIntervals.Sum(DurationHours);

        return new ReportingDashboard(
            period,
            new TodayKpis(
                BookingsToday: todayBookings.Count,
                OccupiedSlots: todayIntervals.Count,
                AvailableSlots: AvailableSlotCount(todayIntervals, data.Settings.OpensAt, data.Settings.ClosesAt),
                PendingApproval: data.Bookings.Count(x => x.Status == BookingStatus.PendingApproval),
                AwaitingPayment: data.Bookings.Count(x => x.Status == BookingStatus.AwaitingPayment)),
            new PeriodKpis(
                TotalBookings: bookingsInPeriod.Count,
                ConfirmedBookings: bookingsInPeriod.Count(x => x.Status == BookingStatus.Confirmed),
                CancelledBookings: bookingsInPeriod.Count(x => x.Status == BookingStatus.Cancelled),
                NoShowBookings: bookingsInPeriod.Count(x => x.Status == BookingStatus.NoShow),
                CollectedRevenue: CollectedRevenue(data.Payments, period),
                PotentialRevenue: bookingsInPeriod.Where(x => x.Status is BookingStatus.PendingApproval or BookingStatus.AwaitingPayment or BookingStatus.Confirmed).Sum(x => x.TotalAmount),
                OccupationRate: theoreticalHours == 0 ? 0 : Math.Round(occupiedHours / theoreticalHours * 100m, 1)),
            BuildBookingsByDay(bookingsInPeriod, period),
            BuildRevenueByDay(data.Payments, period),
            BuildCategoryBreakdown(bookingsInPeriod),
            BuildActivityBreakdown(bookingsInPeriod),
            BuildOccupationByHour(occupationIntervals, data.Settings.OpensAt, data.Settings.ClosesAt),
            BuildInsights(data.Bookings, occupationIntervals, period));
    }

    private static IReadOnlyList<DailyCountPoint> BuildBookingsByDay(IEnumerable<ReportingBooking> bookings, ReportingPeriod period)
        => Dates(period)
            .Select(date => new DailyCountPoint(date, bookings.Count(x => DateOnly.FromDateTime(x.StartsAt.Date) == date)))
            .ToList();

    private static IReadOnlyList<DailyAmountPoint> BuildRevenueByDay(IEnumerable<ReportingPayment> payments, ReportingPeriod period)
        => Dates(period)
            .Select(date => new DailyAmountPoint(date, payments
                .Where(x => x.PaidAt is not null && DateOnly.FromDateTime(x.PaidAt.Value.Date) == date)
                .Sum(x => x.Amount)))
            .ToList();

    private static decimal CollectedRevenue(IEnumerable<ReportingPayment> payments, ReportingPeriod period)
        => payments
            .Where(x => x.PaidAt is not null && DateInPeriod(DateOnly.FromDateTime(x.PaidAt.Value.Date), period))
            .Sum(x => x.Amount);

    private static bool DateInPeriod(DateOnly date, ReportingPeriod period)
        => date >= period.StartsOn && date <= period.EndsOn;

    private static IReadOnlyList<BreakdownPoint> BuildCategoryBreakdown(IEnumerable<ReportingBooking> bookings)
        => bookings
            .GroupBy(x => CategoryLabel(x.CustomerCategory))
            .Select(g => new BreakdownPoint(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Label)
            .ToList();

    private static IReadOnlyList<BreakdownPoint> BuildActivityBreakdown(IEnumerable<ReportingBooking> bookings)
        => bookings
            .GroupBy(x => string.IsNullOrWhiteSpace(x.ActivityType) ? "Non renseigne" : x.ActivityType)
            .Select(g => new BreakdownPoint(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Label)
            .ToList();

    private static IReadOnlyList<HourlyOccupationPoint> BuildOccupationByHour(
        IReadOnlyList<(DateTimeOffset StartsAt, DateTimeOffset EndsAt)> intervals,
        TimeOnly opensAt,
        TimeOnly closesAt)
    {
        var result = new List<HourlyOccupationPoint>();
        for (var hour = opensAt.Hour; hour < closesAt.Hour; hour++)
        {
            var occupied = intervals.Sum(interval =>
            {
                var slotStart = new DateTimeOffset(interval.StartsAt.Date.AddHours(hour), TimeSpan.Zero);
                var slotEnd = slotStart.AddHours(1);
                return DurationHours(Overlap(interval, (slotStart, slotEnd)));
            });

            result.Add(new HourlyOccupationPoint(hour, occupied));
        }

        return result;
    }

    private static IReadOnlyList<string> BuildInsights(
        IReadOnlyList<ReportingBooking> allBookings,
        IReadOnlyList<(DateTimeOffset StartsAt, DateTimeOffset EndsAt)> intervals,
        ReportingPeriod period)
    {
        var insights = new List<string>();
        var pendingOver24h = allBookings.Count(x =>
            x.Status == BookingStatus.PendingApproval &&
            x.CreatedAt <= DateTimeOffset.UtcNow.AddHours(-24));
        insights.Add($"{pendingOver24h} demande(s) en attente depuis plus de 24h.");
        insights.Add($"{allBookings.Count(x => x.Status == BookingStatus.AwaitingPayment)} paiement(s) en attente.");

        var peakHour = allBookings
            .Where(x => IntersectsDateRange(x.StartsAt, x.EndsAt, period))
            .GroupBy(x => x.StartsAt.Hour)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select(g => $"{g.Key:00}:00")
            .FirstOrDefault() ?? "Non disponible";
        insights.Add($"Creneau le plus demande : {peakHour}.");

        var busiestDay = intervals
            .GroupBy(x => DateOnly.FromDateTime(x.StartsAt.Date))
            .Select(g => new { Date = g.Key, Hours = g.Sum(DurationHours) })
            .OrderByDescending(x => x.Hours)
            .ThenBy(x => x.Date)
            .FirstOrDefault();
        insights.Add(busiestDay is null
            ? "Aucune journee occupee sur la periode."
            : $"Journee la plus occupee : {busiestDay.Date:dd/MM/yyyy}.");

        return insights;
    }

    private static int AvailableSlotCount(
        IReadOnlyList<(DateTimeOffset StartsAt, DateTimeOffset EndsAt)> occupiedIntervals,
        TimeOnly opensAt,
        TimeOnly closesAt)
    {
        var totalSlots = Math.Max(0, closesAt.Hour - opensAt.Hour);
        var occupiedSlots = occupiedIntervals
            .SelectMany(x => Enumerable.Range(x.StartsAt.Hour, Math.Max(1, x.EndsAt.Hour - x.StartsAt.Hour)))
            .Distinct()
            .Count();
        return Math.Max(0, totalSlots - occupiedSlots);
    }

    private static IReadOnlyList<(DateTimeOffset StartsAt, DateTimeOffset EndsAt)> BuildOccupationIntervals(
        IEnumerable<(DateTimeOffset StartsAt, DateTimeOffset EndsAt)> bookingIntervals,
        IEnumerable<(DateTimeOffset StartsAt, DateTimeOffset EndsAt)> blockIntervals,
        ReportingPeriod period,
        TimeOnly opensAt,
        TimeOnly closesAt)
    {
        var clipped = bookingIntervals
            .Concat(blockIntervals)
            .SelectMany(interval => ClipToPeriodAndOpeningHours(interval, period, opensAt, closesAt))
            .Where(x => x.EndsAt > x.StartsAt)
            .OrderBy(x => x.StartsAt)
            .ToList();

        var merged = new List<(DateTimeOffset StartsAt, DateTimeOffset EndsAt)>();
        foreach (var interval in clipped)
        {
            if (merged.Count == 0 || interval.StartsAt > merged[^1].EndsAt)
            {
                merged.Add(interval);
                continue;
            }

            if (interval.EndsAt > merged[^1].EndsAt)
                merged[^1] = (merged[^1].StartsAt, interval.EndsAt);
        }

        return merged;
    }

    private static IEnumerable<(DateTimeOffset StartsAt, DateTimeOffset EndsAt)> ClipToPeriodAndOpeningHours(
        (DateTimeOffset StartsAt, DateTimeOffset EndsAt) interval,
        ReportingPeriod period,
        TimeOnly opensAt,
        TimeOnly closesAt)
    {
        foreach (var date in Dates(period))
        {
            var openStart = new DateTimeOffset(date.ToDateTime(opensAt), TimeSpan.Zero);
            var openEnd = new DateTimeOffset(date.ToDateTime(closesAt), TimeSpan.Zero);
            var overlap = Overlap(interval, (openStart, openEnd));
            if (overlap.EndsAt > overlap.StartsAt)
                yield return overlap;
        }
    }

    private static (DateTimeOffset StartsAt, DateTimeOffset EndsAt) Overlap(
        (DateTimeOffset StartsAt, DateTimeOffset EndsAt) a,
        (DateTimeOffset StartsAt, DateTimeOffset EndsAt) b)
    {
        var start = a.StartsAt > b.StartsAt ? a.StartsAt : b.StartsAt;
        var end = a.EndsAt < b.EndsAt ? a.EndsAt : b.EndsAt;
        return end > start ? (start, end) : (start, start);
    }

    private static decimal TheoreticalOpenHours(ReportingPeriod period, TimeOnly opensAt, TimeOnly closesAt)
        => Dates(period).Count() * (decimal)(closesAt - opensAt).TotalHours;

    private static decimal DurationHours((DateTimeOffset StartsAt, DateTimeOffset EndsAt) interval)
        => (decimal)(interval.EndsAt - interval.StartsAt).TotalHours;

    private static bool IntersectsDateRange(DateTimeOffset startsAt, DateTimeOffset endsAt, ReportingPeriod period)
    {
        var rangeStart = new DateTimeOffset(period.StartsOn.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var rangeEnd = new DateTimeOffset(period.EndsOn.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        return startsAt < rangeEnd && rangeStart < endsAt;
    }

    private static IEnumerable<DateOnly> Dates(ReportingPeriod period)
    {
        for (var date = period.StartsOn; date <= period.EndsOn; date = date.AddDays(1))
            yield return date;
    }

    private static string CategoryLabel(CustomerCategory category) => category switch
    {
        CustomerCategory.Individual => "Particulier",
        CustomerCategory.LocalAsc => "ASC de Camberene",
        CustomerCategory.Club => "Club",
        CustomerCategory.Association => "Association",
        CustomerCategory.School => "Ecole",
        CustomerCategory.Company => "Entreprise",
        _ => category.ToString()
    };
}
