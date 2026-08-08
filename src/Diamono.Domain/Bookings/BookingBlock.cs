namespace Diamono.Domain.Bookings;

public sealed class BookingBlock
{
    private BookingBlock() { }

    public BookingBlock(
        Guid resourceId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        BookingBlockType type,
        string description = "")
    {
        if (endsAt <= startsAt) throw new ArgumentException("End time must be after start time.");

        Id = Guid.NewGuid();
        ResourceId = resourceId;
        StartsAt = startsAt;
        EndsAt = endsAt;
        Type = type;
        Reason = BlockReason.For(type);
        Description = description.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid ResourceId { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public BookingBlockType Type { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public string? CancelledBy { get; private set; }
    public bool IsActive => CancelledAt is null;

    public void Cancel()
    {
        if (!IsActive) throw new InvalidOperationException("Ce blocage est deja annule.");

        CancelledAt = DateTimeOffset.UtcNow;
    }
}

public static class BlockReason
{
    public static string For(BookingBlockType type) => type switch
    {
        BookingBlockType.Maintenance => "Maintenance pelouse",
        BookingBlockType.MunicipalEvent => "Activite municipale",
        BookingBlockType.OfficialMatch => "Match officiel",
        BookingBlockType.Cleaning => "Nettoyage",
        BookingBlockType.Works => "Travaux",
        BookingBlockType.TechnicalIssue => "Indisponibilite technique",
        BookingBlockType.Other => "Autre",
        _ => "Autre"
    };
}
