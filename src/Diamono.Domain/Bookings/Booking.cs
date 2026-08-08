namespace Diamono.Domain.Bookings;

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

    public void Approve() => Status = BookingStatus.AwaitingPayment;
    public void ConfirmPayment() => Status = BookingStatus.Confirmed;
    public void Reject() => Status = BookingStatus.Rejected;
    public void Cancel() => Status = BookingStatus.Cancelled;
}
