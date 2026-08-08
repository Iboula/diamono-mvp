namespace Diamono.Domain.Bookings;

public sealed class BookingBlock
{
    private BookingBlock() { }
    public BookingBlock(Guid resourceId, DateTimeOffset startsAt, DateTimeOffset endsAt, string reason)
    {
        if (endsAt <= startsAt) throw new ArgumentException("End time must be after start time.");
        Id = Guid.NewGuid();
        ResourceId = resourceId;
        StartsAt = startsAt;
        EndsAt = endsAt;
        Reason = reason;
    }

    public Guid Id { get; private set; }
    public Guid ResourceId { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public string Reason { get; private set; } = string.Empty;
}
