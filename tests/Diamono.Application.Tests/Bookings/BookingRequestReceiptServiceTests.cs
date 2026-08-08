using Diamono.Application.Bookings;
using Diamono.Domain.Audit;
using Diamono.Domain.Bookings;
using Diamono.Domain.Security;
using Xunit;

namespace Diamono.Application.Tests;

public sealed class BookingRequestReceiptServiceTests
{
    private static readonly Guid ResourceId = Guid.Parse("7f099973-c811-4afe-beca-c9a1b8fcd026");

    [Fact]
    public async Task Creation_booking_rend_recu_provisoire_disponible()
    {
        var booking = Booking(rentalAmount: 50_000m);

        var receipt = await Service(new FakeBookingRepository(booking))
            .GeneratePublicReceiptAsync(booking.PublicAccessToken);

        Assert.NotEmpty(receipt.Content);
        Assert.Equal("application/pdf", receipt.ContentType);
        Assert.StartsWith("RPD-", receipt.ReceiptReference);
    }

    [Fact]
    public async Task Reference_booking_et_montant_historique_corrects()
    {
        var booking = Booking(rentalAmount: 60_000m, lightingAmount: 10_000m, depositAmount: 25_000m);
        var renderer = new FakeBookingRequestReceiptRenderer();

        await Service(new FakeBookingRepository(booking), renderer: renderer)
            .GeneratePublicReceiptAsync(booking.PublicAccessToken);

        var data = Assert.Single(renderer.Receipts);
        Assert.Equal(booking.Reference, data.BookingReference);
        Assert.Equal(95_000m, data.TotalAmount);
        Assert.Equal(60_000m, data.RentalAmount);
    }

    [Fact]
    public async Task Changement_tarif_ulterieur_ne_modifie_pas_recu()
    {
        var booking = Booking(rentalAmount: 50_000m, lightingAmount: 0m, depositAmount: 25_000m);
        var renderer = new FakeBookingRequestReceiptRenderer();

        await Service(new FakeBookingRepository(booking), renderer: renderer)
            .GeneratePublicReceiptAsync(booking.PublicAccessToken);

        Assert.Equal(75_000m, Assert.Single(renderer.Receipts).TotalAmount);
    }

    [Fact]
    public async Task Mention_provisoire_presente_dans_le_pdf()
    {
        var booking = Booking();

        var receipt = await Service(new FakeBookingRepository(booking))
            .GeneratePublicReceiptAsync(booking.PublicAccessToken);

        var content = System.Text.Encoding.UTF8.GetString(receipt.Content);
        Assert.Contains("ne constitue pas une reservation confirmee", content);
    }

    [Fact]
    public async Task Token_public_valide_retourne_pdf()
    {
        var booking = Booking();

        var receipt = await Service(new FakeBookingRepository(booking))
            .GeneratePublicReceiptAsync(booking.PublicAccessToken);

        Assert.NotEmpty(receipt.Content);
    }

    [Fact]
    public async Task Token_invalide_retourne_introuvable()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => Service(new FakeBookingRepository(Booking()))
                .GeneratePublicReceiptAsync("token-invalide"));
    }

    [Fact]
    public async Task Token_ne_donne_pas_acces_a_une_autre_reservation()
    {
        var booking1 = Booking();
        var booking2 = Booking();
        var renderer = new FakeBookingRequestReceiptRenderer();

        await Service(new FakeBookingRepository(booking1, booking2), renderer: renderer)
            .GeneratePublicReceiptAsync(booking1.PublicAccessToken);

        Assert.Equal(booking1.Reference, Assert.Single(renderer.Receipts).BookingReference);
        Assert.NotEqual(booking2.Reference, renderer.Receipts[0].BookingReference);
    }

    [Fact]
    public async Task Token_non_expose_dans_audit()
    {
        var booking = Booking();
        var audit = new FakeAuditWriter();

        await Service(new FakeBookingRepository(booking), audit: audit)
            .GeneratePublicReceiptAsync(booking.PublicAccessToken);

        var serializedAudit = string.Join(" ", audit.Entries.Select(x =>
            $"{x.Action} {x.EntityId} {x.Description} {x.Metadata}"));
        Assert.DoesNotContain(booking.PublicAccessToken, serializedAudit);
    }

    [Fact]
    public async Task Backoffice_autorise_peut_telecharger()
    {
        var booking = Booking();

        var receipt = await Service(
                new FakeBookingRepository(booking),
                permissionGuard: FakePermissionGuard.ForRoles(DiamonoRoles.Gestionnaire))
            .GenerateBackOfficeReceiptAsync(booking.Id);

        Assert.NotEmpty(receipt.Content);
    }

    [Fact]
    public async Task Audit_booking_request_receipt_generated_cree()
    {
        var booking = Booking();
        var audit = new FakeAuditWriter();

        await Service(new FakeBookingRepository(booking), audit: audit)
            .GeneratePublicReceiptAsync(booking.PublicAccessToken);

        var entry = Assert.Single(audit.Entries);
        Assert.Equal(AuditActions.BookingRequestReceiptGenerated, entry.Action);
        Assert.Contains(booking.Id.ToString(), entry.Metadata!.ToString());
    }

    [Fact]
    public async Task Generation_ne_modifie_ni_statut_ni_montants()
    {
        var booking = Booking();
        var status = booking.Status;
        var total = booking.TotalAmount;

        await Service(new FakeBookingRepository(booking))
            .GeneratePublicReceiptAsync(booking.PublicAccessToken);

        Assert.Equal(status, booking.Status);
        Assert.Equal(total, booking.TotalAmount);
    }

    [Fact]
    public async Task Recu_officiel_reste_distinct_du_recu_provisoire()
    {
        var booking = Booking();

        var receipt = await Service(new FakeBookingRepository(booking))
            .GeneratePublicReceiptAsync(booking.PublicAccessToken);

        Assert.StartsWith("RPD-", receipt.ReceiptReference);
        Assert.DoesNotContain("RCT-", receipt.ReceiptReference);
    }

    private static Booking Booking(
        decimal rentalAmount = 50_000m,
        decimal lightingAmount = 5_000m,
        decimal depositAmount = 25_000m)
        => new(
            ResourceId,
            new DateTimeOffset(2026, 8, 20, 18, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 20, 20, 0, 0, TimeSpan.Zero),
            "Awa Diop",
            "+221 77 000 00 00",
            CustomerCategory.Individual,
            "Football",
            rentalAmount,
            lightingAmount,
            depositAmount);

    private static BookingRequestReceiptService Service(
        FakeBookingRepository bookingRepository,
        FakePermissionGuard? permissionGuard = null,
        FakeAuditWriter? audit = null,
        FakeBookingRequestReceiptRenderer? renderer = null)
        => new(
            bookingRepository,
            permissionGuard ?? FakePermissionGuard.AllowAll(),
            audit ?? new FakeAuditWriter(),
            renderer ?? new FakeBookingRequestReceiptRenderer());
}
