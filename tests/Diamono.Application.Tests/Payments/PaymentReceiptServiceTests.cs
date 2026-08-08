using Diamono.Application.Payments;
using Diamono.Domain.Audit;
using Diamono.Domain.Bookings;
using Diamono.Domain.Payments;
using Diamono.Domain.Security;
using Xunit;

namespace Diamono.Application.Tests;

public sealed class PaymentReceiptServiceTests
{
    private static readonly Guid ResourceId = Guid.Parse("7f099973-c811-4afe-beca-c9a1b8fcd001");

    [Fact]
    public async Task Paiement_pending_pas_de_recu()
    {
        var (booking, payment) = BookingAndPayment(PaymentStatus.Pending);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Service(new FakePaymentRepository(payment), new FakeBookingRepository(booking))
                .GenerateReceiptAsync(payment.Id));
    }

    [Fact]
    public async Task Paiement_failed_pas_de_recu()
    {
        var (booking, payment) = BookingAndPayment(PaymentStatus.Failed);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Service(new FakePaymentRepository(payment), new FakeBookingRepository(booking))
                .GenerateReceiptAsync(payment.Id));
    }

    [Fact]
    public async Task Paiement_paid_genere_recu_pdf_non_vide()
    {
        var (booking, payment) = BookingAndPayment(PaymentStatus.Paid);

        var receipt = await Service(new FakePaymentRepository(payment), new FakeBookingRepository(booking))
            .GenerateReceiptAsync(payment.Id);

        Assert.NotEmpty(receipt.Content);
        Assert.Equal("application/pdf", receipt.ContentType);
        Assert.EndsWith(".pdf", receipt.FileName);
    }

    [Fact]
    public async Task Montant_et_booking_reference_sont_corrects()
    {
        var (booking, payment) = BookingAndPayment(PaymentStatus.Paid, amount: 85_000m);
        var renderer = new FakePaymentReceiptRenderer();

        await Service(new FakePaymentRepository(payment), new FakeBookingRepository(booking), renderer: renderer)
            .GenerateReceiptAsync(payment.Id);

        var data = Assert.Single(renderer.Receipts);
        Assert.Equal(85_000m, data.Amount);
        Assert.Equal(booking.Reference, data.BookingReference);
    }

    [Fact]
    public async Task Reference_recu_unique_par_paiement()
    {
        var (booking1, payment1) = BookingAndPayment(PaymentStatus.Paid);
        var (booking2, payment2) = BookingAndPayment(PaymentStatus.Paid);
        var repository = new FakePaymentRepository(payment1, payment2);
        var bookingRepository = new FakeBookingRepository(booking1, booking2);
        var service = Service(repository, bookingRepository);

        var receipt1 = await service.GenerateReceiptAsync(payment1.Id);
        var receipt2 = await service.GenerateReceiptAsync(payment2.Id);

        Assert.NotEqual(receipt1.ReceiptReference, receipt2.ReceiptReference);
        Assert.StartsWith("RCT-", receipt1.ReceiptReference);
    }

    [Fact]
    public async Task Permission_payments_markpaid_requise()
    {
        var (booking, payment) = BookingAndPayment(PaymentStatus.Paid);

        var error = await Assert.ThrowsAsync<Diamono.Application.Security.PermissionDeniedException>(
            () => Service(
                    new FakePaymentRepository(payment),
                    new FakeBookingRepository(booking),
                    permissionGuard: FakePermissionGuard.ForRoles(DiamonoRoles.Gestionnaire))
                .GenerateReceiptAsync(payment.Id));

        Assert.Equal(Permissions.PaymentsMarkPaid, error.Permission);
    }

    [Fact]
    public async Task Audit_payment_receipt_generated_cree()
    {
        var (booking, payment) = BookingAndPayment(PaymentStatus.Paid);
        var audit = new FakeAuditWriter();

        await Service(new FakePaymentRepository(payment), new FakeBookingRepository(booking), audit: audit)
            .GenerateReceiptAsync(payment.Id);

        var entry = Assert.Single(audit.Entries);
        Assert.Equal(AuditActions.PaymentReceiptGenerated, entry.Action);
        Assert.Contains(payment.Id.ToString(), entry.Metadata!.ToString());
    }

    [Fact]
    public async Task Generation_ne_modifie_pas_payment()
    {
        var (booking, payment) = BookingAndPayment(PaymentStatus.Paid);
        var status = payment.Status;
        var paidAt = payment.PaidAt;

        await Service(new FakePaymentRepository(payment), new FakeBookingRepository(booking))
            .GenerateReceiptAsync(payment.Id);

        Assert.Equal(status, payment.Status);
        Assert.Equal(paidAt, payment.PaidAt);
    }

    private static (Booking Booking, Payment Payment) BookingAndPayment(PaymentStatus status, decimal amount = 75_000m)
    {
        var booking = new Booking(
            ResourceId,
            new DateTimeOffset(2026, 8, 15, 18, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 15, 20, 0, 0, TimeSpan.Zero),
            "Awa Diop",
            "+221 77 000 00 00",
            CustomerCategory.Individual,
            "Football",
            amount,
            lightingAmount: 5_000m,
            depositAmount: 25_000m);
        booking.Approve();
        var payment = new Payment(booking.Id, amount, PaymentMethod.Cash);

        if (status == PaymentStatus.Paid)
        {
            payment.MarkPaid("TEST-PAID");
            booking.ConfirmPayment();
        }
        else if (status == PaymentStatus.Failed)
        {
            payment.MarkFailed("TEST-FAILED");
        }

        return (booking, payment);
    }

    private static PaymentReceiptService Service(
        FakePaymentRepository paymentRepository,
        FakeBookingRepository bookingRepository,
        FakePermissionGuard? permissionGuard = null,
        FakeAuditWriter? audit = null,
        FakePaymentReceiptRenderer? renderer = null)
        => new(
            paymentRepository,
            bookingRepository,
            permissionGuard ?? FakePermissionGuard.AllowAll(),
            audit ?? new FakeAuditWriter(),
            renderer ?? new FakePaymentReceiptRenderer());
}
