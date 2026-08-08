using Diamono.Application.Reporting;
using Diamono.Domain.Bookings;
using Diamono.Domain.Settings;
using Xunit;

namespace Diamono.Application.Tests;

public sealed class ReportingApplicationServiceTests
{
    private static readonly DateOnly Day = new(2026, 8, 15);

    [Fact]
    public async Task Zero_reservation_donne_kpis_a_zero()
    {
        var service = Service([], []);

        var dashboard = await service.GetDashboardAsync(new ReportingPeriod(Day, Day));

        Assert.Equal(0, dashboard.PeriodKpis.TotalBookings);
        Assert.Equal(0m, dashboard.PeriodKpis.CollectedRevenue);
        Assert.Equal(0m, dashboard.PeriodKpis.PotentialRevenue);
        Assert.Equal(0m, dashboard.PeriodKpis.OccupationRate);
    }

    [Fact]
    public async Task Reservation_confirmee_donne_recettes_encaissees()
    {
        var bookingId = Guid.NewGuid();
        var service = Service([Booking(10, 12, BookingStatus.Confirmed, 75_000m, id: bookingId)], [], [Payment(bookingId, 75_000m)]);

        var dashboard = await service.GetDashboardAsync(new ReportingPeriod(Day, Day));

        Assert.Equal(75_000m, dashboard.PeriodKpis.CollectedRevenue);
        Assert.Equal(75_000m, dashboard.PeriodKpis.PotentialRevenue);
    }

    [Fact]
    public async Task Recettes_encaissees_viennent_des_paiements_paid()
    {
        var confirmedWithoutPayment = Guid.NewGuid();
        var awaitingWithPayment = Guid.NewGuid();
        var service = Service([
            Booking(10, 12, BookingStatus.Confirmed, 75_000m, id: confirmedWithoutPayment),
            Booking(12, 14, BookingStatus.AwaitingPayment, 90_000m, id: awaitingWithPayment)
        ], [], [Payment(awaitingWithPayment, 90_000m)]);

        var dashboard = await service.GetDashboardAsync(new ReportingPeriod(Day, Day));

        Assert.Equal(90_000m, dashboard.PeriodKpis.CollectedRevenue);
        Assert.Equal(165_000m, dashboard.PeriodKpis.PotentialRevenue);
        Assert.Equal(90_000m, Assert.Single(dashboard.RevenueByDay).Amount);
    }

    [Fact]
    public async Task AwaitingPayment_compte_potentiel_pas_encaisse()
    {
        var service = Service([Booking(10, 12, BookingStatus.AwaitingPayment, 75_000m)], []);

        var dashboard = await service.GetDashboardAsync(new ReportingPeriod(Day, Day));

        Assert.Equal(0m, dashboard.PeriodKpis.CollectedRevenue);
        Assert.Equal(75_000m, dashboard.PeriodKpis.PotentialRevenue);
    }

    [Fact]
    public async Task BookingBlock_influence_taux_occupation()
    {
        var service = Service([], [Block(8, 10)]);

        var dashboard = await service.GetDashboardAsync(new ReportingPeriod(Day, Day));

        Assert.Equal(Math.Round(2m / 15m * 100m, 1), dashboard.PeriodKpis.OccupationRate);
    }

    [Fact]
    public async Task Annulation_non_comptee_dans_recettes()
    {
        var service = Service([Booking(10, 12, BookingStatus.Cancelled, 75_000m)], []);

        var dashboard = await service.GetDashboardAsync(new ReportingPeriod(Day, Day));

        Assert.Equal(0m, dashboard.PeriodKpis.CollectedRevenue);
        Assert.Equal(0m, dashboard.PeriodKpis.PotentialRevenue);
        Assert.Equal(1, dashboard.PeriodKpis.CancelledBookings);
    }

    [Fact]
    public async Task Regroupement_par_categorie_correct()
    {
        var service = Service([
            Booking(10, 12, BookingStatus.Confirmed, 75_000m, CustomerCategory.LocalAsc),
            Booking(12, 14, BookingStatus.Confirmed, 75_000m, CustomerCategory.LocalAsc),
            Booking(14, 16, BookingStatus.Confirmed, 75_000m, CustomerCategory.Company)
        ], []);

        var dashboard = await service.GetDashboardAsync(new ReportingPeriod(Day, Day));

        Assert.Equal(2, dashboard.CategoryBreakdown.Single(x => x.Label == "ASC de Camberene").Count);
        Assert.Equal(1, dashboard.CategoryBreakdown.Single(x => x.Label == "Entreprise").Count);
    }

