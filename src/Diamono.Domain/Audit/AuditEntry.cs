namespace Diamono.Domain.Audit;

public sealed class AuditEntry
{
    private AuditEntry() { }

    public AuditEntry(
        string userId,
        string userEmail,
        string action,
        string entityType,
        string entityId,
        string description,
        string? oldValuesJson = null,
        string? newValuesJson = null,
        string? metadataJson = null)
    {
        if (string.IsNullOrWhiteSpace(action)) throw new ArgumentException("Audit action is required.", nameof(action));
        if (string.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Audit entity type is required.", nameof(entityType));
        if (string.IsNullOrWhiteSpace(entityId)) throw new ArgumentException("Audit entity id is required.", nameof(entityId));

        Id = Guid.NewGuid();
        OccurredAt = DateTimeOffset.UtcNow;
        UserId = userId.Trim();
        UserEmail = userEmail.Trim();
        Action = action.Trim();
        EntityType = entityType.Trim();
        EntityId = entityId.Trim();
        Description = description.Trim();
        OldValuesJson = oldValuesJson;
        NewValuesJson = newValuesJson;
        MetadataJson = metadataJson;
    }

    public Guid Id { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string UserEmail { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? OldValuesJson { get; private set; }
    public string? NewValuesJson { get; private set; }
    public string? MetadataJson { get; private set; }
}
