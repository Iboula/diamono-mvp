namespace Diamono.Domain.Bookings;

using System.Security.Cryptography;

public sealed class Booking
{
    private Booking() { }

    public Booking(Guid resourceId, DateTimeOffset startsAt, DateTimeOffset endsAt,
        string customerName, string phone, CustomerCategory customerCategory,
        string activityType, decimal rentalAmount, decimal lightingAmount, decimal depositAmount)
    {
        if (endsAt <= startsAt) throw new ArgumentException("End time must be after start time.");
        if (string.IsNullOrWhiteSpace(customerName)) throw new ArgumentException("Customer name is required.");

        Id = Guid.NewGuid();
        Reference = $"DIA-{DateTime.UtcNow:yyyy}-{Random.Shared.Next(10000, 99999)}";
        PublicAccessToken = CreatePublicAccessToken();
        ResourceId = resourceId;
        StartsAt = startsAt;
        EndsAt = endsAt;
        CustomerName = customerName.Trim();
        Phone = phone.Trim();
        CustomerCategory = customerCategory;
        ActivityType = activityType.Trim();
        RentalAmount = rentalAmount;
        LightingAmount = lightingAmount;
        DepositAmount = depositAmount;
        Status = BookingStatus.PendingApproval;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Reference { get; private set; } = string.Empty;
    public string PublicAccessToken { get; private set; } = string.Empty;
    public Guid ResourceId { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public CustomerCategory CustomerCategory { get; private set; }
    public string ActivityType { get; private set; } = string.Empty;
    public decimal RentalAmount { get; private set; }
    public decimal LightingAmount { get; private set; }
    public decimal DepositAmount { get; private set; }
    public decimal TotalAmount => RentalAmount + LightingAmount + DepositAmount;
    public BookingStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? CancellationReason { get; private set; }

    public void Approve()
    {
        EnsureStatus(BookingStatus.PendingApproval, "Seule une reservation en attente d'approbation peut etre approuvee.");

        Status = BookingStatus.AwaitingPayment;
        ApprovedAt = DateTimeOffset.UtcNow;
    }

    public void ConfirmPayment()
    {
        EnsureStatus(BookingStatus.AwaitingPayment, "Seule une reservation en attente de paiement peut etre marquee payee.");

        Status = BookingStatus.Confirmed;
        PaidAt = DateTimeOffset.UtcNow;
    }

    public void Reject(string reason)
    {
        EnsureStatus(BookingStatus.PendingApproval, "Seule une reservation en attente d'approbation peut etre rejetee.");
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Le motif de rejet est requis.", nameof(reason));

        Status = BookingStatus.Rejected;
        RejectedAt = DateTimeOffset.UtcNow;
        RejectionReason = reason.Trim();
    }

    public void Cancel(string reason)
    {
        EnsureStatus(BookingStatus.AwaitingPayment, "Seule une reservation en attente de paiement peut etre annulee.");
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Le motif d'annulation est requis.", nameof(reason));

        Status = BookingStatus.Cancelled;
        CancelledAt = DateTimeOffset.UtcNow;
        CancellationReason = reason.Trim();
    }

    private void EnsureStatus(BookingStatus expected, string message)
    {
        if (Status != expected) throw new InvalidOperationException(message);
    }

    private static string CreatePublicAccessToken()
        => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
}