    [Fact]
    public async Task Regroupement_par_activite_correct()
    {
        var service = Service([
            Booking(10, 12, BookingStatus.Confirmed, 75_000m, activity: "Football"),
            Booking(12, 14, BookingStatus.Confirmed, 75_000m, activity: "Football"),
            Booking(14, 16, BookingStatus.Confirmed, 75_000m, activity: "Athletisme")
        ], []);

        var dashboard = await service.GetDashboardAsync(new ReportingPeriod(Day, Day));

        Assert.Equal(2, dashboard.ActivityBreakdown.Single(x => x.Label == "Football").Count);
        Assert.Equal(1, dashboard.ActivityBreakdown.Single(x => x.Label == "Athletisme").Count);
    }

    [Fact]
    public async Task Filtre_dates_correct()
    {
        var outside = new DateOnly(2026, 8, 16);
        var bookingId = Guid.NewGuid();
        var service = Service([
            Booking(10, 12, BookingStatus.Confirmed, 75_000m, id: bookingId),
            Booking(10, 12, BookingStatus.Confirmed, 75_000m, date: outside)
        ], [], [Payment(bookingId, 75_000m)]);

        var dashboard = await service.GetDashboardAsync(new ReportingPeriod(Day, Day));

        Assert.Equal(1, dashboard.PeriodKpis.TotalBookings);
        Assert.Equal(75_000m, dashboard.PeriodKpis.CollectedRevenue);
    }

    [Fact]
    public async Task Taux_occupation_correct_avec_horaires_configures_et_overlap()
    {
        var service = Service([
            Booking(10, 12, BookingStatus.Confirmed, 75_000m),
            Booking(11, 13, BookingStatus.PendingApproval, 75_000m)
        ], [Block(12, 14)]);

        var dashboard = await service.GetDashboardAsync(new ReportingPeriod(Day, Day));

        Assert.Equal(Math.Round(4m / 15m * 100m, 1), dashboard.PeriodKpis.OccupationRate);
    }

    [Fact]
    public async Task Service_reporting_appelle_un_seul_repository()
    {
        var repository = new FakeReportingRepository(Data([Booking(10, 12, BookingStatus.Confirmed, 75_000m)], [], []));
        var service = new ReportingApplicationService(repository, FakePermissionGuard.AllowAll());

        await service.GetDashboardAsync(new ReportingPeriod(Day, Day));

        Assert.Equal(1, repository.CallCount);
    }

    private static ReportingApplicationService Service(
        IReadOnlyList<ReportingBooking> bookings,
        IReadOnlyList<ReportingBlock> blocks,
        IReadOnlyList<ReportingPayment>? payments = null)
        => new(new FakeReportingRepository(Data(bookings, blocks, payments ?? [])), FakePermissionGuard.AllowAll());

    private static ReportingData Data(
        IReadOnlyList<ReportingBooking> bookings,
        IReadOnlyList<ReportingBlock> blocks,
        IReadOnlyList<ReportingPayment> payments)
        => new(bookings, payments, blocks, StadiumBookingSettings.MvpDefaults());

    private static ReportingBooking Booking(
        int startHour,
        int endHour,
        BookingStatus status,
        decimal total,
        CustomerCategory category = CustomerCategory.Individual,
        string activity = "Football",
        DateOnly? date = null,
        Guid? id = null)
        => new(
            id ?? Guid.NewGuid(),
            At(date ?? Day, startHour),
            At(date ?? Day, endHour),
            category,
            activity,
            total,
            status,
            At(date ?? Day, 7));

    private static ReportingBlock Block(int startHour, int endHour)
        => new(Guid.NewGuid(), At(Day, startHour), At(Day, endHour));

    private static ReportingPayment Payment(Guid bookingId, decimal amount, DateOnly? date = null)
        => new(Guid.NewGuid(), bookingId, amount, At(date ?? Day, 9));

    private static DateTimeOffset At(DateOnly date, int hour)
        => new(date.ToDateTime(new TimeOnly(hour, 0)), TimeSpan.Zero);
}
