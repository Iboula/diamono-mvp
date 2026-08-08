namespace Diamono.Application.Reporting;

public sealed record ReportingDashboard(
    ReportingPeriod Period,
    TodayKpis Today,
    PeriodKpis PeriodKpis,
    IReadOnlyList<DailyCountPoint> BookingsByDay,
    IReadOnlyList<DailyAmountPoint> RevenueByDay,
    IReadOnlyList<BreakdownPoint> CategoryBreakdown,
    IReadOnlyList<BreakdownPoint> ActivityBreakdown,
    IReadOnlyList<HourlyOccupationPoint> OccupationByHour,
    IReadOnlyList<string> Insights);

public sealed record TodayKpis(
    int BookingsToday,
    int OccupiedSlots,
    int AvailableSlots,
    int PendingApproval,
    int AwaitingPayment);

public sealed record PeriodKpis(
    int TotalBookings,
    int ConfirmedBookings,
    int CancelledBookings,
    int NoShowBookings,
    decimal CollectedRevenue,
    decimal PotentialRevenue,
    decimal OccupationRate);

public sealed record DailyCountPoint(DateOnly Date, int Count);

public sealed record DailyAmountPoint(DateOnly Date, decimal Amount);

public sealed record BreakdownPoint(string Label, int Count);

public sealed record HourlyOccupationPoint(int Hour, decimal OccupiedHours);
