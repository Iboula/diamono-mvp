namespace Diamono.Domain.Bookings;

public enum BookingStatus
{
    PendingApproval = 1,
    AwaitingPayment = 2,
    Confirmed = 3,
    Rejected = 4,
    Cancelled = 5,
    Completed = 6,
    NoShow = 7
}
